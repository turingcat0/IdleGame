using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成随从升级 UI 的编辑器工具（竖屏 9:16，1080x1920）。
/// 节点命名严格对应 Lua 侧 ServantUpgradeWindow.lua.txt 里的 transform:Find 路径，
/// 改名会直接让 Lua 取不到组件——本次重排仅调整各节点的 RectTransform（锚点/尺寸/层级顺序），
/// 未新增/删除/改名任何 Lua 依赖的节点。
/// 视觉配色与 ServantWindowBuilder.cs 严格对齐：深蓝黑背景 + 暖金主色 + 冷蓝辅助色 + 深色卡片。
/// 布局改为竖屏单列：TopBar → CharacterPanel(整行) → UpgradePanel(整行) → SkillArea(整行) → BottomBar(整行)。
/// 同时生成 Prefabs/ServantUpgradeCard 预制体（运行时填充底部随从列表）。
/// </summary>
public static class ServantUpgradeWindowBuilder
{
    const string RootName       = "ServantUpgradeUI";
    const string WindowName     = "ServantUpgradeWindow";
    const string WindowLuaPath  = "Assets/Resources/Lua/LuaBehavior/UI/ServantUpgradeWindow.lua.txt";
    const string CardPrefabPath = "Assets/Resources/Prefabs/ServantUpgradeCard.prefab";
    const string FontPath       = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";

    static TMP_FontAsset _font;
    static TextAsset _windowLua;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色（与 ServantWindowBuilder.cs 保持一致：深蓝黑背景 / 暖金主色 / 冷蓝辅助色 / 深色卡片） ──
    static readonly Color ColBg       = new Color(0.025f, 0.035f, 0.055f, 0.92f);
    static readonly Color ColPanel    = new Color(0.055f, 0.075f, 0.105f, 1f);
    static readonly Color ColCard     = new Color(0.075f, 0.095f, 0.135f, 1f);
    static readonly Color ColTitle    = new Color(0.95f, 0.78f, 0.38f, 1f);   // 暖金主色
    static readonly Color ColBody     = new Color(0.90f, 0.91f, 0.92f, 1f);
    static readonly Color ColLabel    = new Color(0.55f, 0.67f, 0.78f, 1f);   // 冷蓝辅助色
    static readonly Color ColOld      = new Color(0.45f, 0.55f, 0.65f, 1f);   // 旧值：冷蓝弱化
    static readonly Color ColNew      = new Color(0.35f, 0.85f, 0.45f, 1f);   // 新值：提升绿（语义色，与主/辅色并存）
    static readonly Color ColGold     = new Color(0.95f, 0.75f, 0.35f, 1f);   // 货币文本：暖金
    static readonly Color ColBtnNorm  = new Color(0.12f, 0.17f, 0.24f, 1f);   // 普通按钮：冷蓝
    static readonly Color ColBtnInact = new Color(0.09f, 0.12f, 0.17f, 1f);   // 占位/未激活
    static readonly Color ColBtnEnter = new Color(0.55f, 0.30f, 0.08f, 1f);   // 主操作按钮：暖橙金
    static readonly Color ColSelected = new Color(0.55f, 0.35f, 0.10f, 1f);   // 选中：暖金边框
    static readonly Color ColScroll   = new Color(0.035f, 0.055f, 0.085f, 0.78f);

    // ── UI 中出现的全部中文 ──
    static readonly string[] AllChineseChars = new[]
    {
        "随从升级金币结晶生命攻击防御光环效果战斗技能被动主位附属连续已满级战力",
        "升级护游侠碎击者钢铁壁垒疾风指引碎骨怒",
        "闭返回箭头前下一个",
        "无暂每秒释放一次伤害随成长中全体友方单位暴击率速度",
    };

    [MenuItem("Tools/UI/生成 ServantUpgradeUI")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null) { EditorUtility.DisplayDialog("缺少字体", $"找不到: {FontPath}", "确定"); return; }

