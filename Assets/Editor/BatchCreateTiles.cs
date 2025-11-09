using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

public static class BatchCreateTiles
{
    [MenuItem("Assets/Create/Batch Create Tile Assets from Sprites", false, 2000)]
    private static void CreateTilesFromSelection()
    {
        var objs = Selection.objects;
        if (objs == null || objs.Length == 0)
        {
            EditorUtility.DisplayDialog("Batch Create Tiles", "No sprites selected. Select one or more sprites in the Project window.", "OK");
            return;
        }

        // Determine target folder: folder of first selected asset (if file) or project root
        string firstPath = AssetDatabase.GetAssetPath(objs[0]);
        string folder = "Assets";
        if (!string.IsNullOrEmpty(firstPath))
        {
            if (Directory.Exists(firstPath)) folder = firstPath;
            else folder = Path.GetDirectoryName(firstPath);
        }

        int created = 0;
        foreach (var o in objs)
        {
            // Only handle Sprite assets
            if (o is Sprite sprite)
            {
                string tilePath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + sprite.name + ".asset");
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                AssetDatabase.CreateAsset(tile, tilePath);
                created++;
            }
            else
            {
                // If a texture with multiple sprites (sprite sheet) was selected, try to extract sub-sprites
                string path = AssetDatabase.GetAssetPath(o);
                if (!string.IsNullOrEmpty(path))
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var a in assets)
                    {
                        if (a is Sprite sp)
                        {
                            string tilePath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + sp.name + ".asset");
                            Tile tile = ScriptableObject.CreateInstance<Tile>();
                            tile.sprite = sp;
                            AssetDatabase.CreateAsset(tile, tilePath);
                            created++;
                        }
                    }
                }
            }
        }

        if (created > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Batch Create Tiles", $"Created {created} Tile asset(s) in {folder}.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Batch Create Tiles", "No sprites found in selection.", "OK");
        }
    }

    [MenuItem("Assets/Create/Batch Create Tile Assets from Sprites", true)]
    private static bool ValidateCreateTiles()
    {
        foreach (var o in Selection.objects)
        {
            if (o is Sprite) return true;
            string path = AssetDatabase.GetAssetPath(o);
            if (!string.IsNullOrEmpty(path))
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in assets) if (a is Sprite) return true;
            }
        }
        return false;
    }
}
