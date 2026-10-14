using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成随从抽奖 UI 的编辑器工具（竖屏 9:16，1080x1920）。
/// 节点命名严格对应 Lua 侧 GachaWindow.lua.txt 里的 transform:Find 路径，
/// 改名会直接让 Lua 取不到组件。
/// 视觉配色与 ServantWindowBuilder.cs / ServantUpgradeWindowBuilder.cs 严格对齐。
/// 同时生成 Prefabs/GachaResultCard 预制体（运行时填充结果区）。
/// </summary>
public static class GachaWindowBuilder
{
    const string RootName       = "GachaUI";
    const string ButtonName    = "GachaButton";
    const string WindowName    = "GachaWindow";
    const string FontPath      = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string ButtonLuaPath = "Assets/Resources/Lua/LuaBehavior/UI/GachaButton.lua.txt";
    const string WindowLuaPath = "Assets/Resources/Lua/LuaBehavior/UI/GachaWindow.lua.txt";
    const string CardPrefabPath = "Assets/Resources/Prefabs/GachaResultCard.prefab";

    static TMP_FontAsset _font;
    static TextAsset _buttonLua;
    static TextAsset _windowLua;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色（与 ServantWindowBuilder / ServantUpgradeWindowBuilder 保持一致） ──
    static readonly Color ColBg       = new Color(0.025f, 0.035f, 0.055f, 0.92f);
    static readonly Color ColPanel    = new Color(0.055f, 0.075f, 0.105f, 1f);
    static readonly Color ColCard     = new Color(0.075f, 0.095f, 0.135f, 1f);
    static readonly Color ColTitle    = new Color(0.95f, 0.78f, 0.38f, 1f);
    static readonly Color ColBody     = new Color(0.90f, 0.91f, 0.92f, 1f);
    static readonly Color ColLabel    = new Color(0.55f, 0.67f, 0.78f, 1f);
    static readonly Color ColGold     = new Color(0.95f, 0.75f, 0.35f, 1f);
    static readonly Color ColBtnNorm  = new Color(0.12f, 0.17f, 0.24f, 1f);
    static readonly Color ColBtnInact = new Color(0.09f, 0.12f, 0.17f, 1f);
    static readonly Color ColBtnEnter = new Color(0.55f, 0.30f, 0.08f, 1f);
    static readonly Color ColScroll   = new Color(0.035f, 0.055f, 0.085f, 0.78f);
    static readonly Color ColPityBg   = new Color(0.06f, 0.08f, 0.11f, 1f);
    static readonly Color ColPityFill = new Color(0.95f, 0.75f, 0.35f, 1f);

    // ── UI 中出现的全部中文 ──
    static readonly string[] AllChineseChars = new[]
    {
        "随从召唤金币保底进度标准十连重复获得精魄新随从",
    };

    [MenuItem("Tools/UI/生成 GachaUI")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null) { EditorUtility.DisplayDialog("缺少字体", $"找不到: {FontPath}", "确定"); return; }

