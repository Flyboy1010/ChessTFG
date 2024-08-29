using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class Search
{
	// constants

	private const int positiveInfinity = 9999999;
	private const int negativeInfinity = -positiveInfinity;
	private const int mateScore = 100000;

	private const int ttSizeInMb = 64;

	// on search complete action

	public event System.Action<Move> onComplete;

	// board

	private Board board;

	// best move found

	private Move bestMoveFound;
	private int bestEvalFound = negativeInfinity;

	private Move bestMoveIteration;
	private int bestEvalIteration;

	// search cancelled flag

	private bool isSearchCanceled;

	// transposition table

	private TranspositionTable tt;

	// ctor

	public Search()
	{
		// transposition table

		int sizeInEntries = (ttSizeInMb * 1024 * 1024) / TranspositionTable.Entry.GetSize();
		tt = new TranspositionTable(sizeInEntries);
	}

	// set board

	public void SetBoard(Board board)
	{
		this.board = board;
	}

	// cancell search

	public void Cancel()
	{
		isSearchCanceled = true;
	}

	// get best move

	public Move GetBestMoveFound()
	{
		return bestMoveFound;
	}

	// start search

	public void StartSearch()
	{
		// prepare the search

		bestMoveFound = Move.NullMove;
		bestEvalFound = int.MinValue;
		isSearchCanceled = false;

		Stopwatch stopwatch = new Stopwatch();

		// iterative deepening

		for (int depth = 1; depth < 100; depth++)
		{
			bestMoveIteration = Move.NullMove;
			bestEvalIteration = negativeInfinity;

			stopwatch.Start();
			SearchMoves(depth, 0, negativeInfinity, positiveInfinity);
			stopwatch.Stop();

			if (!bestMoveIteration.IsEqual(Move.NullMove))
			{
				bestMoveFound = bestMoveIteration;
				bestEvalFound = bestEvalIteration;
			}

			if (isSearchCanceled)
			{
				GD.Print($"Search canceled at depth: {depth}");
				GD.Print($"Partial search result best move: {Utils.FromMoveToString(bestMoveFound)}, eval: {bestEvalFound}, time: {stopwatch.ElapsedMilliseconds} ms");
				break;
			}
			else
			{
				if (Math.Abs(bestEvalFound) >= mateScore - 1000)
				{
					int numPlyToMate = mateScore - Math.Abs(bestEvalFound);
					int numMovesToMate = numPlyToMate / 2;
					GD.Print($"Depth: {depth}, best move: {Utils.FromMoveToString(bestMoveFound)}, eval: Mate in {numPlyToMate} ply ({numMovesToMate} moves), time: {stopwatch.ElapsedMilliseconds} ms");

					if (numPlyToMate <= depth)
					{
						break;
					}
				}
				else
				{
					GD.Print($"Depth: {depth}, best move: {Utils.FromMoveToString(bestMoveFound)}, eval: {bestEvalFound}, time: {stopwatch.ElapsedMilliseconds} ms");
				}
			}
		}

		GD.Print();

		// on search complete

		onComplete?.Invoke(bestMoveFound);
	}

	// just search for captures

	private int QuiescenceSearch(int alpha, int beta)
	{
		// evaluate board

		int evaluation = Evaluation.EvaluateBoard(board, board.GetTurnColor());

		if (evaluation >= beta) // Beta cutoff
		{
			return beta;
		}

		alpha = Math.Max(alpha, evaluation); // Update alpha

		// get moves just captures

		List<Move> moves = MoveGeneration.GetAllLegalMovesByColor(board, board.GetTurnColor(), true);

		// sort

		SortMoves(moves, board.GetTurnColor(), Move.NullMove);

		// calculate eval

		foreach (Move move in moves)
		{
			board.MakeMove(move, true);
			evaluation = -QuiescenceSearch(-beta, -alpha);
			board.UndoMove(true);

			if (evaluation >= beta) // Beta cutoff
			{
				return beta;
			}

			alpha = Math.Max(alpha, evaluation); // Update alpha
		}

		return alpha;
	}

	// search moves

	public int SearchMoves(int depth, int plyFromRoot, int alpha, int beta)
	{
		// if search canceled

		if (isSearchCanceled)
		{
			return 0;
		}

		// get zobrist from board

		ulong zobrist = board.GetZobrist();

		// check the transposition table

		int ttVal = tt.Lookup(zobrist, depth, alpha, beta);
		if (ttVal != TranspositionTable.lookupFailed)
		{
			if (plyFromRoot == 0)
			{
				TranspositionTable.Entry tEntry = tt.GetEntry(zobrist);
				bestMoveIteration = tEntry.move;
				bestEvalIteration = tEntry.value;
			}

			return ttVal;
		}

		// when reached 0 depth perform a quiescence search until a stable state (to get good results from evaluation)

		if (depth == 0)
		{
			int result = QuiescenceSearch(alpha, beta);
			return result;
		}

		// check checkmate

		List<Move> moves = MoveGeneration.GetAllLegalMovesByColor(board, board.GetTurnColor());

		if (moves.Count == 0)
		{
			if (MoveGeneration.IsKingInCheck(board, board.GetTurnColor()))
			{
				int result = -mateScore + plyFromRoot;
				return result;
			}

			// stale mate

			return 0;
		}

		// sort moves

		Move hashMove;

		if (plyFromRoot == 0)
		{
			hashMove = bestMoveFound;
		}
		else
		{
			TranspositionTable.Entry entry = tt.GetEntry(zobrist);
			hashMove = entry.move;
		}

		SortMoves(moves, board.GetTurnColor(), hashMove);

		// calculate eval

		TranspositionTable.NodeType nodeType = TranspositionTable.NodeType.UpperBound;
		Move bestMoveInThisPosition = Move.NullMove;

		for (int i = 0; i < moves.Count; i++)
		{
			board.MakeMove(moves[i], true);

			// late move reduction

			int evaluation = 0;
			bool needsFullSearch = true;
			bool isCapture = moves[i].pieceTarget.type != Piece.Type.None;
			bool isInCheck = MoveGeneration.IsKingInCheck(board, board.GetTurnColor());

			if (i >= 3 && depth > 3 && !isCapture && !isInCheck)
			{
				const int reduction = 2; // incremented to 2
				evaluation = -SearchMoves(depth - 1 - reduction, plyFromRoot + 1, -beta, -alpha);
				needsFullSearch = evaluation > alpha;
			}

			if (needsFullSearch)
			{
				evaluation = -SearchMoves(depth - 1, plyFromRoot + 1, -beta, -alpha);
			}

			board.UndoMove(true);

			// if search canceled

			if (isSearchCanceled)
			{
				return 0;
			}

			if (evaluation >= beta) // Beta cutoff
			{
				tt.Store(zobrist, depth, beta, TranspositionTable.NodeType.LowerBound, moves[i]);
				return beta;
			}

			if (evaluation > alpha)
			{
				nodeType = TranspositionTable.NodeType.Exact;
				bestMoveInThisPosition = moves[i];

				alpha = evaluation;

				if (plyFromRoot == 0)
				{
					bestMoveIteration = moves[i];
					bestEvalIteration = evaluation;
				}
			}
		}

		tt.Store(zobrist, depth, alpha, nodeType, bestMoveInThisPosition);

		return alpha;
	}

	// sort moves

	private void SortMoves(List<Move> moves, Piece.Color color, Move hashMove)
	{
		int[] moveScore = new int[moves.Count];

		for (int i = 0; i < moves.Count; i++)
		{
			moveScore[i] = 0;

			// check if is the hash move

			if (moves[i].IsEqual(hashMove))
			{
				// bonus for hash move

				moveScore[i] += 10000000;
				continue;
			}

			// check if it is a capture

			Piece.Type pieceTypeTarget = moves[i].pieceTarget.type;
			Piece.Type pieceTypeSource = moves[i].pieceSource.type;

			if (pieceTypeTarget != Piece.Type.None)
			{
				// bonus for capture

				moveScore[i] += 10 * Evaluation.GetPieceValue(pieceTypeTarget) - Evaluation.GetPieceValue(pieceTypeSource);
			}
			else
			{
				// bonus for moving the piece into a better square

				int[] table = PieceTables.GetTable(pieceTypeSource);
				moveScore[i] += PieceTables.Read(table, moves[i].squareTargetIndex, color) - PieceTables.Read(table, moves[i].squareSourceIndex, color);
			}

			// bonus for promotion

			if (moves[i].flags == Move.Flags.Promotion)
			{
				moveScore[i] += Evaluation.GetPieceValue(moves[i].promotionPieceType);
			}
		}

		// Sort the moves list based on scores

		for (int i = 0; i < moves.Count - 1; i++)
		{
			for (int j = i + 1; j > 0; j--)
			{
				int swapIndex = j - 1;
				if (moveScore[swapIndex] < moveScore[j])
				{
					(moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
					(moveScore[j], moveScore[swapIndex]) = (moveScore[swapIndex], moveScore[j]);
				}
			}
		}
	}
}
