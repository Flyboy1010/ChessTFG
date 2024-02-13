using Godot;
using System;
using System.Collections.Generic;
using static Board;

public partial class BoardGraphics : Node2D
{
    // enum with the zindex values that sprites could have

    private enum SpriteZIndex : int
    {
        PieceDeselected = 0,
        PieceSelected = 1
    }

	// exported variables

	[Export] private int squareSize;
	[Export] private Texture2D piecesTexture;
    [Export] private Color lastMoveColor;
    [Export] private Material hintCircleMaterial;
    [Export] private Material hintCircleWithHoleMaterial;
    [Export] private float animationTime;

	// board 

	private Board board;

	// array with the sprites representing the pieces

	private Sprite2D[] piecesSprites = new Sprite2D[64];

    // array with the "sprites"(color rects) that represent the hints for the moves

    private ColorRect[] hintsSprites = new ColorRect[64];

    // board flipped flag

    private bool isBoardFlipped = false;

    // the size of a piece inside the texture (in pixels)

    private Vector2I pieceTextureSize;

    // tween for animating the pieces

    private Tween tween;

    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
	{
        // init

        pieceTextureSize = (Vector2I)piecesTexture.GetSize() / new Vector2I(6, 2);

        // create the graphics

        CreateGraphics();
	}

	// set board

	public void ConnectToBoard(Board board)
	{
		this.board = board;
	}

    // get piece sprite

    public Sprite2D GetPieceSprite(int index)
    {
        return piecesSprites[index];
    }

    // set sprite piece

    private void SetPieceSprite(Sprite2D pieceSprite, Piece piece)
    {
        if (piece.type == Piece.Type.None)
        {
            pieceSprite.Visible = false;
        }
        else
        {
            // set the region the piece is inside the pieces texture

            pieceSprite.RegionRect = new Rect2(pieceTextureSize.X * ((int)piece.type - 1), pieceTextureSize.Y * ((int)piece.color - 1), pieceTextureSize.X, pieceTextureSize.Y);
            pieceSprite.Visible = true;
        }
    }

    // get square index at x, y world coordinates

    public bool TryGetSquareIndexFromCoords(Vector2 coords, out int squareIndex)
    {
        Vector2I square = (Vector2I)(coords / squareSize).Floor();

        squareIndex = square.X + square.Y * 8;

        if (isBoardFlipped)
        {
            squareIndex = 63 - squareIndex;
        }

        return (square.X >= 0 && square.X < 8 && square.Y >= 0 && square.Y < 8);
    }

    // create the graphics

    private void CreateGraphics()
    {
        // we need to adjust the pieceSprite scale so it matches the size of the squareSize

        Vector2 pieceScale = new Vector2(squareSize, squareSize) / (Vector2)pieceTextureSize;

        // create hints sprites

        for (int j = 0; j < 8; j++)
        {
            for (int i = 0; i < 8; i++)
            {
                ColorRect hintSprite = new ColorRect();
                hintSprite.Size = new Vector2(squareSize, squareSize);
                hintSprite.Position = new Vector2(i, j) * squareSize;
                hintSprite.Visible = false;

                hintsSprites[i + j * 8] = hintSprite;
                AddChild(hintSprite);
            }
        }

        // create pieces sprites

        for (int j = 0; j < 8; j++)
        {
            for (int i = 0; i < 8; i++)
            {
                Sprite2D pieceSprite = new Sprite2D();
                pieceSprite.RegionEnabled = true;
                pieceSprite.Visible = false;
                pieceSprite.ZIndex = (int)SpriteZIndex.PieceDeselected;
                pieceSprite.TextureFilter = TextureFilterEnum.LinearWithMipmapsAnisotropic;
                pieceSprite.Texture = piecesTexture;
                pieceSprite.Scale = pieceScale;

                piecesSprites[i + j * 8] = pieceSprite;
                AddChild(pieceSprite);
            }
        }
    }

    // update the sprites

    public void UpdateGraphics()
    {
        for (int j = 0; j < 8; j++)
        {
            for (int i = 0; i < 8; i++)
            {
                int index = i + j * 8;

                // update the pieces sprites to match the ones in the board

                Sprite2D pieceSprite = piecesSprites[index];
                pieceSprite.ZIndex = (int)SpriteZIndex.PieceDeselected;
                Piece piece = board.GetPiece(index);

                SetPieceSprite(pieceSprite, piece);

                Vector2 pieceSpritePosition = isBoardFlipped ? new Vector2((7 - i) + 0.5f, (7 - j) + 0.5f) : new Vector2(i + 0.5f, j + 0.5f);

                pieceSprite.Position = pieceSpritePosition * squareSize;

                // hint moves sprites

                ColorRect hintSprite = hintsSprites[index];
                Vector2 hintSpritePosition = isBoardFlipped ? new Vector2(7 - i, 7 - j) : new Vector2(i, j);
                hintSprite.Position = hintSpritePosition * squareSize;
            }
        }
    }

    // update selected piece

    public void SetPieceSpritePosition(int index, Vector2 position)
    {
        // get the piece sprite & update its position and zindex

        Sprite2D pieceSprite = piecesSprites[index];
        pieceSprite.ZIndex = (int)SpriteZIndex.PieceSelected;
        pieceSprite.Position = position;
    }

    // set hint moves

