using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    // game over reasons

    public enum GameOverReason
    {
        WhiteMated,
        BlackMated,
        Drowned,
        Repetition
    }

    // game state machine

    private enum GameState
    {
        PlayerWhiteTurn,
        PlayerBlackTurn,
        MakeMove,
        WaitUntilCompleted,
        NextTurn,
        Over
    }

    // ui state machine testing

    private enum BoardGraphicsState
    {
        Idle,
        Dragging,
        PieceSelected
    }

    // signals

    [Signal] public delegate void OnGameTurnEventHandler();
    [Signal] public delegate void OnGameOverEventHandler(GameOverReason gameOverReason);

    // board class that contains everything related to the pieces

    private Board board;

	// graphical representation of a board

	private BoardGraphics boardGraphics;

    // ui

    private UI ui;

    // selected piece index

    private int pieceSelectedIndex = -1; // -1 means nothing selected
    private List<Move> pieceSelectedMoves = null;
    private Move moveSelected = Move.NullMove;
    private bool isMoveAnimated = false;

    // state of the game and boardUI

    private GameState gameState = GameState.NextTurn;
    private BoardGraphicsState boardUIState = BoardGraphicsState.Idle;

    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
	{
		// get the nodes

		boardGraphics = GetNode<BoardGraphics>("BoardGraphics");
        ui = GetNode<UI>("UI");

		// init the board and load the fen

		board = new Board();
		board.LoadFenString(Board.StartFEN);

		// connect the board graphical representation with the board itself

		boardGraphics.ConnectToBoard(board);

        // connect the ui to the game

        ui.ConnectToGame(this);

		// update the board graphics

		boardGraphics.UpdateGraphics();
    }

    // get the board

    public Board GetBoard()
    {
        return board;
    }

    // play as color

    public void PlayAsColor(Piece.Color color)
    {
        // board reset fen

        board.LoadFenString(Board.StartFEN);

        // set game state to next turn

        gameState = GameState.NextTurn;
        boardUIState = BoardGraphicsState.Idle;

        // check if white or black

        switch (color)
        {
            case Piece.Color.White:
                boardGraphics.FlipBoard(false);
                break;
            case Piece.Color.Black:
                boardGraphics.FlipBoard(true);
                break;
        }

        // reset graphics

        boardGraphics.SelectSquare(-1);
        boardGraphics.SetHintMoves(null);
        boardGraphics.UpdateGraphics();
    }

    // player human

    private bool PlayerHumanUpdateState()
    {
        // handle everything related to move selection

        // get turn color

        Piece.Color turnColor = board.GetTurnColor();

        // get the mouse coordinates

        Vector2 mouse = boardGraphics.GetLocalMousePosition();

        // get the square the mouse is on

        bool isOnSquare = boardGraphics.TryGetSquareIndexFromCoords(mouse, out int squareIndex);

        // state machine

        switch (boardUIState)
        {
            case BoardGraphicsState.Idle:
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

                            // highlight square

                            boardGraphics.HightlightSquare(squareIndex);

                            // set hint moves

                            boardGraphics.SetHintMoves(pieceSelectedMoves);

                            // change state

                            boardUIState = BoardGraphicsState.Dragging;
                        }
                    }
                }
                break;
            case BoardGraphicsState.Dragging:
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

                        foreach (Move move in pieceSelectedMoves)
                        {
                            if (move.squareTargetIndex == squareIndex)
                            {
                                // select the move

                                moveSelected = move;
                                isMoveAnimated = false;

                                // reset board state

                                boardUIState = BoardGraphicsState.Idle;

                                // move selected

                                return true;
                            }
                        }
                    }

                    // disable selected square

                    boardGraphics.SelectSquare(-1);

                    // update graphics

                    boardGraphics.UpdateGraphics();

                    // go to piece selected state

                    boardUIState = BoardGraphicsState.PieceSelected;
                }
                break;
            case BoardGraphicsState.PieceSelected:
                // the first frame you click

                if (Input.IsActionJustPressed("Select"))
                {
                    if (isOnSquare)
                    {
                        // if the square is a valid move

                        foreach (Move move in pieceSelectedMoves)
                        {
                            if (move.squareTargetIndex == squareIndex)
                            {
                                // select the move

                                moveSelected = move;
                                isMoveAnimated = true;

                                // reset board state

                                boardUIState = BoardGraphicsState.Idle;

                                // move selected

                                return true;
                            }
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

                            // highlight square

                            boardGraphics.HightlightSquare(squareIndex);

                            // set hint moves

                            boardGraphics.SetHintMoves(pieceSelectedMoves);

                            // change state to holding the piece

                            boardUIState = BoardGraphicsState.Dragging;

                            // exit

                            return false;
                        }
                    }

                    // stop highlighting square

                    boardGraphics.HightlightSquare(-1);

                    // disable hint moves

                    boardGraphics.SetHintMoves(null);

                    // go back to idle state

                    boardUIState = BoardGraphicsState.Idle;
                }
                break;
        }

        // no move selected

        return false;
    }

    // update game state

    private void UpdateState()
    {
        switch (gameState)
        {
            case GameState.NextTurn:
                // emit signal

                EmitSignal(SignalName.OnGameTurn);

                // get turn color

                Piece.Color turnColor = board.GetTurnColor();

                // check if the game is over

                bool isGameOver = false;

                // check if the current player has any moves available

                List<Move> moves = MoveGeneration.GetAllLegalMovesByColor(board, turnColor);

                if (moves.Count == 0)
                {
                    if (MoveGeneration.IsKingInCheck(board, turnColor))
                    {
                        switch (turnColor)
                        {
                            case Piece.Color.White:
                                EmitSignal(SignalName.OnGameOver, (int)GameOverReason.WhiteMated);
                                break;
                            case Piece.Color.Black:
                                EmitSignal(SignalName.OnGameOver, (int)GameOverReason.BlackMated);
                                break;
                        }
                    } else
                    {
                        EmitSignal(SignalName.OnGameOver, (int)GameOverReason.Drowned);
                    }

                    isGameOver = true;
                }

                // 3 fold

                if (board.zobristPositionHistory[board.zobristPosition] >= 3)
                {
                    EmitSignal(SignalName.OnGameOver, (int)GameOverReason.Repetition);
                    isGameOver = true;
                }

                // if the games continues

                if (!isGameOver)
                {
                    // next player turn

                    switch (turnColor)
                    {
                        case Piece.Color.White:
                            gameState = GameState.PlayerWhiteTurn;
                            break;
                        case Piece.Color.Black:
                            gameState = GameState.PlayerBlackTurn;
                            break;
                    }
                }
                else
                {
                    gameState = GameState.Over;
                }
                break;
            case GameState.PlayerWhiteTurn:
            case GameState.PlayerBlackTurn:
                // player white

                bool isMoveSelected = PlayerHumanUpdateState();

                if (isMoveSelected)
                {
                    gameState = GameState.MakeMove;
                }
                break;
            case GameState.MakeMove:
                // make the move & calculate zobrist

                board.MakeMove(moveSelected);

                // disable highlight square

                boardGraphics.HightlightSquare(-1);

                // disable square selection

                boardGraphics.SelectSquare(-1);

                // disable hint moves

                boardGraphics.SetHintMoves(null);

                // animate the move

                boardGraphics.AnimateMove(moveSelected, isMoveAnimated, Callable.From(() =>
                {
                    boardGraphics.UpdateGraphics();
                    gameState = GameState.NextTurn;
                }));

                // change state

                gameState = GameState.WaitUntilCompleted;
                break;
            case GameState.WaitUntilCompleted:
                break;
            case GameState.Over:
                break;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
	{
        UpdateState();
    }
}
