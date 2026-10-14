using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成 Boss 血条 UI 的编辑器工具。
/// 节点命名严格对应 Lua 侧 BossBar.lua.txt 里的 transform:Find 路径，
/// 改名会直接让 Lua 取不到组件。
/// </summary>
public static class BossBarBuilder
{
    const string RootName = "BossBar";
    const string FontPath = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string LuaPath  = "Assets/Resources/Lua/LuaBehavior/UI/BossBar.lua.txt";

    static TMP_FontAsset _font;
    static TextAsset _luaScript;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色 ──
    static readonly Color ColBorder = new Color(0.10f, 0.10f, 0.10f, 0.90f);
    static readonly Color ColBg      = new Color(0.04f, 0.04f, 0.04f, 0.95f);
    static readonly Color ColFill    = new Color(0.80f, 0.15f, 0.15f, 1f);
    static readonly Color ColGhost   = new Color(1.00f, 0.85f, 0.30f, 0.80f);
    static readonly Color ColName    = new Color(0.95f, 0.92f, 0.80f, 1f);

    // ── UI 中出现的全部中文（供 TMP Font Atlas 生成用） ──
    // Boss 名（Enemies.csv）
    static readonly string[] AllChineseChars = new[]
    {
        "巨龙领主深渊守卫",
        "熔岩巨人暗影领主",
        "炎魔毁灭",
    };

    [MenuItem("Tools/UI/生成 BossBar")]
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
        rootCanvas.sortingOrder = 10; // 压在 LevelProgressUI(5) 之上

        var rootScaler = root.GetComponent<CanvasScaler>();
        rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootScaler.referenceResolution = new Vector2(1920f, 1080f);

        Stretch(root.GetComponent<RectTransform>());

        var lb = root.GetComponent<LuaBehaviour>();
        lb.luaScript = _luaScript;
        lb.injections = new Injection[0];

        BuildBossBar(root.GetComponent<RectTransform>());

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseCharsForFontGeneration();

        EditorUtility.DisplayDialog("完成",
            "BossBar 已生成。\n请重新生成 TMP 字体 Atlas 以包含全部中文。", "确定");
    }

    static void BuildBossBar(RectTransform parent)
    {
        // Border —— 外框，锚定屏幕顶部居中（位于 TopBar 下方）
        var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(parent, false);
        borderGo.layer = UILayer;

        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 1f);
        borderRect.anchorMax = new Vector2(0.5f, 1f);
        borderRect.pivot     = new Vector2(0.5f, 1f);
        borderRect.sizeDelta = new Vector2(720f, 36f);
        borderRect.anchoredPosition = new Vector2(0f, -60f);
        borderGo.GetComponent<Image>().color = ColBorder;

        // Bg —— 血条底，内框留 3px 边
        var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(borderGo.transform, false);
        bgGo.layer = UILayer;

        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.pivot     = new Vector2(0.5f, 0.5f);
        bgRect.offsetMin = new Vector2(3f, 3f);
        bgRect.offsetMax = new Vector2(-3f, -3f);
        bgGo.GetComponent<Image>().color = ColBg;

        // Ghost —— 滞后残影（先创建，渲染在 Fill 之下）
        var ghostGo = NewFilled("Ghost", bgGo.transform, ColGhost);
        // Fill —— 即时血量（渲染在 Ghost 之上）
        var fillGo  = NewFilled("Fill",  bgGo.transform, ColFill);

        // BossName —— 名字（root 的直接子节点，居中覆盖在 Border 上）
        var nameGo = NewText(parent, "BossName", "", 20, ColName,
            TextAlignmentOptions.Center, false);
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot     = new Vector2(0.5f, 1f);
        nameRect.sizeDelta = new Vector2(720f, 36f);
        nameRect.anchoredPosition = new Vector2(0f, -60f);
    }

    // ==================== Helpers ====================

    static GameObject NewFilled(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = UILayer;
        Stretch(go.GetComponent<RectTransform>());

        var img = go.GetComponent<Image>();
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = 0; // Left
        img.fillAmount = 1f;
        return go;
    }

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

    [MenuItem("Tools/UI/导出 BossBar 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/BossBar_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[BossBarBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
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
