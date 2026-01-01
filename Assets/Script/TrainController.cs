using System.Collections.Generic;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotateSpeed = 10f;

    [Header("Grid Positions")]
    public Vector2Int startGridPos;
    public Vector2Int targetGridPos;

    private List<GridNode> currentPath;
    private int pathIndex;
    private bool isMoving;

    private void Start()
    {
        CalculatePath();
    }

    private void Update()
    {
        if (!isMoving || currentPath == null || pathIndex >= currentPath.Count)
            return;

        MoveAlongPath();
    }

    void CalculatePath()
    {
        GridNode startNode = GridManager.Instance.GetNode(startGridPos);
        GridNode targetNode = GridManager.Instance.GetNode(targetGridPos);

        currentPath = TrainPathfinder.Instance.FindPath(startNode, targetNode);
        pathIndex = 0;

        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning("No path found for train");
            isMoving = false;
            return;
        }

        isMoving = true;
    }

    void MoveAlongPath()
    {
        GridNode targetNode = currentPath[pathIndex];
        Vector3 targetPos = targetNode.worldPos;

        // Move
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        // Rotate
        Vector3 dir = (targetPos - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rot,
                rotateSpeed * Time.deltaTime
            );
        }

        // Reached node
        if (Vector3.Distance(transform.position, targetPos) < 0.05f)
        {
            pathIndex++;
        }
    }

    // 🔥 This will be called later when stone breaks
    public void SetNewPath(List<GridNode> newPath)
    {
        if (newPath == null || newPath.Count == 0)
            return;

        currentPath = newPath;
        pathIndex = 0;
        isMoving = true;
    }
}
