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
    public GridCell(GridCell other)
    {
        row = other.row;
        column = other.column;
        walkable = other.walkable;
    }
}
[CreateAssetMenu(menuName = "TrainMiner/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 10;
    public int height = 10;
    public List<GridCell> cells = new();
    public List<GridCell> DefauldGridCells = new();
    
    public void InitializeGrid()
    {
        DefauldGridCells.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                DefauldGridCells.Add(new GridCell(x, y, false));
            }
        }
    }
    public void OnEnable()
    {
        cells = new List<GridCell>();

        foreach (var cell in DefauldGridCells)
        {
            cells.Add(new GridCell(cell)); // deep copy
        }
    }

    public GridCell GetCell(int x, int y)
    {
        return cells.Find(c => c.row == x && c.column == y);
    }
}