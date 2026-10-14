using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CombatStatsWindowBuilder
{
    const string RootName = "CombatStatsUI";
    const string ButtonName = "CombatStatsButton";
    const string WindowName = "CombatStatsWindow";
    const string FontPath = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string ButtonLuaPath = "Assets/Resources/Lua/LuaBehavior/UI/CombatStatsButton.lua.txt";
    const string WindowLuaPath = "Assets/Resources/Lua/LuaBehavior/UI/CombatStatsWindow.lua.txt";

    static TMP_FontAsset _font;
    static TextAsset _buttonLua;
    static TextAsset _windowLua;

    static readonly Color ColBg = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    static readonly Color ColPanel = new Color(0.12f, 0.12f, 0.12f, 1f);
    static readonly Color ColTitle = new Color(0.95f, 0.95f, 0.95f, 1f);
    static readonly Color ColBody = new Color(0.85f, 0.85f, 0.85f, 1f);
    static readonly Color ColButtonNormal = new Color(0.25f, 0.25f, 0.25f, 1f);
    static readonly Color ColButtonInactive = new Color(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color ColButtonActive = new Color(0.35f, 0.55f, 0.85f, 1f);

    static readonly string[] AllChineseChars = new[]
    {
        "战斗统计关闭效果伤害综合模式本局生涯",
        "施加次数移除叠层变化护盾中毒眩晕属性",
        "总吸收时长死亡暂无",
    };

    [MenuItem("Tools/UI/生成 CombatStatsUI")]
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

        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing);
        }

        var root = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        root.layer = LayerMask.NameToLayer("UI");
        root.transform.SetParent(UIRootHelper.GetOrCreateUIRoot(), false);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.Stretch();

        BuildButton(rootRect);
        BuildWindow(rootRect);

        ShowChineseCharsForFontGeneration();

        EditorUtility.DisplayDialog("完成", "CombatStatsUI 已生成。请在 Unity 中重新生成字体 Atlas 以包含弹窗中的全部中文。", "确定");
    }

    static void BuildButton(RectTransform parent)
    {
        var btnGo = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LuaBehaviour));
        btnGo.transform.SetParent(parent, false);
        btnGo.layer = LayerMask.NameToLayer("UI");

        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(120f, 44f);
        rect.anchoredPosition = new Vector2(-20f, -20f);

        var image = btnGo.GetComponent<Image>();
        image.color = ColButtonNormal;

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = image;

        var lb = btnGo.GetComponent<LuaBehaviour>();
        lb.luaScript = _buttonLua;
        lb.injections = new Injection[0];

        var textGo = NewTextChild(btnGo.transform, "Text", "统计", 22, TextAlignmentOptions.Center, false);
        textGo.GetComponent<RectTransform>().Stretch();
    }

    static void BuildWindow(RectTransform parent)
    {
        var windowGo = new GameObject(WindowName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(LuaBehaviour));
        windowGo.transform.SetParent(parent, false);
        windowGo.layer = LayerMask.NameToLayer("UI");

        var windowCanvas = windowGo.GetComponent<Canvas>();
        windowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        windowCanvas.overrideSorting = true;
        windowCanvas.sortingOrder = 10;
        windowCanvas.enabled = false;

        var windowRect = windowGo.GetComponent<RectTransform>();
        windowRect.Stretch();

        var lb = windowGo.GetComponent<LuaBehaviour>();
        lb.luaScript = _windowLua;
        lb.injections = new Injection[0];

        // Background
        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(windowRect, false);
        bgGo.layer = LayerMask.NameToLayer("UI");
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.Stretch();
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color = ColBg;

        // WindowPanel
        var panelGo = new GameObject("WindowPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(bgRect, false);
        panelGo.layer = LayerMask.NameToLayer("UI");
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 520f);
        panelRect.anchoredPosition = Vector2.zero;
        var panelImg = panelGo.GetComponent<Image>();
        panelImg.color = ColPanel;

        // Title
        var titleGo = NewTextChild(panelGo.transform, "Title", "战斗统计", 32, TextAlignmentOptions.Center, false);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 50f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);

        // CloseButton
        var closeBtn = NewButtonChild(panelGo.transform, "CloseButton", "X", 24);
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(44f, 44f);
        closeRect.anchoredPosition = new Vector2(-10f, -10f);

        // BookmarkBar (left side)
        var bookmarkBarGo = new GameObject("BookmarkBar", typeof(RectTransform));
        bookmarkBarGo.transform.SetParent(panelRect, false);
        bookmarkBarGo.layer = LayerMask.NameToLayer("UI");
        var bookmarkBarRect = bookmarkBarGo.GetComponent<RectTransform>();
        bookmarkBarRect.anchorMin = new Vector2(0f, 0f);
        bookmarkBarRect.anchorMax = new Vector2(0f, 1f);
        bookmarkBarRect.pivot = new Vector2(0f, 0.5f);
        bookmarkBarRect.sizeDelta = new Vector2(110f, -80f);
        bookmarkBarRect.anchoredPosition = new Vector2(10f, -25f);

        var bookmarkNames = new[] { ("effects", "效果"), ("damage", "伤害"), ("summary", "综合") };
        float bookmarkHeight = 44f;
        float bookmarkSpacing = 8f;
        for (int i = 0; i < bookmarkNames.Length; i++)
        {
            var (id, label) = bookmarkNames[i];
            var bookmarkName = $"Bookmark_{char.ToUpper(id[0])}{id.Substring(1)}";
            var bmGo = NewButtonChild(bookmarkBarGo.transform, bookmarkName, label, 20);
            var bmRect = bmGo.GetComponent<RectTransform>();
            bmRect.anchorMin = new Vector2(0f, 1f);
            bmRect.anchorMax = new Vector2(1f, 1f);
            bmRect.pivot = new Vector2(0.5f, 1f);
            bmRect.sizeDelta = new Vector2(0f, bookmarkHeight);
            bmRect.anchoredPosition = new Vector2(0f, -i * (bookmarkHeight + bookmarkSpacing) - 10f);

            var img = bmGo.GetComponent<Image>();
            img.color = ColButtonInactive;
            img.type = Image.Type.Simple;
        }

        // ModeBar (top right area)
        var modeBarGo = new GameObject("ModeBar", typeof(RectTransform));
        modeBarGo.transform.SetParent(panelRect, false);
        modeBarGo.layer = LayerMask.NameToLayer("UI");
        var modeBarRect = modeBarGo.GetComponent<RectTransform>();
        modeBarRect.anchorMin = new Vector2(0f, 1f);
        modeBarRect.anchorMax = new Vector2(1f, 1f);
        modeBarRect.pivot = new Vector2(0.5f, 1f);
        modeBarRect.sizeDelta = new Vector2(-140f, 40f);
        modeBarRect.anchoredPosition = new Vector2(60f, -60f);

        var modeHeaderGo = NewTextChild(modeBarGo.transform, "ModeHeader", "模式: 本局", 20, TextAlignmentOptions.Left, false);
        var modeHeaderRect = modeHeaderGo.GetComponent<RectTransform>();
        modeHeaderRect.anchorMin = new Vector2(0f, 0f);
        modeHeaderRect.anchorMax = new Vector2(0f, 1f);
        modeHeaderRect.pivot = new Vector2(0f, 0.5f);
        modeHeaderRect.sizeDelta = new Vector2(180f, 0f);
        modeHeaderRect.anchoredPosition = new Vector2(0f, 0f);

        var sessionBtn = NewButtonChild(modeBarGo.transform, "SessionButton", "本局", 18);
        var sessionRect = sessionBtn.GetComponent<RectTransform>();
        sessionRect.anchorMin = new Vector2(1f, 0f);
        sessionRect.anchorMax = new Vector2(1f, 1f);
        sessionRect.pivot = new Vector2(1f, 0.5f);
        sessionRect.sizeDelta = new Vector2(70f, 0f);
        sessionRect.anchoredPosition = new Vector2(-80f, 0f);
        sessionBtn.GetComponent<Image>().color = ColButtonActive;

        var careerBtn = NewButtonChild(modeBarGo.transform, "CareerButton", "生涯", 18);
        var careerRect = careerBtn.GetComponent<RectTransform>();
        careerRect.anchorMin = new Vector2(1f, 0f);
        careerRect.anchorMax = new Vector2(1f, 1f);
        careerRect.pivot = new Vector2(1f, 0.5f);
        careerRect.sizeDelta = new Vector2(70f, 0f);
        careerRect.anchoredPosition = new Vector2(0f, 0f);
        careerBtn.GetComponent<Image>().color = ColButtonInactive;

        // ScrollView
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
        scrollGo.transform.SetParent(panelRect, false);
        scrollGo.layer = LayerMask.NameToLayer("UI");
        var scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.offsetMin = new Vector2(130f, 20f);
        scrollRect.offsetMax = new Vector2(-20f, -110f);

        var scrollImg = scrollGo.GetComponent<Image>();
        scrollImg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        scrollGo.GetComponent<Mask>().showMaskGraphic = false;

        var scroll = scrollGo.GetComponent<ScrollRect>();

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGo.transform.SetParent(scrollRect, false);
        viewportGo.layer = LayerMask.NameToLayer("UI");
        var viewportRect = viewportGo.GetComponent<RectTransform>();
        viewportRect.Stretch();
        viewportGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewportRect, false);
        contentGo.layer = LayerMask.NameToLayer("UI");
        var contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);
        contentRect.anchoredPosition = Vector2.zero;

        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        BuildStatItems(contentRect);
    }

    static void BuildStatItems(RectTransform content)
    {
        var items = new[]
        {
            ("ShieldAdded", "护盾施加次数"),
            ("ShieldRemoved", "护盾移除次数"),
            ("PoisonAdded", "中毒施加次数"),
            ("PoisonRemoved", "中毒移除次数"),
            ("StunAdded", "眩晕施加次数"),
            ("StunRemoved", "眩晕移除次数"),
            ("StatModAdded", "属性施加次数"),
            ("StatModRemoved", "属性移除次数"),
            ("StackChanges", "叠层变化次数"),
            ("EffectDamageDealt", "效果总伤害"),
            ("EffectDamageAbsorbed", "效果总吸收"),
            ("TotalPlayTime", "总游戏时长(秒)"),
            ("DeathCount", "死亡次数"),
            ("SessionEmpty", "本局暂无综合统计"),
        };

        float itemHeight = 38f;
        for (int i = 0; i < items.Length; i++)
        {
            var (id, label) = items[i];
            var itemGo = new GameObject($"StatItem_{id}", typeof(RectTransform));
            itemGo.transform.SetParent(content, false);
            itemGo.layer = LayerMask.NameToLayer("UI");
            itemGo.SetActive(false);

            var itemRect = itemGo.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(0f, itemHeight);
            itemRect.anchoredPosition = new Vector2(0f, -i * itemHeight);

            var labelGo = NewTextChild(itemGo.transform, "Label", label, 20, TextAlignmentOptions.Left, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(0f, 0f);
            labelRect.anchoredPosition = new Vector2(10f, 0f);

            var valueGo = NewTextChild(itemGo.transform, "Value", "0", 20, TextAlignmentOptions.Right, false);
            var valueRect = valueGo.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.sizeDelta = new Vector2(-10f, 0f);
            valueRect.anchoredPosition = new Vector2(-10f, 0f);
        }
    }

    static GameObject NewTextChild(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = _font;
        tmp.fontSharedMaterial = _font.material;
        tmp.fontSize = fontSize;
        tmp.color = ColBody;
        tmp.alignment = alignment;
        tmp.raycastTarget = raycast;

        return go;
    }

    static GameObject NewButtonChild(Transform parent, string name, string label, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        var img = go.GetComponent<Image>();
        img.color = ColButtonInactive;
        img.type = Image.Type.Simple;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var textGo = NewTextChild(go.transform, "Text", label, fontSize, TextAlignmentOptions.Center, false);
        textGo.GetComponent<RectTransform>().Stretch();

        return go;
    }

    [MenuItem("Tools/UI/导出 CombatStats 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/CombatStatsUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[CombatStatsWindowBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
    }

    static string CollectChineseChars()
    {
        var charSet = new HashSet<char>();
        foreach (var line in AllChineseChars)
        {
            foreach (var c in line)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)
                {
                    charSet.Add(c);
                }
            }
        }
        var sorted = new List<char>(charSet);
        sorted.Sort();
        return new string(sorted.ToArray());
    }
}

public static class RectTransformExtensions
{
    public static void Stretch(this RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
}
