using Godot;
using System;
using System.Threading.Tasks;

public class PlayerAI : Player
{
    // board

    private Board board;

    private Move moveSelected = Move.NullMove;
    private bool moveFound = false;

    // ctor

    public PlayerAI(Board board)
    {
        // init

        this.board = board;
    }

    public override void NotifyTurnToMove()
    {
        moveFound = false;

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
        Board newBoard = board.Copy();
        Search search = new Search(newBoard, newBoard.GetTurnColor());
        search.onComplete += OnSearchCompleted;
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
