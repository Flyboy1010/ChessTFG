using Godot;
using System;
using System.Collections.Generic;

public partial class ChessGame : Node2D
{
	// board class that contains everything related to the pieces

	private Board board;

	// graphical representation of a board

	private BoardGraphics boardGraphics;

    // selected piece index

    private bool isPieceSelected = false;
    private int pieceSelectedIndex = -1; // -1 means nothing selected
    private List<Move> pieceSelectedMoves = null;

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
        // get turn color

        Piece.Color turnColor = board.GetTurnColor();

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

                if (piece.type != Piece.Type.None && piece.color == turnColor)
                {
                    // select piece

                    isPieceSelected = true;
                    pieceSelectedIndex = squareIndex;
                    pieceSelectedMoves = MoveGeneration.GetLegalMoves(board, squareIndex);

                    // set hint moves

                    boardGraphics.SetHintMoves(pieceSelectedMoves);
                }
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
                    // check if the square is a valid move

                    foreach (Move move in pieceSelectedMoves)
                    {
                        // if the move is found

                        if (move.squareTargetIndex == squareIndex)
                        {
                            // make the move

                            board.MakeMove(move);

                            break;
                        }
                    }
                }
            }

            // deselect the piece

            isPieceSelected = false;
            pieceSelectedIndex = -1;
            pieceSelectedMoves = null;

            // clear hint moves

            boardGraphics.SetHintMoves(null);

            // update graphics

            boardGraphics.UpdateGraphics();
        }
    }
}
