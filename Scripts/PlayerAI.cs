using Godot;
using System;
using System.Threading.Tasks;

public class PlayerAI : Player
{
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
        search = new Search(6);
    }

    public override void NotifyTurnToMove()
    {
        moveFound = false;
        Board boardCopy = board.Copy();
        search.SetBoard(boardCopy);
        search.onComplete += OnSearchCompleted;

        // Start a new Task to calculate the best move asynchronously

        Task.Run(() =>
        {
            CalculateBestMove();
        });
    }

    private void CalculateBestMove()
    {
        // Your code to calculate the best move goes here
        // This method will execute asynchronously in a separate thread
        // Make sure to handle synchronization if needed

        // Thread.Sleep(2000);
        
        search.StartSearch();
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
