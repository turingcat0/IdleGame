using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成副本 UI 的编辑器工具（竖屏 1080×1920）。
/// 节点命名严格对应 Lua 侧 DungeonWindow / SweepResultUI / DungeonDeathUI / DungeonButton 里的 transform:Find 路径，
/// 改名会直接让 Lua 取不到组件。
/// 同时生成 Prefabs/DungeonGroupTab 和 Prefabs/DungeonListItem 预制体（运行时 DungeonWindow:BuildTabs / BuildList 加载）。
/// </summary>
public static class DungeonUIBuilder
{
    const string RootName           = "DungeonUI";
    const string ButtonName         = "DungeonButton";
    const string WindowName        = "DungeonWindow";
    const string SweepResultName   = "SweepResultUI";
    const string DeathUIName       = "DungeonDeathUI";
    const string FontPath          = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string ButtonLuaPath     = "Assets/Resources/Lua/LuaBehavior/UI/DungeonButton.lua.txt";
    const string WindowLuaPath     = "Assets/Resources/Lua/LuaBehavior/UI/DungeonWindow.lua.txt";
    const string SweepLuaPath      = "Assets/Resources/Lua/LuaBehavior/UI/SweepResultUI.lua.txt";
    const string DeathLuaPath      = "Assets/Resources/Lua/LuaBehavior/UI/DungeonDeathUI.lua.txt";
    const string GroupTabPrefabPath  = "Assets/Resources/Prefabs/DungeonGroupTab.prefab";
    const string ListItemPrefabPath  = "Assets/Resources/Prefabs/DungeonListItem.prefab";

    static TMP_FontAsset _font;
    static TextAsset _buttonLua;
    static TextAsset _windowLua;
    static TextAsset _sweepLua;
    static TextAsset _deathLua;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色（与 ServantWindowBuilder 同一套配色） ──
    static readonly Color ColBg       = new Color(0.025f, 0.035f, 0.055f, 0.92f);
    static readonly Color ColPanel    = new Color(0.055f, 0.075f, 0.105f, 1f);
    static readonly Color ColCard      = new Color(0.075f, 0.095f, 0.135f, 1f);
    static readonly Color ColTitle    = new Color(0.95f, 0.78f, 0.38f, 1f);
    static readonly Color ColBody      = new Color(0.90f, 0.91f, 0.92f, 1f);
    static readonly Color ColLabel    = new Color(0.55f, 0.67f, 0.78f, 1f);
    static readonly Color ColBtnNorm  = new Color(0.12f, 0.17f, 0.24f, 1f);
    static readonly Color ColBtnInact = new Color(0.09f, 0.12f, 0.17f, 1f);
    static readonly Color ColBtnEnter = new Color(0.55f, 0.30f, 0.08f, 1f);
    static readonly Color ColBtnSweep = new Color(0.20f, 0.38f, 0.18f, 1f);
    static readonly Color ColScroll   = new Color(0.035f, 0.055f, 0.085f, 0.78f);
    static readonly Color ColTabBg    = new Color(0.06f, 0.08f, 0.12f, 1f);

    // ── UI 中出现的全部中文（供 TMP Font Atlas 生成用） ──
    static readonly string[] AllChineseChars = new[]
    {
        "副本选择经验金币洞窟深初一个进入扫荡确认返回死亡",
        "推荐等级今日剩余次已通关未解锁已解锁掉落装备Boss",
        "水晶未掉落",
    };

    [MenuItem("Tools/UI/生成 DungeonUI")]
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
        _sweepLua  = AssetDatabase.LoadAssetAtPath<TextAsset>(SweepLuaPath);
        _deathLua  = AssetDatabase.LoadAssetAtPath<TextAsset>(DeathLuaPath);
        if (_buttonLua == null || _windowLua == null || _sweepLua == null || _deathLua == null)
        {
            EditorUtility.DisplayDialog("缺少 Lua 脚本",
                $"以下路径至少一个找不到:\n{ButtonLuaPath}\n{WindowLuaPath}\n{SweepLuaPath}\n{DeathLuaPath}", "确定");
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
        BuildSweepResult(root.GetComponent<RectTransform>());
        BuildDeathUI(root.GetComponent<RectTransform>());
        BuildGroupTabPrefab();
        BuildListItemPrefab();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseCharsForFontGeneration();

        EditorUtility.DisplayDialog("完成",
            "DungeonUI 已生成，DungeonGroupTab / DungeonListItem 预制体已保存。\n" +
            "请重新生成 TMP 字体 Atlas 以包含全部中文。", "确定");
    }

