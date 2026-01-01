using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private LevelData levelData;


    public Dictionary<Vector2Int, GridNode> Grid { get; private set; }

    private void Awake()
    {
        Instance = this;
        GenerateGrid();
    }
    [Button(ButtonSizes.Large)]
    [GUIColor(0.3f, 0.9f, 0.3f)]
    public void GenerateGrid()
    {
        BuildGrid();
        Debug.Log("Grid Generated");
    }
    [Button(ButtonSizes.Large)]
    [GUIColor(1f, 0.4f, 0.4f)]
    public void ClearGrid()
    {
        if (Grid != null)
        {
            Grid.Clear();
            Grid = null;
        }

        Debug.Log("Grid Cleared");
    }
    private void BuildGrid()
    {
        Grid = new Dictionary<Vector2Int, GridNode>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = transform.position + new Vector3(
                    x * cellSize, 0f, y * cellSize
                );

                GridNode node = new GridNode(gridPos, worldPos);

                // INITIAL STATE FROM LEVEL DATA
                if (levelData != null && levelData.blockedCells.Contains(gridPos))
                {
                    node.walkable = false;
                }

                Grid.Add(gridPos, node);
            }
        }
    }

    public GridNode GetNode(Vector2Int pos)
    {
        Grid.TryGetValue(pos, out GridNode node);
        return node;
    }

    public List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();

        Vector2Int[] dirs =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in dirs)
        {
            GridNode neighbor = GetNode(node.gridPos + dir);
            if (neighbor != null && neighbor.walkable)
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    public void OnGridUpdated()
    {
        TrainPathfinder.Instance.RecalculatePath();
    }
    private void OnDrawGizmos()
    {
        if (Grid == null) return;

        foreach (var node in Grid.Values)
        {
            Vector3 center = node.worldPos + new Vector3(0, 0.05f, 0);
            Vector3 size = new Vector3(cellSize, 0.1f, cellSize);

            if (node.walkable)
            {
                // Walkable → green wireframe
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                // Non-walkable → red filled + wireframe
                Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
                Gizmos.DrawCube(center, size);

                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }



}