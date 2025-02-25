using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
	[Export] private Board _board;
	[Export] private TileManager _tileManager;
	[Export] private Button _submitButton;
	[Export] private Label _scoreLabel;
	[Export] private Label _messageLabel;
	
	private int _currentScore = 0;
	private List<Tile> _pendingTiles = new List<Tile>();
	
	// Game rules
	private bool _isFirstWord = true;
	private Vector2I _centerPosition = new Vector2I(7, 7);
	
	public override void _Ready()
	{
		// Setup camera position
		var camera = GetNode<Camera2D>("../Camera2D");
		if (camera != null)
		{
			camera.MakeCurrent();
		}
		
		// Setup UI
		if (_submitButton != null)
		{
			_submitButton.Pressed += OnSubmitPressed;
		}
		
		if (_scoreLabel != null)
		{
			UpdateScoreDisplay();
		}
		
		if (_messageLabel != null)
		{
			_messageLabel.Text = "Place your first word through the center square";
		}
		
		// Register for tile placement events
		if (_tileManager != null)
		{
			_tileManager.TilePlaced += OnTilePlaced;
			_tileManager.TileRemoved += OnTileRemoved;
		}
		
		// Initialize game state here
		GD.Print("Game initialized!");
	}
	
	public void RegisterPendingTile(Tile tile, Vector2I position)
	{
		_pendingTiles.Add(tile);
		GD.Print($"Registered pending tile {tile.Letter} at position {position}");
	}
	
	public void RemovePendingTile(Tile tile)
	{
		_pendingTiles.Remove(tile);
		GD.Print($"Removed pending tile {tile.Letter}");
	}
	
	private void OnTilePlaced(Tile tile, Vector2I position)
	{
		RegisterPendingTile(tile, position);
	}
	
	private void OnTileRemoved(Tile tile)
	{
		RemovePendingTile(tile);
	}
	
	private void OnSubmitPressed()
	{
		if (_pendingTiles.Count == 0)
		{
			ShowMessage("No tiles placed! Place tiles before submitting.");
			return;
		}
		
		// Check placement rules
		if (!ValidatePlacement())
		{
			ReturnPendingTilesToTray();
			return;
		}
		
		// Calculate score for the word(s)
		int wordScore = CalculateScore();
		_currentScore += wordScore;
		
		// Commit the tiles (make them permanent)
		CommitPendingTiles();
		
		// Update UI
		UpdateScoreDisplay();
		ShowMessage($"Word accepted! +{wordScore} points");
		
		// First word has been played
		_isFirstWord = false;
	}
	
	private bool ValidatePlacement()
	{
		// Get all tiles in order
		var tilesInOrder = GetOrderedTiles();
		
		// Check if we have at least one tile
		if (tilesInOrder.Count == 0)
		{
			ShowMessage("No tiles placed!");
			return false;
		}
		
		// For the first word, check if it includes the center square
		if (_isFirstWord)
		{
			bool throughCenter = false;
			foreach (var tileInfo in tilesInOrder)
			{
				if (tileInfo.Position == _centerPosition)
				{
					throughCenter = true;
					break;
				}
			}
			
			if (!throughCenter)
			{
				ShowMessage("The first word must be placed through the center square!");
				return false;
			}
		}
		else
		{
			// After first word, check if the word connects to existing tiles
			bool connected = false;
			foreach (var tileInfo in tilesInOrder)
			{
				// Check adjacent cells
				Vector2I[] adjacentPositions = new Vector2I[]
				{
					new Vector2I(tileInfo.Position.X + 1, tileInfo.Position.Y),
					new Vector2I(tileInfo.Position.X - 1, tileInfo.Position.Y),
					new Vector2I(tileInfo.Position.X, tileInfo.Position.Y + 1),
					new Vector2I(tileInfo.Position.X, tileInfo.Position.Y - 1)
				};
				
				foreach (var adjPos in adjacentPositions)
				{
					if (_board.IsPositionValid(adjPos) && _board.IsCellOccupied(adjPos) && 
						!_pendingTiles.Any(t => t.GridPosition == adjPos))
					{
						connected = true;
						break;
					}
				}
				
				if (connected) break;
			}
			
			if (!connected)
			{
				ShowMessage("New words must connect to existing tiles on the board!");
				return false;
			}
		}
		
		// Check if tiles are in a line
		bool isHorizontal = tilesInOrder.All(t => t.Position.Y == tilesInOrder[0].Position.Y);
		bool isVertical = tilesInOrder.All(t => t.Position.X == tilesInOrder[0].Position.X);
		
		if (!isHorizontal && !isVertical)
		{
			ShowMessage("Tiles must be placed in a straight line!");
			return false;
		}
		
		// Check if the word is continuous (no gaps)
		if (isHorizontal)
		{
			var xPositions = tilesInOrder.Select(t => t.Position.X).OrderBy(x => x).ToList();
			for (int x = xPositions.First(); x < xPositions.Last(); x++)
			{
				Vector2I checkPos = new Vector2I(x, tilesInOrder[0].Position.Y);
				if (!_board.IsCellOccupied(checkPos) && !_pendingTiles.Any(t => t.GridPosition == checkPos))
				{
					ShowMessage("Words must be continuous with no gaps!");
					return false;
				}
			}
		}
		else // isVertical
		{
			var yPositions = tilesInOrder.Select(t => t.Position.Y).OrderBy(y => y).ToList();
			for (int y = yPositions.First(); y < yPositions.Last(); y++)
			{
				Vector2I checkPos = new Vector2I(tilesInOrder[0].Position.X, y);
				if (!_board.IsCellOccupied(checkPos) && !_pendingTiles.Any(t => t.GridPosition == checkPos))
				{
					ShowMessage("Words must be continuous with no gaps!");
					return false;
				}
			}
		}
		
		// Check if the word is at least 2 letters long
		string word = GetWordFromTiles(tilesInOrder);
		if (word.Length < 2)
		{
			ShowMessage("Words must be at least 2 letters long!");
			return false;
		}
		
		// Word is valid!
		return true;
	}
	
	private void ReturnPendingTilesToTray()
	{
		// Create a copy of the list to avoid modification issues during iteration
		var tilesToReturn = new List<Tile>(_pendingTiles);
		
		foreach (var tile in tilesToReturn)
		{
			_tileManager.ReturnTileToTray(tile);
		}
		
		_pendingTiles.Clear();
	}
	
	private void CommitPendingTiles()
	{
		foreach (var tile in _pendingTiles)
		{
			// Mark the tile as permanently placed
			tile.ConfirmPlacement();
		}
		
		// Clear the pending tiles list
		_pendingTiles.Clear();
		
		// Refill the player's tray
		_tileManager.RefillTray();
	}
	
	private List<(Tile Tile, Vector2I Position)> GetOrderedTiles()
	{
		var tilesWithPositions = new List<(Tile Tile, Vector2I Position)>();
		
		foreach (var tile in _pendingTiles)
		{
			tilesWithPositions.Add((tile, tile.GridPosition));
		}
		
		// Sort by position (first by X, then by Y)
		return tilesWithPositions.OrderBy(t => t.Position.X).ThenBy(t => t.Position.Y).ToList();
	}
	
	private string GetWordFromTiles(List<(Tile Tile, Vector2I Position)> tilesInOrder)
	{
		string word = "";
		foreach (var tileInfo in tilesInOrder)
		{
			word += tileInfo.Tile.Letter;
		}
		
		return word;
	}
	
	private int CalculateScore()
	{
		// Get the main word
		var tilesInOrder = GetOrderedTiles();
		string word = GetWordFromTiles(tilesInOrder);
		
		// Get the bonuses for each letter
		BonusType[] bonuses = new BonusType[word.Length];
		for (int i = 0; i < word.Length; i++)
		{
			// Get the cell's bonus at this position
			Vector2I position = tilesInOrder[i].Position;
			
			// In a real implementation, we'd check what bonus this cell has
			// For now, we'll just use None as a placeholder
			bonuses[i] = BonusType.None;
		}
		
		return CalculateWordScore(word, bonuses);
	}
	
	// Simple method to calculate the score for a word
	public int CalculateWordScore(string word, BonusType[] bonuses)
	{
		int score = 0;
		int wordMultiplier = 1;
		
		for (int i = 0; i < word.Length; i++)
		{
			char letter = word[i];
			int letterValue = GetLetterValue(letter);
			int letterScore = letterValue;
			
			// Apply letter multipliers
			if (i < bonuses.Length)
			{
				switch (bonuses[i])
				{
					case BonusType.DoubleLetter:
						letterScore *= 2;
						break;
					case BonusType.TripleLetter:
						letterScore *= 3;
						break;
					case BonusType.DoubleWord:
						wordMultiplier *= 2;
						break;
					case BonusType.TripleWord:
						wordMultiplier *= 3;
						break;
				}
			}
			
			score += letterScore;
		}
		
		// Apply word multiplier
		score *= wordMultiplier;
		
		return score;
	}
	
	private int GetLetterValue(char letter)
	{
		// This should match the values in TileManager
		switch (char.ToUpper(letter))
		{
			case 'A': case 'E': case 'I': case 'L': case 'N': case 'O': case 'R': case 'S': case 'T': case 'U':
				return 1;
			case 'D': case 'G':
				return 2;
			case 'B': case 'C': case 'M': case 'P':
				return 3;
			case 'F': case 'H': case 'V': case 'W': case 'Y':
				return 4;
			case 'K':
				return 5;
			case 'J': case 'X':
				return 8;
			case 'Q': case 'Z':
				return 10;
			default:
				return 0;
		}
	}
	
	private void UpdateScoreDisplay()
	{
		if (_scoreLabel != null)
		{
			_scoreLabel.Text = $"Score: {_currentScore}";
		}
	}
	
	private void ShowMessage(string message)
	{
		if (_messageLabel != null)
		{
			_messageLabel.Text = message;
			
			// Optional: Add a timer to clear the message after a few seconds
			var timer = new Timer();
			timer.WaitTime = 5.0f;
			timer.OneShot = true;
			AddChild(timer);
			timer.Timeout += () => {
				_messageLabel.Text = "";
				timer.QueueFree();
			};
			timer.Start();
		}
		
		GD.Print(message);
	}
}
