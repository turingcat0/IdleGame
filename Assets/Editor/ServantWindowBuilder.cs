using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成随从管理 UI 的编辑器工具。
/// 节点命名严格对应 Lua 侧 ServantWindow.lua.txt / ServantButton.lua.txt 里的 transform:Find 路径，
/// 改名会直接让 Lua 取不到组件。
/// 同时生成 Prefabs/ServantListItem 预制体（运行时 ServantWindow:BuildList 加载）。
/// </summary>
public static class ServantWindowBuilder
{
    const string RootName       = "ServantUI";
    const string ButtonName    = "ServantButton";
    const string WindowName    = "ServantWindow";
    const string FontPath      = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string ButtonLuaPath = "Assets/Resources/Lua/LuaBehavior/UI/ServantButton.lua.txt";
    const string WindowLuaPath = "Assets/Resources/Lua/LuaBehavior/UI/ServantWindow.lua.txt";
    const string ListItemPrefabPath = "Assets/Resources/Prefabs/ServantListItem.prefab";

    static TMP_FontAsset _font;
    static TextAsset _buttonLua;
    static TextAsset _windowLua;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色 ──
    static readonly Color ColBg        = new Color(0.025f, 0.035f, 0.055f, 0.92f);
    static readonly Color ColPanel     = new Color(0.055f, 0.075f, 0.105f, 1f);
    static readonly Color ColPanelEdge = new Color(0.18f, 0.22f, 0.28f, 1f);
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
        "随从主副空选择一个未上阵升级",
        "攻击防御生命法力速度光环普攻技能无",
        "护卫游侠碎击者钢铁壁垒疾风指引碎骨怒",
    };

    [MenuItem("Tools/UI/生成 ServantUI")]
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
            "ServantUI 已生成，ServantListItem 预制体已保存。\n" +
            "请重新生成 TMP 字体 Atlas 以包含全部中文。", "确定");
    }

    // ==================== ServantButton ====================

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
        rect.anchoredPosition = new Vector2(-20f, -20f);

        var img = go.GetComponent<Image>();
        img.color = ColBtnNorm;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lb = go.GetComponent<LuaBehaviour>();
        lb.luaScript = _buttonLua;
        lb.injections = new Injection[0];

        var textGo = NewText(go.transform, "Text", "随从", 22, ColBody,
            TextAlignmentOptions.Center, false);
        Stretch(textGo.GetComponent<RectTransform>());
    }

    // ==================== ServantWindow ====================

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

        var titleGo = NewText(panelGo.transform, "Title", "随从编队", 42, ColTitle,
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

        var upBtnGo = NewButton("UpgradeButton", panelRect, "升级", 22, ColBtnEnter, "Text");
        var upBtnRect = upBtnGo.GetComponent<RectTransform>();
        upBtnRect.anchorMin = new Vector2(1f, 1f);
        upBtnRect.anchorMax = new Vector2(1f, 1f);
        upBtnRect.pivot = new Vector2(1f, 1f);
        upBtnRect.sizeDelta = new Vector2(140f, 54f);
        upBtnRect.anchoredPosition = new Vector2(-104f, -30f);

        var detailCard = NewImage("DetailCard", panelRect, ColCard);
        var detailCardRect = detailCard.GetComponent<RectTransform>();
        detailCardRect.anchorMin = new Vector2(0f, 1f);
        detailCardRect.anchorMax = new Vector2(1f, 1f);
        detailCardRect.pivot = new Vector2(0.5f, 1f);
        detailCardRect.sizeDelta = new Vector2(-56f, 400f);
        detailCardRect.anchoredPosition = new Vector2(0f, -126f);

        var detailGo = NewText(detailCard.transform, "Detail", "选择一个随从", 24, ColBody,
            TextAlignmentOptions.TopLeft, true);
        var detailRect = detailGo.GetComponent<RectTransform>();
        Stretch(detailRect, 28f);

        var slotsGo = NewRect("SlotsPanel", panelRect);
        var slotsRect = slotsGo.GetComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0f, 1f);
        slotsRect.anchorMax = new Vector2(1f, 1f);
        slotsRect.pivot = new Vector2(0.5f, 1f);
        slotsRect.sizeDelta = new Vector2(-56f, 390f);
        slotsRect.anchoredPosition = new Vector2(0f, -550f);
        BuildSlots(slotsGo.GetComponent<RectTransform>());

        BuildScrollView(panelRect);

        var enterGo = NewButton("EnterButton", sharedBottomGroup, "随从", 24, ColBtnEnter, "Text");
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
        var labelMain = NewText(parent, "LabelMain", "主战位 · 上场战斗", 22, ColLabel,
            TextAlignmentOptions.Left, false);
        var lmRect = labelMain.GetComponent<RectTransform>();
        lmRect.anchorMin = new Vector2(0f, 1f);
        lmRect.anchorMax = new Vector2(1f, 1f);
        lmRect.pivot = new Vector2(0f, 1f);
        lmRect.sizeDelta = new Vector2(0f, 34f);
        lmRect.anchoredPosition = new Vector2(5f, 0f);

        float mainW = 300f, mainGap = 20f;
        for (int i = 0; i < 3; i++)
        {
            var go = NewButton($"SlotMain{i + 1}", parent, "空", 20, ColBtnInact, "Name");
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(mainW, 92f);
            r.anchoredPosition = new Vector2(5f + i * (mainW + mainGap), -46f);
        }

        var labelSub = NewText(parent, "LabelSub", "附属位 · 提供光环", 22, ColLabel,
            TextAlignmentOptions.Left, false);
        var lsRect = labelSub.GetComponent<RectTransform>();
        lsRect.anchorMin = new Vector2(0f, 1f);
        lsRect.anchorMax = new Vector2(1f, 1f);
        lsRect.pivot = new Vector2(0f, 1f);
        lsRect.sizeDelta = new Vector2(0f, 34f);
        lsRect.anchoredPosition = new Vector2(5f, -166f);

        float subW = 176f, subGap = 16f;
        for (int i = 0; i < 5; i++)
        {
            var go = NewButton($"SlotSub{i + 1}", parent, "空", 18, ColBtnInact, "Name");
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(subW, 82f);
            r.anchoredPosition = new Vector2(5f + i * (subW + subGap), -212f);
        }
    }

    static void BuildScrollView(RectTransform parent)
    {
        var scrollGo = NewImage("ScrollView", parent, ColScroll, raycast: false);
        // 补 ScrollRect + Mask（NewImage 只加了 Image）
        scrollGo.gameObject.AddComponent<ScrollRect>();
        scrollGo.gameObject.AddComponent<Mask>();
        scrollGo.GetComponent<Mask>().showMaskGraphic = false;

        var sr = scrollGo.GetComponent<RectTransform>();
        sr.anchorMin = Vector2.zero;
        sr.anchorMax = Vector2.one;
        sr.offsetMin = new Vector2(28f, 36f);
        sr.offsetMax = new Vector2(-28f, -1020f);

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

    // ==================== ServantListItem Prefab ====================

    static void BuildListItemPrefab()
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(ListItemPrefabPath));

        // 删除旧预制体
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ListItemPrefabPath) != null)
            AssetDatabase.DeleteAsset(ListItemPrefabPath);

        // 临时 GameObject
        var go = new GameObject("ServantListItem",
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
        var nameGo = NewText(go.transform, "Name", "随从名", 16, ColBody,
            TextAlignmentOptions.Left, false);
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0f, 0f);
        nr.anchorMax = new Vector2(0.5f, 1f);
        nr.pivot     = new Vector2(0f, 0.5f);
        nr.offsetMin = new Vector2(10f, 0f);
        nr.offsetMax = Vector2.zero;

        // Info（右半）
        var infoGo = NewText(go.transform, "Info", "未上阵", 14, ColLabel,
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

    [MenuItem("Tools/UI/导出 ServantUI 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/ServantUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[ServantWindowBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
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
