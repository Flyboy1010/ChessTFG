using Godot;
using System;
using System.Collections.Generic;
using static Board;

public static class MoveGeneration
{
	private enum Direction
	{
		Up, Left, Down, Right, D1, D2, D3, D4
	}

	/* 
	 * up left down, right, d1, d2, d3, d4
	 * 
	 * +----+----+----+
	 * | D2 | UP | D1 |
	 * +----+----+----+
	 * | LE | -- | RI |
	 * +----+----+----+
	 * | D3 | DW | D4 |
	 * +----+----+----+
	 * 
	 */

	private static readonly int[] directionOffsets = { -8, -1, 8, 1, -7, -9, 7, 9 };

	private static readonly int[][] preCalculatedSquaresToEdge = new int[64][];
	private static readonly int[][] preCalculatedKnightMoves = new int[64][];
	private static readonly int[][] preCalculatedKingMoves = new int[64][];
	private static readonly int[][][] preCalculatedPawnCapturesMoves = new int[2][][];

	// for castling [0] -> white, [1] -> black

	private static readonly int[][] shortCastleSquaresIndices = new int[2][]
	{
		new int[] { F1, G1 },
		new int[] { F8, G8 }
	};

	private static readonly int[][] longCastleSquaresIndices = new int[2][]
	{
		new int[] { D1, C1 },
		new int[] { D8, C8 }
	};

	private static readonly int[] longCastleEmptySquareIndex = new int[2] { B1, B8 };

	private static readonly int[] shortCastleTargetKingSquareIndex = new int[2] { G1, G8 };
	private static readonly int[] longCastleTargetKingSquareIndex = new int[2] { C1, C8 };

	// static ctor

	static MoveGeneration()
	{
		// Precalculate the moves

		PrecalculateMoves();
	}

	// check if a square is in bounds

	private static bool IsInBounds(int i, int j)
	{
		return i >= 0 && i < 8 && j >= 0 && j < 8;
	}

	// precalculate moves

	public static void PrecalculateMoves()
	{
		// init arrays pawn captures

		preCalculatedPawnCapturesMoves[0] = new int[64][]; // white
		preCalculatedPawnCapturesMoves[1] = new int[64][]; // black

		// helper buffer

		List<int> movesBuffer = new List<int>();

		for (int j = 0; j < 8; j++)
		{
			for (int i = 0; i < 8; i++)
			{
				// index

				int index = i + j * 8;

				// squares to edge

				int up = j;
				int down = 7 - j;
				int left = i;
				int right = 7 - i;

				int d1 = Math.Min(up, right);
				int d2 = Math.Min(up, left);
				int d3 = Math.Min(down, left);
				int d4 = Math.Min(down, right);

				preCalculatedSquaresToEdge[i + j * 8] = new int[8] { up, left, down, right, d1, d2, d3, d4 };

				// knight moves

				movesBuffer.Clear();

				for (int jj = -2; jj <= 2; jj += 4)
				{
					for (int ii = -1; ii <= 1; ii += 2)
					{
						if (IsInBounds(i + ii, j + jj))
						{
							movesBuffer.Add((i + ii) + (j + jj) * 8);
						}

						if (IsInBounds(i + jj, j + ii))
						{
							movesBuffer.Add((i + jj) + (j + ii) * 8);
						}
					}
				}

				preCalculatedKnightMoves[index] = movesBuffer.ToArray();

				// king moves

				movesBuffer.Clear();

				for (int jj = -1; jj <= 1; jj++)
				{
					for (int ii = -1; ii <= 1; ii++)
					{
						if (!(ii == 0 && jj == 0) && IsInBounds(i + ii, j + jj))
						{
							movesBuffer.Add((i + ii) + (j + jj) * 8);
						}
					}
				}

				preCalculatedKingMoves[index] = movesBuffer.ToArray();

				/* PAWN CAPTURES */

				// white pawns

				movesBuffer.Clear();

				if (j > 0)
				{
					if (i > 0)
					{
						movesBuffer.Add(index + directionOffsets[(int)Direction.D2]);
					}

					if (i < 7)
					{
						movesBuffer.Add(index + directionOffsets[(int)Direction.D1]);
					}
				}

				preCalculatedPawnCapturesMoves[0][index] = movesBuffer.ToArray();

				// black pawns

				movesBuffer.Clear();

				if (j < 7)
				{
					if (i > 0)
					{
						movesBuffer.Add(index + directionOffsets[(int)Direction.D3]);
					}

					if (i < 7)
					{
						movesBuffer.Add(index + directionOffsets[(int)Direction.D4]);
					}
				}

				preCalculatedPawnCapturesMoves[1][index] = movesBuffer.ToArray();
			}
		}
	}

