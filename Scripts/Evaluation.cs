using Godot;
using System;

public static class Evaluation
{
	// pieces values

	private const int pawnValue = 100;
	private const int knightValue = 300;
	private const int bishopValue = 320;
	private const int rookValue = 500;
	private const int queenValue = 900;

	public static int GetPieceValue(Piece.Type type)
	{
		switch (type)
		{
			case Piece.Type.Pawn:
				return pawnValue;
			case Piece.Type.Knight:
				return knightValue;
			case Piece.Type.Bishop:
				return bishopValue;
			case Piece.Type.Rook:
				return rookValue;
			case Piece.Type.Queen:
				return queenValue;
		}

		return 0;
	}

	// evaluate board

	public static int EvaluateBoard(Board board, Piece.Color colorPerspective)
	{
		int materialWhite = CountMaterial(board, Piece.Color.White);
		int materialBlack = CountMaterial(board, Piece.Color.Black);

		return (materialWhite - materialBlack) * (colorPerspective == Piece.Color.White ? 1 : -1);
	}

	// count material

	private static int CountMaterial(Board board, Piece.Color color)
	{
        int count = 0;

        foreach (int index in board.GetPiecesIndices(color))
        {
            Piece piece = board.GetPiece(index);

			count += GetPieceValue(piece.type) + PieceTables.Read(PieceTables.GetTable(piece.type), index, color);
        }

        return count;
    }
}
