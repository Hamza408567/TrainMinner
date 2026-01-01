using System.Collections.Generic;
using UnityEngine;

public class TrainLoopFinder : MonoBehaviour
{
    public static TrainLoopFinder Instance;

    private List<GridNode> loop;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Finds and returns a closed loop of walkable GridNodes.
    /// </summary>
    public List<GridNode> FindLoop()
    {
        loop = null;

        foreach (GridNode start in GridManager.Instance.Grid.Values)
        {
            if (!start.walkable)
                continue;

            List<GridNode> path = TraceLoop(start);
            if (path != null && path.Count > 3)
            {
                loop = path;
                Debug.Log("✅ Loop found with " + loop.Count + " nodes");
                return loop;
            }
        }

        Debug.LogError("❌ No loop found in grid");
        return null;
    }

    private List<GridNode> TraceLoop(GridNode start)
    {
        HashSet<GridNode> visited = new HashSet<GridNode>();
        List<GridNode> path = new List<GridNode>();

        GridNode current = start;
        GridNode previous = null;

        while (true)
        {
            if (visited.Contains(current))
            {
                if (current == start && path.Count > 3)
                    return path;

                return null;
            }

            visited.Add(current);
            path.Add(current);

            List<GridNode> neighbors = GridManager.Instance.GetNeighbors(current);

            // Remove where we came from
            if (previous != null)
                neighbors.Remove(previous);

            if (neighbors.Count != 1)
                return null;

            previous = current;
            current = neighbors[0];
        }
    }

    private void OnDrawGizmos()
    {
        if (loop == null)
            return;

        Gizmos.color = Color.magenta;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 from = loop[i].worldPos + Vector3.up * 0.2f;
            Vector3 to = loop[(i + 1) % loop.Count].worldPos + Vector3.up * 0.2f;
            Gizmos.DrawLine(from, to);
        }
    }
}
