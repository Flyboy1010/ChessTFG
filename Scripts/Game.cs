using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Game : Node
{
	// game over reasons

	public enum GameOverReason
	{
		WhiteMated,
		BlackMated,
		Drowned,
		Repetition,
		FiftyMoveRule
	}

	// game state machine

	private enum GameState
	{
		PlayerTurn,
		WaitUntilCompleted,
		NextTurn,
		Over
	}

	// signals

	[Signal] public delegate void OnGameTurnEventHandler();
	[Signal] public delegate void OnGameOverEventHandler(GameOverReason gameOverReason);

	// board class that contains everything related to the pieces

	private Board board;

	// graphical representation of a board

	private BoardGraphics boardGraphics;

	// ui

	private UI ui;

	// state of the game and boardUI

	private GameState gameState = GameState.NextTurn;

	// players

	private Player playerWhite;
	private Player playerBlack;

	// current player

	private Player playerToMove;

	// audio

	private AudioStreamPlayer audioPlayerWhite;
	private AudioStreamPlayer audioPlayerBlack;
	private Dictionary<string, AudioStream> audioStreams = new Dictionary<string, AudioStream>();

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		// get the nodes

		boardGraphics = GetNode<BoardGraphics>("BoardGraphics");
		ui = GetNode<UI>("UI");
		audioPlayerWhite = GetNode<AudioStreamPlayer>("AudioPlayerWhite");
		audioPlayerBlack = GetNode<AudioStreamPlayer>("AudioPlayerBlack");

		// init the board and load the fen

		board = new Board();
		board.LoadFenString(Board.StartFEN);

		// connect the board graphical representation with the board itself

		boardGraphics.ConnectToBoard(board);

		// connect the ui to the game

		ui.ConnectToGame(this);

		// update the board graphics

		boardGraphics.UpdateGraphics();

		// audio

		audioStreams.Add("move", GD.Load<AudioStream>("res://Assets/Sounds/move.ogg"));
		audioStreams.Add("capture", GD.Load<AudioStream>("res://Assets/Sounds/capture.ogg"));

		// create players

		playerWhite = new PlayerHuman(board, boardGraphics);
		playerWhite.onMoveChosen += OnMoveChosen;
		playerBlack = new PlayerAI(board);
		playerBlack.onMoveChosen += OnMoveChosen;
	}

	// get the board

	public Board GetBoard()
	{
		return board;
	}

	// play sound

	private void PlaySound(Move move)
	{
		AudioStreamPlayer audioPlayer = move.pieceSource.color == Piece.Color.White ? audioPlayerWhite : audioPlayerBlack;

		switch (move.flags)
		{
			case Move.Flags.EnPassant:
				audioPlayer.Stream = audioStreams["capture"];
				break;
			default:
				audioPlayer.Stream = move.pieceTarget.type == Piece.Type.None ? audioStreams["move"] : audioStreams["capture"];
				break;
		}

		audioPlayer.Play();
	}

	// play as color

	public void PlayAsColor(Piece.Color color, bool areBothHumans = false)
	{
		// board reset position

		board.LoadFenString(Board.StartFEN);

		// set game state to next turn

		gameState = GameState.NextTurn;

		// unsubscribe from previous players on-move events

		playerWhite.onMoveChosen -= OnMoveChosen;
		playerBlack.onMoveChosen -= OnMoveChosen;

		// check if white or black

		switch (color)
		{
			case Piece.Color.White:
				playerWhite = new PlayerHuman(board, boardGraphics);
				playerBlack = areBothHumans ? new PlayerHuman(board, boardGraphics) : new PlayerAI(board);
				boardGraphics.FlipBoard(false);
				break;
			case Piece.Color.Black:
				playerWhite = areBothHumans ? new PlayerHuman(board, boardGraphics) : new PlayerAI(board);
				playerBlack = new PlayerHuman(board, boardGraphics);
				boardGraphics.FlipBoard(true);
				break;
			case Piece.Color.None:
				playerWhite = new PlayerAI(board);
				playerBlack = new PlayerAI(board);
				break;
		}

		// subscribe to new players on-move events

		playerWhite.onMoveChosen += OnMoveChosen;
		playerBlack.onMoveChosen += OnMoveChosen;

		// reset board graphics

		boardGraphics.SetCheckIndicatorIndex(-1);
		boardGraphics.SelectSquare(-1);
		boardGraphics.SetHintMoves(null);
		boardGraphics.UpdateGraphics();
	}

	// on move chosen

	private void OnMoveChosen(Move move, bool isAnimated)
	{
		// make the move & calculate zobrist

		board.MakeMove(move);

		// disable highlight square

		boardGraphics.HightlightSquare(-1);

		// disable square selection

		boardGraphics.SelectSquare(-1);

		// disable hint moves

		boardGraphics.SetHintMoves(null);

		// animate the move

		boardGraphics.AnimateMove(move, isAnimated, Callable.From(() =>
		{
			boardGraphics.UpdateGraphics();
			gameState = GameState.NextTurn;
		}));

		// play sound

		PlaySound(move);

		// change state

		gameState = GameState.WaitUntilCompleted;
	}

	// update game state

	private void UpdateState()
	{
		switch (gameState)
		{
			case GameState.NextTurn:
				// emit signal

				EmitSignal(SignalName.OnGameTurn);

				// get turn color

				Piece.Color turnColor = board.GetTurnColor();

				// check if the game is over

				bool isGameOver = false;

				// check if the current player has any moves available

				List<Move> moves = MoveGeneration.GetAllLegalMovesByColor(board, turnColor);

				if (moves.Count == 0)
				{
					if (MoveGeneration.IsKingInCheck(board, turnColor))
					{
						switch (turnColor)
						{
							case Piece.Color.White:
								EmitSignal(SignalName.OnGameOver, (int)GameOverReason.WhiteMated);
								break;
							case Piece.Color.Black:
								EmitSignal(SignalName.OnGameOver, (int)GameOverReason.BlackMated);
								break;
						}
					}
					else
					{
						EmitSignal(SignalName.OnGameOver, (int)GameOverReason.Drowned);
					}

					isGameOver = true;
				}
				else
				{
					// 3 fold repetition

					if (board.GetRepetitions() >= 3)
					{
						EmitSignal(SignalName.OnGameOver, (int)GameOverReason.Repetition);
						isGameOver = true;
					}
					else if (board.GetHalfMoveCount() >= 100)// fifty move
					{
						EmitSignal(SignalName.OnGameOver, (int)GameOverReason.FiftyMoveRule);
						isGameOver = true;
					}
				}

				// check if the games continues

				if (!isGameOver)
				{
					// next player turn

					playerToMove = turnColor == Piece.Color.White ? playerWhite : playerBlack;

					// notify player

					playerToMove.NotifyTurnToMove();

					// change game state

					gameState = GameState.PlayerTurn;
				}
				else
				{
					// change game state

					gameState = GameState.Over;
				}
				break;
			case GameState.PlayerTurn:
				// update player to move

				playerToMove.Update();
				break;
			case GameState.WaitUntilCompleted:
				break;
			case GameState.Over:
				break;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.

	public override void _Process(double delta)
	{
		UpdateState();
	}
}