	// get controlled squares by color

	public static ulong GetControlledSquaresBitboardByColor(Board board, Piece.Color color)
	{
		ulong squares = 0;

		List<int> piecesIndices = board.GetPiecesIndices(color);

		foreach (int index in piecesIndices)
		{
			Piece piece = board.GetPiece(index);

			switch (piece.type)
			{
				case Piece.Type.Pawn:
					int[] pawnCaptures = preCalculatedPawnCapturesMoves[(int)piece.color - 1][index];

					foreach (int targetIndex in pawnCaptures)
					{
						Piece targetPiece = board.GetPiece(targetIndex);

						if (targetPiece.color != piece.color) // none pieces have also none color
						{
							squares |= (1UL << targetIndex);
						}
					}
					break;
				case Piece.Type.Knight:
					foreach (int targetIndex in preCalculatedKnightMoves[index])
					{
						Piece targetPiece = board.GetPiece(targetIndex);

						// if the square is empty or the pieces color are diferent then add the move to the list

						if (targetPiece.type == Piece.Type.None || targetPiece.color != piece.color)
						{
							squares |= (1UL << targetIndex);
						}
					}
					break;
				case Piece.Type.Bishop:
				case Piece.Type.Rook:
				case Piece.Type.Queen:
					int startDirection = (piece.type != Piece.Type.Bishop) ? 0 : 4;
					int endDirection = (piece.type != Piece.Type.Rook) ? 8 : 4;

					for (int d = startDirection; d < endDirection; d++)
					{
						int n = preCalculatedSquaresToEdge[index][d];

						for (int i = 0; i < n; i++)
						{
							int targetIndex = index + directionOffsets[d] * (i + 1);

							Piece targetPiece = board.GetPiece(targetIndex);

							// check pieces in the path

							if (targetPiece.type == Piece.Type.None)
							{
								squares |= (1UL << targetIndex);
							}
							else
							{
								if (targetPiece.color != piece.color)
								{
									squares |= (1UL << targetIndex);
								}

								break;
							}
						}
					}
					break;
				case Piece.Type.King:
					int[] kingMoves = preCalculatedKingMoves[index];

					foreach (int targetIndex in kingMoves)
					{
						Piece targetPiece = board.GetPiece(targetIndex);

						if (targetPiece.type == Piece.Type.None || targetPiece.color != piece.color)
						{
							squares |= (1UL << targetIndex);
						}
					}
					break;
			}
		}

		return squares;
	}

	// generate knight moves

	private static List<Move> GenerateKnightMoves(Board board, int index, bool onlyCaptures = false)
	{
		List<Move> moves = new List<Move>();

		Piece piece = board.GetPiece(index);

		foreach (int targetIndex in preCalculatedKnightMoves[index])
		{
			Piece targetPiece = board.GetPiece(targetIndex);

			// if the square is empty or the pieces color are diferent then add the move to the list

			if (targetPiece.type == Piece.Type.None)
			{
				if (onlyCaptures)
					continue;
			}

			if (targetPiece.color == piece.color)
				continue;

			moves.Add(new Move()
			{
				squareSourceIndex = index,
				squareTargetIndex = targetIndex,
				pieceSource = piece,
				pieceTarget = targetPiece
			});
		}

		return moves;
	}

	// generate sliding moves (for bishop rook & queen)

	private static List<Move> GenerateSlidingMoves(Board board, int index, bool onlyCaptures = false)
	{
		List<Move> moves = new List<Move>();

		Piece piece = board.GetPiece(index);

		int startDirection = (piece.type != Piece.Type.Bishop) ? 0 : 4;
		int endDirection = (piece.type != Piece.Type.Rook) ? 8 : 4;

		for (int d = startDirection; d < endDirection; d++)
		{
			int n = preCalculatedSquaresToEdge[index][d];

			for (int i = 0; i < n; i++)
			{
				int targetIndex = index + directionOffsets[d] * (i + 1);

				Piece targetPiece = board.GetPiece(targetIndex);

				if (targetPiece.type == Piece.Type.None)
				{
					if (onlyCaptures)
						continue;

					moves.Add(new Move()
					{
						squareSourceIndex = index,
						squareTargetIndex = targetIndex,
						pieceSource = piece,
						pieceTarget = targetPiece
					});
				}
				else
				{
					if (targetPiece.color != piece.color)
					{ 
						moves.Add(new Move()
						{
							squareSourceIndex = index,
							squareTargetIndex = targetIndex,
							pieceSource = piece,
							pieceTarget = targetPiece
						});
					}

					break;
				}
			}
		}

		return moves;
	}

