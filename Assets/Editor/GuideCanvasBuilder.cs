using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 生成新手引导遮罩 UI 的编辑器工具。
/// 节点命名严格对应 Lua 侧 GuideOverlay.lua.txt 里的 transform:Find 路径，
/// 改名会直接让 Lua 取不到组件。
///
/// 兄弟顺序：四块 Mask 必须在 Bubble 和 SkipButton 之前，
/// 否则后者会被遮罩盖住、跳过按钮点不到。
/// </summary>
public static class GuideCanvasBuilder
{
    const string RootName = "GuideCanvas";
    const string FontPath = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";
    const string LuaPath  = "Assets/Resources/Lua/LuaBehavior/UI/GuideOverlay.lua.txt";

    static TMP_FontAsset _font;
    static TextAsset _luaScript;

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    // ── 颜色 ──
    static readonly Color ColMask   = new Color(0f, 0f, 0f, 0.70f);
    static readonly Color ColBubble = new Color(0.10f, 0.10f, 0.12f, 0.95f);
    static readonly Color ColText   = new Color(0.95f, 0.95f, 0.95f, 1f);
    static readonly Color ColBtn    = new Color(0.25f, 0.25f, 0.25f, 1f);

    // ── UI 中出现的全部中文（供 TMP Font Atlas 生成用） ──
    static readonly string[] AllChineseChars = new[]
    {
        "跳过引导",
        // 引导气泡文本示例（Guides.csv 里的 text 字段）
        "点击这里开始战斗这里是攻击按钮点它开始战斗",
        "击败所有敌人即可进入下一关",
        // 新增：召唤/随从/技能/统计/副本引导文本
        "金币足够啦点击这里召唤新随从单抽一次试试手气会自带光环加成召唤成功自动解锁重复随从转化为精魄关闭返回主城",
        "获得新随从了点这里打开随从面板每个仆从带自己的光环加成注意搭配选中后点击升级可消耗精魄提升属性升级后的会在战斗中更强记得合理搭配阵容",
        "学会了新技能点这里查看和装备普通技自动循环回蓝战斗技决定普攻意图掌控技可手动或自动释放点击槽位筛选类型再点列表中的技能即可装备",
        "切换效果伤害综合标签查看本局或生涯统计",
        "达到级解锁副本点击这里挑战选择副本后点击进入可获得大量经验和金币已通关副本还能扫荡准备好就点击进入副本吧",
        // 新增引导标点（确保 TMP Atlas 包含）
        "，。！；：",
    };

    [MenuItem("Tools/UI/生成 GuideCanvas")]
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
        // 2000 压在 StageTransition/FadeOverlay（1000）之上，避免通关结算面板盖住引导
        rootCanvas.sortingOrder = 2000;

        var rootScaler = root.GetComponent<CanvasScaler>();
        rootScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        rootScaler.referenceResolution = new Vector2(1920f, 1080f);

        Stretch(root.GetComponent<RectTransform>());

        var lb = root.GetComponent<LuaBehaviour>();
        lb.luaScript = _luaScript;
        lb.injections = new Injection[0];

        // ── 四块遮罩（顺序不能乱，必须在 Bubble 和 SkipButton 之前） ──
        NewImage("MaskTop",    root.transform, ColMask);
        NewImage("MaskBottom", root.transform, ColMask);
        NewImage("MaskLeft",   root.transform, ColMask);
        NewImage("MaskRight",  root.transform, ColMask);

        // ── 气泡 ──
        var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
        bubbleGo.transform.SetParent(root.transform, false);
        bubbleGo.layer = UILayer;

        var bubbleRect = bubbleGo.GetComponent<RectTransform>();
        bubbleRect.sizeDelta = new Vector2(400f, 100f);

        var bubbleImg = bubbleGo.GetComponent<Image>();
        bubbleImg.color = ColBubble;
        bubbleImg.raycastTarget = false;

        var bubbleTextGo = NewText(bubbleGo.transform, "Text", "", 22, ColText,
            TextAlignmentOptions.Center, false);
        bubbleTextGo.GetComponent<RectTransform>().Stretch();

        // ── 跳过按钮 ──
        var skipGo = new GameObject("SkipButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        skipGo.transform.SetParent(root.transform, false);
        skipGo.layer = UILayer;

        var skipRect = skipGo.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 0f);
        skipRect.anchorMax = new Vector2(1f, 0f);
        skipRect.pivot     = new Vector2(1f, 0f);
        skipRect.sizeDelta = new Vector2(160f, 44f);
        skipRect.anchoredPosition = new Vector2(-20f, 20f);

        var skipImg = skipGo.GetComponent<Image>();
        skipImg.color = ColBtn;

        var skipBtn = skipGo.GetComponent<Button>();
        skipBtn.targetGraphic = skipImg;

        var skipTextGo = NewText(skipGo.transform, "Text", "跳过引导", 20, ColText,
            TextAlignmentOptions.Center, false);
        skipTextGo.GetComponent<RectTransform>().Stretch();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        ShowChineseCharsForFontGeneration();

        EditorUtility.DisplayDialog("完成",
            "GuideCanvas 已生成。\n请重新生成 TMP 字体 Atlas 以包含全部中文。", "确定");
    }

    // ════════════════════════════════════════
    // 辅助方法
    // ════════════════════════════════════════

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    static GameObject NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = UILayer;
        var img = go.GetComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject NewText(Transform parent, string name, string content,
        float fontSize, Color color, TextAlignmentOptions alignment, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = UILayer;

        var tmp = go.GetComponent<TMP_Text>();
        tmp.text = content;
        tmp.font = _font;
        tmp.fontSharedMaterial = _font.material;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = raycast;

        return go;
    }

    [MenuItem("Tools/UI/导出 GuideCanvas 用字")]
    public static void ExportChineseChars()
    {
        var chars = CollectChineseChars();
        var path = "Assets/Fonts/GuideUI_Chars.txt";
        System.IO.File.WriteAllText(path, chars);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"用字已导出到 {path}，请把内容贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static void ShowChineseCharsForFontGeneration()
    {
        var chars = CollectChineseChars();
        Debug.Log($"[GuideCanvasBuilder] 请将以下字符复制到 TMP Font Asset Creator 生成字体 Atlas:\n{chars}");
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
