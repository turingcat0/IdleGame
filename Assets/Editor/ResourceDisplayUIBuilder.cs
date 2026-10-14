using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成战斗资源显示 UI 的编辑器工具。
/// 右上角显示金币和水晶数量，节点命名严格对应 Lua 侧 ResourceDisplayUI.lua.txt
/// 里的 transform:Find 路径，改名会直接让 Lua 取不到组件。
/// </summary>
public static class ResourceDisplayUIBuilder
{
    const string RootName  = "ResourceDisplayUI";
    const string FontPath = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string LuaPath  = "Assets/Resources/Lua/LuaBehavior/UI/ResourceDisplayUI.lua.txt";

    static TMP_FontAsset _font;
    static TextAsset _luaScript;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色 ──
    static readonly Color ColPanel  = new Color(0.08f, 0.08f, 0.08f, 0.80f);
    static readonly Color ColText   = new Color(0.92f, 0.92f, 0.92f, 1f);
    static readonly Color ColCoin    = new Color(0.95f, 0.78f, 0.25f, 1f);   // 金色
    static readonly Color ColCrystal = new Color(0.30f, 0.70f, 0.95f, 1f);   // 蓝色

    // ── UI 中出现的全部中文（供 TMP Font Atlas 生成用） ──
    static readonly string[] AllChineseChars = new[]
    {
        "金币水晶",
    };

    [MenuItem("Tools/UI/生成 ResourceDisplayUI")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
        {
            EditorUtility.DisplayDialog("缺少字体", $"找不到字体资源: {FontPath}", "确定");
            return;
        }

        _luaScript = AssetDatabase.LoadAssetAtPath<TextAsset>(LuaPath);
        if (_luaScript == null)
        {
            EditorUtility.DisplayDialog("缺少 Lua 脚本", $"找不到 Lua 脚本: {LuaPath}", "确定");
            return;
        }

        // 删除旧的同名根节点
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("已存在同名对象",
                $"场景里已经有 {RootName}。\n重新生成会删掉它。", "删掉重建", "取消");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        // 根 Canvas
        var root = new GameObject(RootName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(LuaBehaviour));
        Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
        root.layer = UILayer;
        root.transform.SetParent(UIRootHelper.GetOrCreateUIRoot(), false);

        var rootCanvas = root.GetComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 6;

        var rootScaler = root.GetComponent<CanvasScaler>();
        rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootScaler.referenceResolution = new Vector2(1920f, 1080f);

        Stretch(root.GetComponent<RectTransform>());

        var lb = root.GetComponent<LuaBehaviour>();
        lb.luaScript = _luaScript;
        lb.injections = new Injection[0];

        BuildPanel(root.GetComponent<RectTransform>());

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseCharsForFontGeneration();

        EditorUtility.DisplayDialog("完成",
            "ResourceDisplayUI 已生成。\n请重新生成 TMP 字体 Atlas 以包含全部中文。", "确定");
    }

    static void BuildPanel(RectTransform parent)
    {
        // Panel 背景
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(parent, false);
        panelGo.layer = UILayer;

        var panelRect = panelGo.GetComponent<RectTransform>();
        // 右上角锚定
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot     = new Vector2(1f, 1f);
        panelRect.sizeDelta = new Vector2(260f, 72f);
        panelRect.anchoredPosition = new Vector2(-16f, -16f);

        var panelImg = panelGo.GetComponent<Image>();
        panelImg.color = ColPanel;

        // ── 金币条目 ──
        BuildEntry(panelGo.transform, "CoinEntry", ColCoin, "金币 100", 0);

        // ── 水晶条目 ──
        BuildEntry(panelGo.transform, "CrystalEntry", ColCrystal, "水晶 100", 1);
    }

    /// <summary>
    /// 构建一行资源条目：图标 + 数量文本
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="entryName">条目名（CoinEntry / CrystalEntry）</param>
    /// <param name="iconColor">图标颜色</param>
    /// <param name="defaultText">默认文本</param>
    /// <param name="rowIndex">行索引 0 或 1</param>
    static void BuildEntry(Transform parent, string entryName, Color iconColor,
        string defaultText, int rowIndex)
    {
        var entryGo = new GameObject(entryName, typeof(RectTransform));
        entryGo.transform.SetParent(parent, false);
        entryGo.layer = UILayer;

        var entryRect = entryGo.GetComponent<RectTransform>();
        // 整行拉伸，高度固定 32
        entryRect.anchorMin = new Vector2(0f, 1f);
        entryRect.anchorMax = new Vector2(1f, 1f);
        entryRect.pivot     = new Vector2(0f, 1f);
        entryRect.sizeDelta = new Vector2(0f, 32f);
        entryRect.anchoredPosition = new Vector2(0f, -rowIndex * 36f);

        // Icon
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(entryGo.transform, false);
        iconGo.layer = UILayer;

        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot     = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(24f, 24f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);

        var iconImg = iconGo.GetComponent<Image>();
        iconImg.color = iconColor;

        // Count 文本
        var textGo = NewText(entryGo.transform, "Count", defaultText, 18, ColText,
            TextAlignmentOptions.Left, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot     = new Vector2(0f, 0.5f);
        textRect.offsetMin = new Vector2(36f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);
    }

    // ==================== Helpers ====================

    static GameObject NewText(Transform parent, string name, string text,
        int fontSize, Color color, TextAlignmentOptions alignment, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = UILayer;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = _font;
        tmp.fontSharedMaterial = _font.material;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = raycast;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return go;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    // ==================== Chinese Chars ====================

    [MenuItem("Tools/UI/导出 ResourceDisplayUI 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/ResourceDisplayUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[ResourceDisplayUIBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
    }

    static string CollectChineseChars()
    {
        var set = new HashSet<char>();
        foreach (var line in AllChineseChars)
            foreach (var c in line)
                if (c >= 0x4E00 && c <= 0x9FFF)
                    set.Add(c);
        var list = new List<char>(set);
        list.Sort();
        return new string(list.ToArray());
    }
}
