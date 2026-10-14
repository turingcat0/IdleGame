using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成过场表现层的编辑器工具：FadeOverlay（黑屏）+ Portal（传送门）+ SettlementPanel（结算面板）。
/// 节点命名严格对应 Lua 侧 transform:Find 路径，改名会让 Lua 取不到组件。
/// 对应 Lua：StageTransition.lua / Portal.lua / BossSettlementUI.lua
/// </summary>
public static class StageTransitionBuilder
{
    const string FontPath = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string StageTransitionLuaPath = "Assets/Resources/Lua/LuaBehavior/UI/StageTransition.lua.txt";
    const string PortalLuaPath          = "Assets/Resources/Lua/LuaBehavior/UI/Portal.lua.txt";
    const string SettlementLuaPath      = "Assets/Resources/Lua/LuaBehavior/UI/BossSettlementUI.lua.txt";
    const string PortalSpritePath       = "Assets/Resources/PortalPlaceholder.png";

    static TMP_FontAsset _font;
    static TextAsset _stageTransitionScript;
    static TextAsset _portalScript;
    static TextAsset _settlementScript;
    static Sprite _portalSprite;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色 ──
    static readonly Color ColPanelBg  = new Color(0.08f, 0.08f, 0.12f, 0.92f);
    static readonly Color ColText     = new Color(0.95f, 0.92f, 0.80f, 1f);
    static readonly Color ColBtnBg    = new Color(0.20f, 0.50f, 0.80f, 1f);
    static readonly Color ColBtnText  = Color.white;

    // ── UI 中出现的全部中文 ──
    static readonly string[] AllChineseChars = new[]
    {
        "经验水晶金币继续",
    };

    [MenuItem("Tools/UI/重建过场表现层")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
        {
            EditorUtility.DisplayDialog("缺少字体", $"找不到字体资源: {FontPath}", "确定");
            return;
        }

        _stageTransitionScript = LoadLua(StageTransitionLuaPath, "StageTransition");
        _portalScript          = LoadLua(PortalLuaPath, "Portal");
        _settlementScript      = LoadLua(SettlementLuaPath, "BossSettlementUI");
        if (_stageTransitionScript == null || _portalScript == null || _settlementScript == null)
            return;

        _portalSprite = GetOrCreatePortalSprite();

        // 删除旧同名对象
        DeleteIfExists("FadeOverlay");
        DeleteIfExists("Portal");

        // 1. FadeOverlay —— 黑屏 Canvas（UIRoot 下）
        var fadeOverlay = BuildFadeOverlay();

        // 2. Portal —— 世界空间传送门（场景根，不挂 UIRoot）
        BuildPortal();

        // 3. SettlementPanel —— 结算面板（FadeOverlay 子节点，排在 Fade 之后）
        BuildSettlementPanel(fadeOverlay.GetComponent<RectTransform>());

        Selection.activeGameObject = fadeOverlay;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseCharsForFontGeneration();

        EditorUtility.DisplayDialog("完成",
            "过场表现层已生成：\n" +
            "- FadeOverlay（黑屏 + StageTransition.lua）\n" +
            "- Portal（传送门 + Portal.lua）\n" +
            "- SettlementPanel（结算面板 + BossSettlementUI.lua）\n" +
            "请重新生成 TMP 字体 Atlas 以包含全部中文。", "确定");
    }

    // ==================== FadeOverlay ====================

    static GameObject BuildFadeOverlay()
    {
        var root = new GameObject("FadeOverlay",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(LuaBehaviour));
        Undo.RegisterCreatedObjectUndo(root, "Create FadeOverlay");
        root.layer = UILayer;
        root.transform.SetParent(UIRootHelper.GetOrCreateUIRoot(), false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 压在所有游戏 UI 之上

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Stretch(root.GetComponent<RectTransform>());

        var lb = root.GetComponent<LuaBehaviour>();
        lb.luaScript = _stageTransitionScript;
        lb.injections = new Injection[0];

        // Fade —— 全屏黑色 Image，初始 alpha=0，raycastTarget=false（不挡点击）
        var fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeGo.transform.SetParent(root.transform, false);
        fadeGo.layer = UILayer;
        Stretch(fadeGo.GetComponent<RectTransform>());
        var fadeImg = fadeGo.GetComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 0);
        fadeImg.raycastTarget = false;

        return root;
    }

