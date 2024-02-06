using Godot;
using System;

public partial class ChessGame : Node2D
{
	// board class that contains everything related to the pieces

	private Board board;

	// graphical representation of a board

	private BoardGraphics boardGraphics;

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		// get the nodes

		boardGraphics = GetNode<BoardGraphics>("BoardGraphics");

		// init the board and load the fen

		board = new Board();
		board.LoadFenString(Board.StartFEN);

		// connect the board graphical representation with the board itself

		boardGraphics.ConnectToBoard(board);

		// update the board graphics

		boardGraphics.UpdateGraphics();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
	{

	}
}
