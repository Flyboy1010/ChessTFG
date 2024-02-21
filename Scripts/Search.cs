using Godot;
using System;
using System.Collections.Generic;

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

    // depth to search

    private int targetDepth;

	// best move found

	private Move bestMoveFound;
	private int bestEvalFound = negativeInfinity;

    // transposition table

    private TranspositionTable tt;

    // ctor

    public Search(int depth)
	{
        // init

        targetDepth = depth;

        // transposition table

        int sizeInEntries = (ttSizeInMb * 1024 * 1024) / TranspositionTable.Entry.GetSize();
        tt = new TranspositionTable(sizeInEntries);
	}

    // set board

    public void SetBoard(Board board)
    {
        this.board = board;
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
        
        // do the search

		int eval = SearchMoves(targetDepth, 0, negativeInfinity, positiveInfinity);

        // check if mate score

        if (Math.Abs(eval) >= mateScore - 1000)
        {
            int numPlyToMate = Math.Abs(eval - mateScore);
            int numMovesToMate = (int)Math.Ceiling(numPlyToMate / 2f);
            GD.Print("Checkmate in " + numPlyToMate + " moves");
        }
        else
        {
            GD.Print(eval / 100.0f);
        }

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

        // get moves

        List<Move> moves = MoveGeneration.GetAllLegalMovesByColor(board, board.GetTurnColor());

        // remove all non captures

        int n = moves.RemoveAll((move) =>
        {
            if (move.pieceTarget.type == Piece.Type.None && move.flags != Move.Flags.EnPassant)
                return true;

            return false;
        });

        // sort

        SortMoves(moves, board.GetTurnColor());

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
        // check the transposition table

        ulong zobrist = board.GetZobrist();
        int ttVal = tt.Lookup(zobrist, depth, alpha, beta);
        if (ttVal != TranspositionTable.lookupFailed)
        {
            if (plyFromRoot == 0)
            {
                TranspositionTable.Entry tEntry = tt.GetEntry(zobrist);
                bestMoveFound = tEntry.move;
                bestEvalFound = tEntry.value;
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

        SortMoves(moves, board.GetTurnColor());

        // calculate eval

        TranspositionTable.NodeType nodeType = TranspositionTable.NodeType.UpperBound;
        Move bestMoveInThisPosition = Move.NullMove;

        foreach (Move move in moves)
        {
            board.MakeMove(move, true);
            int evaluation = -SearchMoves(depth - 1, plyFromRoot + 1, -beta, -alpha);
            board.UndoMove(true);

            if (evaluation >= beta) // Beta cutoff
            {
                tt.Store(zobrist, depth, beta, TranspositionTable.NodeType.LowerBound, move);
                return beta;
            }

            if (evaluation > alpha)
            {
                nodeType = TranspositionTable.NodeType.Exact;
                bestMoveInThisPosition = move;

                alpha = evaluation;

                if (plyFromRoot == 0)
                {
                    bestMoveFound = move;
                    bestEvalFound = evaluation;
                }
            }
        }

        tt.Store(zobrist, depth, alpha, nodeType, bestMoveInThisPosition);

        return alpha;
    }

    // sort moves

    private void SortMoves(List<Move> moves, Piece.Color color)
    {
        int[] moveScore = new int[moves.Count];

        for (int i = 0; i < moves.Count; i++)
        {
            moveScore[i] = 0;

            Piece.Type pieceTypeTarget = moves[i].pieceTarget.type;
            Piece.Type pieceTypeSource = moves[i].pieceSource.type;

            if (pieceTypeTarget != Piece.Type.None)
            {
                moveScore[i] += 10 * Evaluation.GetPieceValue(pieceTypeTarget) - Evaluation.GetPieceValue(pieceTypeSource);
            }
            else
            {
                int indexSource = color == Piece.Color.White ? 63 - moves[i].squareSourceIndex : moves[i].squareSourceIndex;
                int indexTarget = color == Piece.Color.White ? 63 - moves[i].squareTargetIndex : moves[i].squareTargetIndex;

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
