using Godot;
using System;
using System.Collections.Generic;

public class PlayerHuman : Player
{
    private enum InputState
    {
        Idle,
        Dragging,
        PieceSelected
    }

    // player state

    private InputState inputState = InputState.Idle;

    // board

    private Board board;

    // board graphics

    private BoardGraphics boardGraphics;

    // piece selection helpers

    private int pieceSelectedIndex = -1; // -1 means nothing selected
    private List<Move> pieceSelectedMoves = null;

    // ctor

    public PlayerHuman(Board board, BoardGraphics boardGraphics)
    {
        // init

        this.board = board;
        this.boardGraphics = boardGraphics;
    }

    // when notified

    public override void NotifyTurnToMove()
    {

    }

    // on update

    public override void Update()
    {
        // handle everything related to move selection

        // get the mouse coordinates

        Vector2 mouse = boardGraphics.GetLocalMousePosition();

        // get the square the mouse is on

        bool isOnSquare = boardGraphics.TryGetSquareIndexFromCoords(mouse, out int squareIndex);

        // state machine

        switch (inputState)
        {
            case InputState.Idle:
                HandlePieceSelection(squareIndex, isOnSquare);
                break;
            case InputState.Dragging:
                HandleDragMovement(mouse, squareIndex, isOnSquare);
                break;
            case InputState.PieceSelected:
                HandleClickMovement(squareIndex, isOnSquare);
                break;
        }
    }

    private void HandlePieceSelection(int squareIndex, bool isOnSquare)
    {
        // the first frame you click

        if (Input.IsActionJustPressed("Select"))
        {
            if (isOnSquare)
            {
                // get the piece

                Piece piece = board.GetPiece(squareIndex);

                // if not none then select it

                if (piece.type != Piece.Type.None && piece.color == board.GetTurnColor())
                {
                    // select piece

                    pieceSelectedIndex = squareIndex;
                    pieceSelectedMoves = MoveGeneration.GetLegalMoves(board, squareIndex);

                    // highlight square

                    boardGraphics.HightlightSquare(squareIndex);

                    // set hint moves

                    boardGraphics.SetHintMoves(pieceSelectedMoves);

                    // change state

                    inputState = InputState.Dragging;
                }
            }
        }
    }

    private void HandleDragMovement(Vector2 mouse, int squareIndex, bool isOnSquare)
    {
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
                        // chose the move (no animation)

                        ChoseMove(move, false);

                        // reset board state

                        inputState = InputState.Idle;

                        // move selected

                        return;
                    }
                }
            }

            // disable selected square

            boardGraphics.SelectSquare(-1);

            // update graphics

            boardGraphics.UpdateGraphics();

            // go to piece selected state

            inputState = InputState.PieceSelected;
        }
    }

    private void HandleClickMovement(int squareIndex, bool isOnSquare)
    {
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

                        ChoseMove(move, true);

                        // reset board state

                        inputState = InputState.Idle;

                        // move selected

                        return;
                    }
                }

                // if the move is not legal then check if another piece is selected

                // get the piece

                Piece piece = board.GetPiece(squareIndex);

                // if not none then select it

                if (piece.type != Piece.Type.None && piece.color == board.GetTurnColor())
                {
                    // select piece

                    pieceSelectedIndex = squareIndex;
                    pieceSelectedMoves = MoveGeneration.GetLegalMoves(board, squareIndex);

                    // highlight square

                    boardGraphics.HightlightSquare(squareIndex);

                    // set hint moves

                    boardGraphics.SetHintMoves(pieceSelectedMoves);

                    // change state to holding the piece

                    inputState = InputState.Dragging;

                    // exit

                    return;
                }
            }

            // stop highlighting square

            boardGraphics.HightlightSquare(-1);

            // disable hint moves

            boardGraphics.SetHintMoves(null);

            // go back to idle state

            inputState = InputState.Idle;
        }
    }
}
