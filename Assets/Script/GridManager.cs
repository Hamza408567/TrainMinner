using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Range(2,10)]
    public int updateAfterStone=4;
    public static GridManager Instance;
    [SerializeField] private PreComputedLoop preComputedLoop;
    [SerializeField] private SpawnRailsAligned railsAligned;
    [SerializeField] private GameObject stonePrefab;

    [Header("Grid Settings")] 
    [SerializeField] private float cellSize = 1f;
    public LevelData levelData;
    [SerializeField] private bool showLevelPreview = true;
    [SerializeField] private Color previewWalkable = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color previewBlocked = new Color(1f, 0f, 0f, 0.4f);

    public GridNode[,] Grid { get; private set; } // 2D array now
    public static event System.Action OnLoopUpdated;
    private int coutstone;
    private void Awake()
    {
        Instance = this;
        preComputedLoop = GetComponent<PreComputedLoop>();
        railsAligned = GetComponent<SpawnRailsAligned>();
        ClearGrid();
        BuildGrid();
        preComputedLoop.PrecomputeLoop();
     Invoke(nameof(Rails),0.1f);
    }

    private void Rails()
    {
        railsAligned.SpawnRails();
    }

    public void GridUpdateAfter4Stonedistroy()
    {
        coutstone++;
        if (coutstone >= updateAfterStone)
        {
            coutstone = 0;
            OnGridUpdated();
        }
    }

    public void OnGridUpdated()
    {
        Debug.LogError("Upgrade grid");
        preComputedLoop.PrecomputeLoop();
        railsAligned.SpawnRails();
        OnLoopUpdated?.Invoke();
    }

    public void ClearGrid()
    {
        if (Grid != null)
        {
            int rows = Grid.GetLength(0);
            int cols = Grid.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (Grid[r, c] != null && Grid[r, c].stone != null)
                    {
                        DestroyImmediate(Grid[r, c].stone.gameObject);
                        Grid[r, c].stone = null;
                    }
                }
            }

            Grid = null;
        }

        Debug.Log("Grid & stones cleared");
    }

    private void BuildGrid()
    {
        if (levelData == null)
        {
            Debug.LogError("LevelData is null! Cannot build grid.");
            return;
        }

        int rows = levelData.width;
        int cols = levelData.height;
        Grid = new GridNode[rows, cols];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                Vector3 worldPos = transform.position + new Vector3(x * cellSize, 0f, y * cellSize);
                GridNode node = new GridNode(new Vector2Int(x, y), worldPos);

                GridCell cellData = levelData.GetCell(x, y); // original LevelData coordinates
                node.walkable = cellData != null ? cellData.walkable : true;

                if (!node.walkable && stonePrefab != null)
                {
                    GameObject tempstone= Instantiate(stonePrefab, worldPos + Vector3.up * 0.5f, Quaternion.identity, transform);
                    node.stone = tempstone.GetComponent<Stone>();
                    tempstone.GetComponent<Stone>().node = node;
                }

                Grid[x, y] = node;
            }
        }

        Debug.Log($"✅ Grid built! Total nodes: {rows * cols}");
    }

    public GridNode GetNode(int row, int col)
    {
        int rows = Grid.GetLength(0);
        int cols = Grid.GetLength(1);

        if (row < 0 || row >= rows || col < 0 || col >= cols) return null;
        return Grid[row, col];
    }

    public List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
        int r = node.gridPos.x;
        int c = node.gridPos.y;

        int rows = Grid.GetLength(0);
        int cols = Grid.GetLength(1);

        Vector2Int[] dirs =
        {
            new Vector2Int(0, 1),  // up
            new Vector2Int(0, -1), // down
            new Vector2Int(-1, 0), // left
            new Vector2Int(1, 0)   // right
        };

        foreach (var dir in dirs)
        {
            int nr = r + dir.x;
            int nc = c + dir.y;
            if (nr >= 0 && nr < rows && nc >= 0 && nc < cols && Grid[nr, nc].walkable)
            {
                neighbors.Add(Grid[nr, nc]);
            }
        }

        return neighbors;
    }

    private void OnDrawGizmos()
    {
        if (!showLevelPreview || levelData == null) return;

        int rows = levelData.width;
        int cols = levelData.height;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                bool isWalkable = true;
                Vector3 pos = transform.position + new Vector3(x * cellSize, 0f, y * cellSize);

                if (Grid != null)
                    isWalkable = Grid[x, y].walkable;
                else
                {
                    GridCell cell = levelData.GetCell(x, y);
                    if (cell != null) isWalkable = cell.walkable;
                }

                Gizmos.color = isWalkable ? previewWalkable : previewBlocked;
                Gizmos.DrawCube(pos + new Vector3(0, 0.05f, 0), new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                Gizmos.color = isWalkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(pos + new Vector3(0, 0.05f, 0), new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
            }
        }
    }
}
