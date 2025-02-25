using Godot;
using System;
using System.Collections.Generic;

public partial class TileManager : Node
{
	[Signal] public delegate void TilePlacedEventHandler(Tile tile, Vector2I position);
	[Signal] public delegate void TileRemovedEventHandler(Tile tile);
	
	[Export] private PackedScene _tileScene;
	[Export] private Node2D _trayNode;
	[Export] private Board _board;
	[Export] private int _trayCapacity = 7;
	[Export] private float _tileSpacing = 70.0f;
	
	private readonly List<Tile> _tilesInTray = new();
	private readonly List<Tile> _tilesOnBoard = new();
	private readonly Dictionary<char, int> _letterValues = new();
	private readonly Random _random = new();
	
	// Letter bag with frequency distribution similar to classic Scrabble
	private readonly List<char> _letterBag = new();
	
	public override void _Ready()
	{
		GD.Print("TileManager initializing...");
		InitializeLetterValues();
		InitializeLetterBag();
		
		// Initial fill of the player's tray
		CallDeferred("RefillTray");
	}
	
	private void InitializeLetterValues()
	{
		_letterValues.Add('A', 1);
		_letterValues.Add('B', 3);
		_letterValues.Add('C', 3);
		_letterValues.Add('D', 2);
		_letterValues.Add('E', 1);
		_letterValues.Add('F', 4);
		_letterValues.Add('G', 2);
		_letterValues.Add('H', 4);
		_letterValues.Add('I', 1);
		_letterValues.Add('J', 8);
		_letterValues.Add('K', 5);
		_letterValues.Add('L', 1);
		_letterValues.Add('M', 3);
		_letterValues.Add('N', 1);
		_letterValues.Add('O', 1);
		_letterValues.Add('P', 3);
		_letterValues.Add('Q', 10);
		_letterValues.Add('R', 1);
		_letterValues.Add('S', 1);
		_letterValues.Add('T', 1);
		_letterValues.Add('U', 1);
		_letterValues.Add('V', 4);
		_letterValues.Add('W', 4);
		_letterValues.Add('X', 8);
		_letterValues.Add('Y', 4);
		_letterValues.Add('Z', 10);
	}
	
	private void InitializeLetterBag()
	{
		// Add letters with their frequency
		AddLettersToBag('A', 9);
		AddLettersToBag('B', 2);
		AddLettersToBag('C', 2);
		AddLettersToBag('D', 4);
		AddLettersToBag('E', 12);
		AddLettersToBag('F', 2);
		AddLettersToBag('G', 3);
		AddLettersToBag('H', 2);
		AddLettersToBag('I', 9);
		AddLettersToBag('J', 1);
		AddLettersToBag('K', 1);
		AddLettersToBag('L', 4);
		AddLettersToBag('M', 2);
		AddLettersToBag('N', 6);
		AddLettersToBag('O', 8);
		AddLettersToBag('P', 2);
		AddLettersToBag('Q', 1);
		AddLettersToBag('R', 6);
		AddLettersToBag('S', 4);
		AddLettersToBag('T', 6);
		AddLettersToBag('U', 4);
		AddLettersToBag('V', 2);
		AddLettersToBag('W', 2);
		AddLettersToBag('X', 1);
		AddLettersToBag('Y', 2);
		AddLettersToBag('Z', 1);
		
		// Shuffle the letter bag
		ShuffleLetterBag();
	}
	
	private void AddLettersToBag(char letter, int count)
	{
		for (int i = 0; i < count; i++)
		{
			_letterBag.Add(letter);
		}
	}
	
	private void ShuffleLetterBag()
	{
		int n = _letterBag.Count;
		while (n > 1)
		{
			n--;
			int k = _random.Next(n + 1);
			(_letterBag[k], _letterBag[n]) = (_letterBag[n], _letterBag[k]);
		}
	}
	
