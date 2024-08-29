using Godot;
using System;

public partial class UI : Control
{
	// ui nodes

	[Export] private Label gameOverLabel;
	[Export] private Label gameOverReasonLabel;
	[Export] private Label zobristHashNumberLabel;
	[Export] private RichTextLabel zobristHashNumberBitsLabel;
	[Export] private Label lastMoveLabel;

	// game connected to

	private Game game;

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		// hide labels

		gameOverLabel.Visible = false;
		gameOverReasonLabel.Visible = false;
	}

	// connect to game

	public void ConnectToGame(Game game)
	{
		this.game = game;
	}

	// on game over

	private void OnGameOver(Game.GameOverReason gameOverReason)
	{
		switch (gameOverReason)
		{
			case Game.GameOverReason.WhiteMated:
				gameOverReasonLabel.Text = "Black wins by checkmate";
				break;
			case Game.GameOverReason.BlackMated:
				gameOverReasonLabel.Text = "White wins by checkmate";
				break;
			case Game.GameOverReason.Drowned:
				gameOverReasonLabel.Text = "Draw by drowned";
				break;
			case Game.GameOverReason.Repetition:
				gameOverReasonLabel.Text = "Draw by repetition";
				break;
		}

		gameOverLabel.Visible = true;
		gameOverReasonLabel.Visible = true;
	}

	// on turn

	private void OnGameTurn()
	{
		// get the board

		Board board = game.GetBoard();

		// last move

		if (board.TryGetLastMove(out Move move))
		{
			string moveString = Utils.FromMoveToString(move);
			lastMoveLabel.Text = "Last move: " + moveString;
		}
		else
		{
			lastMoveLabel.Text = "Last move: -";
		}

		// zobrist 

		ulong zobristKey = board.GetZobrist();
		zobristHashNumberLabel.Text = "[" + zobristKey + "] - " + board.GetRepetitions();
		string zobristKeyString = Utils.ConvertULongToBinaryString(zobristKey);
		string zobristKeyStringColored = zobristKeyString.Replace("1", "[color=cd4e53]1[/color]");
		zobristHashNumberBitsLabel.Text = "[center]" + zobristKeyStringColored + "[/center]";
	}

	// on play as white

	private void OnPlayWhite()
	{
		game.PlayAsColor(Piece.Color.White);

		// hide labels

		gameOverLabel.Visible = false;
		gameOverReasonLabel.Visible = false;
	}

	// on play as black

	private void OnPlayBlack()
	{
		game.PlayAsColor(Piece.Color.Black);

		// hide labels

		gameOverLabel.Visible = false;
		gameOverReasonLabel.Visible = false;
	}

	// on play vs human

	private void OnPlayHuman()
	{
		game.PlayAsColor(Piece.Color.White, true);

		// hide labels

		gameOverLabel.Visible = false;
		gameOverReasonLabel.Visible = false;
	}

	// on play ai

	private void OnPlayAI()
	{
		game.PlayAsColor(Piece.Color.None);

		// hide labels

		gameOverLabel.Visible = false;
		gameOverReasonLabel.Visible = false;
	}

	// on quit

	private void OnQuit()
	{
		// quit

		GetTree().Quit();
	}

	// update

	public override void _Process(double delta)
	{

	}
}
