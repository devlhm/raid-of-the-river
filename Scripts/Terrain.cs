using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RiverRaid.Scripts;


public partial class Terrain : Node2D
{

	enum RiverState
	{
		Single,
		Branched
	}
	
	// Called when the node enters the scene tree for the first time.
	private Rect2 _screenRect;

	[Export] private PackedScene _terrainScene;
	[Export] private PackedScene _waterScene;

	[Export] private float _maxOffset = 400f;
	[Export] private float _minOffset = 180f;
	[Export] private float _straightLineOccurrence = 0.5f;
	[Export] private float _maxDeg = 30f;
	[Export] private float _minYStep = 50f;
	[Export] private float _maxYStep = 100f;
	[Export] private float _sectionLength = 500f;
	
	[ExportCategory("Bifurcation")]
	[Export] private float _bifurcationOccurrence = 0.2f;

	[Export] private float _bifurcationLength = 100f;
	

	private RiverState _riverState = RiverState.Single;
	
	public override void _Ready()
	{
		GD.Randomize();
		_screenRect = GetViewportRect();
		GenerateTerrain();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void GenerateTerrain()
	{
		//GD.Seed(52);
		var leftTerrain = _terrainScene.Instantiate<StaticBody2D>();
		var rightTerrain = _terrainScene.Instantiate<StaticBody2D>();
		var water = _waterScene.Instantiate<Polygon2D>();

		List<Vector2> lPoly = new();
		List<Vector2> rPoly = new();
		
		float centerX = _screenRect.GetCenter().X;

		float startY = _screenRect.End.Y;
		
		var offset = (float)GD.RandRange(_minOffset, _maxOffset);
		
		lPoly.Add(new Vector2(centerX - offset, startY));
		rPoly.Add(new Vector2(centerX + offset, startY));
		
		while(startY - lPoly.Last().Y < _sectionLength)
		{
			if (startY - lPoly.Last().Y < 2000|| GD.Randf() > _bifurcationOccurrence)
			{
				(Vector2 leftPoint, Vector2 rightPoint) = GenerateTerrainSegment(rPoly.Last(), centerX, _minOffset, _maxOffset);
				
				lPoly.Add(leftPoint);
				rPoly.Add(rightPoint);
			}
			else
			{
				GenerateBifurcation(rPoly.Last(), centerX, rPoly, lPoly);
			}
		}

		float firstY = rPoly.First().Y;
		float lastY = rPoly.Last().Y;

		//List<Vector2> waterPoly = new List<Vector2>();

		//Vector2[] rPolyArr = rPoly.ToArray();
		
		//waterPoly.AddRange(lPoly);
		//waterPoly.AddRange(rPolyArr.Reverse());
		
		lPoly.Add(new Vector2(0, lastY));
		lPoly.Add(new Vector2(0, firstY));
		
		rPoly.Add(new Vector2(_screenRect.End.X,lastY));
		rPoly.Add(new Vector2(_screenRect.End.X, firstY));
		
		RenderTerrain(leftTerrain, lPoly);
		RenderTerrain(rightTerrain, rPoly);
	}

	private void RenderTerrain(StaticBody2D terrainNode, List<Vector2> points)
	{
		terrainNode.GetNode<Polygon2D>("Polygon2D").Polygon = points.ToArray();
		terrainNode.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon = points.ToArray();
		AddChild(terrainNode);
	}
	
	private (Vector2, Vector2) GenerateTerrainSegment(Vector2 lastPoint, float centerX, float minOff, float maxOff)
	{
		float currY = lastPoint.Y - (float)GD.RandRange(_minYStep, _maxYStep);
		float offset = lastPoint.X - centerX;
		
		if (GD.Randf() > _straightLineOccurrence)
		{
			float tg = Mathf.Tan(Mathf.DegToRad(_maxDeg));
			float maxOffsetFromLast = Math.Abs((lastPoint.Y - currY) *  tg);
			float min = Math.Max(lastPoint.X - centerX - maxOffsetFromLast, minOff);
			float max = Math.Min(lastPoint.X - centerX + maxOffsetFromLast, maxOff);
			offset = (float)GD.RandRange(min, max);
		}
		
		return (new Vector2(centerX - offset, currY), new Vector2(centerX + offset, currY));
	}

	private void GenerateBifurcation(Vector2 lastPoint, float centerX, List<Vector2> rPoly, List<Vector2> lPoly)
	{
		// TODO: fix respawning inside center terrain when dead
		var centerTerrain = _terrainScene.Instantiate<StaticBody2D>();

		List<Vector2> cPoly = new() { new Vector2(centerX, lastPoint.Y) };
		List<Vector2> cLeftPoly = new ();

		float length = 0;
		while (length < _bifurcationLength)
		{
			(Vector2 lPoint, Vector2 rPoint) = GenerateTerrainSegment(lastPoint, centerX, _minOffset + 100, _maxOffset + 100);
			rPoly.Add(rPoint);
			lPoly.Add(lPoint);
			cPoly.Add(rPoint with { X = rPoint.X - 200});
			cLeftPoly.Add(lPoint with { X = lPoint.X + 200});
			length += lastPoint.Y - rPoly.Last().Y;
			lastPoint = rPoly.Last();
		}
		
		(Vector2 lPointF, Vector2 rPointF) = GenerateTerrainSegment(rPoly.Last(), centerX, _minOffset, _maxOffset);
		cPoly.Add(new Vector2(centerX, lPointF.Y));
		
		cLeftPoly.Reverse();
		cPoly.AddRange(cLeftPoly);
		
		lPoly.Add(lPointF);
		rPoly.Add(rPointF);
		
		RenderTerrain(centerTerrain, cPoly);
	}
}

