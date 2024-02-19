using Godot;
using System;
using System.Collections.Generic;

public class Search
{
    // constants

    private const int positiveInfinity = 9999999;
    private const int negativeInfinity = -positiveInfinity;
    private const int mateScore = 100000;

    // on search complete action

    public event System.Action<Move> onComplete;

	// board

	private Board board;

    // color

    private Piece.Color color;

	// best move found

	private Move bestMoveFound;
	private int bestMoveEval = negativeInfinity;

    // ctor

    public Search(Board board, Piece.Color color)
	{
		// init

		this.board = board;
        this.color = color;
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

		int eval = SearchMoves(4, 0, negativeInfinity, positiveInfinity);

        //// check if mate score

        //if (Math.Abs(eval) >= mateScore - 1000)
        //{
        //    int numPlyToMate = Math.Abs(eval - mateScore);
        //    int numMovesToMate = (int)Math.Ceiling(numPlyToMate / 2f);
        //    GD.Print("Checkmate in " + numMovesToMate + " moves");
        //}
        //else
        //{
        //    GD.Print(eval / 100.0f);
        //}

        // on search complete

        onComplete?.Invoke(bestMoveFound);
	}

    // just search for captures

    private int QuiescenceSearch(int alpha, int beta)
    {
        // evaluate board

        int evaluation = Evaluation.EvaluateBoard(board, color);

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

        SortMoves(board, moves, board.GetTurnColor());

        // calculate eval

        foreach (Move move in moves)
        {
            board.MakeMove(move, true);
            evaluation = -QuiescenceSearch(-beta, -alpha);
            board.UndoMove();

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
        //

        if (depth == 0)
        {
            return QuiescenceSearch(alpha, beta);
        }

        // check checkmate

        List<Move> moves = MoveGeneration.GetAllLegalMovesByColor(board, board.GetTurnColor());

        if (moves.Count == 0)
        {
            if (MoveGeneration.IsKingInCheck(board, board.GetTurnColor()))
            {
                return -mateScore + plyFromRoot;
            }

            return 0;
        }

        SortMoves(board, moves, board.GetTurnColor());

        // calculate eval

        foreach (Move move in moves)
        {
            board.MakeMove(move, true);
            int evaluation = -SearchMoves(depth - 1, plyFromRoot + 1, -beta, -alpha);
            board.UndoMove();

            if (evaluation >= beta) // Beta cutoff
            {
                return beta;
            }

            if (evaluation > alpha)
            {
                if (plyFromRoot == 0)
                {
                    bestMoveFound = move;
                    bestMoveEval = evaluation;
                }
            }

            alpha = Math.Max(alpha, evaluation); // Update alpha
        }

        return alpha;
    }

    // sort moves

    private void SortMoves(Board board, List<Move> moves, Piece.Color color)
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
                // Piece.Color color = moves[i].pieceSource.color;
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
