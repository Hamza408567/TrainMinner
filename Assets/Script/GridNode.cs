using UnityEngine;

public class GridNode

{
    public Vector2Int gridPos;
    public Vector3 worldPos;

    public bool walkable = true;
    public bool NewUnlock;
    public Stone stone; // null if no stone

    public GridNode(Vector2Int gridPos, Vector3 worldPos)
    {
        this.gridPos = gridPos;
        this.worldPos = worldPos;
    }
}