using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor utility to align selected GameObjects to the nearest Tilemap cell center.
/// Also provides diagnostics about PPU, Grid cell size, and Tilemap transform offsets.
/// Open via Tools -> Tilemap -> Align Selection To Tilemap
/// </summary>
public class TilemapAligner : EditorWindow
{
    private bool reparentToGrid = true;
    private Tilemap targetTilemap;

    [MenuItem("Tools/Tilemap/Align Selection To Tilemap")]
    public static void ShowWindow()
    {
        GetWindow<TilemapAligner>("Tilemap Aligner");
    }

    void OnGUI()
    {
        GUILayout.Label("Tilemap Aligner", EditorStyles.boldLabel);
        targetTilemap = (Tilemap)EditorGUILayout.ObjectField("Target Tilemap", targetTilemap, typeof(Tilemap), true);
        reparentToGrid = EditorGUILayout.Toggle("Reparent To Grid", reparentToGrid);

        if (GUILayout.Button("Align Selected to Tilemap Cells"))
        {
            AlignSelected();
        }

        if (GUILayout.Button("Run Diagnostics"))
        {
            RunDiagnostics();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Select GameObjects (walls, props) and a Tilemap. Align will snap them to the nearest cell centers. If your Tilemap or sprites are offset, use the diagnostics to check PPU and Grid cell size.", MessageType.Info);
    }

    void AlignSelected()
    {
        if (targetTilemap == null)
        {
            EditorUtility.DisplayDialog("Tilemap Aligner", "Please assign a Target Tilemap.", "OK");
            return;
        }

        var selected = Selection.transforms;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Tilemap Aligner", "No objects selected.", "OK");
            return;
        }

        Undo.RecordObjects(selected, "Align Selected to Tilemap");
        Grid grid = targetTilemap.layoutGrid;
        foreach (var t in selected)
        {
            Vector3 worldPos = t.position;
            Vector3Int cell = targetTilemap.WorldToCell(worldPos);
            Vector3 cellCenter = targetTilemap.GetCellCenterWorld(cell);
            t.position = cellCenter;
            if (reparentToGrid && grid != null)
            {
                Undo.SetTransformParent(t, grid.transform, "Reparent to Grid");
            }
            EditorUtility.SetDirty(t);
        }

        EditorUtility.DisplayDialog("Tilemap Aligner", "Aligned selected objects to tilemap cells.", "OK");
    }

    void RunDiagnostics()
    {
        if (targetTilemap == null)
        {
            EditorUtility.DisplayDialog("Diagnostics", "Please assign a Target Tilemap to run diagnostics.", "OK");
            return;
        }

        Grid grid = targetTilemap.layoutGrid;
        string msg = "Tilemap Diagnostics:\n";
        if (grid != null)
        {
            msg += $"Grid cell size: {grid.cellSize} (should match tile world size)\n";
            msg += $"Grid cell gap: {grid.cellGap}\n";
        }
        else
        {
            msg += "No Grid parent found for this Tilemap. Ensure Tilemap is under a Grid.\n";
        }

        // Try to inspect tile anchor from tilemap's tiles (best-effort)
        msg += $"Tilemap transform position: {targetTilemap.transform.position}\n";
        msg += "Note: Ensure Tilemap transform is at integer multiples of cell size and sprite PPU matches your intended world units.\n";

        EditorUtility.DisplayDialog("Tilemap Diagnostics", msg, "OK");
    }
}
