using Godot;
using System;

public enum TileState
{
	TRAY,
	DRAGGING,
	PENDING,
	CONFIRMED
}

public partial class Tile : Node2D
{
	[Signal] public delegate void TilePickedUpEventHandler(Tile tile);
	[Signal] public delegate void TileDroppedEventHandler(Tile tile, Vector2 position);
	
	[Export] private Sprite2D _sprite;
	[Export] private Label _letterLabel;
	[Export] private Label _valueLabel;
	
	public char Letter { get; private set; }
	public int Value { get; private set; }
	public TileState State { get; private set; }
	public Vector2 OriginalPosition { get; set; }
	public Vector2I GridPosition { get; set; }
	
	private bool _isDragging = false;
	private Vector2 _dragOffset;
	private Area2D _dragArea;
	private ColorRect _tileBorder;
	private ColorRect _tileFace;
	
	public override void _Ready()
	{
		State = TileState.TRAY;
		
		// Get references to the visual elements
		_tileBorder = GetNode<ColorRect>("Border");
		_tileFace = GetNode<ColorRect>("TileFace");
		
		// Create a dedicated Area2D for input handling
		_dragArea = new Area2D();
		AddChild(_dragArea);
		
		// Create collision shape for input detection
		var collisionShape = new CollisionShape2D();
		var shape = new RectangleShape2D();
		shape.Size = new Vector2(60, 60);
		collisionShape.Shape = shape;
		_dragArea.AddChild(collisionShape);
		
		// Connect input event
		_dragArea.InputEvent += OnInputEvent;
		
		// Set process input only when dragging
		SetProcessInput(false);
	}
	
	public void Initialize(char letter, int value)
	{
		Letter = letter;
		Value = value;
		
		_letterLabel.Text = letter.ToString();
		_valueLabel.Text = value.ToString();
	}
	
	private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		if (State == TileState.CONFIRMED)
			return;
			
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			if (mouseEvent.Pressed)
			{
				GD.Print($"Tile {Letter} clicked");
				_isDragging = true;
				State = TileState.DRAGGING;
				_dragOffset = GlobalPosition - GetGlobalMousePosition();
				
				// Scale up the tile slightly when picked up
				Scale = new Vector2(1.1f, 1.1f);
				ZIndex = 10; // Ensure dragged tile appears above others
				
				EmitSignal(SignalName.TilePickedUp, this);
				
				// Enable process input to handle movement and release
				SetProcessInput(true);
				
				// Accept the event to prevent it from propagating
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (!_isDragging)
			return;
			
		if (@event is InputEventMouseButton mouseEvent && 
			mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
		{
			// Release
			_isDragging = false;
			Scale = Vector2.One;
			EmitSignal(SignalName.TileDropped, this, GlobalPosition);
			
			// Disable process input when not dragging
			SetProcessInput(false);
			
			// Accept the event to prevent it from propagating
			GetViewport().SetInputAsHandled();
		}
		else if (@event is InputEventMouseMotion && _isDragging)
		{
			// Update position during drag
			GlobalPosition = GetGlobalMousePosition() + _dragOffset;
			
			// Accept the event to prevent it from propagating
			GetViewport().SetInputAsHandled();
		}
	}
	
	public override void _Process(double delta)
	{
		// Visual feedback during dragging
		if (_isDragging)
		{
			// Subtle pulsing effect
			float pulse = (float)Math.Sin(Time.GetTicksMsec() * 0.01) * 0.05f + 1.1f;
			Scale = new Vector2(pulse, pulse);
		}
		
		// Update visual appearance based on state
		UpdateVisuals();
	}
	
	private void UpdateVisuals()
	{
		// Set colors based on tile state
		switch (State)
		{
			case TileState.TRAY:
				_tileBorder.Color = new Color(0.65f, 0.44f, 0.15f);
				_tileFace.Color = new Color(1.0f, 0.93f, 0.83f);
				break;
				
			case TileState.DRAGGING:
				_tileBorder.Color = new Color(0.65f, 0.44f, 0.15f);
				_tileFace.Color = new Color(1.0f, 0.95f, 0.9f);
				break;
				
			case TileState.PENDING:
				_tileBorder.Color = new Color(0.8f, 0.65f, 0.2f);
				_tileFace.Color = new Color(1.0f, 0.9f, 0.8f);
				break;
				
			case TileState.CONFIRMED:
				_tileBorder.Color = new Color(0.4f, 0.55f, 0.3f);
				_tileFace.Color = new Color(0.9f, 1.0f, 0.9f);
				break;
		}
	}
	
	public void ReturnToTray()
	{
		State = TileState.TRAY;
		GlobalPosition = OriginalPosition;
		ZIndex = 0;
		Scale = Vector2.One;
		_isDragging = false;
		SetProcessInput(false);
	}
	
	public void Place(Vector2 position, Vector2I gridPos)
	{
		State = TileState.PENDING;
		GlobalPosition = position;
		GridPosition = gridPos;
		ZIndex = 0;
		Scale = Vector2.One;
		_isDragging = false;
		SetProcessInput(false);
	}
	
	public void ConfirmPlacement()
	{
		State = TileState.CONFIRMED;
		
		// Disable dragging once confirmed
		_dragArea.InputEvent -= OnInputEvent;
		SetProcessInput(false);
	}
}
