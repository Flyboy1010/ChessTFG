using Godot;
using System;

public abstract class Player
{
	// on move chosen action

	public event System.Action<Move, bool> onMoveChosen;

	// updates

	public abstract void Update();

	// called when its the player's  turn

	public abstract void NotifyTurnToMove();

	// 

	protected virtual void ChoseMove(Move move, bool isAnimated)
	{
		onMoveChosen?.Invoke(move, isAnimated);
	}
}
