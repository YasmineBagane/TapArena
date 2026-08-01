using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SnakeGameManager : MonoBehaviour
{
    public static SnakeGameManager Instance { get; private set; }

    public int Score { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsGameOver { get; private set; }

    [Header("Board")]
    [SerializeField] private int width = 28;
    [SerializeField] private int height = 18;
    [SerializeField] private float cellSize = 0.5f;
    [SerializeField] private float stepSeconds = 0.12f;
    [SerializeField] private float roundSeconds = 90f;

    [Header("Colors")]
    [SerializeField] private Color boardColor = new Color(0.025f, 0.055f, 0.12f, 1f);
    [SerializeField] private Color gridColor = new Color(0.10f, 0.20f, 0.36f, 0.55f);
    [SerializeField] private Color borderColor = new Color(0.0f, 0.85f, 1f, 1f);
    [SerializeField] private Color snakeHeadColor = new Color(0.52f, 1f, 0.17f, 1f);
    [SerializeField] private Color snakeBodyColor = new Color(0.22f, 0.86f, 0.14f, 1f);
    [SerializeField] private Color foodColor = new Color(1f, 0.17f, 0.15f, 1f);

    private SnakeController snake;
    private SnakeUIController ui;
    private Transform boardRoot;
    private Transform snakeRoot;
    private Transform foodTransform;
    private Sprite squareSprite;
    private Sprite circleSprite;
    private Vector2Int foodCell;
    private float stepTimer;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public float StepSeconds => stepSeconds;
    public Vector2Int FoodCell => foodCell;
    public Color SnakeHeadColor => snakeHeadColor;
    public Color SnakeBodyColor => snakeBodyColor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<SnakeGameManager>() != null)
        {
            return;
        }

        GameObject gameObject = new GameObject("Snake Game Manager");
        gameObject.AddComponent<SnakeGameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Application.targetFrameRate = 60;
        CreateSprites();
        BuildCamera();
        BuildBoard();
        BuildSnake();
        ui = SnakeUIController.Create();
        ResetGame();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetGame();
            return;
        }

        if (IsGameOver)
        {
            return;
        }

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            EndGame("Time Up");
            return;
        }

        stepTimer += Time.deltaTime;
        while (stepTimer >= stepSeconds && !IsGameOver)
        {
            stepTimer -= stepSeconds;
            snake.Step();
        }

        ui.UpdateHud(Score, TimeRemaining);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        float x = (cell.x - (width - 1) * 0.5f) * cellSize;
        float y = (cell.y - (height - 1) * 0.5f) * cellSize;
        return new Vector3(x, y, 0f);
    }

    public bool IsOutsideBoard(Vector2Int cell)
    {
        return cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height;
    }

    public GameObject CreateSegmentObject(string segmentName, bool head)
    {
        GameObject segment = new GameObject(segmentName);
        segment.transform.SetParent(snakeRoot, false);
        SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = head ? snakeHeadColor : snakeBodyColor;
        renderer.sortingOrder = head ? 10 : 9;
        segment.transform.localScale = Vector3.one * (cellSize * 0.92f);
        return segment;
    }

    public void EatFood()
    {
        if (IsGameOver)
        {
            return;
        }

        Score += 10;
        snake.GrowPending += 1;
        SpawnFood();
        ui.UpdateHud(Score, TimeRemaining);
    }

    public void EndGame(string reason)
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        ui.ShowGameOver(Score, reason);
    }

    public void ResetGame()
    {
        Score = 0;
        TimeRemaining = roundSeconds;
        IsGameOver = false;
        stepTimer = 0f;
        snake.ResetSnake(new Vector2Int(width / 2, height / 2));
        SpawnFood();
        ui.HideGameOver();
        ui.UpdateHud(Score, TimeRemaining);
    }

    private void SpawnFood()
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!snake.Occupies(cell))
                {
                    emptyCells.Add(cell);
                }
            }
        }

        if (emptyCells.Count == 0)
        {
            EndGame("Board Filled");
            return;
        }

        foodCell = emptyCells[Random.Range(0, emptyCells.Count)];
        foodTransform.position = CellToWorld(foodCell);
    }

    private void BuildCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        camera.orthographic = true;
        camera.orthographicSize = height * cellSize * 0.68f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.backgroundColor = new Color(0.005f, 0.01f, 0.025f, 1f);
    }

    private void BuildBoard()
    {
        boardRoot = new GameObject("Arcade Board").transform;

        GameObject board = CreateSpriteObject("Board Background", boardColor, 0);
        board.transform.SetParent(boardRoot, false);
        board.transform.position = Vector3.zero;
        board.transform.localScale = new Vector3(width * cellSize, height * cellSize, 1f);

        for (int x = 0; x <= width; x++)
        {
            GameObject line = CreateSpriteObject("Vertical Grid Line", gridColor, 1);
            line.transform.SetParent(boardRoot, false);
            float worldX = (x - width * 0.5f) * cellSize;
            line.transform.position = new Vector3(worldX, 0f, 0f);
            line.transform.localScale = new Vector3(0.015f, height * cellSize, 1f);
        }

        for (int y = 0; y <= height; y++)
        {
            GameObject line = CreateSpriteObject("Horizontal Grid Line", gridColor, 1);
            line.transform.SetParent(boardRoot, false);
            float worldY = (y - height * 0.5f) * cellSize;
            line.transform.position = new Vector3(0f, worldY, 0f);
            line.transform.localScale = new Vector3(width * cellSize, 0.015f, 1f);
        }

        CreateBorder("Top Border", new Vector3(0f, height * cellSize * 0.5f + 0.08f, 0f), new Vector3(width * cellSize + 0.25f, 0.12f, 1f));
        CreateBorder("Bottom Border", new Vector3(0f, -height * cellSize * 0.5f - 0.08f, 0f), new Vector3(width * cellSize + 0.25f, 0.12f, 1f));
        CreateBorder("Left Border", new Vector3(-width * cellSize * 0.5f - 0.08f, 0f, 0f), new Vector3(0.12f, height * cellSize + 0.25f, 1f));
        CreateBorder("Right Border", new Vector3(width * cellSize * 0.5f + 0.08f, 0f, 0f), new Vector3(0.12f, height * cellSize + 0.25f, 1f));

        foodTransform = CreateSpriteObject("Food", foodColor, 8).transform;
        foodTransform.localScale = Vector3.one * (cellSize * 0.78f);
        foodTransform.GetComponent<SpriteRenderer>().sprite = circleSprite;
    }

    private void CreateBorder(string objectName, Vector3 position, Vector3 scale)
    {
        GameObject border = CreateSpriteObject(objectName, borderColor, 3);
        border.transform.SetParent(boardRoot, false);
        border.transform.position = position;
        border.transform.localScale = scale;
    }

    private void BuildSnake()
    {
        snakeRoot = new GameObject("Snake").transform;
        snake = gameObject.AddComponent<SnakeController>();
        snake.Initialize(this);
    }

    private GameObject CreateSpriteObject(string objectName, Color color, int sortingOrder)
    {
        GameObject spriteObject = new GameObject(objectName);
        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return spriteObject;
    }

    private void CreateSprites()
    {
        squareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        Texture2D circle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        circle.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(31.5f, 31.5f);
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(32f - distance);
                circle.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        circle.Apply();
        circleSprite = Sprite.Create(circle, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
    }
}