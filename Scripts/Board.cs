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
    public static readonly int B1 = 57, B8 = 1;

    // start fen string

    public static readonly string StartFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";

    // promotion piece

    public Piece.Type PromotionPieceType = Piece.Type.Queen;

    // array with the pieces

    private Piece[] pieces = new Piece[64];
    private List<int> piecesIndicesWhite = new List<int>();
    private List<int> piecesIndicesBlack = new List<int>();

    // list of board states & moves done

    private Stack<BoardState> boardStates = new Stack<BoardState>();
    private Stack<Move> moves = new Stack<Move>();

    // hash map with all the zobrist positions and the times it occured

    public Dictionary<ulong, int> zobristPositionHistory = new Dictionary<ulong, int>();
    public ulong zobristPosition;

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

    // set piece

    public void SetPiece(int index, Piece piece)
    {
        // handle previous piece

        Piece piecePrevious = pieces[index];

        if (piecePrevious.type != Piece.Type.None)
        {
            switch (piecePrevious.color)
            {
                case Piece.Color.White:
                    piecesIndicesWhite.Remove(index);
                    break;
                case Piece.Color.Black:
                    piecesIndicesBlack.Remove(index);
                    break;
            }
        }

        // handle new piece

        if (piece.type != Piece.Type.None)
        {
            switch (piece.color)
            {
                case Piece.Color.White:
                    piecesIndicesWhite.Add(index);
                    break;
                case Piece.Color.Black:
                    piecesIndicesBlack.Add(index);
                    break;
            }
        }

        // set the piece

        pieces[index] = piece;
    }

    // get pieces indices by color

    public List<int> GetPiecesIndices(Piece.Color color)
    {
        switch (color)
        {
            case Piece.Color.White: return piecesIndicesWhite;
            case Piece.Color.Black: return piecesIndicesBlack;
        }

        return new List<int>();
    }

    // get last move

    public bool TryGetLastMove(out Move move)
    {
        return moves.TryPeek(out move);
    }

    // get current board state

    public ref readonly BoardState GetBoardState()
    {
        return ref currentBoardState;
    }

    // get turn color

    public Piece.Color GetTurnColor()
    {
        return currentBoardState.GetTurnColor();
    }

    // find king 

    public int FindKing(Piece.Color color)
    {
        List<int> indices = GetPiecesIndices(color);

        foreach (int index in indices)
        {
            if (pieces[index].type == Piece.Type.King)
            {
                return index;
            }
        }

        GD.PrintErr("there is no king?");

        return 0;
    }

    // load fen string

    public void LoadFenString(string fen)
    {
        // clear

        boardStates.Clear();
        moves.Clear();
        zobristPositionHistory.Clear();

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
                    SetPiece((i + ii) + j * 8, Piece.NullPiece);
                }

                i += n;
            }
            else
            {
                int index = i + j * 8;
                Piece.Type pieceType = Utils.CharToPieceType(symbol); // must be a piece symbol then
                Piece.Color pieceColor = char.IsUpper(symbol) ? Piece.Color.White : Piece.Color.Black;
                SetPiece(index, new Piece()
                {
                    type = pieceType,
                    color = pieceColor
                });
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

        // zobrist position

        zobristPosition = ZobristHashing.GetKey(this);
        zobristPositionHistory[zobristPosition] = 1;
    }

    // make move

    public void MakeMove(Move move, bool isTesting = false)
    {
        // save current board state

        boardStates.Push(currentBoardState);

        // check move flags

        switch (move.flags)
        {
            case Move.Flags.DoublePush:
                // enable en passant

                currentBoardState.SetEnPassant(true);
                currentBoardState.SetEnPassantColor(move.pieceSource.color);
                currentBoardState.SetEnPassantSquareIndex(move.squareTargetIndex);

                // move the piece

                SetPiece(move.squareSourceIndex, Piece.NullPiece);
                SetPiece(move.squareTargetIndex, move.pieceSource);
                break;
            case Move.Flags.Promotion:
                // disable en passant

                currentBoardState.SetEnPassant(false);

                SetPiece(move.squareSourceIndex, Piece.NullPiece);
                SetPiece(move.squareTargetIndex, new Piece()
                {
                    type = move.promotionPieceType,
                    color = move.pieceSource.color
                });
                break;
            case Move.Flags.EnPassant:
                // do en passant capture

                SetPiece(currentBoardState.GetEnPassantSquareIndex(), Piece.NullPiece);
                SetPiece(move.squareSourceIndex, Piece.NullPiece);
                SetPiece(move.squareTargetIndex, move.pieceSource);

                // disable en passant

                currentBoardState.SetEnPassant(false);

                break;
            case Move.Flags.CastleShort:
                // disable en passant

                currentBoardState.SetEnPassant(false);

                // move the king

                SetPiece(move.squareSourceIndex, Piece.NullPiece);
                SetPiece(move.squareTargetIndex, move.pieceSource);

                // move the tower

                switch (move.pieceSource.color)
                {
                    case Piece.Color.White:
                        SetPiece(F1, pieces[H1]);
                        SetPiece(H1, Piece.NullPiece);
                        break;
                    case Piece.Color.Black:
                        SetPiece(F8, pieces[H8]);
                        SetPiece(H8, Piece.NullPiece);
                        break;
                }
                break;
            case Move.Flags.CastleLong:
                // disable en passant

                currentBoardState.SetEnPassant(false);

                // move the king

                SetPiece(move.squareSourceIndex, Piece.NullPiece);
                SetPiece(move.squareTargetIndex, move.pieceSource);

                switch (move.pieceSource.color)
                {
                    case Piece.Color.White:
                        SetPiece(D1, pieces[A1]);
                        SetPiece(A1, Piece.NullPiece);
                        break;
                    case Piece.Color.Black:
                        SetPiece(D8, pieces[A8]);
                        SetPiece(A8, Piece.NullPiece);
                        break;
                }
                break;
            default:
                // disable en passant

                currentBoardState.SetEnPassant(false);

                // move the piece to the target

                SetPiece(move.squareSourceIndex, Piece.NullPiece);
                SetPiece(move.squareTargetIndex, move.pieceSource);
                break;
        }

        // check if the king or the towers moved

        if (move.squareSourceIndex == E1 || move.squareTargetIndex == E1) // white king
        {
            currentBoardState.SetCastle(Piece.Color.White, false);
        }
        else if (move.squareSourceIndex == E8 || move.squareTargetIndex == E8) // black king
        {
            currentBoardState.SetCastle(Piece.Color.Black, false);
        }

        if (move.squareSourceIndex == A1 || move.squareTargetIndex == A1) // white queen side tower
        {
            currentBoardState.SetCastleLong(Piece.Color.White, false);
        }
        else if (move.squareSourceIndex == H1 || move.squareTargetIndex == H1) // white king side tower
        {
            currentBoardState.SetCastleShort(Piece.Color.White, false);
        }

        if (move.squareSourceIndex == A8 || move.squareTargetIndex == A8) // black queen side tower
        {
            currentBoardState.SetCastleLong(Piece.Color.Black, false);
        }
        else if (move.squareSourceIndex == H8 || move.squareTargetIndex == H8) // black king side tower
        {
            currentBoardState.SetCastleShort(Piece.Color.Black, false);
        }

        // push the move onto the stack

        moves.Push(move);

        // change turn color

        Piece.Color turnColor = currentBoardState.GetTurnColor();
        currentBoardState.SetTurnColor(Piece.GetOppositeColor(turnColor));

        // zobrist hashing

        if (!isTesting)
        {
            zobristPosition = ZobristHashing.GetKey(this);

            if (zobristPositionHistory.TryGetValue(zobristPosition, out int count))
            {
                zobristPositionHistory[zobristPosition] = count + 1;
            }
            else
            {
                zobristPositionHistory[zobristPosition] = 1;
            }
        }
    }

    // undoes the last move

    public void UndoMove()
    {
        if (boardStates.Count > 0)
        {
            // get back to the last state

            currentBoardState = boardStates.Pop();

            // get the last move and undo it

            Move move = moves.Pop();

            // undo move

            SetPiece(move.squareSourceIndex, move.pieceSource);
            SetPiece(move.squareTargetIndex, move.pieceTarget);

            switch (move.flags)
            {
                case Move.Flags.CastleShort:
                    switch (move.pieceSource.color)
                    {
                        case Piece.Color.White:
                            SetPiece(F1, Piece.NullPiece);
                            SetPiece(H1, new Piece()
                            {
                                type = Piece.Type.Rook,
                                color = Piece.Color.White
                            });
                            break;
                        case Piece.Color.Black:
                            SetPiece(F8, Piece.NullPiece);
                            SetPiece(H8, new Piece()
                            {
                                type = Piece.Type.Rook,
                                color = Piece.Color.Black
                            });
                            break;
                    }
                    break;
                case Move.Flags.CastleLong:
                    switch (move.pieceSource.color)
                    {
                        case Piece.Color.White:
                            SetPiece(D1, Piece.NullPiece);
                            SetPiece(A1, new Piece()
                            {
                                type = Piece.Type.Rook,
                                color = Piece.Color.White
                            });
                            break;
                        case Piece.Color.Black:
                            SetPiece(D8, Piece.NullPiece);
                            SetPiece(A8, new Piece()
                            {
                                type = Piece.Type.Rook,
                                color = Piece.Color.Black
                            });
                            break;
                    }
                    break;
                case Move.Flags.EnPassant:
                    SetPiece(currentBoardState.GetEnPassantSquareIndex(), new Piece()
                    {
                        type = Piece.Type.Pawn,
                        color = currentBoardState.GetEnPassantColor()
                    });
                    break;
            }
        }
    }
}
