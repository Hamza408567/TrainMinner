using System.Collections.Generic;
using UnityEngine;

public class TrainPathfinder : MonoBehaviour
{
    public static TrainPathfinder Instance;

    private void Awake()
    {
        Instance = this;
    }

    public List<GridNode> FindPath(GridNode start, GridNode target)
    {
        if (start == null || target == null)
            return null;

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

                queue.Enqueue(neighbor);
                cameFrom[neighbor] = current;
            }
        }

        // No path
        if (!cameFrom.ContainsKey(target))
            return null;

        return ReconstructPath(cameFrom, target);
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

    // Will be called later when stone breaks
    public void RecalculatePath()
    {
        //Hooked later by TrainController
    }
    /*private void OnDrawGizmos()
    {
        if (_lastPath == null) return;

        Gizmos.color = Color.cyan;
        foreach (var node in _lastPath)
            Gizmos.DrawSphere(node.worldPos + Vector3.up * 0.2f, 0.1f);
    }*/

}