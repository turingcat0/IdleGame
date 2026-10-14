using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: 把选中 Sprite 贴图的 pivot 设到“最低可见像素”（角色脚底），
/// 解决 boss 等 size 放大后因 pivot=Center 下陷、以及贴图底部透明留白导致的悬浮。
///
/// 像素扫描参考 SpriteVisualBoundsGenerator：用 Texture2D.LoadImage 直接解码 PNG，
/// 不改 isReadable。为避免逐帧抖动，所有选中贴图共用同一个 pivotY
/// （取所有帧 lowestY/height 的最小值 = 最低那一帧贴地，其余帧在其上方 —— 任何帧都不会下陷）。
/// 适用 spriteMode==1 的单 Sprite 贴图（boss_1 的 Slimer 各帧即此类）。
/// </summary>
public static class SpritePivotFixer
{
    private const byte AlphaThreshold = 128; // alpha >= 0.5

    [MenuItem("Tools/Sprite/修复贴图锚点 (最低可见像素)")]
    public static void FixSelected()
    {
        var absPaths = CollectSelectedPngPaths();
        if (absPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("未选择贴图",
                "在 Project 窗口选中要修复的 Sprite 贴图或其所在文件夹（可多选），再运行本工具。\n" +
                "boss_1 用的是 Assets/SunnyLand Artwork/Sprites/Enemies/Slimer 和 Slimer-Idle 下的贴图。",
                "确定");
            return;
        }

        absPaths.Sort(StringComparer.Ordinal);

        // Phase 1: 扫描每张贴图的最低可见像素行（自底向上）。
        var scanned = new List<KeyValuePair<string, float>>(); // path -> lowestY/height
        foreach (string absPath in absPaths)
        {
            int lowestY, height;
            if (!TryGetLowestOpaqueRow(absPath, out lowestY, out height))
            {
                Debug.LogWarning($"[SpritePivotFixer] 跳过（无法读取像素）: {Path.GetFileName(absPath)}");
                continue;
            }
            float frac = (float)lowestY / height;
            scanned.Add(new KeyValuePair<string, float>(absPath, frac));
            Debug.Log($"[SpritePivotFixer] {Path.GetFileName(absPath)}: lowestY={lowestY}/{height} = {frac:0.####}");
        }

        if (scanned.Count == 0)
        {
            EditorUtility.DisplayDialog("无可用贴图", "没有可读取像素的 PNG。", "确定");
            return;
        }

        // Phase 2: 公共 pivotY = min(lowestY/height)（最低那一帧贴地，其余帧在其上）
        float commonPivotY = float.MaxValue;
        foreach (var kv in scanned)
            if (kv.Value < commonPivotY) commonPivotY = kv.Value;

        // Phase 3: 对每个 importer 写 alignment=Custom + spritePivot=(0.5, commonPivotY)
        int applied = 0;
        foreach (var kv in scanned)
        {
            string assetPath = AbsToAssetPath(kv.Key);
            if (string.IsNullOrEmpty(assetPath)) continue;
            if (ApplyCustomPivot(assetPath, 0.5f, commonPivotY))
                applied++;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"已处理 {applied}/{scanned.Count} 张贴图。\n公共 pivotY = {commonPivotY:0.####}\n" +
            "进 Play 模式确认 boss 不再下陷 / 悬浮。", "确定");
    }

    // ---- 路径收集 ----
    static List<string> CollectSelectedPngPaths()
    {
        var result = new List<string>();
        string projectRoot = Path.GetDirectoryName(Application.dataPath);

        foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) continue;

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                string abs = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(abs))
                    foreach (string p in Directory.GetFiles(abs, "*.png", SearchOption.AllDirectories))
                        result.Add(p);
            }
            else if (assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
            }
        }
        return result;
    }

    static string AbsToAssetPath(string absPath)
    {
        string dataPath = Application.dataPath; // .../Assets
        if (absPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            return "Assets" + absPath.Substring(dataPath.Length).Replace('\\', '/');
        return null;
    }

    // ---- 像素扫描（参考 SpriteVisualBoundsGenerator）----
    static bool TryGetLowestOpaqueRow(string absPath, out int lowestY, out int height)
    {
        lowestY = -1;
        height = 0;

        byte[] data = File.ReadAllBytes(absPath);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(data); // 与 SpriteVisualBoundsGenerator 一致，不改 isReadable
        int w = tex.width, h = tex.height;
        if (w <= 0 || h <= 0 || w * h <= 1)
        {
            UnityEngine.Object.DestroyImmediate(tex);
            return false;
        }
        Color32[] px = tex.GetPixels32(); // bottom-left origin, row-major bottom->top
        UnityEngine.Object.DestroyImmediate(tex);
        if (px == null || px.Length != w * h) return false;

        height = h;
        for (int y = 0; y < h; y++)
        {
            int rowBase = y * w;
            bool rowHasOpaque = false;
            for (int x = 0; x < w; x++)
            {
                if (px[rowBase + x].a >= AlphaThreshold) { rowHasOpaque = true; break; }
            }
            if (rowHasOpaque) { lowestY = y; break; }
        }
        return lowestY >= 0;
    }

    // ---- 写 pivot ----
    static bool ApplyCustomPivot(string assetPath, float pivotX, float pivotY)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return false;

        // 字段名取自 slimer*.png.meta: alignment / spritePivot（SerializedObject 直写最稳）
        var so = new SerializedObject(importer);
        so.FindProperty("alignment").intValue = 9;   // SpriteAlignment.Custom
        so.FindProperty("spritePivot").vector2Value = new Vector2(pivotX, pivotY);
        so.ApplyModifiedProperties();
        importer.SaveAndReimport();
        return true;
    }
}
