using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成技能选择 UI 的编辑器工具。
/// 节点命名严格对应 Lua 侧 SkillWindow.lua.txt / SkillButton.lua.txt 里的 transform:Find 路径，
/// 改名会直接让 Lua 取不到组件。
/// 同时生成 Prefabs/SkillListItem 预制体（运行时 SkillWindow:BuildList 加载）。
/// </summary>
public static class SkillWindowBuilder
{
    const string RootName       = "SkillUI";
    const string ButtonName     = "SkillButton";
    const string WindowName     = "SkillWindow";
    const string FontPath       = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string ButtonLuaPath  = "Assets/Resources/Lua/LuaBehavior/UI/SkillButton.lua.txt";
    const string WindowLuaPath  = "Assets/Resources/Lua/LuaBehavior/UI/SkillWindow.lua.txt";
    const string ListItemPrefabPath = "Assets/Resources/Prefabs/SkillListItem.prefab";

    static TMP_FontAsset _font;
    static TextAsset _buttonLua;
    static TextAsset _windowLua;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色 ──
    static readonly Color ColBg        = new Color(0.025f, 0.035f, 0.055f, 0.92f);
    static readonly Color ColPanel     = new Color(0.055f, 0.075f, 0.105f, 1f);
    static readonly Color ColCard      = new Color(0.075f, 0.095f, 0.135f, 1f);
    static readonly Color ColTitle     = new Color(0.95f, 0.78f, 0.38f, 1f);
    static readonly Color ColBody      = new Color(0.90f, 0.91f, 0.92f, 1f);
    static readonly Color ColLabel     = new Color(0.55f, 0.67f, 0.78f, 1f);
    static readonly Color ColBtnNorm   = new Color(0.12f, 0.17f, 0.24f, 1f);
    static readonly Color ColBtnInact  = new Color(0.09f, 0.12f, 0.17f, 1f);
    static readonly Color ColBtnEnter  = new Color(0.55f, 0.30f, 0.08f, 1f);
    static readonly Color ColScroll    = new Color(0.035f, 0.055f, 0.085f, 0.78f);

    // ── UI 中出现的全部中文（供 TMP Font Atlas 生成用） ──
    static readonly string[] AllChineseChars = new[]
    {
        "技能选择关闭普通战斗掌控",
        "锁定空已装备槽回蓝兜底消耗法力无类型",
        "选择一个未解锁等级倍率射程冷却演出回复",
        "火球碎甲毒液剑雨陨星天怒盾霜坠暴",
    };

