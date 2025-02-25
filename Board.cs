using Godot;
using System;

public partial class Board : Node2D
{
	[Export] private PackedScene _cellScene;
	[Export] private int _boardSize = 15;
	[Export] private float _cellSize = 64.0f;
	
	private Cell[,] _cells;
	
	public override void _Ready()
	{
		_cells = new Cell[_boardSize, _boardSize];
		GenerateBoard();
	}
	
	private void GenerateBoard()
	{
		for (int y = 0; y < _boardSize; y++)
		{
			for (int x = 0; x < _boardSize; x++)
			{
				var cell = _cellScene.Instantiate<Cell>();
				AddChild(cell);
				
				Vector2 position = new Vector2(x * _cellSize, y * _cellSize);
				cell.Position = position;
				cell.Initialize(new Vector2I(x, y));
				
				_cells[x, y] = cell;
				
				// Add bonus cells (would typically be loaded from data)
				SetCellBonus(cell, x, y);
			}
		}
	}
	
	private void SetCellBonus(Cell cell, int x, int y)
	{
		// Triple Word cells
		if ((x == 0 || x == 7 || x == 14) && (y == 0 || y == 7 || y == 14) ||
			(x == 7 && y == 7))
		{
			cell.SetBonus(BonusType.TripleWord);
			return;
		}
		
		// Double Word cells
		if (x == y || x == _boardSize - 1 - y)
		{
			if (x != 0 && x != 7 && x != 14)
			{
				cell.SetBonus(BonusType.DoubleWord);
				return;
			}
		}
		
		// Triple Letter cells
		if ((x == 1 || x == 5 || x == 9 || x == 13) && (y == 5 || y == 9) ||
			(y == 1 || y == 5 || y == 9 || y == 13) && (x == 5 || x == 9))
		{
			cell.SetBonus(BonusType.TripleLetter);
			return;
		}
		
		// Double Letter cells
		if ((x == 3 || x == 11) && (y == 0 || y == 7 || y == 14) ||
			(y == 3 || y == 11) && (x == 0 || x == 7 || x == 14) ||
			(x == 2 || x == 6 || x == 8 || x == 12) && (y == 6 || y == 8) ||
			(y == 2 || y == 6 || y == 8 || y == 12) && (x == 6 || x == 8))
		{
			cell.SetBonus(BonusType.DoubleLetter);
		}
	}
	
	public bool IsCellOccupied(Vector2I gridPosition)
	{
		if (!IsPositionValid(gridPosition))
			return true;
			
		return _cells[gridPosition.X, gridPosition.Y].HasTile;
	}
	
	public bool IsPositionValid(Vector2I gridPosition)
	{
		return gridPosition.X >= 0 && gridPosition.X < _boardSize &&
			   gridPosition.Y >= 0 && gridPosition.Y < _boardSize;
	}
	
	public Vector2 GetCellCenterPosition(Vector2I gridPosition)
	{
		if (!IsPositionValid(gridPosition))
			return Vector2.Zero;
			
		return _cells[gridPosition.X, gridPosition.Y].Position + new Vector2(_cellSize / 2, _cellSize / 2);
	}
	
	public Vector2I WorldToGrid(Vector2 worldPosition)
	{
		int x = Mathf.FloorToInt(worldPosition.X / _cellSize);
		int y = Mathf.FloorToInt(worldPosition.Y / _cellSize);
		return new Vector2I(x, y);
	}
	
	public bool PlaceTile(Tile tile, Vector2I gridPosition)
	{
		if (IsCellOccupied(gridPosition))
			return false;
			
		Cell targetCell = _cells[gridPosition.X, gridPosition.Y];
		targetCell.SetTile(tile);
		return true;
	}
}
