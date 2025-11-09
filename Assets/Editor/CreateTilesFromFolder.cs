using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public static class CreateTilesFromFolder
{
    // Change this to the folder you mentioned
    private const string sourceFolder = "Assets/Art/Tilesets/tileset_16x16_interior";

    [MenuItem("Tools/Tiles/Create Tiles From tileset_16x16_interior Folder")]
    public static void CreateTiles()
    {
        string folder = sourceFolder;
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog("Create Tiles", $"Folder '{folder}' not found.", "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Create Tiles", "No sprites found in the folder.", "OK");
            return;
        }

        string tilesOut = folder + "/Tiles";
        if (!AssetDatabase.IsValidFolder(tilesOut))
        {
            AssetDatabase.CreateFolder(folder, "Tiles");
        }

        int created = 0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;

            string tilePath = AssetDatabase.GenerateUniqueAssetPath(tilesOut + "/" + sprite.name + ".asset");
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, tilePath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Create Tiles", $"Created {created} Tile assets in {tilesOut}. Drag them into a Tile Palette to use.", "OK");
    }
}
