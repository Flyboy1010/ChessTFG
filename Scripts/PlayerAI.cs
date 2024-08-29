using Godot;
using System;
using System.Threading.Tasks;

public class PlayerAI : Player
{
	// constants

	private const int searchTime = 3000; // ms

	// board

	private Board board;

	// search

	private Search search;

	// move selected

	private Move moveSelected = Move.NullMove;
	private bool moveFound = false;

	// ctor

	public PlayerAI(Board board)
	{
		// init

		this.board = board;
		search = new Search();
		search.onComplete += OnSearchCompleted;
	}

	public override void NotifyTurnToMove()
	{
		moveFound = false;
		Board boardCopy = board.Copy();
		search.SetBoard(boardCopy);

		// Start a new Task to calculate the best move asynchronously

		Task.Run(() =>
		{
			search.StartSearch();
		});

		// 

		Task.Delay(searchTime).ContinueWith((t) => 
		{
			search.Cancel();
		});
	}

	private void OnSearchCompleted(Move move)
	{
		moveSelected = move;
		moveFound = true;
	}

	public override void Update()
	{
		if (moveFound)
		{
			ChoseMove(moveSelected, true);
		}
	}
}
