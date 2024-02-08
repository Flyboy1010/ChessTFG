using Godot;
using System;
using System.Collections.Generic;

public class Board
{
    // towers squares & king squares

    public static readonly int A1 = 56, H1 = 63, A8 = 0, H8 = 7; // tower squares
    public static readonly int E1 = 60, E8 = 4; // king squares
    public static readonly int C1 = 58, C8 = 2, G1 = 62, G8 = 6;
    public static readonly int F1 = 61, D1 = 59, F8 = 5, D8 = 3; // tower castling target squares

    // start fen string

    public static readonly string StartFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";

    // promotion piece

    public Piece.Type PromotionPieceType = Piece.Type.Queen;

    // array with the pieces

    private Piece[] pieces = new Piece[64];

    // list of board states & moves done

    private Stack<BoardState> boardStates = new Stack<BoardState>();
    private Stack<Move> moves = new Stack<Move>();

    // current state

    private BoardState currentBoardState = new BoardState();

    // ctor

    public Board()
	{
           
	}

    // get the piece at index location

    public Piece GetPiece(int index)
    {
        return pieces[index];
    }

    // get turn color

    public Piece.Color GetTurnColor()
    {
        return currentBoardState.GetTurnColor();
    }

    // get current board state

    public ref readonly BoardState GetBoardState()
    {
        return ref currentBoardState;
    }

    // load fen string

    public void LoadFenString(string fen)
    {
        // clear

        boardStates.Clear();
        moves.Clear();

        // split fen string

        string[] subFEN = fen.Split(' ');

        // pieces placement (subFEN[0])

        int i = 0, j = 0;

        foreach (char symbol in subFEN[0])
        {
            if (symbol == '/')
            {
                i = 0;
                j++;
            }
            else if (char.IsDigit(symbol))
            {
                int n = symbol - '0'; // number of empy squares

                for (int ii = 0; ii < n; ii++)
                {
                    pieces[(i + ii) + j * 8] = new Piece(); // "none" piece
                }

                i += n;
            }
            else
            {
                int index = i + j * 8;
                Piece.Type pieceType = Utils.CharToPieceType(symbol); // must be a piece symbol then
                Piece.Color pieceColor = char.IsUpper(symbol) ? Piece.Color.White : Piece.Color.Black;
                pieces[index] = new Piece { type = pieceType, color = pieceColor };
                i++;
            }
        }

        // turn color (subFEN[1])

        Piece.Color turnColor = subFEN[1].Equals("w") ? Piece.Color.White : Piece.Color.Black;
        currentBoardState.SetTurnColor(turnColor);

        // castling rights (subFEN[2])

        currentBoardState.SetCastleShort(Piece.Color.White, false);
        currentBoardState.SetCastleLong(Piece.Color.White, false);
        currentBoardState.SetCastleShort(Piece.Color.Black, false);
        currentBoardState.SetCastleLong(Piece.Color.Black, false);

        if (!subFEN[2].Equals("-"))
        { 
            foreach (char symbol in subFEN[2])
            {
                switch (symbol)
                {
                    case 'K':
                        currentBoardState.SetCastleShort(Piece.Color.White, true);
                        break;
                    case 'Q':
                        currentBoardState.SetCastleLong(Piece.Color.White, true);
                        break;
                    case 'k':
                        currentBoardState.SetCastleShort(Piece.Color.Black, true);
                        break;
                    case 'q':
                        currentBoardState.SetCastleLong(Piece.Color.Black, true);
                        break;
                }
            }
        }

        // en passant (subFEN[3])

        if (subFEN[3].Equals("-"))
        {
            currentBoardState.SetEnPassant(false);
            currentBoardState.SetEnPassantColor(Piece.Color.None);
            currentBoardState.SetEnPassantSquareIndex(-1);
        }
        else
        {
            // en passant is available

            currentBoardState.SetEnPassant(true);

            // the format is then the char of the column & the number of the row

            int column = subFEN[3][0] - 'a';
            int row = 0;

            switch (subFEN[3][1])
            {
                case '3':
                    row = 4;
                    currentBoardState.SetEnPassantColor(Piece.Color.White);
                    break;
                case '6':
                    row = 3;
                    currentBoardState.SetEnPassantColor(Piece.Color.Black);
                    break;
            }

            currentBoardState.SetEnPassantSquareIndex(column + row * 8);
        }
    }

    // make move

    public void MakeMove(Move move)
    {
        // save current board state

        boardStates.Push(currentBoardState);

        // clear the piece at the source square

        pieces[move.squareSourceIndex] = new Piece(); // remove piece at the "Source" square

        // check move flags

        switch (move.flags)
        {
            case Move.Flags.DoublePush:
                // enable en passant

                currentBoardState.SetEnPassant(true);
                currentBoardState.SetEnPassantColor(move.pieceSource.color);
                currentBoardState.SetEnPassantSquareIndex(move.squareTargetIndex);

                // move the piece

                pieces[move.squareTargetIndex] = move.pieceSource;
                break;
            case Move.Flags.Promotion:
                // disable en passant

                currentBoardState.SetEnPassant(false);

                pieces[move.squareTargetIndex] = new Piece { type = move.promotionPieceType, color = move.pieceSource.color };
                break;
            case Move.Flags.EnPassant:
                // do en passant capture

                pieces[currentBoardState.GetEnPassantSquareIndex()] = new Piece();
                pieces[move.squareTargetIndex] = move.pieceSource;

                // disable en passant

                currentBoardState.SetEnPassant(false);

                break;
            default:
                // disable en passant

                currentBoardState.SetEnPassant(false);

                // move the piece to the target

                pieces[move.squareTargetIndex] = move.pieceSource;
                break;
        }

        // push the move onto the stack

        moves.Push(move);

        // change turn color

        Piece.Color turnColor = currentBoardState.GetTurnColor();
        currentBoardState.SetTurnColor(turnColor == Piece.Color.White ? Piece.Color.Black : Piece.Color.White);
    }

    // undoes the last move

    public void UndoMove()
    {

    }
}
