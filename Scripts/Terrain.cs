using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using Godot;

namespace RiverRaid.Scripts;


public partial class Terrain : Node2D
{
	// Called when the node enters the scene tree for the first time.
	private Rect2 _screenRect;
	
	[Export] private bool _useManualSeed;
	[Export] private uint _manualSeed;

	[Export] private PackedScene _terrainScene;
	[Export] private PackedScene _waterScene;
	[Export] private PackedScene _bridgeScene;

	[Export] private float _maxOffset = 400f;
	[Export] private float _minOffset = 180f;
	[Export(PropertyHint.Range, "0,1,")] private float _straightLineOccurrence = 0.5f;
	[Export(PropertyHint.Range, "0,90,")] private float _maxDeg = 30f;
	[Export] private float _minYStep = 50f;
	[Export] private float _maxYStep = 100f;
	[Export] private float _sectionLength = 500f;
	
	[ExportCategory("Bifurcation")]
	[Export(PropertyHint.Range, "0,1,")] private float _bifurcationOccurrence = 0.2f;
	[Export] private float _bifurcationLength = 100f;
	[Export] private float _bifuractionMinDistance = 500f;
	
	[ExportCategory("Enemies")]
	[Export] private int _enemiesPerSection = 30;
	[Export] private PackedScene _boatEnemyScene;
	[Export] private PackedScene _helicopterEnemyScene;
	[Export] private PackedScene _jetEnemyScene;
	
	[Export(PropertyHint.Range, "0,1,")] private float _boatRatio = 0.5f;
	[Export(PropertyHint.Range, "0,1,")] private float _helicopterRatio = 0.25f;
	[Export(PropertyHint.Range, "0,1,")] private float _jetRatio = 0.25f;

	private float _centerX;
	
	[Export] private float _leftBridgeX = 300;
	[Export] private float _rightBridgeX = 852;
	[Export] private float _bridgeHeight = 648;
	
	private readonly Queue<Node2D> _sections = new();

