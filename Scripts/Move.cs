using Godot;
using System;

public struct Move
{
	// move flags

	public enum Flags
	{
		None,
		DoublePush,
		Promotion,
		EnPassant,
		CastleShort,
		CastleLong
	}

	// null move

	public static readonly Move NullMove = new Move()
	{
		squareSourceIndex = -1,
		squareTargetIndex = -1,
		pieceSource = Piece.NullPiece,
		pieceTarget = Piece.NullPiece,
		flags = Flags.None,
		promotionPieceType = Piece.Type.None
	};

	public int squareSourceIndex, squareTargetIndex;
	public Piece pieceSource, pieceTarget;
	public Flags flags;

	public Piece.Type promotionPieceType;

	/* 
	 * Serializes a move into an integer
	 * 
	 * FORMAT BITS =>  FFF|RRR|cc|ppp|CC|PPP|TTTTTT|SSSSSS
	 * SSSSSS => sourceSquareIndex bits
	 * TTTTTT => targetSquareIndex bits
	 * PPP => source piece type bits
	 * CC => source piece color bits
	 * ppp => target piece type bits
	 * cc => target piece color bits
	 * RRR => promotion piece bits
	 * FFF => flags bits
	 * 
	 */

	public int Serialize()
	{
		int result = squareSourceIndex
			| (squareTargetIndex << 6)
			| ((int)pieceSource.type << 12)
			| ((int)pieceSource.color << 15)
			| ((int)pieceTarget.type << 17)
			| ((int)pieceTarget.color << 20)
			| ((int)promotionPieceType << 22)
			| ((int)flags << 25);

		return result;
	}

	// check if two moves are the same

	public bool IsEqual(Move move)
	{
		int s1 = Serialize();
		int s2 = move.Serialize();

		return s1 == s2;
	}
}
