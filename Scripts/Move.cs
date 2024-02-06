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

    public int squareSourceIndex, squareTargetIndex;
    public Piece pieceSource, pieceTarget;
    public Flags flags;

    public Piece.Type promotionPieceType;
}
