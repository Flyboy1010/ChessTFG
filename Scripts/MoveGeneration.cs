using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

public static class MoveGeneration
{
    private enum Direction
    {
        Up, Left, Down, Right, D1, D2, D3, D4
    }

    /* 
     * up left down, right, d1, d2, d3, d4
     * 
     * +----+----+----+
     * | D2 | UP | D1 |
     * +----+----+----+
     * | LE | -- | RI |
     * +----+----+----+
     * | D3 | DW | D4 |
     * +----+----+----+
     * 
     */

    private static readonly int[] directionOffsets = { -8, -1, 8, 1, -7, -9, 7, 9 };

    private static readonly int[][] preCalculatedSquaresToEdge = new int[64][];
    private static readonly int[][] preCalculatedKnightMoves = new int[64][];
    private static readonly int[][] preCalculatedKingMoves = new int[64][];
    private static readonly int[][][] preCalculatedPawnCapturesMoves = new int[2][][];

    // static ctor

    static MoveGeneration()
    {
        // Precalculate the moves

        PrecalculateMoves();
    }

    // check if a square is in bounds

    private static bool IsInBounds(int i, int j)
    {
        return i >= 0 && i < 8 && j >= 0 && j < 8;
    }

    // precalculate moves

    public static void PrecalculateMoves()
    {
        // init arrays pawn captures

        preCalculatedPawnCapturesMoves[0] = new int[64][]; // white
        preCalculatedPawnCapturesMoves[1] = new int[64][]; // black

        // helper buffer

        List<int> movesBuffer = new List<int>();

        for (int j = 0; j < 8; j++)
        {
            for (int i = 0; i < 8; i++)
            {
                // index

                int index = i + j * 8;

                // squares to edge

                int up = j;
                int down = 7 - j;
                int left = i;
                int right = 7 - i;

                int d1 = Math.Min(up, right);
                int d2 = Math.Min(up, left);
                int d3 = Math.Min(down, left);
                int d4 = Math.Min(down, right);

                preCalculatedSquaresToEdge[i + j * 8] = new int[8] { up, left, down, right, d1, d2, d3, d4 };

                // knight moves

                movesBuffer.Clear();

                for (int jj = -2; jj <= 2; jj += 4)
                {
                    for (int ii = -1; ii <= 1; ii += 2)
                    {
                        if (IsInBounds(i + ii, j + jj))
                        {
                            movesBuffer.Add((i + ii) + (j + jj) * 8);
                        }

                        if (IsInBounds(i + jj, j + ii))
                        {
                            movesBuffer.Add((i + jj) + (j + ii) * 8);
                        }
                    }
                }

                preCalculatedKnightMoves[index] = movesBuffer.ToArray();

                // king moves

                movesBuffer.Clear();

                for (int jj = -1; jj <= 1; jj++)
                {
                    for (int ii = -1; ii <= 1; ii++)
                    {
                        if (!(ii == 0 && jj == 0) && IsInBounds(i + ii, j + jj))
                        {
                            movesBuffer.Add((i + ii) + (j + jj) * 8);
                        }
                    }
                }

                preCalculatedKingMoves[index] = movesBuffer.ToArray();

                /* PAWN CAPTURES */

                // white pawns

                movesBuffer.Clear();

                if (j > 0)
                {
                    if (i > 0)
                    {
                        movesBuffer.Add(index + directionOffsets[(int)Direction.D2]);
                    }

                    if (i < 7)
                    {
                        movesBuffer.Add(index + directionOffsets[(int)Direction.D1]);
                    }
                }

                preCalculatedPawnCapturesMoves[0][index] = movesBuffer.ToArray();

                // black pawns

                movesBuffer.Clear();

                if (j < 7)
                {
                    if (i > 0)
                    {
                        movesBuffer.Add(index + directionOffsets[(int)Direction.D3]);
                    }

                    if (i < 7)
                    {
                        movesBuffer.Add(index + directionOffsets[(int)Direction.D4]);
                    }
                }

                preCalculatedPawnCapturesMoves[1][index] = movesBuffer.ToArray();
            }
        }
    }

    // generate knight moves

    private static List<Move> GenerateKnightMoves(Board board, int index)
    {
        List<Move> moves = new List<Move>();

        Piece piece = board.GetPiece(index);

        foreach (int targetIndex in preCalculatedKnightMoves[index])
        {
            Piece targetPiece = board.GetPiece(targetIndex);

            // if the square is empty or the pieces color are diferent then add the move to the list

            if (targetPiece.type == Piece.Type.None || targetPiece.color != piece.color)
            {
                Move move = new Move();
                move.squareSourceIndex = index;
                move.squareTargetIndex = targetIndex;
                move.pieceSource = piece;
                move.pieceTarget = targetPiece;

                moves.Add(move);
            }
        }

        return moves;
    }

    // generate sliding moves (for bishop rook & queen)

    private static List<Move> GenerateSlidingMoves(Board board, int index)
    {
        List<Move> moves = new List<Move>();

        Piece piece = board.GetPiece(index);

        int startDirection = (piece.type != Piece.Type.Bishop) ? 0 : 4;
        int endDirection = (piece.type != Piece.Type.Rook) ? 8 : 4;

        for (int d = startDirection; d < endDirection; d++)
        {
            int n = preCalculatedSquaresToEdge[index][d];

            for (int i = 0; i < n; i++)
            {
                int targetIndex = index + directionOffsets[d] * (i + 1);

                Piece targetPiece = board.GetPiece(targetIndex);

                // construct move

                Move move = new Move();
                move.squareSourceIndex = index;
                move.squareTargetIndex = targetIndex;
                move.pieceSource = piece;
                move.pieceTarget = targetPiece;

                // check pieces in the path

                if (targetPiece.type == Piece.Type.None)
                {
                    moves.Add(move);
                }
                else
                {
                    if (targetPiece.color != piece.color)
                    {
                        moves.Add(move);
                    }

                    break;
                }
            }
        }

        return moves;
    }

    // generate pseudo legal moves

    public static List<Move> GetPseudoLegalMoves(Board board, int index)
    {
        Piece piece = board.GetPiece(index);

        switch (piece.type)
        {
            case Piece.Type.Knight:
                return GenerateKnightMoves(board, index);
            case Piece.Type.Bishop:
            case Piece.Type.Queen:
            case Piece.Type.Rook:
                return GenerateSlidingMoves(board, index);
        }

        return new List<Move>();
    }
}
