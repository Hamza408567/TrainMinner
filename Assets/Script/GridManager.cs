using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    [SerializeField] private GameObject stonePrefab;
    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [InlineEditor,SerializeField] private LevelData levelData;
    [SerializeField] private bool showLevelPreview = true;

    [SerializeField] private Color previewWalkable = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color previewBlocked = new Color(1f, 0f, 0f, 0.4f);

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

        for (int x = 0; x < levelData.width; x++)
        {
            for (int y = 0; y < levelData.height; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = transform.position + new Vector3(x * cellSize, 0f, y * cellSize);

                GridNode node = new GridNode(gridPos, worldPos);

                // Use LevelData to set walkable
                GridCell cellData = levelData.GetCell(x, y);
                if (cellData != null)
                {
                    node.walkable = cellData.walkable;

                    // Spawn stone if cell is blocked
                    if (!cellData.walkable && stonePrefab != null)
                    {
                        GameObject stoneObj = Instantiate(stonePrefab, worldPos + Vector3.up * 0.5f, Quaternion.identity, transform);
                        node.stone = stoneObj.GetComponent<Stone>();
                    }
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
       // TrainPathfinder.Instance.RecalculatePath();
    }
    [Button]
    public void DebugNeighbors()
    {
        foreach (var node in Grid.Values)
        {
            if (!node.walkable)
                continue;

            int count = GetNeighbors(node).Count;

            if (count != 2)
            {
                Debug.LogWarning(
                    $"❌ Not loop tile at {node.gridPos} → neighbors: {count}"
                );
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showLevelPreview || levelData == null)
            return;

        for (int x = 0; x < levelData.width; x++)
        {
            for (int y = 0; y < levelData.height; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = transform.position + new Vector3(
                    x * cellSize,
                    0f,
                    y * cellSize
                );

                Vector3 center = worldPos + new Vector3(0, 0.05f, 0);
                Vector3 size = new Vector3(cellSize, 0.1f, cellSize);

                // ✅ Get the cell from LevelData
                GridCell cell = levelData.GetCell(x, y);
                bool blocked = (cell != null) ? !cell.walkable : false; // default walkable

                if (blocked)
                {
                    Gizmos.color = previewBlocked;
                    Gizmos.DrawCube(center, size);

                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(center, size);
                }
                else
                {
                    Gizmos.color = previewWalkable;
                    Gizmos.DrawCube(center, size);

                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }
    }
}