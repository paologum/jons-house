using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Small editor utility to combine selected Sprite assets into a single PNG.
/// Menu: Assets/Tools/Combine Selected Sprites/{Horizontal,Vertical,Overlay}
///
/// Notes:
/// - This will temporarily enable read/write on source textures if necessary and restore
///   the original importer settings afterward.
/// - Result is written to Assets/CombinedSprites/combined_{timestamp}.png and imported as a Sprite.
/// </summary>
public static class CombineSpritesEditor
{
    [MenuItem("Assets/Tools/Combine Selected Sprites/Horizontal")]
    public static void CombineHorizontal()
    {
        CombineSelected(horizontal: true, overlay: false);
    }

    [MenuItem("Assets/Tools/Combine Selected Sprites/Vertical")]
    public static void CombineVertical()
    {
        CombineSelected(horizontal: false, overlay: false);
    }

    [MenuItem("Assets/Tools/Combine Selected Sprites/Overlay")]
    public static void CombineOverlay()
    {
        CombineSelected(horizontal: false, overlay: true);
    }

    private static void CombineSelected(bool horizontal, bool overlay)
    {
        var objs = Selection.objects;
        List<Sprite> sprites = new List<Sprite>();
        foreach (var o in objs)
        {
            if (o is Sprite) sprites.Add((Sprite)o);
            else
            {
                // If user selected the texture asset, try to find its primary sprite
                string path = AssetDatabase.GetAssetPath(o);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in assets)
                    if (a is Sprite) { sprites.Add((Sprite)a); break; }
            }
        }

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Combine Sprites", "No sprites selected. Select one or more Sprite assets in the Project window.", "OK");
            return;
        }

        // Ensure textures are readable: remember original importer settings so we can restore them
        Dictionary<string, bool> originalReadable = new Dictionary<string, bool>();
        HashSet<string> touchedPaths = new HashSet<string>();

        try
        {
            // compute combined size
            int totalW = 0, totalH = 0;
            int maxW = 0, maxH = 0;
            List<Texture2D> texs = new List<Texture2D>();
            List<Rect> rects = new List<Rect>();

            foreach (var s in sprites)
            {
                // Use the sprite's texture asset path so we can reimport and then reload
                string texPath = AssetDatabase.GetAssetPath(s.texture);
                if (!touchedPaths.Contains(texPath))
                {
                    var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                    if (importer != null)
                    {
                        originalReadable[texPath] = importer.isReadable;
                        if (!importer.isReadable)
                        {
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                        }
                    }
                    touchedPaths.Add(texPath);
                }

                // After potential reimport, reload the Texture2D from the asset database so we get a readable instance
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null)
                {
                    Debug.LogError($"CombineSpritesEditor: failed to load texture at path {texPath}");
                    continue;
                }

                texs.Add(tex);
                rects.Add(s.rect);
                int w = (int)s.rect.width;
                int h = (int)s.rect.height;
                totalW += horizontal && !overlay ? w : Mathf.Max(totalW, w);
                totalH += !horizontal && !overlay ? h : Mathf.Max(totalH, h);
                maxW = Mathf.Max(maxW, w);
                maxH = Mathf.Max(maxH, h);
            }

            if (horizontal && !overlay)
            {
                totalW = 0; foreach (var r in rects) totalW += (int)r.width;
                totalH = maxH;
            }
            else if (!horizontal && !overlay)
            {
                totalH = 0; foreach (var r in rects) totalH += (int)r.height;
                totalW = maxW;
            }
            else if (overlay)
            {
                totalW = maxW; totalH = maxH;
            }

            // create canvas
            Texture2D canvas = new Texture2D(Mathf.Max(1, totalW), Mathf.Max(1, totalH), TextureFormat.RGBA32, false);
            Color32[] blank = new Color32[canvas.width * canvas.height];
            for (int i = 0; i < blank.Length; i++) blank[i] = new Color32(0, 0, 0, 0);
            canvas.SetPixels32(blank);

            int offsetX = 0;
            int offsetY = 0;

            for (int i = 0; i < sprites.Count; i++)
            {
                var s = sprites[i];
                var tex = texs[i];
                Rect r = rects[i];
                int w = (int)r.width;
                int h = (int)r.height;
                int srcX = (int)r.x;
                int srcY = (int)r.y;
                Color[] pixels = null;
                try
                {
                    // try the fast path first
                    pixels = tex.GetPixels(srcX, srcY, w, h);
                }
                catch
                {
                    // fallback: render the sub-rect into a temporary RenderTexture and read pixels
                    pixels = ReadPixelsFallback(tex, srcX, srcY, w, h);
                }

                int destX = offsetX;
                int destY = overlay ? 0 : offsetY;

                // For horizontal layout we keep destY aligned top (so put at 0) but Unity textures origin is bottom-left,
                // so to top-align inside canvas we compute destY = canvas.height - h - topPadding (we choose top=0 here)
                if (horizontal && !overlay)
                {
                    destY = canvas.height - h; // top-align
                }
                else if (!horizontal && !overlay)
                {
                    destX = 0; // left align
                    // stack top-to-bottom: compute y offset from top
                    int prevHeight = 0;
                    for (int j = 0; j < i; j++) prevHeight += (int)rects[j].height;
                    destY = canvas.height - prevHeight - h;
                }
                else if (overlay)
                {
                    destX = (canvas.width - w) / 2;
                    destY = (canvas.height - h) / 2;
                }

                // copy pixels with alpha composite if overlay, otherwise overwrite
                for (int yy = 0; yy < h; yy++)
                {
                    for (int xx = 0; xx < w; xx++)
                    {
                        int srcIdx = yy * w + xx;
                        int dstX = destX + xx;
                        int dstY = destY + yy;
                        if (dstX < 0 || dstX >= canvas.width || dstY < 0 || dstY >= canvas.height) continue;
                        Color src = pixels[srcIdx];
                        Color dst = canvas.GetPixel(dstX, dstY);
                        Color outc;
                        if (overlay)
                        {
                            // alpha composite: out = src.a*src + (1-src.a)*dst
                            float a = src.a + dst.a * (1 - src.a);
                            if (a <= 0f) outc = Color.clear;
                            else
                            {
                                outc.r = (src.r * src.a + dst.r * dst.a * (1 - src.a)) / a;
                                outc.g = (src.g * src.a + dst.g * dst.a * (1 - src.a)) / a;
                                outc.b = (src.b * src.a + dst.b * dst.a * (1 - src.a)) / a;
                                outc.a = a;
                            }
                        }
                        else
                        {
                            outc = src;
                        }
                        canvas.SetPixel(dstX, dstY, outc);
                    }
                }

                // increment offsets
                if (horizontal && !overlay) offsetX += w;
                if (!horizontal && !overlay) offsetY += h;
            }

            canvas.Apply(false, false);

            // write to file
            string outDir = "Assets/CombinedSprites";
            if (!AssetDatabase.IsValidFolder(outDir)) AssetDatabase.CreateFolder("Assets", "CombinedSprites");
            string outPath = Path.Combine(outDir, $"combined_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            byte[] png = canvas.EncodeToPNG();
            File.WriteAllBytes(outPath, png);
            AssetDatabase.ImportAsset(outPath);

            // configure imported asset as Sprite
            var newImp = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (newImp != null)
            {
                newImp.textureType = TextureImporterType.Sprite;
                newImp.filterMode = FilterMode.Point;
                newImp.mipmapEnabled = false;
                newImp.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            var created = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
            Selection.activeObject = created;
            EditorUtility.DisplayDialog("Combine Sprites", $"Combined {sprites.Count} sprites -> {outPath}", "OK");
        }
        finally
        {
            // restore importer read/write flags
            foreach (var kv in originalReadable)
            {
                var imp = AssetImporter.GetAtPath(kv.Key) as TextureImporter;
                if (imp != null && imp.isReadable != kv.Value)
                {
                    imp.isReadable = kv.Value;
                    imp.SaveAndReimport();
                }
            }
        }
    }

    // Fallback read: draw the specified sub-rect of the source texture into a temporary
    // RenderTexture and read back pixels. This works even when the Texture2D is not readable.
    private static Color[] ReadPixelsFallback(Texture2D srcTex, int srcX, int srcY, int w, int h)
    {
        int texW = srcTex.width;
        int texH = srcTex.height;

        RenderTexture prev = RenderTexture.active;
        RenderTexture tempRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = tempRT;
        GL.Clear(true, true, new Color(0, 0, 0, 0));

        // source UV rect (normalized)
        Rect srcUV = new Rect((float)srcX / texW, (float)srcY / texH, (float)w / texW, (float)h / texH);
        Rect dest = new Rect(0, 0, w, h);

        // Draw the portion of the texture into the RT
        Graphics.DrawTexture(dest, srcTex, srcUV, 0, 0, 0, 0);

        Texture2D tmp = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tmp.Apply();
        Color[] pixels = tmp.GetPixels();

        // cleanup
        Object.DestroyImmediate(tmp);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(tempRT);

        return pixels;
    }
}
