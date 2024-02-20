using Godot;
using System;

public static class ZobristHashing
{
	// array with the piece keys

	private static readonly ulong[,] pieceKeys = new ulong[64, 12];

    // en passant castling keys

    private static readonly ulong[,] castlingKeys = new ulong[2, 2];

    // en passant keys

    private static readonly ulong enPassantKey;

    // array with the turn keys

    private static readonly ulong[] turnColorKeys = new ulong[2];

    // random number generator

    private static readonly Random random = new Random(0);

	// init

	static ZobristHashing()
	{
		// random values for the piece keys

		for (int i = 0; i < 64; i++)
		{
			for (int j = 0; j < 12; j++)
			{
				pieceKeys[i, j] = (ulong)random.NextInt64();
			}
		}

        // random values for the castling keys

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                castlingKeys[i, j] = (ulong)random.NextInt64();
            }
        }

		// random values for enPassantKeys

		enPassantKey = (ulong)random.NextInt64();

        // random values for turnColorKeys

        for (int i = 0; i < 2; i++)
        {
            turnColorKeys[i] = (ulong)random.NextInt64();
        }
    }

    // get pieces keys

    public static ulong GetPieceKey(int index, Piece piece)
    {
        int pieceIndex = 0;

        switch (piece.color)
        {
            case Piece.Color.White:
                pieceIndex = (int)piece.type - 1;
                break;
            case Piece.Color.Black:
                pieceIndex = (int)piece.type - 1 + 5;
                break;
        }

        return pieceKeys[index, pieceIndex];
    }

    // get short castle keys

    public static ulong GetShortCastleKey(Piece.Color color)
    {
        return castlingKeys[(int)color - 1, 0];
    }

    // get long castle keys

    public static ulong GetLongCastleKey(Piece.Color color)
    {
        return castlingKeys[(int)color - 1, 1];
    }

    // get en passant key

    public static ulong GetEnPassantKey()
    {
        return enPassantKey;
    }

    public static ulong GetTurnColorKey(Piece.Color color)
    {
        return turnColorKeys[(int)color - 1];
    }

    // get key from board

    public static ulong GetKey(Board board)
	{
		ulong hashKey = 0;

		// add each white piece

		foreach (int i in board.GetPiecesIndices(Piece.Color.White))
		{
			Piece piece = board.GetPiece(i);

			hashKey ^= pieceKeys[i, (int)(piece.type - 1)];
		}

        // add each black piece

        foreach (int i in board.GetPiecesIndices(Piece.Color.Black))
        {
            Piece piece = board.GetPiece(i);

            hashKey ^= pieceKeys[i, (int)(piece.type - 1) + 5];
        }

        // get board state

        ref readonly BoardState boardState = ref board.GetBoardState();

        // TODO: castleling

        if (boardState.CanCastleShort(Piece.Color.White))
        {
            hashKey ^= castlingKeys[0, 0];
        }
        if (boardState.CanCastleLong(Piece.Color.White))
        {
            hashKey ^= castlingKeys[0, 1];
        }

        if (boardState.CanCastleShort(Piece.Color.Black))
        {
            hashKey ^= castlingKeys[1, 0];
        }
        if (boardState.CanCastleLong(Piece.Color.Black))
        {
            hashKey ^= castlingKeys[1, 1];
        }

        // en passant

        if (boardState.IsEnPassantAvailable())
        {
            hashKey ^= enPassantKey;
        }

        // turn

        hashKey ^= turnColorKeys[(int)boardState.GetTurnColor() - 1];

        return hashKey;
	}
}
