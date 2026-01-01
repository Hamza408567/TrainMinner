using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TrainMiner/Level Data")]
public class LevelData : ScriptableObject
{
    public int width;
    public int height;

    // Blocked grid positions
    public List<Vector2Int> blockedCells;
}