	// generate pawn moves

	private static List<Move> GeneratePawnMoves(Board board, int index, bool onlyCaptures = false)
	{
		List<Move> moves = new List<Move>();

		Piece piece = board.GetPiece(index);

		// moves

		int i = index % 8;
		int j = index / 8;

		// if just captures ignore pushes and double pushes

		if (!onlyCaptures)
		{
			// check if the pawn is white or black

			switch (piece.color)
			{
				case Piece.Color.White:
					// double push

					if (j == 6)
					{
						for (int jj = 0; jj < 2; jj++)
						{
							int targetIndex = index + (jj + 1) * directionOffsets[(int)Direction.Up];
							Piece targetPiece = board.GetPiece(targetIndex);

							if (targetPiece.type == Piece.Type.None)
							{
								moves.Add(new Move
								{
									squareSourceIndex = index,
									squareTargetIndex = targetIndex,
									pieceSource = piece,
									pieceTarget = targetPiece,
									flags = (jj == 0) ? Move.Flags.None : Move.Flags.DoublePush
								});
							}
							else
							{
								// if there is a piece in between then you cant double push

								break;
							}
						}
					}
					else if (j < 6 && j > 0) // single push
					{
						int targetIndex = index + directionOffsets[(int)Direction.Up];
						Piece targetPiece = board.GetPiece(targetIndex);

						if (targetPiece.type == Piece.Type.None)
						{
							moves.Add(new Move
							{
								squareSourceIndex = index,
								squareTargetIndex = targetIndex,
								pieceSource = piece,
								pieceTarget = targetPiece,
								flags = (j == 1) ? Move.Flags.Promotion : Move.Flags.None,
								promotionPieceType = board.PromotionPieceType
							});
						}
					}

					break;
				case Piece.Color.Black:
					// double push

					if (j == 1)
					{
						for (int jj = 0; jj < 2; jj++)
						{
							int targetIndex = index + (jj + 1) * directionOffsets[(int)Direction.Down];
							Piece targetPiece = board.GetPiece(targetIndex);

							if (targetPiece.type == Piece.Type.None)
							{
								moves.Add(new Move
								{
									squareSourceIndex = index,
									squareTargetIndex = targetIndex,
									pieceSource = piece,
									pieceTarget = targetPiece,
									flags = (jj == 0) ? Move.Flags.None : Move.Flags.DoublePush
								});
							}
							else
							{
								// if there is a piece in between then you cant double push

								break;
							}
						}
					}
					else if (j > 1 && j < 7) // single push
					{
						int targetIndex = index + directionOffsets[(int)Direction.Down];
						Piece targetPiece = board.GetPiece(targetIndex);

						if (targetPiece.type == Piece.Type.None)
						{
							moves.Add(new Move
							{
								squareSourceIndex = index,
								squareTargetIndex = targetIndex,
								pieceSource = piece,
								pieceTarget = targetPiece,
								flags = (j == 6) ? Move.Flags.Promotion : Move.Flags.None,
								promotionPieceType = board.PromotionPieceType
							});
						}
					}
					break;
			}
		}

		// captures moves

		int[] pawnCaptures = preCalculatedPawnCapturesMoves[(int)piece.color - 1][index];

		foreach (int targetIndex in pawnCaptures)
		{
			Piece targetPiece = board.GetPiece(targetIndex);

			if (targetPiece.type != Piece.Type.None && targetPiece.color != piece.color)
			{
				Move move = new Move
				{
					squareSourceIndex = index,
					squareTargetIndex = targetIndex,
					pieceSource = piece,
					pieceTarget = targetPiece,
					promotionPieceType = board.PromotionPieceType
				};

				// check color for capture with promotion

				switch (piece.color)
				{
					case Piece.Color.White:
						if (j == 1) // promotion
						{
							move.flags = Move.Flags.Promotion;
						}
						break;
					case Piece.Color.Black:
						if (j == 6) // promotion
						{
							move.flags = Move.Flags.Promotion;
						}
						break;
				}

				// add the move to the list

				moves.Add(move);
			}
		}

		// en passant

		ref readonly BoardState boardState = ref board.GetBoardState();

		if (boardState.IsEnPassantAvailable())
		{
			int enPassantSquareI = boardState.GetEnPassantSquareIndex() % 8;

			switch (piece.color)
			{
				case Piece.Color.White:
					if (j == 3)
					{
						if (i + 1 == enPassantSquareI)
						{
							int targetIndex = index + directionOffsets[(int)Direction.D1];
							moves.Add(new Move
							{
								squareSourceIndex = index,
								squareTargetIndex = targetIndex,
								pieceSource = piece,
								pieceTarget = Piece.NullPiece,
								flags = Move.Flags.EnPassant
							});
						}
						else if (i - 1 == enPassantSquareI)
						{
							int targetIndex = index + directionOffsets[(int)Direction.D2];
							moves.Add(new Move
							{
								squareSourceIndex = index,
								squareTargetIndex = targetIndex,
								pieceSource = piece,
								pieceTarget = Piece.NullPiece,
								flags = Move.Flags.EnPassant
							});
						}
					}
					break;
				case Piece.Color.Black:
					if (j == 4)
					{
						if (i + 1 == enPassantSquareI)
						{
							int targetIndex = index + directionOffsets[(int)Direction.D4];
							moves.Add(new Move
							{
								squareSourceIndex = index,
								squareTargetIndex = targetIndex,
								pieceSource = piece,
								pieceTarget = Piece.NullPiece,
								flags = Move.Flags.EnPassant
							});
						}
						else if (i - 1 == enPassantSquareI)
						{
							int targetIndex = index + directionOffsets[(int)Direction.D3];
							moves.Add(new Move
							{
								squareSourceIndex = index,
								squareTargetIndex = targetIndex,
								pieceSource = piece,
								pieceTarget = Piece.NullPiece,
								flags = Move.Flags.EnPassant
							});
						}
					}
					break;
			}
		}

		return moves;
	}


