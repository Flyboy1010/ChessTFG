using Godot;
using System;

public struct BoardState
{
	// turn color 

	private Piece.Color turnColor = Piece.Color.None;

	/* EN PASSANT */

	// en passant flag

	private bool isEnPassantAvailable = false;

	// color of pawn that doublepushed for check if enPassant is available

	private Piece.Color doublePushedPawnColor = Piece.Color.None;

	// pawn tile that can be captured by en-passant

	private int enPassantSquareIndex = -1;

	/* CASTLELING */

	// castleling flags for white

	private bool canCastleShortWhite = false;
	private bool canCastleLongWhite = false;

	// castleling flags for black

	private bool canCastleShortBlack = false;
	private bool canCastleLongBlack = false;

	// half move count

	private int halfMoveCount = 0;

	// ctor

	public BoardState()
	{
	}

	// set turn color

	public void SetTurnColor(Piece.Color color)
	{
		turnColor = color;
	}

	// get turn color

	public Piece.Color GetTurnColor()
	{
		return turnColor;
	}

	// en passant

	public void SetEnPassant(bool enabled)
	{
		isEnPassantAvailable = enabled;
	}

	public bool IsEnPassantAvailable()
	{
		return isEnPassantAvailable;
	}

	public void SetEnPassantSquareIndex(int index)
	{
		enPassantSquareIndex = index;
	}

	public int GetEnPassantSquareIndex()
	{
		return enPassantSquareIndex;
	}

	public void SetEnPassantColor(Piece.Color color)
	{
		doublePushedPawnColor = color;
	}

	public Piece.Color GetEnPassantColor()
	{
		return doublePushedPawnColor;
	}

	// fifty move count

	public void SetHalfMoveCount(int count)
	{
		halfMoveCount = count;
	}

	public int GetHalfMoveCount()
	{
		return halfMoveCount;
	}

	// get if you can castle

	public bool CanCastleShort(Piece.Color color)
	{
		switch (color)
		{
			case Piece.Color.White: return canCastleShortWhite;
			case Piece.Color.Black: return canCastleShortBlack;
			default: return false;
		}
	}

	public bool CanCastleLong(Piece.Color color)
	{
		switch (color)
		{
			case Piece.Color.White: return canCastleLongWhite;
			case Piece.Color.Black: return canCastleLongBlack;
			default: return false;
		}
	}

	public bool CanCastle(Piece.Color color)
	{
		switch (color)
		{
			case Piece.Color.White: return canCastleShortWhite || canCastleLongWhite;
			case Piece.Color.Black: return canCastleShortBlack || canCastleLongBlack;
			default: return false;
		}
	}

	// set castleling flags

	public void SetCastleShort(Piece.Color color, bool enabled)
	{
		switch (color)
		{
			case Piece.Color.White:
				canCastleShortWhite = enabled;
				break;
			case Piece.Color.Black:
				canCastleShortBlack = enabled;
				break;
		}
	}

	public void SetCastleLong(Piece.Color color, bool enabled)
	{
		switch (color)
		{
			case Piece.Color.White:
				canCastleLongWhite = enabled;
				break;
			case Piece.Color.Black:
				canCastleLongBlack = enabled;
				break;
		}
	}

	public void SetCastle(Piece.Color color, bool enabled)
	{
		switch (color)
		{
			case Piece.Color.White:
				canCastleShortWhite = enabled;
				canCastleLongWhite = enabled;
				break;
			case Piece.Color.Black:
				canCastleShortBlack = enabled;
				canCastleLongBlack = enabled;
				break;
		}
	}
}
