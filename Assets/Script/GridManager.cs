using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private GameObject straightRailPrefab;
    [SerializeField] private GameObject cornerRailPrefab;
    [SerializeField] private Transform railParent; // optional, organize rails under a parent
    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [InlineEditor, SerializeField] private LevelData levelData;
    [SerializeField] private bool showLevelPreview = true;

    [SerializeField] private Color previewWalkable = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color previewBlocked = new Color(1f, 0f, 0f, 0.4f);

    public Dictionary<Vector2Int, GridNode> Grid { get; private set; }

    // ✅ Precomputed loop stored here
    public List<GridNode> PrecomputedLoop { get; private set; }

    private void Awake()
    {
        Instance = this;
        GenerateGrid();
        PrecomputeLoop();
        SpawnRailsAligned();// Compute the loop at start
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
            foreach (var node in Grid.Values)
            {
                if (node.stone != null)
                {
                    DestroyImmediate(node.stone.gameObject);
                    node.stone = null;
                }
            }

            Grid.Clear();
            Grid = null;
        }

        Debug.Log("Grid & stones cleared");
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

                GridCell cellData = levelData.GetCell(x, y);
                if (cellData != null)
                {
                    node.walkable = cellData.walkable;

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
    [ContextMenu("DEBUG LOOP VALIDITY")]
    public void DebugLoopValidity()
    {
        if (Grid == null)
        {
            Debug.LogError("Grid is null");
            return;
        }
        foreach (var node in Grid.Values)
        {
            if (!node.walkable)
                continue;

            int n = GetNeighbors(node).Count;

            if (n != 2)
            {
                Debug.LogError(
                    $"❌ NOT LOOP TILE at {node.gridPos} → neighbors = {n}"
                );
            }
            else
            {
                Debug.Log(
                    $"✔ OK LOOP TILE at {node.gridPos}"
                );
            }
        }
    }
 
    [Button("Spawn Rails with Corners")]
    public void SpawnRailsAligned()
    {
        if (PrecomputedLoop == null || PrecomputedLoop.Count < 3)
        {
            Debug.LogError("Need at least 3 nodes in loop to spawn rails!");
            return;
        }

        // Clear previous rails
        if (railParent != null)
        {
            for (int i = railParent.childCount - 1; i >= 0; i--)
                DestroyImmediate(railParent.GetChild(i).gameObject);
        }

        int count = PrecomputedLoop.Count;

        for (int i = 0; i < count - 1; i++)
        {
            GridNode prev = PrecomputedLoop[(i - 1 + count) % count];
            GridNode current = PrecomputedLoop[i];
            GridNode next = PrecomputedLoop[(i + 1) % count];

            Vector3 dir1 = (current.worldPos - prev.worldPos).normalized;
            Vector3 dir2 = (next.worldPos - current.worldPos).normalized;

            GameObject railPrefabToUse;
            Quaternion rotation;

            // Straight if direction almost same
            // Straight if direction almost same
            if (Vector3.Dot(dir1, dir2) > 0.99f)
            {
                railPrefabToUse = straightRailPrefab;
                rotation = Quaternion.LookRotation(dir2, Vector3.up); // straight rails stay same
            }
            else
            {
                railPrefabToUse = cornerRailPrefab;

                // Determine left/right turn using cross product
                float cross = Vector3.Cross(dir1, dir2).y;

                if (cross > 0) // left turn
                    rotation = Quaternion.LookRotation(dir1, Vector3.up) * Quaternion.Euler(0, 90, 0);
                else           // right turn
                    rotation = Quaternion.LookRotation(dir1, Vector3.up) * Quaternion.Euler(0, -90, 0);
            }


            Vector3 position = current.worldPos;
            GameObject rail = Instantiate(railPrefabToUse, position, rotation, railParent);
            rail.name = $"Rail_{i}_{railPrefabToUse.name}";
        }

        Debug.Log("✅ Rails aligned along loop!");
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

                GridCell cell = levelData.GetCell(x, y);
                bool blocked = (cell != null) ? !cell.walkable : false;

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

        // Draw precomputed loop
        if (PrecomputedLoop != null)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < PrecomputedLoop.Count - 1; i++)
            {
                Gizmos.DrawLine(
                    PrecomputedLoop[i].worldPos + Vector3.up * 0.25f,
                    PrecomputedLoop[i + 1].worldPos + Vector3.up * 0.25f
                );
            }
        }
    }

    // =============================
    // ✅ NEW: Precompute clockwise loop
    // =============================
    public void PrecomputeLoop()
    {
        PrecomputedLoop = null;

        foreach (var node in Grid.Values)
        {
            if (!node.walkable)
                continue;

            List<GridNode> loop = WalkClockwise(node);
            if (loop != null)
            {
                PrecomputedLoop = loop;
                Debug.Log("✅ Loop precomputed successfully!");
                return;
            }
        }

        Debug.LogError("❌ Could not precompute loop!");
    }

    private List<GridNode> WalkClockwise(GridNode start)
    {
        List<GridNode> path = new List<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        GridNode current = start;
        GridNode previous = null;

        path.Add(current);
        visited.Add(current);

        while (true)
        {
            List<GridNode> neighbors = GetNeighbors(current);

            if (previous != null)
                neighbors.Remove(previous);

            if (neighbors.Count == 0)
                return null;

            // Clockwise sort: Right → Down → Left → Up
            neighbors.Sort((a, b) =>
            {
                Vector2Int dirA = a.gridPos - current.gridPos;
                Vector2Int dirB = b.gridPos - current.gridPos;

                int GetDirOrder(Vector2Int dir)
                {
                    if (dir == Vector2Int.right) return 0;
                    if (dir == Vector2Int.down) return 1;
                    if (dir == Vector2Int.left) return 2;
                    if (dir == Vector2Int.up) return 3;
                    return 4;
                }

                return GetDirOrder(dirA).CompareTo(GetDirOrder(dirB));
            });

            GridNode next = neighbors[0];

            if (next == start && path.Count > 2)
            {
                path.Add(start);
                return path;
            }

            if (visited.Contains(next))
                return null;

            path.Add(next);
            visited.Add(next);

            previous = current;
            current = next;
        }
    }
}
