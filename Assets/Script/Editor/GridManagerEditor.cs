using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector first
        DrawDefaultInspector();

        GridManager gm = (GridManager)target;

        if (gm.Grid == null || gm.Grid.Length == 0)
        {
            EditorGUILayout.HelpBox("Grid is empty. Run the scene or call BuildGrid first.", MessageType.Info);
            return;
        }

        GUILayout.Space(10);
        GUILayout.Label("Grid Preview", EditorStyles.boldLabel);

        int width = gm.levelData != null ? gm.levelData.width : gm.Grid.GetLength(0);
        int height = gm.levelData != null ? gm.levelData.height : gm.Grid.GetLength(1);

        // Draw grid from top to bottom (y descending)
        for (int y = height - 1; y >= 0; y--)
        {
            GUILayout.BeginHorizontal();

            for (int x = 0; x < width; x++)
            {
                GridNode node = gm.Grid[x, y];

                if (node != null)
                {
                    // Color based on walkable
                    GUI.backgroundColor = node.walkable ? Color.green : Color.red;

                    // Draw a button for each cell (optional: toggle walkable)
                    if (GUILayout.Button("", GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        node.walkable = !node.walkable;
                        EditorUtility.SetDirty(gm); // mark scene dirty
                    }
                }
                else
                {
                    GUI.backgroundColor = Color.gray;
                    GUILayout.Box("X", GUILayout.Width(30), GUILayout.Height(30));
                }
            }

            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
    }
}