	// generate king moves

	private static List<Move> GenerateKingMoves(Board board, int index, bool onlyCaptures = false)
	{
		List<Move> moves = new List<Move>();

		Piece piece = board.GetPiece(index);

		// normal moves

		int[] kingMoves = preCalculatedKingMoves[index];

		foreach (int targetIndex in kingMoves)
		{
			Piece targetPiece = board.GetPiece(targetIndex);

			if (targetPiece.type == Piece.Type.None)
			{
				if (onlyCaptures)
					continue;
			}

			if (targetPiece.color == piece.color)
				continue;

			moves.Add(new Move()
			{
				squareSourceIndex = index,
				squareTargetIndex = targetIndex,
				pieceSource = piece,
				pieceTarget = targetPiece
			});
		}

		// just captures

		if (onlyCaptures)
			return moves;

		// castling moves

		ref readonly BoardState boardState = ref board.GetBoardState();
		bool canCastleShort = boardState.CanCastleShort(piece.color);
		bool canCastleLong = boardState.CanCastleLong(piece.color);

		if (canCastleShort || canCastleLong)
		{
			ulong controlledSquaresByOpponent = GetControlledSquaresBitboardByColor(board, Piece.GetOppositeColor(piece.color));

			// first check if the king is in check

			bool isKingInCheck = (controlledSquaresByOpponent & (1UL << index)) != 0;

			// if the king is not in check then

			if (!isKingInCheck)
			{
				if (canCastleShort)
				{
					// check squares in between

					bool isShortCastleLegal = true;

					foreach (int squareIndex in shortCastleSquaresIndices[(int)piece.color - 1])
					{
						Piece targetPiece = board.GetPiece(squareIndex);

						if (((controlledSquaresByOpponent & (1UL << squareIndex)) != 0) || targetPiece.type != Piece.Type.None)
						{
							isShortCastleLegal = false;
							break;
						}
					}

					if (isShortCastleLegal)
					{
						moves.Add(new Move
						{
							squareSourceIndex = index,
							squareTargetIndex = shortCastleTargetKingSquareIndex[(int)piece.color - 1],
							pieceSource = piece,
							pieceTarget = Piece.NullPiece,
							flags = Move.Flags.CastleShort
						});
					}
				}

				if (canCastleLong)
				{
					// check squares in between

					bool isLongCastleLegal = true;

					foreach (int squareIndex in longCastleSquaresIndices[(int)piece.color - 1])
					{
						Piece targetPiece = board.GetPiece(squareIndex);

						if (((controlledSquaresByOpponent & (1UL << squareIndex)) != 0) || targetPiece.type != Piece.Type.None)
						{
							isLongCastleLegal = false;
							break;
						}
					}

					// check the knight square (bug fixed)

					{
						Piece targetPiece = board.GetPiece(longCastleEmptySquareIndex[(int)piece.color - 1]);

						if (targetPiece.type != Piece.Type.None)
						{
							isLongCastleLegal = false;
						}
					}

					if (isLongCastleLegal)
					{
						moves.Add(new Move
						{
							squareSourceIndex = index,
							squareTargetIndex = longCastleTargetKingSquareIndex[(int)piece.color - 1],
							pieceSource = piece,
							pieceTarget = Piece.NullPiece,
							flags = Move.Flags.CastleLong
						});
					}
				}
			}
		}

		return moves;
	}

