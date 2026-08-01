using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SnakeController : MonoBehaviour
{
    public int GrowPending { get; set; }

    private readonly List<Vector2Int> cells = new List<Vector2Int>();
    private readonly List<Transform> segments = new List<Transform>();
    private SnakeGameManager manager;
    private Vector2Int direction = Vector2Int.right;
    private Vector2Int queuedDirection = Vector2Int.right;

    public void Initialize(SnakeGameManager gameManager)
    {
        manager = gameManager;
    }

    private void Update()
    {
        ReadInput();
    }

    public bool Occupies(Vector2Int cell)
    {
        return cells.Contains(cell);
    }

    public void ResetSnake(Vector2Int startCell)
    {
        foreach (Transform segment in segments)
        {
            if (segment != null)
            {
                Destroy(segment.gameObject);
            }
        }

        cells.Clear();
        segments.Clear();
        direction = Vector2Int.right;
        queuedDirection = Vector2Int.right;
        GrowPending = 2;

        cells.Add(startCell);
        Transform head = manager.CreateSegmentObject("Snake Head", true).transform;
        segments.Add(head);
        SyncSegmentVisuals();
    }

    public void Step()
    {
        direction = queuedDirection;
        Vector2Int nextHead = cells[0] + direction;

        if (manager.IsOutsideBoard(nextHead))
        {
            manager.EndGame("Hit Border");
            return;
        }

        bool willGrow = GrowPending > 0 || nextHead == manager.FoodCell;
        int tailIndex = cells.Count - 1;
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == nextHead && (willGrow || i != tailIndex))
            {
                manager.EndGame("Snake Ate Itself");
                return;
            }
        }

        cells.Insert(0, nextHead);
        if (segments.Count == 0)
        {
            segments.Add(manager.CreateSegmentObject("Snake Head", true).transform);
        }
        else
        {
            segments[0].name = "Snake Body";
            segments[0].GetComponent<SpriteRenderer>().color = manager.SnakeBodyColor;
        }

        Transform newHead = manager.CreateSegmentObject("Snake Head", true).transform;
        segments.Insert(0, newHead);

        if (nextHead == manager.FoodCell)
        {
            manager.EatFood();
        }

        if (GrowPending > 0)
        {
            GrowPending--;
        }
        else
        {
            int last = cells.Count - 1;
            cells.RemoveAt(last);
            Transform tail = segments[last];
            segments.RemoveAt(last);
            Destroy(tail.gameObject);
        }

        SyncSegmentVisuals();
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        Vector2Int requested = queuedDirection;

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            requested = Vector2Int.up;
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            requested = Vector2Int.down;
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            requested = Vector2Int.left;
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            requested = Vector2Int.right;
        }

        if (requested + direction != Vector2Int.zero || cells.Count <= 1)
        {
            queuedDirection = requested;
        }
    }

    private void SyncSegmentVisuals()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].position = manager.CellToWorld(cells[i]);
            segments[i].localScale = Vector3.one * (manager.CellSize * (i == 0 ? 0.96f : 0.86f));
            SpriteRenderer renderer = segments[i].GetComponent<SpriteRenderer>();
            renderer.color = i == 0 ? manager.SnakeHeadColor : manager.SnakeBodyColor;
            renderer.sortingOrder = i == 0 ? 10 : 9;
        }
    }
}