    // ==================== DungeonButton（入口按钮） ====================

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
        rect.anchoredPosition = new Vector2(-20f, -72f);

        var img = go.GetComponent<Image>();
        img.color = ColBtnNorm;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lb = go.GetComponent<LuaBehaviour>();
        lb.luaScript = _buttonLua;
        lb.injections = new Injection[0];

        var textGo = NewText(go.transform, "Text", "副本", 22, ColBody,
            TextAlignmentOptions.Center, false);
        Stretch(textGo.GetComponent<RectTransform>());
    }

    // ==================== DungeonWindow ====================

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

        // Background
        var bgGo = NewImage("Background", winRect, ColBg, raycast: true);
        Stretch(bgGo.GetComponent<RectTransform>());

        // WindowPanel
        var panelGo = NewImage("WindowPanel", bgGo.transform, ColPanel);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1000f, 1820f);

        // Title
        var titleGo = NewText(panelGo.transform, "Title", "副本选择", 42, ColTitle,
            TextAlignmentOptions.Left, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-64f, 80f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);

        // CloseButton
        var closeGo = NewButton("CloseButton", panelRect, "×", 36, ColBtnNorm, "Text");
        var closeRect = closeGo.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(64f, 64f);
        closeRect.anchoredPosition = new Vector2(-24f, -24f);

        // GroupTabs（水平 tab 条）
        BuildGroupTabs(panelRect);

        // ScrollView（副本列表）
        BuildScrollView(panelRect);

        // DetailCard
        var detailCard = NewImage("DetailCard", panelRect, ColCard);
        var detailRect = detailCard.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0f, 0f);
        detailRect.anchorMax = new Vector2(1f, 0f);
        detailRect.pivot = new Vector2(0.5f, 0f);
        detailRect.sizeDelta = new Vector2(-56f, 200f);
        detailRect.anchoredPosition = new Vector2(0f, 180f);

        var detailGo = NewText(detailCard.transform, "Detail", "选择一个副本", 24, ColBody,
            TextAlignmentOptions.TopLeft, true);
        Stretch(detailGo.GetComponent<RectTransform>(), 24f);

        // ActionBar
        var actionBar = NewRect("ActionBar", panelRect);
        var abRect = actionBar.GetComponent<RectTransform>();
        abRect.anchorMin = new Vector2(0f, 0f);
        abRect.anchorMax = new Vector2(1f, 0f);
        abRect.pivot = new Vector2(0.5f, 0f);
        abRect.sizeDelta = new Vector2(-56f, 64f);
        abRect.anchoredPosition = new Vector2(0f, 40f);

        var enterGo = NewButton("EnterButton", abRect, "进入", 24, ColBtnEnter, "Text");
        var enterRect = enterGo.GetComponent<RectTransform>();
        enterRect.anchorMin = new Vector2(0f, 0f);
        enterRect.anchorMax = new Vector2(0.5f, 1f);
        enterRect.pivot = new Vector2(0.5f, 0.5f);
        enterRect.offsetMin = new Vector2(10f, 0f);
        enterRect.offsetMax = new Vector2(-5f, 0f);

        var sweepGo = NewButton("SweepButton", abRect, "扫荡", 24, ColBtnSweep, "Text");
        var sweepRect = sweepGo.GetComponent<RectTransform>();
        sweepRect.anchorMin = new Vector2(0.5f, 0f);
        sweepRect.anchorMax = new Vector2(1f, 1f);
        sweepRect.pivot = new Vector2(0.5f, 0.5f);
        sweepRect.offsetMin = new Vector2(5f, 0f);
        sweepRect.offsetMax = new Vector2(-10f, 0f);

        // 入口按钮放到共享底栏
        var entryGo = NewButton("DungeonEntryButton", sharedBottomGroup, "副本", 24, ColBtnEnter, "Text");
        var entryLua = entryGo.AddComponent<LuaBehaviour>();
        entryLua.luaScript = _buttonLua;
        entryLua.injections = new Injection[0];

        var entryLayout = entryGo.AddComponent<LayoutElement>();
        entryLayout.preferredWidth = 240f;
        entryLayout.preferredHeight = 64f;
        entryLayout.flexibleWidth = 0f;
    }

    static void BuildGroupTabs(RectTransform parent)
    {
        var tabsGo = NewImage("GroupTabs", parent, ColTabBg, raycast: false);
        tabsGo.gameObject.AddComponent<ScrollRect>();
        tabsGo.gameObject.AddComponent<Mask>();
        tabsGo.GetComponent<Mask>().showMaskGraphic = false;

        var tabsRect = tabsGo.GetComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0f, 1f);
        tabsRect.anchorMax = new Vector2(1f, 1f);
        tabsRect.pivot = new Vector2(0.5f, 1f);
        tabsRect.sizeDelta = new Vector2(-56f, 60f);
        tabsRect.anchoredPosition = new Vector2(0f, -110f);

        var scroll = tabsGo.GetComponent<ScrollRect>();

        // Viewport
        var vpGo = NewImage("Viewport", tabsRect, new Color(0, 0, 0, 0), raycast: false);
        vpGo.gameObject.AddComponent<Mask>();
        vpGo.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(vpGo.GetComponent<RectTransform>());

        // Content (HorizontalLayoutGroup)
        var ctGo = new GameObject("Content",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        ctGo.transform.SetParent(vpGo, false);
        ctGo.layer = UILayer;
        var ctRect = ctGo.GetComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0f, 0.5f);
        ctRect.anchorMax = new Vector2(0f, 0.5f);
        ctRect.pivot = new Vector2(0f, 0.5f);
        ctRect.sizeDelta = Vector2.zero;
        ctRect.anchoredPosition = Vector2.zero;

        var hlg = ctGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        ctGo.GetComponent<ContentSizeFitter>().horizontalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = ctRect;
        scroll.viewport = vpGo.GetComponent<RectTransform>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
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
        sr.offsetMin = new Vector2(28f, 300f);
        sr.offsetMax = new Vector2(-28f, -190f);

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
        ctRect.pivot = new Vector2(0.5f, 1f);
        ctRect.sizeDelta = Vector2.zero;
        ctRect.anchoredPosition = Vector2.zero;

        var vlg = ctGo.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ctGo.GetComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = ctRect;
        scroll.viewport = vpGo.GetComponent<RectTransform>();
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    // ==================== SweepResultUI ====================

    static void BuildSweepResult(RectTransform parent)
    {
        var go = new GameObject(SweepResultName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup),
            typeof(GraphicRaycaster), typeof(LuaBehaviour));
        go.transform.SetParent(parent, false);
        go.layer = UILayer;

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        Stretch(go.GetComponent<RectTransform>());

        var lb = go.GetComponent<LuaBehaviour>();
        lb.luaScript = _sweepLua;
        lb.injections = new Injection[0];

        // 背景
        var bgGo = NewImage("Background", go.transform, ColBg, raycast: true);
        Stretch(bgGo.GetComponent<RectTransform>());

        // 居中面板（直接挂在根下，Lua 用 self.transform:Find 查找）
        var panelGo = NewImage("Panel", go.transform, ColPanel);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600f, 500f);

        var titleGo = NewText(go.transform, "Title", "扫荡结果", 32, ColTitle,
            TextAlignmentOptions.Center, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(400f, 50f);
        titleRect.anchoredPosition = new Vector2(0f, 180f);

        float yPos = 100f;
        var labels = new[] {
            ("ExpText", "经验"),
            ("CrystalText", "水晶"),
            ("CoinText", "金币"),
            ("ItemText", "掉落"),
        };
        foreach (var kv in labels)
        {
            var tGo = NewText(go.transform, kv.Item1, kv.Item2 + ": 0", 24, ColBody,
                TextAlignmentOptions.Left, false);
            var tRect = tGo.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 0.5f);
            tRect.anchorMax = new Vector2(0.5f, 0.5f);
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.sizeDelta = new Vector2(300f, 40f);
            tRect.anchoredPosition = new Vector2(-180f, yPos);
            yPos -= 50f;
        }

        var confirmGo = NewButton("ConfirmButton", go.GetComponent<RectTransform>(), "确认", 24, ColBtnEnter, "Text");
        var confirmRect = confirmGo.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
        confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
        confirmRect.pivot = new Vector2(0.5f, 0.5f);
        confirmRect.sizeDelta = new Vector2(200f, 54f);
        confirmRect.anchoredPosition = new Vector2(0f, -180f);
    }

    // ==================== DungeonDeathUI ====================

    static void BuildDeathUI(RectTransform parent)
    {
        var go = new GameObject(DeathUIName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup),
            typeof(GraphicRaycaster), typeof(LuaBehaviour));
        go.transform.SetParent(parent, false);
        go.layer = UILayer;

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        Stretch(go.GetComponent<RectTransform>());

        var lb = go.GetComponent<LuaBehaviour>();
        lb.luaScript = _deathLua;
        lb.injections = new Injection[0];

        // 背景
        var bgGo = NewImage("Background", go.transform, ColBg, raycast: true);
        Stretch(bgGo.GetComponent<RectTransform>());

        // 居中面板
        var panelGo = NewImage("Panel", go.transform, ColPanel);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 300f);

        var titleGo = NewText(go.transform, "Title", "挑战失败", 36, ColTitle,
            TextAlignmentOptions.Center, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(400f, 50f);
        titleRect.anchoredPosition = new Vector2(0f, 60f);

        var returnGo = NewButton("ReturnButton", go.GetComponent<RectTransform>(), "返回副本选择", 24, ColBtnEnter, "Text");
        var returnRect = returnGo.GetComponent<RectTransform>();
        returnRect.anchorMin = new Vector2(0.5f, 0.5f);
        returnRect.anchorMax = new Vector2(0.5f, 0.5f);
        returnRect.pivot = new Vector2(0.5f, 0.5f);
        returnRect.sizeDelta = new Vector2(280f, 54f);
        returnRect.anchoredPosition = new Vector2(0f, -60f);
    }

    // ==================== Prefabs ====================

    static void BuildGroupTabPrefab()
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(GroupTabPrefabPath));

        if (AssetDatabase.LoadAssetAtPath<GameObject>(GroupTabPrefabPath) != null)
            AssetDatabase.DeleteAsset(GroupTabPrefabPath);

        var go = new GameObject("DungeonGroupTab",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = UILayer;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160f, 50f);

        var img = go.GetComponent<Image>();
        img.color = ColBtnInact;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 160f;
        le.preferredHeight = 50f;
        le.flexibleWidth = 0f;

        var nameGo = NewText(go.transform, "Name", "分组", 20, ColBody,
            TextAlignmentOptions.Center, false);
        Stretch(nameGo.GetComponent<RectTransform>());

        PrefabUtility.SaveAsPrefabAsset(go, GroupTabPrefabPath);
        GameObject.DestroyImmediate(go);
    }

    static void BuildListItemPrefab()
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(ListItemPrefabPath));

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ListItemPrefabPath) != null)
            AssetDatabase.DeleteAsset(ListItemPrefabPath);

        var go = new GameObject("DungeonListItem",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = UILayer;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 60f);

        var img = go.GetComponent<Image>();
        img.color = ColBtnInact;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 60f;
        le.flexibleWidth = 1f;

        // Name（左半）
        var nameGo = NewText(go.transform, "Name", "副本名", 18, ColBody,
            TextAlignmentOptions.Left, false);
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0f, 0f);
        nr.anchorMax = new Vector2(0.6f, 1f);
        nr.pivot = new Vector2(0f, 0.5f);
        nr.offsetMin = new Vector2(12f, 0f);
        nr.offsetMax = Vector2.zero;

        // Info（右半）
        var infoGo = NewText(go.transform, "Info", "推荐 0 级", 14, ColLabel,
            TextAlignmentOptions.Right, false);
        var ir = infoGo.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.6f, 0f);
        ir.anchorMax = new Vector2(1f, 1f);
        ir.pivot = new Vector2(1f, 0.5f);
        ir.offsetMin = Vector2.zero;
        ir.offsetMax = new Vector2(-12f, 0f);

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
        rect.pivot = new Vector2(0.5f, 0.5f);
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

    [MenuItem("Tools/UI/导出 DungeonUI 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/DungeonUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[DungeonUIBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
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