	public override void _Ready()
	{
		GD.Randomize();
		_screenRect = GetViewportRect();
		
		GenerateSection(new Vector2(800, _screenRect.End.Y));
		//GenerateSection();

		if (Math.Abs(_boatRatio + _jetRatio + _helicopterRatio - 1) > 0.001)
		{
			GD.PrintErr("wrong enemy ratios");
			
			GetTree().Quit();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnCheckpoint()
	{
		Node2D section = _sections.Dequeue();
		RemoveChild(section);
		section.QueueFree();
		//GenerateSection();
	}

	private Node2D GenerateSection(Vector2 startPoint)
	{
		
		uint seed = _useManualSeed ? _manualSeed : GD.Randi();
		GD.Print($"seed: {seed}" );
		GD.Seed(seed);
		
		for(var i = 0; i < 3; i++)
		{
			try
			{
				Node2D section = new();
		
				var leftTerrain = _terrainScene.Instantiate<StaticBody2D>();
				var rightTerrain = _terrainScene.Instantiate<StaticBody2D>();
				var river = _waterScene.Instantiate<StaticBody2D>();

				List<Vector2> lPoly = new();
				List<Vector2> rPoly = new();
			
				_centerX = _screenRect.GetCenter().X;
				float startY = startPoint.Y;
			
				var offset = startPoint.X - _centerX;
			
				lPoly.Add(new Vector2(_centerX - offset, startY));
				rPoly.Add(new Vector2(_centerX + offset, startY));
				List<List<Vector2>> cPolys = new();

				List<Vector2> spawnLine = new() { new Vector2(_centerX, startY) };
				float distFromLastBifurcation = 0;
				float lastY = startY;

				while(startY - lPoly.Last().Y < _sectionLength)
				{
					if (distFromLastBifurcation >= _bifuractionMinDistance && GD.Randf() <= _bifurcationOccurrence)
					{
						distFromLastBifurcation = 0;
						spawnLine[^1] = spawnLine[^1] with { Y = spawnLine[^1].Y + 100 };
						List<Vector2> cPoly = GenerateBifurcation(section, rPoly.Last(), rPoly, lPoly, spawnLine);
						cPolys.Add(cPoly);
					}
					else
					{
						(Vector2 leftPoint, Vector2 rightPoint) =
							GenerateTerrainSegment(rPoly.Last(), _minOffset, _maxOffset);

						distFromLastBifurcation += lastY - rightPoint.Y;
						lastY = rightPoint.Y;

						lPoly.Add(leftPoint);
						rPoly.Add(rightPoint);

						//if(spawnLine.Count > 1 && spawnLine.Last().X == _centerX)
						//spawnLine[^1] = spawnLine[^1] with { Y = rightPoint.Y };
						//else
						spawnLine.Add(new Vector2(_centerX, rightPoint.Y));
					}
				}
				
				SpawnBridge(section, rPoly, lPoly);
				//RenderTerrain(section, river, GenerateRiverPoly(lPoly, rPoly, cPolys));
				
				lastY = rPoly.Last().Y;
			
				lPoly.Add(new Vector2(0, lastY));
				lPoly.Add(new Vector2(0, startY));
			
				rPoly.Add(new Vector2(_screenRect.End.X, lastY));
				rPoly.Add(new Vector2(_screenRect.End.X, startY));
			
				RenderTerrain(section, leftTerrain, lPoly);
				RenderTerrain(section, rightTerrain, rPoly);
	
				//Line2D spawnLineVisualization = new();
				//spawnLineVisualization.Points = spawnLine.ToArray();
				//AddChild(spawnLineVisualization);
			
				spawnLine.RemoveRange(0, 5);
				SpawnEnemies(section, spawnLine);
				
				AddChild(section);
				_sections.Enqueue(section);

				return section;
			}
			catch (Exception e)
			{
				GD.PrintErr(e);
			}
		}

		return null;
	}

	private void SpawnEnemies(Node2D section, List<Vector2> spawnLine)
	{
		for (var i = 0; i < _enemiesPerSection; i++)
		{
			Enemy enemy;
			float enemyType = GD.Randf();
			int pointIdx = GD.RandRange(0, spawnLine.Count - 1);
			Vector2 enemyPos = Vector2.Zero;
			Vector2 linePoint = spawnLine[pointIdx];
			
			if (enemyType < _boatRatio)
			{
				enemy = _boatEnemyScene.Instantiate<BoatEnemy>();
				
				if (pointIdx - 1 < 0)
					enemyPos = linePoint;
				else
				{
					float w = GD.Randf();
					
					Vector2 prevLinePoint = spawnLine[pointIdx - 1];
					enemyPos = new Vector2(Mathf.Lerp(prevLinePoint.X, linePoint.X, w), 
						Mathf.Lerp(linePoint.Y, prevLinePoint.Y, w));
				} 

				if (Math.Abs(enemyPos.X - _centerX) > 0.1 && GD.Randf() > 0.5)
					enemyPos.X = MirrorX(enemyPos.X);
				
				enemyPos += new Vector2(GD.RandRange(-25, 25), 0);
			}
			else if (enemyType + _boatRatio < _helicopterRatio)
			{
				enemy = _helicopterEnemyScene.Instantiate<HelicopterEnemy>();
			}
			else
			{
				enemy = _jetEnemyScene.Instantiate<JetEnemy>();
			}
			
			enemy.GlobalPosition = enemyPos;
			spawnLine.RemoveAt(pointIdx);

			if (spawnLine.Count == 0)
				return;
			
			section.AddChild(enemy);
		}
	}
	
	private float MirrorX(float x)
	{
		return _centerX - (x - _centerX);
	}

	private List<Vector2> GenerateRiverPoly(List<Vector2> lPoly, List<Vector2> rPoly, List<List<Vector2>> cPolys)
	{
		
		Vector2[] riverPolyArr = lPoly.ToArray().Concat(rPoly.ToArray().Reverse()).ToArray();
		 
		return riverPolyArr.ToList();
	}

	private void SpawnBridge(Node2D section, List<Vector2> lPoly, List<Vector2> rPoly)
	{
		(Vector2 lPoint, Vector2 rPoint) = GenerateTerrainSegment(rPoly.Last(), 224, 224);

		lPoint.X = 300;
		rPoint.X = 852;
		lPoly.Add(lPoint);
		rPoly.Add(rPoint);
		
		var bridge = _bridgeScene.Instantiate<Node2D>();
		bridge.Position = new Vector2(0, rPoint.Y - _bridgeHeight);
		//section.AddChild(bridge);
	}

	private void RenderTerrain(Node2D section, StaticBody2D terrainNode, List<Vector2> points)
	{
		terrainNode.GetNode<Polygon2D>("Polygon2D").Polygon = points.ToArray();
		terrainNode.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon = points.ToArray();
		section.AddChild(terrainNode);
	}
	
	private (Vector2, Vector2) GenerateTerrainSegment(Vector2 lastPoint, float minOff, float maxOff)
	{
		float currY = lastPoint.Y - (float)GD.RandRange(_minYStep, _maxYStep);
		float offset = lastPoint.X - _centerX;
		
		if (GD.Randf() > _straightLineOccurrence)
		{
			float tg = Mathf.Tan(Mathf.DegToRad(_maxDeg));
			float maxOffsetFromLast = Math.Abs((lastPoint.Y - currY) *  tg);
			float min = Math.Max(lastPoint.X - _centerX - maxOffsetFromLast, minOff);
			float max = Math.Min(lastPoint.X - _centerX + maxOffsetFromLast, maxOff);
			offset = (float)GD.RandRange(min, max);
		}
		
		return (new Vector2(_centerX - offset, currY), new Vector2(_centerX + offset, currY));
	}

	private List<Vector2> GenerateBifurcation(Node2D section, Vector2 lastPoint, List<Vector2> rPoly, List<Vector2> lPoly, List<Vector2> spawnLine)
	{
		var centerTerrain = _terrainScene.Instantiate<StaticBody2D>();

		List<Vector2> cPoly = new() { new Vector2(_centerX, lastPoint.Y) };
		List<Vector2> cLeftPoly = new ();
		
		float length = 0;
		while (length < _bifurcationLength)
		{
			(Vector2 lPoint, Vector2 rPoint) = GenerateTerrainSegment(lastPoint, _minOffset + 100, _maxOffset + 100);
			rPoly.Add(rPoint);
			lPoly.Add(lPoint);
			cPoly.Add(rPoint with { X = rPoint.X - 200});
			cLeftPoly.Add(lPoint with { X = lPoint.X + 200});
			length += lastPoint.Y - rPoly.Last().Y;
			lastPoint = rPoly.Last();
			
			spawnLine.Add((cPoly.Last() + rPoly.Last()) / 2);
		}
		
		(Vector2 lPointF, Vector2 rPointF) = GenerateTerrainSegment(rPoly.Last(), _minOffset, _maxOffset);
		cPoly.Add(new Vector2(_centerX, lPointF.Y));
		
		spawnLine.Add(new Vector2(_centerX, cPoly.Last().Y - 50));
		
		cLeftPoly.Reverse();
		cPoly.AddRange(cLeftPoly);
		
		lPoly.Add(lPointF);
		rPoly.Add(rPointF);
		
		RenderTerrain(section, centerTerrain, cPoly);

		return cPoly;
	}
}