        _buttonLua = AssetDatabase.LoadAssetAtPath<TextAsset>(ButtonLuaPath);
        _windowLua = AssetDatabase.LoadAssetAtPath<TextAsset>(WindowLuaPath);
        if (_buttonLua == null || _windowLua == null)
        {
            EditorUtility.DisplayDialog("缺少 Lua", $"{ButtonLuaPath}\n{WindowLuaPath}", "确定");
            return;
        }

        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("已存在", $"场景里已有 {RootName}，删掉重建？", "删掉重建", "取消")) return;
            Undo.DestroyObjectImmediate(existing);
        }

        var uiRoot = UIRootHelper.GetOrCreateUIRoot();

        var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
        root.layer = UILayer;
        root.transform.SetParent(uiRoot, false);

        var rootCanvas = root.GetComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var rootScaler = root.GetComponent<CanvasScaler>();
        rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootScaler.referenceResolution = new Vector2(1080f, 1920f);
        rootScaler.matchWidthOrHeight = 0f;

        Stretch(root.GetComponent<RectTransform>());

        BuildButton(root.GetComponent<RectTransform>());
        BuildWindow(root.GetComponent<RectTransform>());
        BuildCardPrefab();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseChars();

        EditorUtility.DisplayDialog("完成", "GachaUI 已生成。\n请重新生成 TMP 字体 Atlas。", "确定");
    }

    // ==================== GachaButton（入口按钮，右上角，在 ServantButton 左边） ====================

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
        rect.anchoredPosition = new Vector2(-150f, -20f);

        var img = go.GetComponent<Image>();
        img.color = ColBtnNorm;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lb = go.GetComponent<LuaBehaviour>();
        lb.luaScript = _buttonLua;
        lb.injections = new Injection[0];

        var textGo = NewText(go.transform, "Text", "召唤", 22, ColBody,
            TextAlignmentOptions.Center, false);
        Stretch(textGo.GetComponent<RectTransform>());
    }

    // ==================== GachaWindow ====================
    // 竖屏单列纵向布局（自上而下）：
    //   TopBar       y: 0.95 ~ 1.00 (含 Title / 金币 / 关闭，固定 90px)
    //   BannerArea   y: 0.72 ~ 0.95 (banner 占位图 + 池名)
    //   PityBar      y: 0.62 ~ 0.72 (保底进度条)
    //   ButtonArea   y: 0.48 ~ 0.62 (单抽 / 十连按钮)
    //   ResultArea   y: 0.00 ~ 0.48 (结果卡横向滚动列表)

    static void BuildWindow(RectTransform parent)
    {
        var win = new GameObject(WindowName,
            typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(LuaBehaviour));
        win.transform.SetParent(parent, false);
        win.layer = UILayer;

        var winCanvas = win.GetComponent<Canvas>();
        winCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        winCanvas.overrideSorting = true;
        winCanvas.sortingOrder = 11;
        winCanvas.enabled = false;

        var winRect = win.GetComponent<RectTransform>();
        Stretch(winRect);

        var lb = win.GetComponent<LuaBehaviour>();
        lb.luaScript = _windowLua;
        lb.injections = new Injection[0];

        var bgGo = NewImage("Background", winRect, ColBg, true);
        Stretch(bgGo.GetComponent<RectTransform>());

        BuildTopBar(bgGo.transform);
        BuildBannerArea(bgGo.transform);
        BuildPityBar(bgGo.transform);
        BuildButtonArea(bgGo.transform);
        BuildResultArea(bgGo.transform);
    }

    // ── TopBar ──
    static void BuildTopBar(Transform parent)
    {
        var barGo = NewRect("TopBar", parent);
        var bar = barGo.GetComponent<RectTransform>();
        bar.anchorMin = new Vector2(0, 1); bar.anchorMax = Vector2.one;
        bar.pivot = new Vector2(0.5f, 1); bar.sizeDelta = new Vector2(0, 90); bar.anchoredPosition = Vector2.zero;

        var title = NewText(barGo, "Title", "随从召唤", 32, ColTitle, TextAlignmentOptions.Left, false);
        var tr = title.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(0.4f, 1);
        tr.offsetMin = new Vector2(24, 0); tr.offsetMax = Vector2.zero;

        var coinLabel = NewText(barGo, "CoinLabel", "金币", 16, ColLabel, TextAlignmentOptions.Right, false);
        var clr = coinLabel.GetComponent<RectTransform>();
        clr.anchorMin = new Vector2(0.50f, 0); clr.anchorMax = new Vector2(0.66f, 1);
        clr.offsetMin = Vector2.zero; clr.offsetMax = Vector2.zero;

        var coinText = NewText(barGo, "CoinText", "0", 18, ColGold, TextAlignmentOptions.Right, false);
        var ctr = coinText.GetComponent<RectTransform>();
        ctr.anchorMin = new Vector2(0.66f, 0); ctr.anchorMax = new Vector2(0.84f, 1);
        ctr.offsetMin = Vector2.zero; ctr.offsetMax = new Vector2(-5, 0);

        var closeGo = NewButton("CloseButton", bar, "×", 26, ColBtnNorm, "Text");
        var cr = closeGo.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(1, 0.5f); cr.anchorMax = new Vector2(1, 0.5f);
        cr.pivot = new Vector2(1, 0.5f); cr.sizeDelta = new Vector2(56, 56);
        cr.anchoredPosition = new Vector2(-12, 0);
    }

    // ── BannerArea（banner 占位图 + 池名） ──
    static void BuildBannerArea(Transform parent)
    {
        var panelGo = NewImage("BannerArea", parent, ColPanel);
        var pr = panelGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0, 0.72f); pr.anchorMax = new Vector2(1, 0.95f);
        pr.offsetMin = new Vector2(20, 0); pr.offsetMax = new Vector2(-20, 0);

        var bannerGo = NewImage("BannerImage", pr, ColBtnInact);
        var br = bannerGo.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0); br.anchorMax = Vector2.one;
        br.offsetMin = new Vector2(10, 10); br.offsetMax = new Vector2(-10, -10);

        var poolName = NewText(pr, "PoolName", "标准召唤", 28, ColTitle, TextAlignmentOptions.Center, false);
        var pnr = poolName.GetComponent<RectTransform>();
        pnr.anchorMin = Vector2.zero; pnr.anchorMax = Vector2.one;
        pnr.offsetMin = Vector2.zero; pnr.offsetMax = Vector2.zero;
    }

    // ── PityBar（保底进度条） ──
    static void BuildPityBar(Transform parent)
    {
        var barGo = NewRect("PityBar", parent);
        var br = barGo.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0.62f); br.anchorMax = new Vector2(1, 0.72f);
        br.offsetMin = new Vector2(20, 5); br.offsetMax = new Vector2(-20, -5);

        var pityLabel = NewText(br, "PityLabel", "保底进度", 18, ColLabel, TextAlignmentOptions.Left, false);
        var plr = pityLabel.GetComponent<RectTransform>();
        plr.anchorMin = new Vector2(0, 0); plr.anchorMax = new Vector2(0.25f, 1);
        plr.offsetMin = new Vector2(10, 0); plr.offsetMax = Vector2.zero;

        // 进度条背景
        var pityBg = NewImage("PityBg", br, ColPityBg);
        var pbr = pityBg.GetComponent<RectTransform>();
        pbr.anchorMin = new Vector2(0.25f, 0.2f); pbr.anchorMax = new Vector2(0.80f, 0.8f);
        pbr.offsetMin = Vector2.zero; pbr.offsetMax = Vector2.zero;

        // 进度条填充（PityBg 的子节点，用 offsetMax 收缩表示进度）
        var pityFill = NewImage("PityFill", pityBg, ColPityFill);
        Stretch(pityFill.GetComponent<RectTransform>());

        var pityCount = NewText(br, "PityCount", "0/50", 18, ColBody, TextAlignmentOptions.Right, false);
        var pcr = pityCount.GetComponent<RectTransform>();
        pcr.anchorMin = new Vector2(0.80f, 0); pcr.anchorMax = new Vector2(1, 1);
        pcr.offsetMin = Vector2.zero; pcr.offsetMax = new Vector2(-10, 0);
    }

    // ── ButtonArea（单抽 / 十连） ──
    static void BuildButtonArea(Transform parent)
    {
        var areaGo = NewRect("ButtonArea", parent);
        var ar = areaGo.GetComponent<RectTransform>();
        ar.anchorMin = new Vector2(0, 0.48f); ar.anchorMax = new Vector2(1, 0.62f);
        ar.offsetMin = new Vector2(20, 5); ar.offsetMax = new Vector2(-20, -5);

        var draw1Go = NewButton("Draw1Button", ar, "召唤 1000金币", 22, ColBtnEnter, "Text");
        var d1r = draw1Go.GetComponent<RectTransform>();
        d1r.anchorMin = new Vector2(0, 0); d1r.anchorMax = new Vector2(0.5f, 1);
        d1r.offsetMin = new Vector2(0, 0); d1r.offsetMax = new Vector2(-5, 0);

        var draw10Go = NewButton("Draw10Button", ar, "十连 9000金币", 22, ColBtnNorm, "Text");
        var d10r = draw10Go.GetComponent<RectTransform>();
        d10r.anchorMin = new Vector2(0.5f, 0); d10r.anchorMax = new Vector2(1, 1);
        d10r.offsetMin = new Vector2(5, 0); d10r.offsetMax = Vector2.zero;
    }

    // ── ResultArea（结果卡横向滚动列表） ──
    static void BuildResultArea(Transform parent)
    {
        var areaGo = NewImage("ResultArea", parent, ColPanel);
        var ar = areaGo.GetComponent<RectTransform>();
        ar.anchorMin = new Vector2(0, 0); ar.anchorMax = new Vector2(1, 0.48f);
        ar.offsetMin = new Vector2(20, 10); ar.offsetMax = new Vector2(-20, -5);

        var scrollGo = NewImage("ScrollView", ar, ColScroll, false);
        scrollGo.gameObject.AddComponent<ScrollRect>();
        scrollGo.gameObject.AddComponent<Mask>();
        scrollGo.GetComponent<Mask>().showMaskGraphic = false;

        var slR = scrollGo.GetComponent<RectTransform>();
        Stretch(slR);

        var vpGo = NewImage("Viewport", slR, new Color(0, 0, 0, 0), false);
        vpGo.gameObject.AddComponent<Mask>();
        vpGo.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(vpGo.GetComponent<RectTransform>());

        var ctGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        ctGo.transform.SetParent(vpGo, false);
        ctGo.layer = UILayer;
        var ctR = ctGo.GetComponent<RectTransform>();
        ctR.anchorMin = new Vector2(0, 0); ctR.anchorMax = new Vector2(0, 1);
        ctR.pivot = new Vector2(0, 0.5f); ctR.sizeDelta = Vector2.zero;
        ctR.anchoredPosition = Vector2.zero;

        var hlg = ctGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        ctGo.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = ctR;
        scroll.viewport = vpGo.GetComponent<RectTransform>();
        scroll.horizontal = true; scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    // ==================== GachaResultCard Prefab ====================

    static void BuildCardPrefab()
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(CardPrefabPath));
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath) != null)
            AssetDatabase.DeleteAsset(CardPrefabPath);

        var go = new GameObject("GachaResultCard",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = UILayer;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(130, 160);

        var img = go.GetComponent<Image>();
        img.color = ColBtnInact;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 130f; le.preferredHeight = 160f; le.flexibleWidth = 0f; le.flexibleHeight = 0f;

        var portraitGo = NewImage("Portrait", go.transform, ColBtnInact);
        var pr = portraitGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 1); pr.anchorMax = new Vector2(0.5f, 1);
        pr.pivot = new Vector2(0.5f, 1); pr.sizeDelta = new Vector2(80, 80);
        pr.anchoredPosition = new Vector2(0, -8);

        var nameGo = NewText(go.transform, "Name", "名称", 16, ColBody, TextAlignmentOptions.Center, false);
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(1, 1);
        nr.offsetMin = new Vector2(4, 38); nr.offsetMax = new Vector2(-4, -92);

        var rarityGo = NewText(go.transform, "RarityText", "结果", 14, ColTitle, TextAlignmentOptions.Center, false);
        var rr = rarityGo.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0, 0); rr.anchorMax = new Vector2(1, 1);
        rr.offsetMin = new Vector2(4, 8); rr.offsetMax = new Vector2(-4, -128);

        PrefabUtility.SaveAsPrefabAsset(go, CardPrefabPath);
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
        var rect = NewImage(name, parent, color, true);
        var btn = rect.gameObject.AddComponent<Button>();
        btn.targetGraphic = rect.GetComponent<Image>();
        var textGo = NewText(rect, childName, label, fontSize, ColBody, TextAlignmentOptions.Center, false);
        Stretch(textGo.GetComponent<RectTransform>());
        return rect.gameObject;
    }

    static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
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

    [MenuItem("Tools/UI/导出 GachaUI 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/GachaUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"用字已导出到 {path}", "确定");
    }

    static void ShowChineseChars()
    {
        Debug.Log($"[GachaWindowBuilder] 请将以下字符复制到 TMP Font Asset Creator:\n{CollectChineseChars()}");
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
