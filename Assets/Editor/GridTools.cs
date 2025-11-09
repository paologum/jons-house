using UnityEngine;
using UnityEditor;

/// <summary>
/// Small Editor utility to snap selected GameObjects to a grid and recenter selected prefab roots to their visual center.
/// Place in Assets/Editor/ and open via Tools -> Grid Tools -> Open Window
/// </summary>
public class GridTools : EditorWindow
{
    private float tileSizeX = 2f;
    private float tileSizeY = 2f;
    private bool snapZ = false;

    [MenuItem("Tools/Grid Tools/Open Window")]
    public static void OpenWindow()
    {
        GetWindow<GridTools>("Grid Tools");
    }

    void OnGUI()
    {
        GUILayout.Label("Snap & Recenter Utilities", EditorStyles.boldLabel);
        tileSizeX = EditorGUILayout.FloatField("Tile Size X (units)", tileSizeX);
        tileSizeY = EditorGUILayout.FloatField("Tile Size Y (units)", tileSizeY);
        snapZ = EditorGUILayout.Toggle("Snap Z axis", snapZ);

        if (GUILayout.Button("Snap Selected To Grid"))
        {
            SnapSelectedToGrid(tileSizeX, tileSizeY, snapZ);
        }

        if (GUILayout.Button("Recenter Selected To Visual Center"))
        {
            RecenterSelectedToVisualCenter();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Notes:", EditorStyles.helpBox);
        EditorGUILayout.LabelField("- Tile Size should match your sprite world size (e.g. 32px/16PPU = 2 units)");
        EditorGUILayout.LabelField("- Recenter will create a new parent at the visual center and preserve visual positions.");
    }

    private static void SnapSelectedToGrid(float sizeX, float sizeY, bool snapZ)
    {
        var selection = Selection.transforms;
        if (selection == null || selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Snap Selected", "No transforms selected.", "OK");
            return;
        }

        Undo.RecordObjects(selection, "Snap Selected To Grid");
        foreach (var t in selection)
        {
            Vector3 p = t.position;
            p.x = Mathf.Round(p.x / sizeX) * sizeX;
            p.y = Mathf.Round(p.y / sizeY) * sizeY;
            if (!snapZ) p.z = t.position.z;
            else p.z = Mathf.Round(p.z / sizeX) * sizeX;
            t.position = p;
            EditorUtility.SetDirty(t);
        }
    }

    private static void RecenterSelectedToVisualCenter()
    {
        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Recenter Selected", "No GameObjects selected.", "OK");
            return;
        }

        foreach (var go in selection)
        {
            // Compute combined bounds of all SpriteRenderers in this root
            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            if (srs == null || srs.Length == 0)
            {
                Debug.LogWarning($"Recenter: '{go.name}' has no SpriteRenderer children; skipping.");
                continue;
            }

            Bounds bounds = srs[0].bounds;
            for (int i = 1; i < srs.Length; i++) bounds.Encapsulate(srs[i].bounds);

            Vector3 visualCenter = bounds.center;

            // Create new parent at visualCenter, and reparent original GameObject under it while preserving world positions
            GameObject newParent = new GameObject(go.name + "_Centered");
            Undo.RegisterCreatedObjectUndo(newParent, "Create Center Parent");
            newParent.transform.position = visualCenter;

            // Move original object under new parent while keeping world positions
            Undo.SetTransformParent(go.transform, newParent.transform, "Reparent to Center");
            // After reparenting, adjust localPosition so children visually remain identical
            // We want the visuals unchanged: so keep the same world positions (already maintained by reparenting in Unity),
            // but ensure the root's transform localPosition reflects its offset from new parent.
            go.transform.localPosition = go.transform.position - newParent.transform.position;
        }
    }
}
