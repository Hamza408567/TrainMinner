using System;
using System.Collections.Generic;
using UnityEngine;

public class PreComputedLoop : MonoBehaviour
{
    public List<GridNode> PrecomputedLoop;

    public void PrecomputeLoop()
    {
        PrecomputedLoop = null;

        int width = GridManager.Instance.levelData.width;
        int height = GridManager.Instance.levelData.height;
        GridNode[,] Grid = GridManager.Instance.Grid;

        // Step 1: Find all boundary cells (walkable cells with 1-2 non-walkable neighbors)
        HashSet<Vector2Int> boundaryCells = new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = Grid[x, y];
                if (node == null || !node.walkable) continue;

                int nonWalkableCount = 0;
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (var dir in dirs)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;

                    if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                    {
                        if (!Grid[nx, ny].walkable) nonWalkableCount++;
                    }
                    else
                    {
                        nonWalkableCount++; // out of bounds is non-walkable
                    }
                }

                if (nonWalkableCount >= 1 && nonWalkableCount <= 2)
                {
                    boundaryCells.Add(node.gridPos);
                }
            }
        }

        if (boundaryCells.Count == 0)
        {
            Debug.LogError("❌ No boundary cells found!");
            return;
        }

        // Step 2: Find top-leftmost boundary cell to start
        Vector2Int startPos = FindTopLeftBoundaryCell(boundaryCells);
        if (!IsValidPos(startPos, width, height))
        {
            Debug.LogError("❌ Could not find start node!");
            return;
        }

        GridNode startNode = Grid[startPos.x, startPos.y];

        // Step 3: Trace the outer boundary using Moore-Neighbor algorithm
        List<GridNode> tracedLoop = TraceBoundaryClockwise(startNode, boundaryCells, Grid, width, height);

        if (tracedLoop == null || tracedLoop.Count < 4)
        {
            Debug.LogError("❌ Could not trace valid loop!");
            return;
        }

        // Step 4: Add corner cells to create orthogonal corners
        tracedLoop = AddCornerCells(tracedLoop, Grid, width, height);

        PrecomputedLoop = tracedLoop;
        Debug.Log($"✅ Loop precomputed successfully! {PrecomputedLoop.Count} nodes.");
    }

    private Vector2Int FindTopLeftBoundaryCell(HashSet<Vector2Int> boundaryCells)
    {
        Vector2Int topLeft = new Vector2Int(int.MaxValue, int.MaxValue);
        foreach (var pos in boundaryCells)
        {
            if (pos.y < topLeft.y || (pos.y == topLeft.y && pos.x < topLeft.x))
                topLeft = pos;
        }
        return topLeft;
    }

    private List<GridNode> TraceBoundaryClockwise(GridNode startNode, HashSet<Vector2Int> boundaryCells, GridNode[,] Grid, int width, int height)
    {
        List<GridNode> loop = new List<GridNode>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int[] mooreNeighbors = new Vector2Int[8]
        {
            new Vector2Int(-1, 1), // Top-Left
            new Vector2Int(0, 1), // Top
            new Vector2Int(1, 1), // Top-Right
            new Vector2Int(1, 0), // Right
            new Vector2Int(1, -1), // Bottom-Right
            new Vector2Int(0, -1), // Bottom
            new Vector2Int(-1, -1), // Bottom-Left
            new Vector2Int(-1, 0) // Left
        };

        Vector2Int currentPos = startNode.gridPos;
        Vector2Int previousPos = currentPos;

        // Find initial direction
        int startDir = 0;
        for (int i = 0; i < 8; i++)
        {
            Vector2Int checkPos = currentPos + mooreNeighbors[i];
            if (boundaryCells.Contains(checkPos))
            {
                startDir = i;
                break;
            }
        }

        int currentDir = startDir;

        do
        {
            if (IsValidPos(currentPos, width, height))
            {
                GridNode currentNode = Grid[currentPos.x, currentPos.y];
                loop.Add(currentNode);
                visited.Add(currentPos);
            }

            bool foundNext = false;

            for (int offset = 7; offset >= 0; offset--)
            {
                int checkDir = (currentDir + offset) % 8;
                Vector2Int checkPos = currentPos + mooreNeighbors[checkDir];

                if (boundaryCells.Contains(checkPos) && checkPos != previousPos)
                {
                    bool isValidMove = true;

                    if (Mathf.Abs(mooreNeighbors[checkDir].x) == 1 && Mathf.Abs(mooreNeighbors[checkDir].y) == 1)
                    {
                        Vector2Int ortho1 = new Vector2Int(mooreNeighbors[checkDir].x, 0);
                        Vector2Int ortho2 = new Vector2Int(0, mooreNeighbors[checkDir].y);

                        GridNode neighbor1 = IsValidPos(currentPos + ortho1, width, height) ? Grid[currentPos.x + ortho1.x, currentPos.y + ortho1.y] : null;
                        GridNode neighbor2 = IsValidPos(currentPos + ortho2, width, height) ? Grid[currentPos.x + ortho2.x, currentPos.y + ortho2.y] : null;

                        if ((neighbor1 != null && neighbor1.walkable) && (neighbor2 != null && neighbor2.walkable))
                        {
                            isValidMove = false;
                        }
                    }

                    if (isValidMove)
                    {
                        previousPos = currentPos;
                        currentPos = checkPos;
                        currentDir = checkDir;
                        foundNext = true;
                        break;
                    }
                }
            }

            if (!foundNext)
            {
                Debug.LogError($"❌ Could not find next boundary cell at {currentPos}");
                return null;
            }

            if (loop.Count > boundaryCells.Count * 2)
            {
                Debug.LogError("❌ Loop tracing took too long - possible infinite loop");
                return null;
            }

        } while (currentPos != startNode.gridPos || loop.Count < 4);

        if (loop.Count > 0 && loop[0] != loop[loop.Count - 1])
        {
            loop.Add(loop[0]);
        }

        return loop;
    }

    private List<GridNode> AddCornerCells(List<GridNode> loop, GridNode[,] Grid, int width, int height)
    {
        if (loop == null || loop.Count < 3) return loop;

        List<GridNode> newLoop = new List<GridNode>();
        
        for (int i = 0; i < loop.Count - 1; i++)
        {
            GridNode current = loop[i];
            GridNode next = loop[i + 1];
            
            // Add current node
            newLoop.Add(current);
            
            // Check if this is a diagonal connection
            int dx = next.gridPos.x - current.gridPos.x;
            int dy = next.gridPos.y - current.gridPos.y;
            
            // If it's a diagonal move (both dx and dy are non-zero)
            if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1)
            {
                // We need to add a corner cell to make an orthogonal corner
                // We have two options for the corner: (current.x, next.y) or (next.x, current.y)
                // Choose the one that's walkable and connects properly
                
                Vector2Int cornerPos1 = new Vector2Int(current.gridPos.x, next.gridPos.y);
                Vector2Int cornerPos2 = new Vector2Int(next.gridPos.x, current.gridPos.y);
                
                GridNode corner1 = IsValidPos(cornerPos1, width, height) ? Grid[cornerPos1.x, cornerPos1.y] : null;
                GridNode corner2 = IsValidPos(cornerPos2, width, height) ? Grid[cornerPos2.x, cornerPos2.y] : null;
                
                // Check if the corner cell exists and is walkable
                if (corner1 != null && corner1.walkable)
                {
                    // Also check if it would create a valid orthogonal path
                    if (IsOrthogonalConnection(current.gridPos, cornerPos1) && 
                        IsOrthogonalConnection(cornerPos1, next.gridPos))
                    {
                        newLoop.Add(corner1);
                    }
                }
                else if (corner2 != null && corner2.walkable)
                {
                    // Also check if it would create a valid orthogonal path
                    if (IsOrthogonalConnection(current.gridPos, cornerPos2) && 
                        IsOrthogonalConnection(cornerPos2, next.gridPos))
                    {
                        newLoop.Add(corner2);
                    }
                }
            }
        }
        
        // Add the last node (which should be the same as first for closed loop)
        if (loop.Count > 0)
        {
            newLoop.Add(loop[loop.Count - 1]);
        }
        
        // Remove any consecutive duplicates
        newLoop = RemoveConsecutiveDuplicates(newLoop);
        
        return newLoop;
    }
    
    private bool IsOrthogonalConnection(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);
        
        // Orthogonal connection means moving in only one axis
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }
    
    private List<GridNode> RemoveConsecutiveDuplicates(List<GridNode> loop)
    {
        if (loop == null || loop.Count <= 1) return loop;
        
        List<GridNode> cleaned = new List<GridNode>();
        cleaned.Add(loop[0]);
        
        for (int i = 1; i < loop.Count; i++)
        {
            if (loop[i] != loop[i - 1])
            {
                cleaned.Add(loop[i]);
            }
        }
        
        return cleaned;
    }

    private bool IsValidPos(Vector2Int pos, int width, int height)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height;
    }

    private void OnDrawGizmos()
    {
        if (PrecomputedLoop != null && PrecomputedLoop.Count > 1)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < PrecomputedLoop.Count - 1; i++)
            {
                if (PrecomputedLoop[i] != null && PrecomputedLoop[i + 1] != null)
                {
                    Vector3 start = PrecomputedLoop[i].worldPos + Vector3.up * 0.2f;
                    Vector3 end = PrecomputedLoop[i + 1].worldPos + Vector3.up * 0.2f;
                    Gizmos.DrawLine(start, end);
                    
                    // Draw corner nodes in yellow
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(start, 0.1f);
                    Gizmos.color = Color.magenta;
                }
            }
            
            // Draw start node in green
            if (PrecomputedLoop.Count > 0 && PrecomputedLoop[0] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(PrecomputedLoop[0].worldPos + Vector3.up * 0.2f, 0.15f);
            }
        }
    }
}