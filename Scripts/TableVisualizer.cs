using Godot;
using System;
using System.Linq;

[Tool]
public partial class TableVisualizer : Node2D
{
	[Export]
	private Piece.Type PieceType
	{
		get => pieceType;
		set
		{
			if (pieceType != value)
			{
				pieceType = value;
				QueueRedraw();
			}
		}
	}
	private Piece.Type pieceType;

	[Export] private Gradient gradient;
	[Export] private int cellSize = 105;
	[Export] private int offset = 8;

	public override void _Draw()
	{
		int[] pieceTable = PieceTables.GetTable(pieceType);
		int max = pieceTable.Max();
		int min = pieceTable.Min();

		for (int j = 0; j < 8; j++)
		{
			for (int i = 0; i < 8; i++)
			{
				int index = i + j * 8;
				float normalizedValue = (float)(pieceTable[index] - min) / (max - min);
				Color color = gradient.Sample(normalizedValue);
				DrawRect(new Rect2(i * cellSize + offset, j * cellSize + offset, cellSize - offset, cellSize - offset), color);
			}
		}
	}
}