    public void SetHintMoves(List<Move> moves)
    {
        // hide previous hint moves

        for (int i = 0; i < 64; i++)
        {
            hintsSprites[i].Visible = false;
        }

        // show the new ones if not null

        if (moves != null)
        {
            foreach (Move move in moves)
            {
                Material m = move.pieceTarget.type == Piece.Type.None ? hintCircleMaterial : hintCircleWithHoleMaterial;

                hintsSprites[move.squareTargetIndex].Material = m;
                hintsSprites[move.squareTargetIndex].Visible = true;
            }
        }
    }

    // play move

    public void AnimateMove(Move move, bool isAnimated, Callable onFinish)
    {
        // kill previous tween

        if (tween != null)
        {
            tween.Kill();
        }

        // create new tween

        tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetParallel(true);

        // get the source piece sprite

        Sprite2D pieceSprite = piecesSprites[move.squareSourceIndex];

        // "select it"

        pieceSprite.ZIndex = (int)SpriteZIndex.PieceSelected;

        // calculate the position of the target square (in grid space)

        int i = move.squareTargetIndex % 8;
        int j = move.squareTargetIndex / 8;

        if (isBoardFlipped)
        {
            i = 7 - i;
            j = 7 - j;
        }

        // check if should be animated or not

        if (isAnimated)
        {
            // tween the piece position to the center of the target square

            tween.TweenProperty(pieceSprite, "position", new Vector2(i + 0.5f, j + 0.5f) * squareSize, animationTime);
        }
        else
        {
            // instantly move the piece to that square

            pieceSprite.Position = new Vector2(i + 0.5f, j + 0.5f) * squareSize;
        }

        // if the move is castling you need to animate the tower aswell

        if (move.flags == Move.Flags.CastleShort)
        {
            int rookTargetSquareIndex = 0;
            Sprite2D towerPieceSprite = null;

            switch (move.pieceSource.color)
            {
                case Piece.Color.White:
                    rookTargetSquareIndex = F1;
                    towerPieceSprite = piecesSprites[H1];
                    break;
                case Piece.Color.Black:
                    rookTargetSquareIndex = F8;
                    towerPieceSprite = piecesSprites[H8];
                    break;
            }

            towerPieceSprite.ZIndex = (int)SpriteZIndex.PieceSelected + 1;

            int rook_i = rookTargetSquareIndex % 8;
            int rook_j = rookTargetSquareIndex / 8;

            if (isBoardFlipped)
            {
                rook_i = 7 - rook_i;
                rook_j = 7 - rook_j;
            }

            tween.TweenProperty(towerPieceSprite, "position", new Vector2(rook_i + 0.5f, rook_j + 0.5f) * squareSize, animationTime);
        }
        else if (move.flags == Move.Flags.CastleLong)
        {
            int rookTargetSquareIndex = 0;
            Sprite2D towerPieceSprite = null;

            switch (move.pieceSource.color)
            {
                case Piece.Color.White:
                    rookTargetSquareIndex = D1;
                    towerPieceSprite = piecesSprites[A1];
                    break;
                case Piece.Color.Black:
                    rookTargetSquareIndex = D8;
                    towerPieceSprite = piecesSprites[A8];
                    break;
            }

            towerPieceSprite.ZIndex = (int)SpriteZIndex.PieceSelected + 1;

            int rook_i = rookTargetSquareIndex % 8;
            int rook_j = rookTargetSquareIndex / 8;

            if (isBoardFlipped)
            {
                rook_i = 7 - rook_i;
                rook_j = 7 - rook_j;
            }

            tween.TweenProperty(towerPieceSprite, "position", new Vector2(rook_i + 0.5f, rook_j + 0.5f) * squareSize, animationTime);
        }

        // at the end of the animation update the pieces & call on finish callback

        tween.Chain().TweenCallback(onFinish);
    }

    // draw

    public override void _Draw()
    {
        // draw last move

        if (board.TryGetLastMove(out Move lastMove))
        {
            int si = lastMove.squareSourceIndex % 8;
            int sj = lastMove.squareSourceIndex / 8;
            int ti = lastMove.squareTargetIndex % 8;
            int tj = lastMove.squareTargetIndex / 8;

            if (isBoardFlipped)
            {
                si = 7 - si;
                sj = 7 - sj;
                ti = 7 - ti;
                tj = 7 - tj;
            }

            DrawRect(new Rect2(new Vector2(si, sj) * squareSize, squareSize, squareSize), lastMoveColor);
            DrawRect(new Rect2(new Vector2(ti, tj) * squareSize, squareSize, squareSize), lastMoveColor);
        }

        //// draw pieces indices

        //List<int> piecesIndicesWhite = board.GetPiecesIndices(Piece.Color.White);
        //List<int> piecesIndicesBlack = board.GetPiecesIndices(Piece.Color.Black);

        //foreach (int i in piecesIndicesWhite)
        //{
        //    int x = i % 8;
        //    int y = i / 8;
        //    DrawRect(new Rect2(x * squareSize, y * squareSize, squareSize, squareSize), Color.Color8(0, 255, 0, 80));
        //}

        //foreach (int i in piecesIndicesBlack)
        //{
        //    int x = i % 8;
        //    int y = i / 8;
        //    DrawRect(new Rect2(x * squareSize, y * squareSize, squareSize, squareSize), Color.Color8(255, 0, 0, 80));
        //}
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
	{
        QueueRedraw();
    }
}
