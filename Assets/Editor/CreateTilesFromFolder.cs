using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;
using System.Xml;
using System.Collections.Generic;

public static class CreateTilesFromFolder
{
    // Convenience menu that processes the two tileset folders you mentioned
    private static readonly string[] defaultTilesetFolders = new[]
    {
        "Assets/Art/Tilesets/lpc-floors",
        "Assets/Art/Tilesets/lpc-walls"
    };

    [MenuItem("Tools/Tiles/Create Tiles From Default Tileset Folders (lpc-floors & lpc-walls)")]
    public static void CreateTilesFromDefaults()
    {
        int totalCreated = 0;
        foreach (var f in defaultTilesetFolders)
        {
            totalCreated += ProcessFolder(f);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Create Tiles", $"Created {totalCreated} Tile assets.", "OK");
    }

    [MenuItem("Tools/Tiles/Create Tiles From Folder...")]
    public static void CreateTilesFromChooser()
    {
        // Let the user choose a folder inside the Unity project
        string abs = EditorUtility.OpenFolderPanel("Select Tileset Folder (inside Assets)", Application.dataPath, "");
        if (string.IsNullOrEmpty(abs)) return;

        // Convert absolute path to project-relative Assets path
        var dataPath = Application.dataPath.Replace("\\", "/");
        abs = abs.Replace("\\", "/");
        if (!abs.StartsWith(dataPath))
        {
            EditorUtility.DisplayDialog("Create Tiles", "Please pick a folder inside the project's Assets folder.", "OK");
            return;
        }

        string relative = "Assets" + abs.Substring(dataPath.Length);
        int created = ProcessFolder(relative);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Create Tiles", $"Created {created} Tile assets in {relative} (Tiles subfolder).", "OK");
    }

    // Returns number of Tile assets created
    private static int ProcessFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"CreateTilesFromFolder: folder not found: {folder}");
            return 0;
        }

        int created = 0;

        // Ensure output folder exists
        string tilesOut = folder + "/Tiles";
        if (!AssetDatabase.IsValidFolder(tilesOut))
        {
            AssetDatabase.CreateFolder(folder, "Tiles");
        }

        // First: process any .tsx tileset files found in the folder (and subfolders)
        string folderAbs = Path.GetFullPath(folder).Replace("\\", "/");
        string projectRoot = Path.GetFullPath(".").Replace("\\", "/");

        var tsxFiles = Directory.GetFiles(folderAbs, "*.tsx", SearchOption.AllDirectories);
        if (tsxFiles != null && tsxFiles.Length > 0)
        {
            foreach (var tsxAbs in tsxFiles)
            {
                try
                {
                    // Parse the tsx (Tiled tileset) XML to find image sources
                    var xml = new XmlDocument();
                    xml.Load(tsxAbs);
                    var imageNodes = xml.GetElementsByTagName("image");
                    for (int i = 0; i < imageNodes.Count; i++)
                    {
                        var img = imageNodes[i] as XmlElement;
                        if (img == null) continue;

                        var src = img.GetAttribute("source");
                        if (string.IsNullOrEmpty(src)) continue;

                        // Resolve the image path relative to the tsx file
                        var tsxDir = Path.GetDirectoryName(tsxAbs).Replace("\\", "/");
                        var imgPathAbs = Path.GetFullPath(Path.Combine(tsxDir, src)).Replace("\\", "/");

                        string assetPath = null;
                        if (imgPathAbs.StartsWith(projectRoot))
                        {
                            // convert to Assets/... by taking the project-relative path
                            string relative = imgPathAbs.Substring(projectRoot.Length).Replace("\\", "/").TrimStart('/', '\\');
                            // Ensure it starts with Assets/
                            if (!relative.StartsWith("Assets/")) relative = "Assets/" + relative;
                            assetPath = relative;
                        }
                        else
                        {
                            // fallback: try to find by filename in the project
                            var fileName = Path.GetFileName(src);
                            var foundGuids = AssetDatabase.FindAssets(fileName);
                            foreach (var g in foundGuids)
                            {
                                var p = AssetDatabase.GUIDToAssetPath(g);
                                if (p.EndsWith(fileName))
                                {
                                    assetPath = p;
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(assetPath))
                        {
                            Debug.LogWarning($"CreateTilesFromFolder: could not resolve image referenced by {tsxAbs}: {src}");
                            continue;
                        }

                        // Load all assets at that path and create tiles for any Sprite assets found
                        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        int spritesFound = 0;
                        if (assets != null)
                        {
                            foreach (var a in assets)
                            {
                                if (a is Sprite sprite)
                                {
                                    spritesFound++;
                                    if (CreateTileForSprite(sprite, tilesOut)) created++;
                                }
                            }
                        }
                        Debug.Log($"CreateTilesFromFolder: tsx='{tsxAbs}' referenced '{src}' -> resolved assetPath='{assetPath}' spritesFound={spritesFound} createdSoFar={created}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"CreateTilesFromFolder: failed parsing {tsxAbs}: {ex.Message}");
                }
            }

            // If parsing .tsx files produced any tiles, return that count.
            // Otherwise fall through and try the sprite-scan fallback below so folders
            // that reference a single texture (not sliced) still get processed.
            if (created > 0)
            {
                return created;
            }
            else
            {
                Debug.Log($"CreateTilesFromFolder: parsed .tsx files but created 0 tiles in '{folder}' — falling back to scanning sprites in folder.");
            }
        }

        // If no .tsx files, fallback to creating tiles for any sprites in the folder
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        if (guids == null || guids.Length == 0)
        {
            Debug.Log($"CreateTilesFromFolder: no sprites found in {folder}");
            return created;
        }

        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;

            if (CreateTileForSprite(sprite, tilesOut)) created++;
        }

        return created;
    }

    private static bool CreateTileForSprite(Sprite sprite, string tilesOut)
    {
        if (sprite == null) return false;

        string tilePath = AssetDatabase.GenerateUniqueAssetPath(tilesOut + "/" + sprite.name + ".asset");
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, tilePath);
        return true;
    }
}
