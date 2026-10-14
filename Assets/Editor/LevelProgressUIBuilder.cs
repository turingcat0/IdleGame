using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成关卡进度 UI 的编辑器工具。
/// 节点命名严格对应 Lua 侧 LevelProgressUI.lua.txt 里的 transform:Find 路径，
/// 改名会直接让 Lua 取不到组件。
/// </summary>
public static class LevelProgressUIBuilder
{
    const string RootName  = "LevelProgressUI";
    const string FontPath = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string LuaPath  = "Assets/Resources/Lua/LuaBehavior/UI/LevelProgressUI.lua.txt";

    static TMP_FontAsset _font;
    static TextAsset _luaScript;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色 ──
    static readonly Color ColBar     = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    static readonly Color ColText    = new Color(0.90f, 0.90f, 0.90f, 1f);
    static readonly Color ColBtnBoss = new Color(0.35f, 0.15f, 0.15f, 1f);

    // ── UI 中出现的全部中文（供 TMP Font Atlas 生成用） ──
    // 包含章节名、关卡名、Boss 名、技能名中可能出现的全部汉字
    static readonly string[] AllChineseChars = new[]
    {
        "进入第章荒野深渊熔岩波次战",
        "入口深处",
        "巨人暗影领主炎魔毁灭巨龙",
        "重击箭烈焰吐息冲击",
        "副本经验洞窟金币矿初",
    };

    [MenuItem("Tools/UI/生成 LevelProgressUI")]
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
        rootCanvas.sortingOrder = 5;

        var rootScaler = root.GetComponent<CanvasScaler>();
        rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootScaler.referenceResolution = new Vector2(1920f, 1080f);

        Stretch(root.GetComponent<RectTransform>());

        var lb = root.GetComponent<LuaBehaviour>();
        lb.luaScript = _luaScript;
        lb.injections = new Injection[0];

        BuildTopBar(root.GetComponent<RectTransform>());

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseCharsForFontGeneration();

        EditorUtility.DisplayDialog("完成",
            "LevelProgressUI 已生成。\n请重新生成 TMP 字体 Atlas 以包含全部中文。", "确定");
    }

    static void BuildTopBar(RectTransform parent)
    {
        // TopBar 背景
        var barGo = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        barGo.transform.SetParent(parent, false);
        barGo.layer = UILayer;

        var barRect = barGo.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot     = new Vector2(0.5f, 1f);
        barRect.sizeDelta = new Vector2(0f, 48f);
        barRect.anchoredPosition = Vector2.zero;

        var barImg = barGo.GetComponent<Image>();
        barImg.color = ColBar;

        // ProgressText
        var textGo = NewText(barGo.transform, "ProgressText", "", 22, ColText,
            TextAlignmentOptions.Left, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(0.6f, 1f);
        textRect.pivot     = new Vector2(0f, 0.5f);
        textRect.offsetMin = new Vector2(20f, 0f);
        textRect.offsetMax = new Vector2(0f, 0f);

        // BossButton
        var btnGo = new GameObject("BossButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(barGo.transform, false);
        btnGo.layer = UILayer;
        btnGo.SetActive(false);

        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 0.5f);
        btnRect.anchorMax = new Vector2(1f, 0.5f);
        btnRect.pivot     = new Vector2(1f, 0.5f);
        btnRect.sizeDelta = new Vector2(140f, 36f);
        btnRect.anchoredPosition = new Vector2(-20f, 0f);

        var btnImg = btnGo.GetComponent<Image>();
        btnImg.color = ColBtnBoss;

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = btnImg;

        var btnTextGo = NewText(btnGo.transform, "Text", "进入Boss战", 20, ColText,
            TextAlignmentOptions.Center, false);
        Stretch(btnTextGo.GetComponent<RectTransform>());
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

    [MenuItem("Tools/UI/导出 LevelProgressUI 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/LevelProgressUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[LevelProgressUIBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
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
