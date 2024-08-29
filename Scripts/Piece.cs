using Godot;
using System;

// piece struct containing the piece type and color

public struct Piece
{
	public enum Type
	{
		None,
		King,
		Queen,
		Bishop,
		Knight,
		Rook,
		Pawn
	}

	public enum Color
	{
		None,
		White,
		Black
	}

	public static readonly Piece NullPiece = new Piece() 
	{ 
		type = Type.None,
		color = Color.None 
	};

	public Type type;
	public Color color;

	// return the opposite color

	public static Color GetOppositeColor(Color color)
	{
		switch (color)
		{
			case Color.White: return Color.Black;
			case Color.Black: return Color.White;
			default: return Color.None;
		}
	}
}
