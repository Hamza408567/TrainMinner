using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridCell
{
    public int row;
    public int column;
    public bool walkable = true;

    public GridCell(int row, int column, bool walkable = true)
    {
        this.row = row;
        this.column = column;
        this.walkable = walkable;
    }
}

[CreateAssetMenu(menuName = "TrainMiner/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 10;
    public int height = 10;

    public List<GridCell> cells = new List<GridCell>();

    // Initialize grid if empty
    public void InitializeGrid()
    {
        cells.Clear();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells.Add(new GridCell(x, y, false));
            }
        }
    }

    // Get a cell by coordinates
    public GridCell GetCell(int x, int y)
    {
        return cells.Find(c => c.row == x && c.column == y);
    }
}