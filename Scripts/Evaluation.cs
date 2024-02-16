using Godot;
using System;

public static class Evaluation
{
    // pieces values

    private const int pawnValue = 100;
    private const int knightValue = 300;
    private const int bishopValue = 300;
    private const int rookValue = 500;
    private const int queenValue = 900;

    // evaluate board

    public static int EvaluateBoard(Board board)
    {
        int materialWhite = CountMaterial(board, Piece.Color.White);
        int materialBlack = CountMaterial(board, Piece.Color.Black);

        return materialWhite - materialBlack;
    }

    // count material

    private static int CountMaterial(Board board, Piece.Color color)
    {
        int count = 0;

        foreach (int index in board.GetPiecesIndices(color))
        {
            int i = color == Piece.Color.White ? index : 63 - index;

            Piece piece = board.GetPiece(index);

            switch (piece.type)
            {
                case Piece.Type.Pawn:
                    count += pawnValue + PieceSquareTables.PawnTable[i];
                    break;
                case Piece.Type.Knight:
                    count += knightValue + PieceSquareTables.KnightTable[i];
                    break;
                case Piece.Type.Bishop:
                    count += bishopValue + PieceSquareTables.BishopTable[i];
                    break;
                case Piece.Type.Rook:
                    count += rookValue + PieceSquareTables.RookTable[i];
                    break;
                case Piece.Type.Queen:
                    count += queenValue + PieceSquareTables.QueenTable[i];
                    break;
            }
        }

        return count;
    }
}