    [MenuItem("Tools/UI/生成 SkillUI")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
        {
            EditorUtility.DisplayDialog("缺少字体", $"找不到字体资源: {FontPath}", "确定");
            return;
        }

        _buttonLua = AssetDatabase.LoadAssetAtPath<TextAsset>(ButtonLuaPath);
        _windowLua = AssetDatabase.LoadAssetAtPath<TextAsset>(WindowLuaPath);
        if (_buttonLua == null || _windowLua == null)
        {
            EditorUtility.DisplayDialog("缺少 Lua 脚本", $"{ButtonLuaPath}\n{WindowLuaPath}", "确定");
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

        var uiRoot = UIRootHelper.GetOrCreateUIRoot();
        var sharedBottomGroup = uiRoot.Find("Bottom/Group");
        if (sharedBottomGroup == null)
        {
            EditorUtility.DisplayDialog("缺少 UI 底部容器", "找不到 UIRoot/Bottom/Group，请先在场景中创建该节点。", "确定");
            return;
        }

        // 根 Canvas
        var root = new GameObject(RootName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
        root.layer = UILayer;
        root.transform.SetParent(uiRoot, false);

        var rootCanvas = root.GetComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var rootScaler = root.GetComponent<CanvasScaler>();
        rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootScaler.referenceResolution = new Vector2(1080f, 1920f);

        Stretch(root.GetComponent<RectTransform>());

        BuildButton(root.GetComponent<RectTransform>());
        BuildWindow(root.GetComponent<RectTransform>(), sharedBottomGroup.GetComponent<RectTransform>());
        BuildListItemPrefab();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseCharsForFontGeneration();

        EditorUtility.DisplayDialog("完成",
            "SkillUI 已生成，SkillListItem 预制体已保存。\n" +
            "请重新生成 TMP 字体 Atlas 以包含全部中文。", "确定");
    }

    // ==================== SkillButton ====================

    static void BuildButton(RectTransform parent)
    {
        var go = new GameObject(ButtonName,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LuaBehaviour));
        go.transform.SetParent(parent, false);
        go.layer = UILayer;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot     = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(120f, 44f);
        rect.anchoredPosition = new Vector2(-150f, -72f);

        var img = go.GetComponent<Image>();
        img.color = ColBtnNorm;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lb = go.GetComponent<LuaBehaviour>();
        lb.luaScript = _buttonLua;
        lb.injections = new Injection[0];

        var textGo = NewText(go.transform, "Text", "技能", 22, ColBody,
            TextAlignmentOptions.Center, false);
        Stretch(textGo.GetComponent<RectTransform>());
    }

    // ==================== SkillWindow ====================

    static void BuildWindow(RectTransform parent, RectTransform sharedBottomGroup)
    {
        var win = new GameObject(WindowName,
            typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(LuaBehaviour));
        win.transform.SetParent(parent, false);
        win.layer = UILayer;

        var winCanvas = win.GetComponent<Canvas>();
        winCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        winCanvas.overrideSorting = true;
        winCanvas.sortingOrder = 10;
        winCanvas.enabled = false;

        var winRect = win.GetComponent<RectTransform>();
        Stretch(winRect);

        var lb = win.GetComponent<LuaBehaviour>();
        lb.luaScript = _windowLua;
        lb.injections = new Injection[0];

        var bgGo = NewImage("Background", winRect, ColBg, raycast: true);
        var bgRect = bgGo.GetComponent<RectTransform>();
        Stretch(bgRect);

        var panelGo = NewImage("WindowPanel", bgRect, ColPanel);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1000f, 1820f);

        var titleGo = NewText(panelGo.transform, "Title", "技能选择", 42, ColTitle,
            TextAlignmentOptions.Left, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-64f, 80f);
        titleRect.anchoredPosition = new Vector2(28f, -24f);

        var closeGo = NewButton("CloseButton", panelRect, "×", 36, ColBtnNorm, "Text");
        var closeRect = closeGo.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(64f, 64f);
        closeRect.anchoredPosition = new Vector2(-24f, -24f);

        var slotsGo = NewRect("SlotsPanel", panelRect);
        var slotsRect = slotsGo.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0f, 1f);
        slotsRect.anchorMax = new Vector2(1f, 1f);
        slotsRect.pivot = new Vector2(0.5f, 1f);
        slotsRect.sizeDelta = new Vector2(-56f, 560f);
        slotsRect.anchoredPosition = new Vector2(0f, -126f);
        BuildSlots(slotsGo.GetComponent<RectTransform>());

        var detailCard = NewImage("DetailCard", panelRect, ColCard);
        var detailCardRect = detailCard.GetComponent<RectTransform>();
        detailCardRect.anchorMin = new Vector2(0f, 1f);
        detailCardRect.anchorMax = new Vector2(1f, 1f);
        detailCardRect.pivot = new Vector2(0.5f, 1f);
        detailCardRect.sizeDelta = new Vector2(-56f, 220f);
        detailCardRect.anchoredPosition = new Vector2(0f, -710f);

        var detailGo = NewText(detailCard.transform, "Detail", "选择一个技能", 22, ColBody,
            TextAlignmentOptions.TopLeft, true);
        var detailRect = detailGo.GetComponent<RectTransform>();
        Stretch(detailRect, 28f);

        BuildScrollView(panelRect);

