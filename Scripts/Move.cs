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
}
