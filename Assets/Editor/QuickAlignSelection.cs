using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class QuickAlignSelection
{
    [MenuItem("Tools/Tilemap/Quick Align Selection")]
    public static void AlignSelectionToFirstTilemap()
    {
        // Find first active Tilemap in the scene
        Tilemap tilemap = Object.FindObjectOfType<Tilemap>();
        if (tilemap == null)
        {
            EditorUtility.DisplayDialog("Quick Align", "No Tilemap found in the scene. Create a Tilemap first.", "OK");
            return;
        }

        var selection = Selection.transforms;
        if (selection == null || selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Quick Align", "No objects selected.", "OK");
            return;
        }

        Undo.RecordObjects(selection, "Quick Align Selection");
        Grid grid = tilemap.layoutGrid;
        int count = 0;
        foreach (var t in selection)
        {
            Vector3 worldPos = t.position;
            Vector3Int cell = tilemap.WorldToCell(worldPos);
            Vector3 cellCenter = tilemap.GetCellCenterWorld(cell);
            t.position = cellCenter;
            if (grid != null)
                Undo.SetTransformParent(t, grid.transform, "Reparent to Grid");
            EditorUtility.SetDirty(t);
            count++;
        }

        EditorUtility.DisplayDialog("Quick Align", $"Aligned {count} selected objects to Tilemap '{tilemap.name}'.", "OK");
    }
}