    // ==================== Portal ====================

    static GameObject BuildPortal()
    {
        var go = new GameObject("Portal",
            typeof(Transform), typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(LuaBehaviour));
        Undo.RegisterCreatedObjectUndo(go, "Create Portal");

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = _portalSprite;
        sr.sortingOrder = 100; // 在敌人和背景之上，UI 之下
        sr.color = new Color(0.5f, 0.8f, 1f, 1f);
        go.transform.localScale = new Vector3(2f, 2f, 1f);

        var col = go.GetComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 2f);
        col.isTrigger = true;

        var lb = go.GetComponent<LuaBehaviour>();
        lb.luaScript = _portalScript;
        lb.injections = new Injection[0];

        // 初始不可见不可触发（Portal.lua 按状态派生）
        sr.enabled = false;
        col.enabled = false;

        return go;
    }

    // ==================== SettlementPanel ====================

    static GameObject BuildSettlementPanel(RectTransform fadeOverlay)
    {
        var panelGo = new GameObject("SettlementPanel",
            typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(LuaBehaviour));
        panelGo.transform.SetParent(fadeOverlay, false);
        panelGo.layer = UILayer;

        // 排在 Fade 之后以盖在黑屏上
        panelGo.transform.SetAsLastSibling();

        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot     = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 400f);
        panelRect.anchoredPosition = Vector2.zero;

        panelGo.GetComponent<Image>().color = ColPanelBg;

        var cg = panelGo.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        var lb = panelGo.GetComponent<LuaBehaviour>();
        lb.luaScript = _settlementScript;
        lb.injections = new Injection[0];

        // 三行奖励文本
        var expGo     = NewText(panelGo.transform, "ExpText",     "", 28, ColText, TextAlignmentOptions.Center);
        var crystalGo = NewText(panelGo.transform, "CrystalText", "", 28, ColText, TextAlignmentOptions.Center);
        var coinGo    = NewText(panelGo.transform, "CoinText",    "", 28, ColText, TextAlignmentOptions.Center);

        SetRect(expGo.GetComponent<RectTransform>(), 0, 60, 460, 40);
        SetRect(crystalGo.GetComponent<RectTransform>(), 0, 10, 460, 40);
        SetRect(coinGo.GetComponent<RectTransform>(), 0, -40, 460, 40);

        // 继续按钮
        var btnGo = new GameObject("ContinueButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panelGo.transform, false);
        btnGo.layer = UILayer;
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot     = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(200f, 56f);
        btnRect.anchoredPosition = new Vector2(0f, -130f);
        btnGo.GetComponent<Image>().color = ColBtnBg;

        var btnLabel = NewText(btnGo.transform, "Label", "继续", 24, ColBtnText,
            TextAlignmentOptions.Center);
        SetRect(btnLabel.GetComponent<RectTransform>(), 0, 0, 200f, 56f);

        return panelGo;
    }

    // ==================== Helpers ====================

    static TextAsset LoadLua(string path, string name)
    {
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset == null)
            EditorUtility.DisplayDialog("缺少 Lua 脚本", $"找不到: {path}", "确定");
        return asset;
    }

    static void DeleteIfExists(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("已存在同名对象",
                $"场景里已经有 {name}。\n重新生成会删掉它。", "删掉重建", "取消");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }
    }

    static GameObject NewText(Transform parent, string name, string text,
        int fontSize, Color color, TextAlignmentOptions alignment)
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
        tmp.raycastTarget = false;
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

    static void SetRect(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y);
    }

    static Sprite GetOrCreatePortalSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PortalSpritePath);
        if (existing != null) return existing;

        // 生成一个蓝色圆环作为传送门占位贴图
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        float center = size / 2f;
        float outer = size / 2f - 1;
        float inner = outer - 8;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * size + x] = (dist <= outer && dist >= inner)
                    ? new Color(0.5f, 0.8f, 1f, 1f)
                    : Color.clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(PortalSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(PortalSpritePath);

        var importer = AssetImporter.GetAtPath(PortalSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(PortalSpritePath);
    }

    // ==================== Chinese Chars ====================

    [MenuItem("Tools/UI/导出过场用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/StageTransition_Chars.txt";
        File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[StageTransitionBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
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
