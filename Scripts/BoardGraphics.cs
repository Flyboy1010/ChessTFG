using Godot;
using System;

public partial class BoardGraphics : Node2D
{
    // enum with the zindex values that sprites could have

    private enum SpriteZIndex : int
    {
        PieceDeselected = 0,
        PieceSelected = 1
    }

	// texture with the pieces

	[Export] private Texture2D piecesTexture;
	[Export] private int squareSize;

	// board 

	private Board board;

	// array with the sprites representing the pieces

	private Sprite2D[] piecesSprites = new Sprite2D[64];

    // board flipped flag

    private bool isBoardFlipped = false;

    // the size of a piece inside the texture (in pixels)

    private Vector2I pieceTextureSize;

    // selected piece index

    private int pieceSelectedIndex = 0;

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

    // create the graphics

    private void CreateGraphics()
    {
        // we need to adjust the pieceSprite scale so it matches the size of the squareSize

        Vector2 pieceScale = new Vector2(squareSize, squareSize) / (Vector2)pieceTextureSize;

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

                SetSpritePiece(pieceSprite, piece);

                Vector2 pieceSpritePosition = isBoardFlipped ? new Vector2((7 - i) + 0.5f, (7 - j) + 0.5f) : new Vector2(i + 0.5f, j + 0.5f);

                pieceSprite.Position = pieceSpritePosition * squareSize;
            }
        }
    }

    // set sprite piece

    private void SetSpritePiece(Sprite2D pieceSprite, Piece piece)
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

    // select piece

    public void SelectPiece(int index)
    {
        pieceSelectedIndex = index;
    }

    // update selected piece

    public void UpdateSelectedPiece()
    {
        // get the mouse coordinates

        Vector2 mouse = GetLocalMousePosition();

        // get the piece sprite & update its position and zindex

        Sprite2D pieceSelected = piecesSprites[pieceSelectedIndex];
        pieceSelected.ZIndex = (int)SpriteZIndex.PieceSelected;
        pieceSelected.Position = mouse;
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

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
	{
        // get the mouse coordinates

        Vector2 mouse = GetLocalMousePosition();

        // get the square the mouse is on

        bool isOnSquare = TryGetSquareIndexFromCoords(mouse, out int squareIndex);

        // the first frame you click

        if (Input.IsActionJustPressed("Select"))
        {
            if (isOnSquare)
            {
                // select a piece

                SelectPiece(squareIndex);
            }
        }

        // if you hold

        if (Input.IsActionPressed("Select"))
        {
            // update the selected piece

            UpdateSelectedPiece();
        }

        // the frame you release

        if (Input.IsActionJustReleased("Select"))
        {
            if (isOnSquare)
            {
                // make the move

                Move move = new Move()
                {
                    squareSourceIndex = pieceSelectedIndex,
                    squareTargetIndex = squareIndex
                };

                board.MakeMove(move);
            }

            // update the graphics

            UpdateGraphics();
        }
    }
}
