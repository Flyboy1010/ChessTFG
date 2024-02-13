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

    // letters of the chess board

    private static readonly char[] boardLetters = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };

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

    // convert a move to a string

    public static string FromMoveToString(Move move)
    {
        // get squares

        int squareSourceI = move.squareSourceIndex % 8;
        int squareSourceJ = move.squareSourceIndex / 8;
        int squareTargetI = move.squareTargetIndex % 8;
        int squareTargetJ = move.squareTargetIndex / 8;

        // str move

        string strMove = string.Format("{0}{1}{2}{3}", boardLetters[squareSourceI], 8 - squareSourceJ, boardLetters[squareTargetI], 8 - squareTargetJ);

        // if needs to add the promotion piece

        if (move.flags == Move.Flags.Promotion)
        {
            strMove += pieceTypeToCharMap[move.promotionPieceType];
        }

        // return the move

        return strMove;
    }

    // Method to convert ulong to binary string

    public static string ConvertULongToBinaryString(ulong number)
    {
        const int bits = sizeof(ulong) * 8;
        char[] result = new char[bits];

        for (int i = 0; i < bits; i++)
        {
            result[bits - 1 - i] = ((number >> i) & 1) == 1 ? '1' : '0';
        }

        return new string(result);
    }
}
