using UnityEngine;

public class GridNode
{
    public Vector2Int gridPos;
    public Vector3 worldPos;

    public bool walkable = true;
    public Stone stone; // null if no stone

    public GridNode(Vector2Int gridPos, Vector3 worldPos)
    {
        this.gridPos = gridPos;
        this.worldPos = worldPos;
    }

    public void BreakStone()
    {
        if (stone != null)
        {
            Object.Destroy(stone.gameObject);
            stone = null;
        }
        walkable = true; // make the cell walkable after stone is broken
    }
}