	public void RefillTray()
	{
		GD.Print("Refilling tray...");
		// Create a copy of the list to avoid collection modification issues
		var tilesToCheck = new List<Tile>(_tilesInTray);
		
		// Clear existing tiles if they are invalid
		foreach (var existingTile in tilesToCheck)
		{
			if (existingTile == null || !IsInstanceValid(existingTile))
			{
				_tilesInTray.Remove(existingTile);
			}
		}
		
		int tilesToAdd = _trayCapacity - _tilesInTray.Count;
		GD.Print($"Need to add {tilesToAdd} tiles to tray");
		
		// If the bag is empty, don't try to add more tiles
		if (_letterBag.Count == 0 && tilesToAdd > 0)
		{
			GD.Print("Letter bag is empty! Game might be over.");
			return;
		}
		
		// Reposition existing tiles to account for any that were removed
		OrganizeTray();
		
		for (int i = 0; i < tilesToAdd && _letterBag.Count > 0; i++)
		{
			// Draw a random letter from the bag
			int index = _random.Next(_letterBag.Count);
			char letter = _letterBag[index];
			_letterBag.RemoveAt(index);
			
			// Create a new tile
			var tile = _tileScene.Instantiate<Tile>();
			_trayNode.AddChild(tile);
			
			int value = _letterValues[letter];
			tile.Initialize(letter, value);
			
			// Position in tray with proper spacing
			float xPos = (_tilesInTray.Count * _tileSpacing);
			tile.Position = new Vector2(xPos, 0);
			// Wait until the tile is actually in the scene tree before setting original position
			CallDeferred(nameof(SetTileOriginalPosition), tile);
			
			// Connect signals
			tile.TilePickedUp += OnTilePickedUp;
			tile.TileDropped += OnTileDropped;
			
			_tilesInTray.Add(tile);
			GD.Print($"Added tile {letter} to tray at position {xPos}");
		}
	}
	
	private void OrganizeTray()
	{
		for (int i = 0; i < _tilesInTray.Count; i++)
		{
			var tile = _tilesInTray[i];
			if (tile.State == TileState.TRAY)
			{
				float xPos = i * _tileSpacing;
				tile.Position = new Vector2(xPos, 0);
				tile.OriginalPosition = tile.GlobalPosition;
			}
		}
	}
	
	private void SetTileOriginalPosition(Tile tile)
	{
		if (tile != null && IsInstanceValid(tile))
		{
			tile.OriginalPosition = tile.GlobalPosition;
			GD.Print($"Set original position for tile {tile.Letter}: {tile.OriginalPosition}");
		}
	}
	
	public void ReturnTileToTray(Tile tile)
	{
		if (tile.State == TileState.PENDING)
		{
			// Emit signal that the tile is being removed from the board
			EmitSignal(SignalName.TileRemoved, tile);
			
			// Return the tile to the tray physically
			tile.ReturnToTray();
			
			// If it's not already in the tray list, add it back
			if (!_tilesInTray.Contains(tile))
			{
				_tilesInTray.Add(tile);
				OrganizeTray();
			}
		}
	}
	
	private void OnTilePickedUp(Tile tile)
	{
		GD.Print($"Tile {tile.Letter} picked up");
		
		// If the tile was on the board (but not confirmed), remove it from the board
		if (tile.State == TileState.PENDING)
		{
			// Update the cell to not have a tile anymore
			_board.GetCellAt(tile.GridPosition)?.RemoveTile();
			
			// Emit signal that the tile is being removed
			EmitSignal(SignalName.TileRemoved, tile);
		}
	}
	
	private void OnTileDropped(Tile tile, Vector2 position)
	{
		// Convert to grid position
		Vector2I gridPosition = _board.WorldToGrid(position);
		
		GD.Print($"Drop at world position: {position}, grid position: {gridPosition}");
		
		// Check if position is valid and not occupied
		if (_board.IsPositionValid(gridPosition) && !_board.IsCellOccupied(gridPosition))
		{
			// Place the tile
			Vector2 centerPos = _board.GetCellCenterPosition(gridPosition);
			tile.Place(centerPos, gridPosition);
			bool success = _board.PlaceTile(tile, gridPosition);
			
			if (success)
			{
				GD.Print($"Successfully placed tile {tile.Letter} at grid position {gridPosition}");
				
				// Remove from tray (but don't clear it completely, since this is just pending placement)
				_tilesInTray.Remove(tile);
				
				// Emit signal that a tile has been placed
				EmitSignal(SignalName.TilePlaced, tile, gridPosition);
				
				// Don't refill the tray yet - wait until the word is confirmed
			}
			else
			{
				GD.Print($"Failed to place tile on board despite valid position check");
				tile.ReturnToTray();
			}
		}
		else
		{
			GD.Print($"Invalid placement: Valid={_board.IsPositionValid(gridPosition)}, Occupied={_board.IsCellOccupied(gridPosition)}");
			// Return to tray
			tile.ReturnToTray();
		}
	}
}
