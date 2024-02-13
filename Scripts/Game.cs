using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    // label with the last move

    [Export] private Label lastMoveLabel;
    [Export] private Label zobristLabel;
    [Export] private Label zobrist2Label;

    // board class that contains everything related to the pieces

    private Board board;

	// graphical representation of a board

	private BoardGraphics boardGraphics;

    // selected piece index

    private int pieceSelectedIndex = -1; // -1 means nothing selected
    private List<Move> pieceSelectedMoves = null;

    // ui state machine testing

    private enum BoardUIState
    {
        Idle,
        Holding,
        PieceSelected,
        Animation
    }

    private BoardUIState boardUIState = BoardUIState.Idle;

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

        // testing

        ulong key = ZobristHashing.GetKey(board);
        zobristLabel.Text = Utils.ConvertULongToBinaryString(key);
        zobrist2Label.Text = "[" + key + "]";
    }

    private void BoardUIUpdate()
    {
        // get turn color

        Piece.Color turnColor = board.GetTurnColor();

        // get the mouse coordinates

        Vector2 mouse = boardGraphics.GetLocalMousePosition();

        // get the square the mouse is on

        bool isOnSquare = boardGraphics.TryGetSquareIndexFromCoords(mouse, out int squareIndex);

        // state machine

        switch (boardUIState)
        {
            case BoardUIState.Idle:
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

                            pieceSelectedIndex = squareIndex;
                            pieceSelectedMoves = MoveGeneration.GetLegalMoves(board, squareIndex);

                            // set hint moves

                            boardGraphics.SetHintMoves(pieceSelectedMoves);

                            // change state

                            boardUIState = BoardUIState.Holding;
                        }
                    }
                }
                break;
            case BoardUIState.Holding:
                // move the piece selected to the mouse position

                boardGraphics.SetPieceSpritePosition(pieceSelectedIndex, mouse);

                // select square

                boardGraphics.SelectSquare(isOnSquare ? squareIndex : -1);

                // if stop holding

                if (Input.IsActionJustReleased("Select"))
                {
                    if (isOnSquare)
                    {
                        // if the square is a valid move

                        bool isLegalMove = false;
                        Move selectedMove = Move.NullMove;

                        foreach (Move move in pieceSelectedMoves)
                        {
                            if (move.squareTargetIndex == squareIndex)
                            {
                                isLegalMove = true;
                                selectedMove = move;
                                break;
                            }
                        }

                        // check if legal move

                        if (isLegalMove)
                        {
                            // make the move

                            board.MakeMove(selectedMove);
                            lastMoveLabel.Text = "Last move: " + Utils.FromMoveToString(selectedMove);
                            ulong key = ZobristHashing.GetKey(board);
                            zobristLabel.Text = Utils.ConvertULongToBinaryString(key);
                            zobrist2Label.Text = "[" + key + "]";

                            // disable square selection

                            boardGraphics.SelectSquare(-1);

                            // disable hint moves

                            boardGraphics.SetHintMoves(null);

                            // animate the move

                            boardGraphics.AnimateMove(selectedMove, false, Callable.From(() =>
                            {
                                boardGraphics.UpdateGraphics();
                                boardUIState = BoardUIState.Idle;
                            }));

                            // change state

                            boardUIState = BoardUIState.Animation;

                            // exit

                            return;
                        }
                    }

                    // disable selected square

                    boardGraphics.SelectSquare(-1);

                    // update graphics

                    boardGraphics.UpdateGraphics();

                    // go to piece selected state

                    boardUIState = BoardUIState.PieceSelected;
                }
                break;
            case BoardUIState.PieceSelected:
                // the first frame you click

                if (Input.IsActionJustPressed("Select"))
                {
                    if (isOnSquare)
                    {
                        // if the square is a valid move

                        bool isLegalMove = false;
                        Move selectedMove = Move.NullMove;

                        foreach (Move move in pieceSelectedMoves)
                        {
                            if (move.squareTargetIndex == squareIndex)
                            {
                                isLegalMove = true;
                                selectedMove = move;
                                break;
                            }
                        }

                        // check if legal move

                        if (isLegalMove)
                        {
                            // make the move

                            board.MakeMove(selectedMove);
                            lastMoveLabel.Text = "Last move: " + Utils.FromMoveToString(selectedMove);
                            ulong key = ZobristHashing.GetKey(board);
                            zobristLabel.Text = Utils.ConvertULongToBinaryString(key);
                            zobrist2Label.Text = "[" + key + "]";

                            // disable hint moves

                            boardGraphics.SetHintMoves(null);

                            // animate the move

                            boardGraphics.AnimateMove(selectedMove, true, Callable.From(() =>
                            {
                                boardGraphics.UpdateGraphics();
                                boardUIState = BoardUIState.Idle;
                            }));

                            // change state

                            boardUIState = BoardUIState.Animation;

                            // exit

                            return;
                        }

                        // if the move is not legal then check if another piece is selected

                        // get the piece

                        Piece piece = board.GetPiece(squareIndex);

                        // if not none then select it

                        if (piece.type != Piece.Type.None && piece.color == turnColor)
                        {
                            // select piece

                            pieceSelectedIndex = squareIndex;
                            pieceSelectedMoves = MoveGeneration.GetLegalMoves(board, squareIndex);

                            // set hint moves

                            boardGraphics.SetHintMoves(pieceSelectedMoves);

                            // change state to holding the piece

                            boardUIState = BoardUIState.Holding;

                            // exit

                            return;
                        }
                    }

                    // disable hint moves

                    boardGraphics.SetHintMoves(null);

                    // go back to idle state

                    boardUIState = BoardUIState.Idle;
                }
                break;
            case BoardUIState.Animation:
                
                // just wait until the animation is completed
                break;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
	{
        BoardUIUpdate();
    }
}
