using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Tilemaps;
using UnityEngine.Tilemaps;

/// <summary>
/// MultiTileBrush: a simple Grid Brush that places a prefab (which can contain multiple child sprites)
/// at the painted cell. Create an asset via Assets -> Create -> Brushes -> MultiTileBrush and assign
/// the prefab to paint. Use it from the Tile Palette Brush dropdown.
/// </summary>
[CreateAssetMenu(fileName = "MultiTileBrush", menuName = "Brushes/MultiTileBrush")]
public class MultiTileBrush : GridBrush
{
    [Tooltip("Prefab to instantiate when painting a cell. Prefab should be aligned so its visual center corresponds to cell center.")]
    public GameObject prefab;

    // When painting, instantiate prefab at the tile cell center under the brush target (usually the Grid)
    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        if (prefab == null || brushTarget == null)
            return;

        // Try get a Tilemap to compute the proper world center for the cell
        Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
        Vector3 worldPos;
        if (tilemap != null)
            worldPos = tilemap.GetCellCenterWorld(position);
        else
            worldPos = grid.CellToWorld(position);

        // Instantiate prefab as a child of the brushTarget (or grid) so it's organized under the Grid
        GameObject parent = brushTarget.transform != null ? brushTarget : grid.gameObject;

        // Record undo
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent as Transform);
        if (instance != null)
        {
            Undo.RegisterCreatedObjectUndo(instance, "Paint MultiTile Prefab");
            instance.transform.position = worldPos;
            // snap to integer multiples of cell size within parent local space
            instance.transform.SetParent(parent.transform, true);
        }
    }

    // Erase: removes any instantiated prefab instances that were placed by this brush at the cell center
    public override void Erase(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        if (prefab == null || brushTarget == null)
            return;

        Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
        Vector3 worldPos;
        if (tilemap != null)
            worldPos = tilemap.GetCellCenterWorld(position);
        else
            worldPos = grid.CellToWorld(position);

        // Find child objects under brushTarget whose prefab matches and which are near the cell center
        foreach (Transform child in brushTarget.transform)
        {
            if (child == null) continue;
            GameObject go = child.gameObject;
            // Compare prefab asset by source prefab
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
            if (source == prefab)
            {
                // If the instance is very close to the cell center, destroy it
                if (Vector3.Distance(go.transform.position, worldPos) < 0.01f)
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }
        }
    }
}
#endif