        _windowLua = AssetDatabase.LoadAssetAtPath<TextAsset>(WindowLuaPath);
        if (_windowLua == null) { EditorUtility.DisplayDialog("缺少 Lua", $"找不到: {WindowLuaPath}", "确定"); return; }

        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("已存在", $"场景里已有 {RootName}，删掉重建？", "删掉重建", "取消")) return;
            Undo.DestroyObjectImmediate(existing);
        }

        var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
        root.layer = UILayer;
        root.transform.SetParent(UIRootHelper.GetOrCreateUIRoot(), false);

        var rootCanvas = root.GetComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var rootScaler = root.GetComponent<CanvasScaler>();
        rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootScaler.referenceResolution = new Vector2(1080f, 1920f);
        rootScaler.matchWidthOrHeight = 0f; // 以宽度为准

        Stretch(root.GetComponent<RectTransform>());

        BuildWindow(root.GetComponent<RectTransform>());
        BuildCardPrefab();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseChars();

        EditorUtility.DisplayDialog("完成", "ServantUpgradeUI 已生成。\n请重新生成 TMP 字体 Atlas。", "确定");
    }

    // ==================== Window ====================
    // 竖屏单列纵向布局（自上而下，各区块整行铺满，不再左右分栏）：
    //   TopBar         y: 0.95 ~ 1.00 (含 Title / 金币 / 结晶 / 关闭，固定 90px)
    //   CharacterPanel y: 0.74 ~ 0.95 (随从头像/名称/战力/前后切换)
    //   UpgradePanel   y: 0.50 ~ 0.74 (等级/属性预览/消耗/升级按钮)
    //   SkillArea      y: 0.25 ~ 0.50 (光环/战斗/被动说明，纵向 3 卡)
    //   BottomBar      y: 0.00 ~ 0.25 (主/副筛选 + 随从卡横向列表)

    static void BuildWindow(RectTransform parent)
    {
        var win = new GameObject(WindowName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(LuaBehaviour));
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
        BuildCharacterPanel(bgGo.transform);
        BuildUpgradePanel(bgGo.transform);
        BuildSkillArea(bgGo.transform);
        BuildBottomBar(bgGo.transform);
    }

    // ── TopBar ──
    static void BuildTopBar(Transform parent)
    {
        var barGo = NewRect("TopBar", parent);
        var bar = barGo.GetComponent<RectTransform>();
        bar.anchorMin = new Vector2(0, 1); bar.anchorMax = Vector2.one;
        bar.pivot = new Vector2(0.5f, 1); bar.sizeDelta = new Vector2(0, 90); bar.anchoredPosition = Vector2.zero;

        var title = NewText(barGo, "Title", "随从升级", 32, ColTitle, TextAlignmentOptions.Left, false);
        var tr = title.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(0.4f, 1);
        tr.offsetMin = new Vector2(24, 0); tr.offsetMax = new Vector2(0, 0);

        // 金币
        var coinLabel = NewText(barGo, "CoinLabel", "金币", 16, ColLabel, TextAlignmentOptions.Right, false);
        var clr = coinLabel.GetComponent<RectTransform>();
        clr.anchorMin = new Vector2(0.42f, 0); clr.anchorMax = new Vector2(0.62f, 1);
        clr.offsetMin = Vector2.zero; clr.offsetMax = Vector2.zero;

        var coinText = NewText(barGo, "CoinText", "0", 18, ColGold, TextAlignmentOptions.Right, false);
        var ctr = coinText.GetComponent<RectTransform>();
        ctr.anchorMin = new Vector2(0.62f, 0); ctr.anchorMax = new Vector2(0.76f, 1);
        ctr.offsetMin = Vector2.zero; ctr.offsetMax = new Vector2(-5, 0);

        // 结晶
        var cryLabel = NewText(barGo, "CrystalLabel", "结晶", 16, ColLabel, TextAlignmentOptions.Right, false);
        var cylr = cryLabel.GetComponent<RectTransform>();
        cylr.anchorMin = new Vector2(0.72f, 0); cylr.anchorMax = new Vector2(0.81f, 1);
        cylr.offsetMin = Vector2.zero; cylr.offsetMax = Vector2.zero;

        var cryText = NewText(barGo, "CrystalText", "0", 18, ColNew, TextAlignmentOptions.Right, false);
        var cytr = cryText.GetComponent<RectTransform>();
        cytr.anchorMin = new Vector2(0.84f, 0); cytr.anchorMax = new Vector2(0.92f, 1);
        cytr.offsetMin = Vector2.zero; cytr.offsetMax = new Vector2(-5, 0);

        var closeGo = NewButton("CloseButton", bar, "×", 26, ColBtnNorm, "Text");
        var cr = closeGo.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(1, 0.5f); cr.anchorMax = new Vector2(1, 0.5f);
        cr.pivot = new Vector2(1, 0.5f); cr.sizeDelta = new Vector2(56, 56);
        cr.anchoredPosition = new Vector2(-12, 0);
    }

    // ── CharacterPanel（整行：头像 + 名称 + 战力 + 前后切换） ──
    static void BuildCharacterPanel(Transform parent)
    {
        var panelGo = NewImage("CharacterPanel", parent, ColPanel);
        var pr = panelGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0, 0.74f); pr.anchorMax = new Vector2(1, 0.95f);
        pr.offsetMin = new Vector2(20, 0); pr.offsetMax = new Vector2(-20, 0);
        // 该面板高度 ≈403px：Portrait(260,顶部) → Name(78~123) → Power(33~73)

        var portraitGo = NewImage("Portrait", pr, ColBtnInact);
        var porR = portraitGo.GetComponent<RectTransform>();
        porR.anchorMin = new Vector2(0.5f, 1); porR.anchorMax = new Vector2(0.5f, 1);
        porR.pivot = new Vector2(0.5f, 1); porR.sizeDelta = new Vector2(260, 260);
        porR.anchoredPosition = new Vector2(0, -10);

        var prevGo = NewButton("PrevButton", pr, "<", 28, ColBtnNorm, "Text");
        var prevR = prevGo.GetComponent<RectTransform>();
        prevR.anchorMin = new Vector2(0, 1); prevR.anchorMax = new Vector2(0, 1);
        prevR.pivot = new Vector2(0, 1); prevR.sizeDelta = new Vector2(70, 90);
        prevR.anchoredPosition = new Vector2(10, -90);

        var nextGo = NewButton("NextButton", pr, ">", 28, ColBtnNorm, "Text");
        var nextR = nextGo.GetComponent<RectTransform>();
        nextR.anchorMin = new Vector2(1, 1); nextR.anchorMax = new Vector2(1, 1);
        nextR.pivot = new Vector2(1, 1); nextR.sizeDelta = new Vector2(70, 90);
        nextR.anchoredPosition = new Vector2(-10, -90);

        var nameGo = NewText(pr, "Name", "随从名", 26, ColTitle, TextAlignmentOptions.Center, false);
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0, 0); nr.anchorMax = Vector2.one;
        nr.offsetMin = new Vector2(90, 78); nr.offsetMax = new Vector2(-90, -270);

        var powerGo = NewText(pr, "Power", "战力 0", 18, ColLabel, TextAlignmentOptions.Center, false);
        var pwr = powerGo.GetComponent<RectTransform>();
        pwr.anchorMin = new Vector2(0, 0); pwr.anchorMax = Vector2.one;
        pwr.offsetMin = new Vector2(90, 33); pwr.offsetMax = new Vector2(-90, -330);
    }

    // ── UpgradePanel（整行：等级 / 属性预览 / 消耗 / 升级按钮） ──
    static void BuildUpgradePanel(Transform parent)
    {
        var panelGo = NewImage("UpgradePanel", parent, ColPanel);
        var pr = panelGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0, 0.50f); pr.anchorMax = new Vector2(1, 0.74f);
        pr.offsetMin = new Vector2(20, 0); pr.offsetMax = new Vector2(-20, 0);
        // 该面板高度 ≈461px，自上而下：LevelText(15~65) → StatsArea(75~263) → CostArea(279~353) → 按钮行(369~445)

        var levelGo = NewText(pr, "LevelText", "Lv.1 → Lv.2", 26, ColTitle, TextAlignmentOptions.Center, false);
        var lr = levelGo.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 1); lr.anchorMax = Vector2.one;
        lr.pivot = new Vector2(0.5f, 1); lr.sizeDelta = new Vector2(0, 50);
        lr.anchoredPosition = new Vector2(0, -15);

        // StatsArea：三行生命/攻击/防御，纵向堆叠，整行铺满
        var statsGo = NewRect("StatsArea", pr);
        var sr = statsGo.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 1); sr.anchorMax = Vector2.one;
        sr.pivot = new Vector2(0.5f, 1); sr.sizeDelta = new Vector2(0, 188);
        sr.anchoredPosition = new Vector2(0, -75);

        string[] statNames = { "Health", "Attack", "Defense" };
        string[] statLabels = { "生命", "攻击", "防御" };
        float rowH = 56f, rowGap = 10f;
        for (int i = 0; i < 3; i++)
        {
            var rowGo = NewRect("StatRow" + statNames[i], statsGo.transform);
            var rr = rowGo.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0, 1); rr.anchorMax = Vector2.one;
            rr.pivot = new Vector2(0.5f, 1); rr.sizeDelta = new Vector2(0, rowH);
            rr.anchoredPosition = new Vector2(0, -i * (rowH + rowGap));

            var label = NewText(rowGo.transform, "Label", statLabels[i], 20, ColLabel, TextAlignmentOptions.Left, false);
            var labR = label.GetComponent<RectTransform>();
            labR.anchorMin = Vector2.zero; labR.anchorMax = new Vector2(0.3f, 1);
            labR.offsetMin = new Vector2(15, 0); labR.offsetMax = Vector2.zero;

            var oldV = NewText(rowGo.transform, "Old", "0", 20, ColOld, TextAlignmentOptions.Right, false);
            var oldR = oldV.GetComponent<RectTransform>();
            oldR.anchorMin = new Vector2(0.3f, 0); oldR.anchorMax = new Vector2(0.6f, 1);
            oldR.offsetMin = Vector2.zero; oldR.offsetMax = Vector2.zero;

            var arrow = NewText(rowGo.transform, "Arrow", "→", 18, ColLabel, TextAlignmentOptions.Center, false);
            var arR = arrow.GetComponent<RectTransform>();
            arR.anchorMin = new Vector2(0.6f, 0); arR.anchorMax = new Vector2(0.7f, 1);
            arR.offsetMin = Vector2.zero; arR.offsetMax = Vector2.zero;

            var newV = NewText(rowGo.transform, "New", "0", 20, ColNew, TextAlignmentOptions.Left, false);
            var newR = newV.GetComponent<RectTransform>();
            newR.anchorMin = new Vector2(0.7f, 0); newR.anchorMax = Vector2.one;
            newR.offsetMin = new Vector2(5, 0); newR.offsetMax = new Vector2(-15, 0);
        }

        // CostArea：金币/结晶消耗，纵向两行
        var costGo = NewRect("CostArea", pr);
        var cr = costGo.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = Vector2.one;
        cr.pivot = new Vector2(0.5f, 1); cr.sizeDelta = new Vector2(0, 74);
        cr.anchoredPosition = new Vector2(0, -279);

        var costCoin = NewText(costGo.transform, "CostCoin", "金币: 0", 18, ColGold, TextAlignmentOptions.Left, false);
        var ccR = costCoin.GetComponent<RectTransform>();
        ccR.anchorMin = new Vector2(0, 1); ccR.anchorMax = new Vector2(1, 1);
        ccR.pivot = new Vector2(0.5f, 1); ccR.sizeDelta = new Vector2(-30, 34);
        ccR.anchoredPosition = new Vector2(0, 0);

        var costCry = NewText(costGo.transform, "CostCrystal", "结晶: 0", 18, ColNew, TextAlignmentOptions.Left, false);
        var ccrR = costCry.GetComponent<RectTransform>();
        ccrR.anchorMin = new Vector2(0, 1); ccrR.anchorMax = new Vector2(1, 1);
        ccrR.pivot = new Vector2(0.5f, 1); ccrR.sizeDelta = new Vector2(-30, 34);
        ccrR.anchoredPosition = new Vector2(0, -40);

        // 按钮行：升级 / 连续升级
        var upGo = NewButton("UpgradeButton", pr, "升级", 24, ColBtnEnter, "Text");
        var upR = upGo.GetComponent<RectTransform>();
        upR.anchorMin = new Vector2(0, 1); upR.anchorMax = new Vector2(0.6f, 1);
        upR.pivot = new Vector2(0.5f, 1); upR.sizeDelta = new Vector2(-20, 76);
        upR.anchoredPosition = new Vector2(15, -369);

        var contGo = NewButton("ContinuousButton", pr, "连续升级", 18, ColBtnNorm, "Text");
        var contR = contGo.GetComponent<RectTransform>();
        contR.anchorMin = new Vector2(0.6f, 1); contR.anchorMax = new Vector2(1f, 1);
        contR.pivot = new Vector2(0.5f, 1); contR.sizeDelta = new Vector2(-20, 76);
        contR.anchoredPosition = new Vector2(-15, -369);
    }

    // ── SkillArea（整行，纵向 3 张技能卡） ──
    static void BuildSkillArea(Transform parent)
    {
        var areaGo = NewRect("SkillArea", parent);
        var ar = areaGo.GetComponent<RectTransform>();
        ar.anchorMin = new Vector2(0, 0.25f); ar.anchorMax = new Vector2(1, 0.50f);
        ar.offsetMin = new Vector2(20, 5); ar.offsetMax = new Vector2(-20, -5);

        string[] cardNames = { "Aura", "Combat", "Passive" };
        string[] titles = { "光环效果", "战斗技能", "被动技能" };
        float cardH = 1f / 3f;

        for (int i = 0; i < 3; i++)
        {
            var cardGo = NewImage("SkillCard_" + cardNames[i], ar, ColCard);
            var cr = cardGo.GetComponent<RectTransform>();
            float yMin = 1f - (i + 1) * cardH;
            float yMax = 1f - i * cardH;
            cr.anchorMin = new Vector2(0, yMin); cr.anchorMax = new Vector2(1, yMax);
            cr.offsetMin = new Vector2(0, 4); cr.offsetMax = new Vector2(0, -4);

            var title = NewText(cr, "Title", titles[i], 15, ColLabel, TextAlignmentOptions.Left, false);
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 1); tr.anchorMax = Vector2.one;
            tr.pivot = new Vector2(0.5f, 1); tr.sizeDelta = new Vector2(-24, 24);
            tr.anchoredPosition = new Vector2(0, 0);

            var name = NewText(cr, "Name", "技能名", 19, ColTitle, TextAlignmentOptions.Left, false);
            var nr = name.GetComponent<RectTransform>();
            nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(0.35f, 1);
            nr.offsetMin = new Vector2(14, 6); nr.offsetMax = new Vector2(0, -24);

            var desc = NewText(cr, "Desc", "技能描述", 15, ColBody, TextAlignmentOptions.Left, true);
            var dr = desc.GetComponent<RectTransform>();
            dr.anchorMin = new Vector2(0.35f, 0); dr.anchorMax = Vector2.one;
            dr.offsetMin = new Vector2(10, 8); dr.offsetMax = new Vector2(-14, -24);
            desc.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
        }
    }

    // ── BottomBar（主/副筛选 + 随从卡横向列表） ──
    static void BuildBottomBar(Transform parent)
    {
        var barGo = NewRect("BottomBar", parent);
        var br = barGo.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0.25f);
        br.offsetMin = new Vector2(20, 10); br.offsetMax = new Vector2(-20, -10);

        // Filter tabs
        var tabMain = NewButton("TabMain", br, "主战位", 19, ColBtnNorm, "Text");
        var tmR = tabMain.GetComponent<RectTransform>();
        tmR.anchorMin = new Vector2(0, 1); tmR.anchorMax = new Vector2(0.5f, 1);
        tmR.pivot = new Vector2(0.5f, 1); tmR.sizeDelta = new Vector2(-5, 40);
        tmR.anchoredPosition = new Vector2(0, 0);

        var tabSub = NewButton("TabSub", br, "附属位", 19, ColBtnNorm, "Text");
        var tsR = tabSub.GetComponent<RectTransform>();
        tsR.anchorMin = new Vector2(0.5f, 1); tsR.anchorMax = new Vector2(1, 1);
        tsR.pivot = new Vector2(0.5f, 1); tsR.sizeDelta = new Vector2(-5, 40);
        tsR.anchoredPosition = new Vector2(0, 0);

        // ScrollView
        var scrollGo = NewImage("ServantList", br, ColScroll, false);
        scrollGo.gameObject.AddComponent<ScrollRect>();
        scrollGo.gameObject.AddComponent<Mask>();
        scrollGo.GetComponent<Mask>().showMaskGraphic = false;

        var slR = scrollGo.GetComponent<RectTransform>();
        slR.anchorMin = Vector2.zero; slR.anchorMax = Vector2.one;
        slR.offsetMin = new Vector2(0, 0); slR.offsetMax = new Vector2(0, -46);

        var scroll = scrollGo.GetComponent<ScrollRect>();

        var vpGo = NewImage("Viewport", slR, new Color(0, 0, 0, 0), false);
        vpGo.gameObject.AddComponent<Mask>();
        vpGo.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(vpGo.GetComponent<RectTransform>());

        var ctGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        ctGo.transform.SetParent(vpGo, false);
        ctGo.layer = UILayer;
        var ctR = ctGo.GetComponent<RectTransform>();
        // Content 纵向拉伸铁贴合 Viewport 高度，横向宽度交给 ContentSizeFitter 计算，避免子卡片被压成 0 高
        ctR.anchorMin = new Vector2(0, 0); ctR.anchorMax = new Vector2(0, 1);
        ctR.pivot = new Vector2(0, 0.5f); ctR.sizeDelta = Vector2.zero;
        ctR.anchoredPosition = Vector2.zero;

        var hlg = ctGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f; hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        ctGo.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = ctR;
        scroll.viewport = vpGo.GetComponent<RectTransform>();
        scroll.horizontal = true; scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    // ==================== ServantUpgradeCard Prefab ====================

    static void BuildCardPrefab()
    {
        EnsureFolder(System.IO.Path.GetDirectoryName(CardPrefabPath));
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath) != null)
            AssetDatabase.DeleteAsset(CardPrefabPath);

        var go = new GameObject("ServantUpgradeCard",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = UILayer;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140, 170);

        var img = go.GetComponent<Image>();
        img.color = ColBtnInact;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 140f; le.preferredHeight = 170f; le.flexibleWidth = 0f; le.flexibleHeight = 0f;

        var portraitGo = NewImage("Portrait", go.transform, ColBtnInact);
        var pr = portraitGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 1); pr.anchorMax = new Vector2(0.5f, 1);
        pr.pivot = new Vector2(0.5f, 1); pr.sizeDelta = new Vector2(90, 90);
        pr.anchoredPosition = new Vector2(0, -8);

        var nameGo = NewText(go.transform, "Name", "名称", 16, ColBody, TextAlignmentOptions.Center, false);
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(1, 1);
        nr.offsetMin = new Vector2(4, 38); nr.offsetMax = new Vector2(-4, -100);

        var levelGo = NewText(go.transform, "Level", "Lv.1", 13, ColLabel, TextAlignmentOptions.Center, false);
        var lvR = levelGo.GetComponent<RectTransform>();
        lvR.anchorMin = new Vector2(0, 0); lvR.anchorMax = new Vector2(1, 1);
        lvR.offsetMin = new Vector2(4, 8); lvR.offsetMax = new Vector2(-4, -136);

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

    [MenuItem("Tools/UI/导出 ServantUpgradeUI 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/ServantUpgradeUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"用字已导出到 {path}", "确定");
    }

    static void ShowChineseChars()
    {
        Debug.Log($"[ServantUpgradeWindowBuilder] 请将以下字符复制到 TMP Font Asset Creator:\n{CollectChineseChars()}");
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
