using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility: create a PNG sprite by rendering a selected prefab in-place.
/// Usage: select a prefab (GameObject) in Project window, then Assets > Tools > Create Sprite from Prefab...
/// The tool will instantiate the prefab temporarily, render it with an orthographic camera to a RenderTexture,
/// save a PNG into the project, and import it as a Sprite.
/// Works for regular SpriteRenderer objects and for UI prefabs (Canvas) by switching Canvas to ScreenSpaceCamera.
/// </summary>
public static class PrefabToSprite
{
    [MenuItem("Assets/Tools/Create Sprite from Prefab...")]
    public static void CreateSpriteFromPrefabMenu()
    {
        var obj = Selection.activeObject as GameObject;
        if (obj == null)
        {
            EditorUtility.DisplayDialog("Create Sprite from Prefab", "Select a prefab (GameObject) in the Project window first.", "OK");
            return;
        }

        string defaultName = obj.name + ".png";
        string path = EditorUtility.SaveFilePanelInProject("Save Sprite PNG", defaultName, "png", "Choose where to save the generated sprite PNG");
        if (string.IsNullOrEmpty(path)) return;

        CreateSpriteFromPrefab(obj, path);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Create Sprite from Prefab", "Sprite saved to: " + path, "OK");
    }

    // Open an editor window that allows supplying sprites to auto-assign before export
    [MenuItem("Assets/Tools/Create Sprite from Prefab (with page sprites)...")]
    public static void OpenPrefabToSpriteWindow()
    {
        PrefabToSpriteWindow.ShowWindow();
    }

    public static void CreateSpriteFromPrefab(GameObject prefab, string assetPath)
    {
        CreateSpriteFromPrefab(prefab, assetPath, null);
    }

    public static void CreateSpriteFromPrefab(GameObject prefab, string assetPath, Sprite[] assignSprites = null)
    {
        // instantiate prefab transiently
        GameObject instance = null;
        // Prefer PrefabUtility.InstantiatePrefab for asset prefabs, but fall back to Object.Instantiate
        try
        {
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
        catch
        {
            instance = null;
        }

        if (instance == null)
        {
            // Fallback: try a normal instantiate (works when prefab is a GameObject asset)
            try
            {
                instance = Object.Instantiate(prefab);
                // mark instantiated object as an editor-only object
                instance.name = prefab.name + "_PreviewInstance";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to instantiate prefab '{prefab.name}': {ex.Message}");
                Debug.LogError("Make sure you selected a prefab asset (in the Project window), not a scene object.");
                return;
            }
        }
        instance.hideFlags = HideFlags.HideAndDontSave;

        // If the caller provided sprites to assign, attempt to assign them to the instance's Image/SpriteRenderer children
        if (assignSprites != null && assignSprites.Length > 0)
        {
            AssignSpritesToInstance(instance, assignSprites);
        }

        // Ensure canvases render with our camera if present
        var canvases = instance.GetComponentsInChildren<Canvas>(true);

        // create camera
        GameObject camGO = new GameObject("_PrefabToSprite_Cam");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        Camera cam = camGO.AddComponent<Camera>();
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.orthographic = true;
        cam.cullingMask = ~0; // everything

        // temporarily switch Canvas render modes to ScreenSpaceCamera and assign our camera
        Canvas[] switched = null;
        if (canvases != null && canvases.Length > 0)
        {
            switched = new Canvas[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                switched[i] = c;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1f;
            }
        }

        // calculate bounds (Renderer or RectTransform)
        Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
        bool hasBounds = false;

        var rends = instance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        // consider UI RectTransforms if no renderers found
        if (!hasBounds)
        {
            var rts = instance.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt in rts)
            {
                Vector3[] worldCorners = new Vector3[4];
                rt.GetWorldCorners(worldCorners);
                for (int i = 0; i < 4; i++)
                {
                    if (!hasBounds)
                    {
                        bounds = new Bounds(worldCorners[i], Vector3.zero);
                        hasBounds = true;
                    }
                    else bounds.Encapsulate(worldCorners[i]);
                }
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(instance.transform.position, Vector3.one);
        }

        // position camera to look at bounds center
        Vector3 center = bounds.center;
        float size = Mathf.Max(bounds.size.x, bounds.size.y);
        cam.orthographicSize = size * 0.5f;
        cam.transform.position = center + Vector3.back * 10f;

        // choose texture size based on bounds and a pixels-per-unit heuristic
        const int defaultPPU = 100;
        int texW = Mathf.Clamp(Mathf.CeilToInt(bounds.size.x * defaultPPU), 32, 4096);
        int texH = Mathf.Clamp(Mathf.CeilToInt(bounds.size.y * defaultPPU), 32, 4096);

            // create render texture
            RenderTexture renderTex = new RenderTexture(texW, texH, 24, RenderTextureFormat.ARGB32);
            renderTex.Create();
            cam.targetTexture = renderTex;

        // render
        RenderTexture prev = RenderTexture.active;
        cam.Render();
        RenderTexture.active = renderTex;

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, texW, texH), 0, 0);
        tex.Apply();

            // encode PNG and write
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(assetPath, png);