	// check if the king of the selected color is in check

	public static bool IsKingInCheck(Board board, Piece.Color color)
	{
		// first find the king of the selected color

		int kingSquareIndex = board.FindKing(color);

		// get all the controlled squares by the opponent pieces

		ulong controlledSquares = GetControlledSquaresBitboardByColor(board, Piece.GetOppositeColor(color));

		// check if the king square is attacked

		return (controlledSquares & (1UL << kingSquareIndex)) != 0;
	}

	// generate pseudo legal moves

	public static List<Move> GetPseudoLegalMoves(Board board, int index, bool onlyCaptures = false)
	{
		Piece piece = board.GetPiece(index);

		switch (piece.type)
		{
			case Piece.Type.Pawn:
				return GeneratePawnMoves(board, index, onlyCaptures);
			case Piece.Type.Knight:
				return GenerateKnightMoves(board, index, onlyCaptures);
			case Piece.Type.Bishop:
			case Piece.Type.Queen:
			case Piece.Type.Rook:
				return GenerateSlidingMoves(board, index, onlyCaptures);
			case Piece.Type.King:
				return GenerateKingMoves(board, index, onlyCaptures);
		}

		return new List<Move>();
	}

	// get all pseudo legal moves

	public static List<Move> GetAllPseudoLegalMovesByColor(Board board, Piece.Color color, bool onlyCaptures = false)
	{
		List<Move> moves = new List<Move>();

		int[] piecesIndices = board.GetPiecesIndices(color).ToArray(); // this because the pieces list is modified by get legal moves function

		foreach (int index in piecesIndices)
		{
			moves.AddRange(GetPseudoLegalMoves(board, index, onlyCaptures));
		}

		return moves;
	}

	// get legal moves

	public static List<Move> GetLegalMoves(Board board, int index, bool onlyCaptures = false)
	{
		List<Move> legalMoves = new List<Move>();
		List<Move> pseudoLegalMoves = GetPseudoLegalMoves(board, index, onlyCaptures);

		Piece piece = board.GetPiece(index);

		foreach (Move move in pseudoLegalMoves)
		{
			// make the move

			board.MakeMove(move, true);

			// check if after the move the king is in check (if not then the move is legal)

			if (!IsKingInCheck(board, piece.color))
			{
				legalMoves.Add(move);
			}

			// undo the move

			board.UndoMove(true);
		}

		return legalMoves;
	}

	// get all legal moves

	public static List<Move> GetAllLegalMovesByColor(Board board, Piece.Color color, bool onlyCaptures = false)
	{
		List<Move> moves = new List<Move>();

		int[] piecesIndices = board.GetPiecesIndices(color).ToArray(); // this because the pieces list is modified by get legal moves function

		foreach (int index in piecesIndices)
		{
			moves.AddRange(GetLegalMoves(board, index, onlyCaptures));
		}

		return moves;
	}

	// test

	public static ulong TestPositions(Board board, Piece.Color startingColor, int depth)
	{
		if (depth == 0)
		{
			return 1;
		}

		List<Move> moves = GetAllPseudoLegalMovesByColor(board, startingColor);

		ulong numPositions = 0;

		foreach (Move move in moves)
		{
			board.MakeMove(move, true);
			if (!IsKingInCheck(board, startingColor))
				numPositions += TestPositions(board, Piece.GetOppositeColor(startingColor), depth - 1);
			board.UndoMove(true);
		}


		return numPositions;
	}
}
