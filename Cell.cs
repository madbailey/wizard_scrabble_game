using Godot;
using System;

public enum BonusType
{
	None,
	DoubleLetter,
	TripleLetter,
	DoubleWord,
	TripleWord
}

public partial class Cell : Node2D
{
	[Export] private Sprite2D _sprite;
	[Export] private Area2D _dropArea;
	[Export] private Label _bonusLabel;
	
	public Vector2I GridPosition { get; private set; }
	public bool HasTile { get; private set; }
	public BonusType Bonus { get; private set; }
	
	private Tile _currentTile;
	
	private readonly Color[] _bonusColors = new Color[]
	{
		new Color(0.9f, 0.9f, 0.9f), // None - Light gray
		new Color(0.5f, 0.7f, 1.0f), // Double Letter - Light blue
		new Color(0.0f, 0.5f, 1.0f), // Triple Letter - Medium blue
		new Color(1.0f, 0.7f, 0.7f), // Double Word - Light red
		new Color(1.0f, 0.3f, 0.3f)  // Triple Word - Medium red
	};
	
	public override void _Ready()
	{
		_dropArea.InputEvent += OnInputEvent;
	}
	
	public void Initialize(Vector2I gridPos)
	{
		GridPosition = gridPos;
		HasTile = false;
	}
	
	public void SetBonus(BonusType bonus)
	{
		Bonus = bonus;
		_sprite.Modulate = _bonusColors[(int)bonus];
		
		if (bonus == BonusType.None)
		{
			_bonusLabel.Text = "";
			return;
		}
		
		switch (bonus)
		{
			case BonusType.DoubleLetter:
				_bonusLabel.Text = "DL";
				break;
			case BonusType.TripleLetter:
				_bonusLabel.Text = "TL";
				break;
			case BonusType.DoubleWord:
				_bonusLabel.Text = "DW";
				break;
			case BonusType.TripleWord:
				_bonusLabel.Text = "TW";
				break;
		}
	}
	
	public void SetTile(Tile tile)
	{
		if (tile == null)
		{
			HasTile = false;
			_currentTile = null;
			return;
		}
		
		HasTile = true;
		_currentTile = tile;
	}
	
	public void RemoveTile()
	{
		HasTile = false;
		_currentTile = null;
	}
	
	private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		// This will be triggered when a tile is dropped on this cell
		// Handled by TileManager
	}
}