        // import and set as sprite
        AssetDatabase.ImportAsset(assetPath);
        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Point;
            imp.SaveAndReimport();
        }

            // cleanup
            RenderTexture.active = prev;
            cam.targetTexture = null;
            renderTex.Release();
            Object.DestroyImmediate(renderTex);
            Object.DestroyImmediate(tex);

        if (switched != null)
        {
            // not restoring original renderModes because instance is destroyed; safe to leave
        }

        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(instance);
    }

    static void AssignSpritesToInstance(GameObject instance, Sprite[] sprites)
    {
        if (instance == null || sprites == null || sprites.Length == 0) return;

        // Try UI Images first
        var images = instance.GetComponentsInChildren<Image>(true);
        var assignedImages = new System.Collections.Generic.HashSet<int>();

        // Try name-based matching first (sprite name -> image name)
        for (int si = 0; si < sprites.Length; si++)
        {
            var s = sprites[si];
            if (s == null) continue;
            bool found = false;
            string sname = s.name.ToLowerInvariant();
            for (int ii = 0; ii < images.Length; ii++)
            {
                if (assignedImages.Contains(ii)) continue;
                var iname = images[ii].gameObject.name.ToLowerInvariant();
                if (iname.Contains(sname) || sname.Contains(iname) || iname.Contains("page") || iname.Contains("left") || iname.Contains("right"))
                {
                    images[ii].sprite = s;
                    assignedImages.Add(ii);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                // will be assigned in next phase if there are remaining images
            }
        }

        // Assign any remaining sprites sequentially to unassigned Image slots
        int seq = 0;
        for (int si = 0; si < sprites.Length; si++)
        {
            var s = sprites[si];
            if (s == null) continue;
            // if already matched by name above, skip
            bool already = false;
            for (int ii = 0; ii < images.Length; ii++) if (images[ii].sprite == s) { already = true; break; }
            if (already) continue;
            // find next unassigned image
            while (seq < images.Length && assignedImages.Contains(seq)) seq++;
            if (seq < images.Length)
            {
                images[seq].sprite = s;
                assignedImages.Add(seq);
                seq++;
                continue;
            }
            // no more images to assign
            break;
        }

        // If no UI Images or still sprites left, try SpriteRenderers
        if (images.Length == 0)
        {
            var srs = instance.GetComponentsInChildren<SpriteRenderer>(true);
            var assignedSR = new System.Collections.Generic.HashSet<int>();
            // name-based matching
            for (int si = 0; si < sprites.Length; si++)
            {
                var s = sprites[si];
                if (s == null) continue;
                string sname = s.name.ToLowerInvariant();
                for (int ri = 0; ri < srs.Length; ri++)
                {
                    if (assignedSR.Contains(ri)) continue;
                    var rname = srs[ri].gameObject.name.ToLowerInvariant();
                    if (rname.Contains(sname) || sname.Contains(rname) || rname.Contains("page") || rname.Contains("left") || rname.Contains("right"))
                    {
                        srs[ri].sprite = s;
                        assignedSR.Add(ri);
                        break;
                    }
                }
            }
            // then sequential
            int rseq = 0;
            for (int si = 0; si < sprites.Length; si++)
            {
                var s = sprites[si];
                if (s == null) continue;
                bool already = false;
                for (int ri = 0; ri < srs.Length; ri++) if (srs[ri].sprite == s) { already = true; break; }
                if (already) continue;
                while (rseq < srs.Length && assignedSR.Contains(rseq)) rseq++;
                if (rseq < srs.Length)
                {
                    srs[rseq].sprite = s;
                    assignedSR.Add(rseq);
                    rseq++;
                }
                else break;
            }
        }
    }

    // Simple Editor window to pick a prefab and an ordered set of sprites to assign before export
    public class PrefabToSpriteWindow : EditorWindow
    {
        GameObject prefab;
        int spriteCount = 0;
        Sprite[] sprites = new Sprite[0];

        public static void ShowWindow()
        {
            var w = GetWindow<PrefabToSpriteWindow>("Prefab To Sprite");
            w.Show();
        }

        void OnEnable()
        {
            if (sprites == null) sprites = new Sprite[0];
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Create Sprite from Prefab (with optional sprite assignments)", EditorStyles.boldLabel);
            prefab = (GameObject)EditorGUILayout.ObjectField("Prefab (asset)", prefab, typeof(GameObject), false);
            spriteCount = EditorGUILayout.IntField("Number of sprites to assign", spriteCount);
            spriteCount = Mathf.Clamp(spriteCount, 0, 16);

            if (sprites == null || sprites.Length != spriteCount)
            {
                System.Array.Resize(ref sprites, spriteCount);
            }

            for (int i = 0; i < spriteCount; i++)
            {
                sprites[i] = (Sprite)EditorGUILayout.ObjectField($"Sprite {i + 1}", sprites[i], typeof(Sprite), false);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Save PNG and Assign Sprites"))
            {
                if (prefab == null)
                {
                    EditorUtility.DisplayDialog("Prefab To Sprite", "Please choose a prefab asset first.", "OK");
                    return;
                }
                string defaultName = prefab.name + ".png";
                string path = EditorUtility.SaveFilePanelInProject("Save Sprite PNG", defaultName, "png", "Choose where to save the generated sprite PNG");
                if (string.IsNullOrEmpty(path)) return;
                CreateSpriteFromPrefab(prefab, path, sprites);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Create Sprite from Prefab", "Sprite saved to: " + path, "OK");
            }
        }
    }
}
