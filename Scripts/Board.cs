using Godot;
using System;
using System.Collections.Generic;

public class Board
{
    // start fen string

    public static readonly string StartFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -";

    // array with the pieces

    private Piece[] pieces = new Piece[64];

    // ctor

    public Board()
	{
        // testing

        for (int i = 0; i < 64; i++)
        {
            pieces[i] = new Piece()
            {
                type = Piece.Type.None
            };
        }

        pieces[0] = new Piece()
        {
            type = Piece.Type.Knight,
            color = Piece.Color.Black
        };
	}

    // get the piece at index location

    public Piece GetPiece(int index)
    {
        return pieces[index];
    }

    // load fen string

    public void LoadFenString(string fen)
    {
        // split fen string

        string[] subFEN = fen.Split(' ');

        // pieces placement (subFEN[0])

        int i = 0, j = 0;

        foreach (char symbol in subFEN[0])
        {
            if (symbol == '/')
            {
                i = 0;
                j++;
            }
            else if (char.IsDigit(symbol))
            {
                int n = symbol - '0'; // number of empy squares

                for (int ii = 0; ii < n; ii++)
                {
                    pieces[(i + ii) + j * 8] = new Piece(); // "none" piece
                }

                i += n;
            }
            else
            {
                int index = i + j * 8;
                Piece.Type pieceType = Utils.CharToPieceType(symbol); // must be a piece symbol then
                Piece.Color pieceColor = char.IsUpper(symbol) ? Piece.Color.White : Piece.Color.Black;
                pieces[index] = new Piece { type = pieceType, color = pieceColor };
                i++;
            }
        }
    }
}
