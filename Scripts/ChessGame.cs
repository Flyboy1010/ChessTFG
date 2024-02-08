using Godot;
using System;

public partial class ChessGame : Node2D
{
	// board class that contains everything related to the pieces

	private Board board;

	// graphical representation of a board

	private BoardGraphics boardGraphics;

    // selected piece index

    private bool isPieceSelected = false;
    private int pieceSelectedIndex = -1; // -1 means nothing selected

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
        // get the mouse coordinates

        Vector2 mouse = boardGraphics.GetLocalMousePosition();

        // get the square the mouse is on

        bool isOnSquare = boardGraphics.TryGetSquareIndexFromCoords(mouse, out int squareIndex);

        // the first frame you click

        if (Input.IsActionJustPressed("Select"))
        {
            if (isOnSquare)
            {
                // get the piece

                Piece piece = board.GetPiece(squareIndex);

                // if not none then select it

                if (piece.type != Piece.Type.None)
                {
                    isPieceSelected = true;
                    pieceSelectedIndex = squareIndex;
                }
            }
            else
            {
                isPieceSelected = false;
                pieceSelectedIndex = -1;
            }
        }

        // if you hold

        if (Input.IsActionPressed("Select"))
        {
            if (isPieceSelected)
            {
                // update the selected piece

                boardGraphics.SetPieceSpritePosition(pieceSelectedIndex, mouse);
            }
        }

        // the frame you release

        if (Input.IsActionJustReleased("Select"))
        {
            if (isPieceSelected)
            {
                if (isOnSquare)
                {
                    // make the move

                    Move move = new Move()
                    {
                        squareSourceIndex = pieceSelectedIndex,
                        squareTargetIndex = squareIndex
                    };

                    board.MakeMove(move);
                }

                // deselect the piece

                isPieceSelected = false;
                pieceSelectedIndex = -1;
            }

            // update graphics

            boardGraphics.UpdateGraphics();
        }
    }
}
