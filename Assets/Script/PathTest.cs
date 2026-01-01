using UnityEngine;
using System.Collections.Generic;

public class PathTest : MonoBehaviour
{
    public Vector2Int startPos;
    public Vector2Int endPos;

    void Start()
    {
        GridNode start = GridManager.Instance.GetNode(startPos);
        GridNode end = GridManager.Instance.GetNode(endPos);

        List<GridNode> path =
            TrainPathfinder.Instance.FindPath(start, end);

        if (path == null)
        {
            Debug.Log("No path found");
            return;
        }

        foreach (var node in path)
        {
            Debug.Log("Path node: " + node.gridPos);
        }
    }
}