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

    public Type type;
    public Color color;
}
