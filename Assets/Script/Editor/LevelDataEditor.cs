using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private LevelData levelData;

    private void OnEnable()
    {
        levelData = (LevelData)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw default Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Initialize Grid"))
        {
            Undo.RecordObject(levelData, "Initialize Grid");
            levelData.InitializeGrid();
            EditorUtility.SetDirty(levelData);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Matrix", EditorStyles.boldLabel);

        if (levelData.cells == null || levelData.cells.Count == 0)
        {
            EditorGUILayout.HelpBox("Grid is empty. Click 'Initialize Grid' first.", MessageType.Info);
            return;
        }

        // Draw the matrix
        for (int y = levelData.height - 1; y >= 0; y--) // top to bottom
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < levelData.width; x++)
            {
                GridCell cell = levelData.GetCell(x, y);
                if (cell != null)
                {
                    bool newWalkable = GUILayout.Toggle(cell.walkable, GUIContent.none, GUILayout.Width(20), GUILayout.Height(20));
                    if (newWalkable != cell.walkable)
                    {
                        Undo.RecordObject(levelData, "Toggle Cell");
                        cell.walkable = newWalkable;
                        EditorUtility.SetDirty(levelData);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}