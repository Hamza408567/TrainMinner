using System.Collections.Generic;
using UnityEngine;

public class TrainLoopFinder : MonoBehaviour
{
    public static TrainLoopFinder Instance;
    public List<GridNode> Loop { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public List<GridNode> FindLoop()
    {
        Loop = null;

        foreach (GridNode start in GridManager.Instance.Grid.Values)
        {
            if (!start.walkable)
                continue;

            var neighbors = GridManager.Instance.GetNeighbors(start);
            if (neighbors.Count != 2)
                continue;

            // 🔁 Try BOTH directions
            foreach (GridNode firstNext in neighbors)
            {
                List<GridNode> path = WalkClockwise(start);
                if (path != null)
                {
                    Loop = path;
                    Debug.Log("✅ Closed loop found");
                    return Loop;
                }
            }
        }

        Debug.LogError("❌ Loop exists but traversal failed");
        return null;
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
            List<GridNode> neighbors = GridManager.Instance.GetNeighbors(current);

            // Remove previous to avoid going back
            if (previous != null)
                neighbors.Remove(previous);

            if (neighbors.Count == 0)
                return null; // dead end

            // Sort neighbors in clockwise order relative to current node
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

            // If we returned to start, loop complete
            if (next == start && path.Count > 2)
            {
                path.Add(start);
                return path;
            }

            // Already visited → stuck, fail
            if (visited.Contains(next))
                return null;

            path.Add(next);
            visited.Add(next);

            previous = current;
            current = next;
        }
    }


    private void OnDrawGizmos()
    {
        if (Loop == null)
            return;

        Gizmos.color = Color.magenta;

        for (int i = 0; i < Loop.Count - 1; i++)
        {
            Gizmos.DrawLine(
                Loop[i].worldPos + Vector3.up * 0.25f,
                Loop[i + 1].worldPos + Vector3.up * 0.25f
            );
        }
    }
}
