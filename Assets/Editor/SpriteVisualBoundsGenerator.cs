using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: scans PNG alpha channels to compute per-sprite visual bounds
/// (opaque-pixel bounding-box top edge, bottom edge and horizontal center) and
/// outputs a generated Lua data table for runtime overhead-UI anchoring and
/// sprite-foot alignment.
///
/// Bypasses TextureImporter.isReadable by loading raw PNG bytes with
/// Texture2D.LoadImage, so no resource import settings are modified.
/// </summary>
public static class SpriteVisualBoundsGenerator
{
    private const string SpritesDir = "Assets/SunnyLand Artwork/Sprites";
    private const string OutputPath = "Assets/Resources/Lua/Config/Generated/SpriteBounds.lua.txt";
    private const float AlphaThreshold = 0.5f;

    [MenuItem("Tools/Generate Sprite Visual Bounds")]
    public static void Generate()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string fullSpritesDir = Path.Combine(projectRoot,
            SpritesDir.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(fullSpritesDir))
        {
            Debug.LogError($"[SpriteBounds] Directory not found: {fullSpritesDir}");
            return;
        }

        string[] pngFiles = Directory.GetFiles(fullSpritesDir, "*.png", SearchOption.AllDirectories);
        if (pngFiles.Length == 0)
        {
            Debug.LogWarning("[SpriteBounds] No PNG files found.");
            return;
        }

        Array.Sort(pngFiles, (a, b) =>
            string.CompareOrdinal(
                Path.GetFileNameWithoutExtension(a),
                Path.GetFileNameWithoutExtension(b)));

        var sb = new StringBuilder();
        sb.AppendLine("local SpriteBounds = {");
        sb.AppendLine();

        int processed = 0;
        int skipped = 0;

        foreach (string pngPath in pngFiles)
        {
            string spriteName = Path.GetFileNameWithoutExtension(pngPath);

            // Read TextureImporter settings (PPU, pivot) — do not hardcode
            string assetPath = "Assets" + pngPath.Substring(Application.dataPath.Length)
                               .Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            float ppu = 100f;
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            if (importer != null)
            {
                ppu = importer.spritePixelsPerUnit;
                pivot = importer.spritePivot;
            }

            // Decode PNG from disk — bypasses isReadable = false
            byte[] fileData = File.ReadAllBytes(pngPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);

            int width = tex.width;
            int height = tex.height;
            Color32[] pixels = tex.GetPixels32();
            UnityEngine.Object.DestroyImmediate(tex);

            // Scan for opaque pixels: find minX, maxX, minY, maxY
            int minX = width, maxX = -1, minY = height, maxY = -1;
            bool found = false;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[rowOffset + x].a >= 128) // alpha > 0.5 threshold
                    {
                        found = true;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (!found)
            {
                Debug.LogWarning($"[SpriteBounds] Fully transparent sprite, skipped: {spriteName}");
                skipped++;
                continue;
            }

            // Convert to sprite local coordinates (world units, pre-GameObject scale)
            // +1: pixel at index y occupies [y, y+1); the top edge is at maxY+1, bottom edge is at minY.
            // GetPixels32 origin is bottom-left, matching sprite local space direction.
            float topY = (maxY + 1 - height * pivot.y) / ppu;
            float bottomY = (minY - height * pivot.y) / ppu;
            float centerX = ((minX + maxX + 1) * 0.5f - width * pivot.x) / ppu;

            sb.AppendLine($"    [\"{spriteName}\"] = {{ topY = {topY:0.####}, bottomY = {bottomY:0.####}, centerX = {centerX:0.####} }},");

            processed++;
        }

        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return SpriteBounds");

        string fullOutputPath = Path.Combine(projectRoot,
            OutputPath.Replace('/', Path.DirectorySeparatorChar));
        string outputDir = Path.GetDirectoryName(fullOutputPath);
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        File.WriteAllText(fullOutputPath, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"[SpriteBounds] Generated {processed} entries, skipped {skipped}. Output: {OutputPath}");
    }
}