        // 底部入口按钮
        var enterGo = NewButton("SkillEnterButton", sharedBottomGroup, "技能", 24, ColBtnEnter, "Text");
        var enterLua = enterGo.AddComponent<LuaBehaviour>();
        enterLua.luaScript = _buttonLua;
        enterLua.injections = new Injection[0];

        var enterLayout = enterGo.AddComponent<LayoutElement>();
        enterLayout.preferredWidth = 240f;
        enterLayout.preferredHeight = 64f;
        enterLayout.flexibleWidth = 0f;
    }

    static void BuildSlots(RectTransform parent)
    {
        float y = 0f;

        // ── 普通技 ──
        var labelNormal = NewText(parent, "LabelNormal", "普通技 · 回蓝兜底", 20, ColLabel,
            TextAlignmentOptions.Left, false);
        var lnRect = labelNormal.GetComponent<RectTransform>();
        lnRect.anchorMin = new Vector2(0f, 1f);
        lnRect.anchorMax = new Vector2(1f, 1f);
        lnRect.pivot = new Vector2(0f, 1f);
        lnRect.sizeDelta = new Vector2(0f, 30f);
        lnRect.anchoredPosition = new Vector2(5f, y);
        y -= 38f;

        var slotNormal = NewButton("SlotNormal", parent, "空", 20, ColBtnInact, "Name");
        var snRect = slotNormal.GetComponent<RectTransform>();
        snRect.anchorMin = new Vector2(0f, 1f);
        snRect.anchorMax = new Vector2(1f, 1f);
        snRect.pivot = new Vector2(0.5f, 1f);
        snRect.sizeDelta = new Vector2(-10f, 80f);
        snRect.anchoredPosition = new Vector2(0f, y);
        y -= 92f;

        // ── 战斗技 ──
        var labelCombat = NewText(parent, "LabelCombat", "战斗技 · 消耗法力", 20, ColLabel,
            TextAlignmentOptions.Left, false);
        var lcRect = labelCombat.GetComponent<RectTransform>();
        lcRect.anchorMin = new Vector2(0f, 1f);
        lcRect.anchorMax = new Vector2(1f, 1f);
        lcRect.pivot = new Vector2(0f, 1f);
        lcRect.sizeDelta = new Vector2(0f, 30f);
        lcRect.anchoredPosition = new Vector2(5f, y);
        y -= 38f;

        var slotCombat = NewButton("SlotCombat", parent, "空", 20, ColBtnInact, "Name");
        var scRect = slotCombat.GetComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0f, 1f);
        scRect.anchorMax = new Vector2(1f, 1f);
        scRect.pivot = new Vector2(0.5f, 1f);
        scRect.sizeDelta = new Vector2(-10f, 80f);
        scRect.anchoredPosition = new Vector2(0f, y);
        y -= 92f;

        // ── 掌控技 ──
        var labelSig = NewText(parent, "LabelSignature", "掌控技 · 无消耗", 20, ColLabel,
            TextAlignmentOptions.Left, false);
        var lsRect = labelSig.GetComponent<RectTransform>();
        lsRect.anchorMin = new Vector2(0f, 1f);
        lsRect.anchorMax = new Vector2(1f, 1f);
        lsRect.pivot = new Vector2(0f, 1f);
        lsRect.sizeDelta = new Vector2(0f, 30f);
        lsRect.anchoredPosition = new Vector2(5f, y);
        y -= 38f;

        float sigH = 68f, sigGap = 8f;
        for (int i = 0; i < 3; i++)
        {
            var go = NewButton($"SlotSignature{i + 1}", parent, "空", 18, ColBtnInact, "Name");
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.sizeDelta = new Vector2(-10f, sigH);
            r.anchoredPosition = new Vector2(0f, y);
            y -= sigH + sigGap;
        }
    }

    static void BuildScrollView(RectTransform parent)
    {
        var scrollGo = NewImage("ScrollView", parent, ColScroll, raycast: false);
        scrollGo.gameObject.AddComponent<ScrollRect>();
        scrollGo.gameObject.AddComponent<Mask>();
        scrollGo.GetComponent<Mask>().showMaskGraphic = false;

        var sr = scrollGo.GetComponent<RectTransform>();
        sr.anchorMin = Vector2.zero;
        sr.anchorMax = Vector2.one;
        sr.offsetMin = new Vector2(28f, 36f);
        sr.offsetMax = new Vector2(-28f, -960f);

        var scroll = scrollGo.GetComponent<ScrollRect>();

        // Viewport
        var vpGo = NewImage("Viewport", sr, new Color(0.1f, 0.1f, 0.1f, 0.3f), raycast: false);
        vpGo.gameObject.AddComponent<Mask>();
        vpGo.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(vpGo.GetComponent<RectTransform>());

        // Content
        var ctGo = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        ctGo.transform.SetParent(vpGo, false);
        ctGo.layer = UILayer;
        var ctRect = ctGo.GetComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0f, 1f);
        ctRect.anchorMax = new Vector2(1f, 1f);
        ctRect.pivot     = new Vector2(0.5f, 1f);
        ctRect.sizeDelta = Vector2.zero;
        ctRect.anchoredPosition = Vector2.zero;

        var vlg = ctGo.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = true;

        ctGo.GetComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scroll.content   = ctRect;
        scroll.viewport  = vpGo.GetComponent<RectTransform>();
        scroll.vertical  = true;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    // ==================== SkillListItem Prefab ====================

    static void BuildListItemPrefab()
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(ListItemPrefabPath));

        // 删除旧预制体
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ListItemPrefabPath) != null)
            AssetDatabase.DeleteAsset(ListItemPrefabPath);

        // 临时 GameObject
        var go = new GameObject("SkillListItem",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = UILayer;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 50f);

        var img = go.GetComponent<Image>();
        img.color = ColBtnInact;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 50f;
        le.flexibleWidth = 1f;

        // Name（左半）
        var nameGo = NewText(go.transform, "Name", "技能名", 16, ColBody,
            TextAlignmentOptions.Left, false);
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0f, 0f);
        nr.anchorMax = new Vector2(0.5f, 1f);
        nr.pivot     = new Vector2(0f, 0.5f);
        nr.offsetMin = new Vector2(10f, 0f);
        nr.offsetMax = Vector2.zero;

        // Info（右半）
        var infoGo = NewText(go.transform, "Info", "普通", 14, ColLabel,
            TextAlignmentOptions.Right, false);
        var ir = infoGo.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.5f, 0f);
        ir.anchorMax = new Vector2(1f, 1f);
        ir.pivot     = new Vector2(1f, 0.5f);
        ir.offsetMin = Vector2.zero;
        ir.offsetMax = new Vector2(-10f, 0f);

        // 保存为预制体
        PrefabUtility.SaveAsPrefabAsset(go, ListItemPrefabPath);
        GameObject.DestroyImmediate(go);
    }

    // ==================== Helpers ====================

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = UILayer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static RectTransform NewImage(string name, Transform parent, Color color, bool raycast = false)
    {
        var rect = NewRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return rect;
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

    /// <summary>
    /// 创建 Button + Image，子节点 childName 为 TMP 文本。
    /// 槽位按钮的子节点必须叫 "Name"，其它按钮子节点叫 "Text"。
    /// </summary>
    static GameObject NewButton(string name, RectTransform parent, string label,
        int fontSize, Color color, string childName)
    {
        var rect = NewImage(name, parent, color, raycast: true);
        var btn = rect.gameObject.AddComponent<Button>();
        btn.targetGraphic = rect.GetComponent<Image>();

        var textGo = NewText(rect, childName, label, fontSize, ColBody,
            TextAlignmentOptions.Center, false);
        Stretch(textGo.GetComponent<RectTransform>());

        return rect.gameObject;
    }

    static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        rect.anchoredPosition = Vector2.zero;
    }

    static void EnsureFolder(string path)
    {
        var parts = path.Replace('\\', '/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ==================== Chinese Chars ====================

    [MenuItem("Tools/UI/导出 SkillUI 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/SkillUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[SkillWindowBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
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
