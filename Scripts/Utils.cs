using Godot;
using System;
using System.Collections.Generic;

public static class Utils
{
    // mapping of the chars to each piece type

    private static readonly Dictionary<char, Piece.Type> charToPieceTypeMap = new Dictionary<char, Piece.Type>()
    {
        { 'p', Piece.Type.Pawn }, { 'n', Piece.Type.Knight }, { 'b', Piece.Type.Bishop },
        { 'r', Piece.Type.Rook }, { 'q', Piece.Type.Queen  }, { 'k', Piece.Type.King   }
    };

    // mapping of the piece type to each char

    private static readonly Dictionary<Piece.Type, char> pieceTypeToCharMap = new Dictionary<Piece.Type, char>()
    {
        { Piece.Type.Pawn, 'p' }, { Piece.Type.Knight, 'n' }, { Piece.Type.Bishop, 'b' },
        { Piece.Type.Rook, 'r' }, { Piece.Type.Queen , 'q' }, { Piece.Type.King  , 'k' }
    };

    // char to piece type

    public static Piece.Type CharToPieceType(char pieceChar)
    {
        return charToPieceTypeMap[char.ToLower(pieceChar)];
    }

    // piece type to char
    
    public static char PieceTypeToChar(Piece.Type pieceType)
    {
        return pieceTypeToCharMap[pieceType];
    }
}
