using System.Collections.Generic;
using UnityEngine;

public class TrainPathfinder : MonoBehaviour
{
    public static TrainPathfinder Instance;

    private List<GridNode> lastPath;

    private void Awake()
    {
        Instance = this;
    }

    public List<GridNode> FindPath(GridNode start, GridNode target)
    {
        if (start == null || target == null)
        {
            Debug.LogWarning("Start or Target node is null");
            return null;
        }

        if (!start.walkable || !target.walkable)
        {
            Debug.LogWarning("Start or Target is not walkable");
            return null;
        }

        Queue<GridNode> queue = new Queue<GridNode>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            GridNode current = queue.Dequeue();

            if (current == target)
                break;

            foreach (GridNode neighbor in GridManager.Instance.GetNeighbors(current))
            {
                if (cameFrom.ContainsKey(neighbor))
                    continue;

                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        if (!cameFrom.ContainsKey(target))
        {
            Debug.LogWarning("❌ Path not found");
            lastPath = null;
            return null;
        }

        lastPath = ReconstructPath(cameFrom, target);
        return lastPath;
    }

    private List<GridNode> ReconstructPath(
        Dictionary<GridNode, GridNode> cameFrom,
        GridNode target)
    {
        List<GridNode> path = new List<GridNode>();
        GridNode current = target;

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }

    private void OnDrawGizmos()
    {
        if (lastPath == null)
            return;

        Gizmos.color = new Color(0.6f, 0f, 0.8f, 1f); // Purple

        foreach (GridNode node in lastPath)
        {
            Gizmos.DrawCube(
                node.worldPos + Vector3.up * 0.2f,
                Vector3.one * 0.5f
            );
        }
    }
}
