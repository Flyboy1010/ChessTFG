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
                GD.Print("Search canceled at depth: " + depth);
                GD.Print("Partial search result move: " + Utils.FromMoveToString(bestMoveFound) + " eval: " + bestEvalFound + " time: " + stopwatch.ElapsedMilliseconds + "ms");
                break;
            }
            else
            {
                if (bestEvalFound >= mateScore - 1000)
                {
                    int numPlyToMate = Math.Abs(bestEvalFound - mateScore);
                    int numMovesToMate = (int)Math.Ceiling(numPlyToMate / 2f);
                    GD.Print("Depth: " + depth + " move: " + Utils.FromMoveToString(bestMoveIteration) + " eval: Mate in " + numMovesToMate + " time: " + stopwatch.ElapsedMilliseconds + "ms");
                    break;
                }
                else
                {
                    GD.Print("Depth: " + depth + " move: " + Utils.FromMoveToString(bestMoveIteration) + " eval: " + bestEvalIteration + " time: " + stopwatch.ElapsedMilliseconds + "ms");
                }
            }

            //if (Math.Abs(bestEvalFound) >= mateScore - 1000)
            //{
            //    int numPlyToMate = Math.Abs(bestEvalFound - mateScore);
            //    int numMovesToMate = (int)Math.Ceiling(numPlyToMate / 2f);
            //    GD.Print("Checkmate in " + numMovesToMate + " moves");
            //    break;
            //}
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

        // check the transposition table

        ulong zobrist = board.GetZobrist();
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

        //

        if (depth == 0)
        {
            //return Evaluation.EvaluateBoard(board, board.GetTurnColor());
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
            TranspositionTable.Entry entry = tt.GetEntry(board.GetZobrist());
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

            if (i >= 3 && depth > 3 && !isCapture)
            {
                const int reduction = 1;
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
            if (moves[i].IsEqual(hashMove))
            {
                moveScore[i] += 10000000;
                continue;
            }

            moveScore[i] = 0;

            Piece.Type pieceTypeTarget = moves[i].pieceTarget.type;
            Piece.Type pieceTypeSource = moves[i].pieceSource.type;

            if (pieceTypeTarget != Piece.Type.None)
            {
                moveScore[i] += 10 * Evaluation.GetPieceValue(pieceTypeTarget) - Evaluation.GetPieceValue(pieceTypeSource);
            }
            else
            {
                int indexSource = color == Piece.Color.White ? moves[i].squareSourceIndex : 63 - moves[i].squareSourceIndex;
                int indexTarget = color == Piece.Color.White ? moves[i].squareTargetIndex : 63 - moves[i].squareTargetIndex;

                switch (pieceTypeSource)
                {
                    case Piece.Type.Pawn:
                        moveScore[i] += PieceSquareTables.PawnTable[indexTarget] - PieceSquareTables.PawnTable[indexSource];
                        break;
                    case Piece.Type.Knight:
                        moveScore[i] += PieceSquareTables.KnightTable[indexTarget] - PieceSquareTables.KnightTable[indexSource];
                        break;
                    case Piece.Type.Bishop:
                        moveScore[i] += PieceSquareTables.BishopTable[indexTarget] - PieceSquareTables.BishopTable[indexSource];
                        break;
                    case Piece.Type.Rook:
                        moveScore[i] += PieceSquareTables.RookTable[indexTarget] - PieceSquareTables.RookTable[indexSource];
                        break;
                    case Piece.Type.Queen:
                        moveScore[i] += PieceSquareTables.QueenTable[indexTarget] - PieceSquareTables.QueenTable[indexSource];
                        break;
                    case Piece.Type.King:
                        moveScore[i] += PieceSquareTables.KingTable[indexTarget] - PieceSquareTables.KingTable[indexSource];
                        break;
                }
            }

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
