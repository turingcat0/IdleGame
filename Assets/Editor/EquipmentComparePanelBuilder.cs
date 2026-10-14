using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 生成 EquipmentComparePanel 的场景节点树。
/// 节点命名严格对应 Lua 侧 EquipmentComparePanel.lua.txt 里的 transform:Find 路径，改名会直接让 Lua 取不到组件。
/// </summary>
public static class EquipmentComparePanelBuilder
{
    const string RootName = "EquipmentComparePanel";
    const string LuaScriptPath = "Assets/Resources/Lua/LuaBehavior/UI/EquipmentComparePanel.lua.txt";
    const string FontPath = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";

    // 布局参数，想改尺寸只动这里
    const float CardW = 250f;
    const float CardH = 420f;
    const float CardGap = 24f;
    const float CardX = (CardW + CardGap) / 2f;   // 卡片中心相对屏幕中心的横向偏移

    // AttributeType 1..7 对应的中文标签
    static readonly string[] AttrLabels =
    {
        "生命", "魔力", "攻击", "防御", "暴击率", "暴击伤害", "速度"
    };

    // 属性行的显示顺序（存的是 AttributeType 索引）。
    // 节点名固定为 Attribute<索引>，但摆放顺序自由，所以这里按设计图的观感排：攻击、防御、生命在前。
    static readonly int[] AttrDisplayOrder = { 3, 4, 1, 5, 6, 7, 2 };

    static readonly Color ColBackdrop = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color ColCardEdge = new Color(0.25f, 0.65f, 1f, 1f);
    static readonly Color ColCardFill = new Color(0.13f, 0.12f, 0.22f, 0.98f);
    static readonly Color ColTitle = new Color(0.55f, 0.82f, 1f, 1f);
    static readonly Color ColBody = new Color(0.88f, 0.9f, 0.95f, 1f);
    static readonly Color ColMuted = new Color(0.62f, 0.64f, 0.72f, 1f);
    static readonly Color ColStrip = new Color(0.22f, 0.21f, 0.32f, 1f);
    static readonly Color ColDecompose = new Color(0.2f, 0.42f, 0.78f, 1f);
    static readonly Color ColEquip = new Color(0.78f, 0.26f, 0.26f, 1f);

    static TMP_FontAsset _font;

    [MenuItem("Tools/UI/生成 EquipmentComparePanel")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
        {
            EditorUtility.DisplayDialog("缺少字体",
                "找不到中文字体资源：\n" + FontPath + "\n\n生成出来的文本会是方块，已中止。", "好");
            return;
        }

        var luaScript = AssetDatabase.LoadAssetAtPath<TextAsset>(LuaScriptPath);
        if (luaScript == null)
        {
            EditorUtility.DisplayDialog("缺少 Lua 脚本",
                "找不到：\n" + LuaScriptPath + "\n\n已中止。", "好");
            return;
        }

        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("已存在同名对象",
                "场景里已经有 " + RootName + "。\n\n重新生成会删掉它，你在 Inspector 里做过的手工调整会丢失。",
                "删掉重建", "取消");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        // 根节点自带 Canvas，Lua 侧是靠 canvas.enabled 开关面板的，不是 SetActive
        var root = new GameObject(RootName, typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LuaBehaviour));
        Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
        root.transform.SetParent(UIRootHelper.GetOrCreateUIRoot(), false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;              // 盖在其它 UI 之上
        canvas.enabled = false;                 // 默认收起，由 Lua 的 Init 打开

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;   // 与场景里 UICanvas 保持一致

        var lb = root.GetComponent<LuaBehaviour>();
        lb.luaScript = luaScript;
        lb.injections = new Injection[0];

        // Background：全屏遮罩，同时是 Lua 里所有 Find 路径的第一段
        var background = NewImage("Background", root.transform, ColBackdrop, raycast: true);
        Stretch(background);

        BuildCard(background, "OldEquipment", -CardX);
        BuildCard(background, "NewEquipment", CardX);

        BuildPowerDelta(background);
        BuildButtons(background);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[EquipmentComparePanelBuilder] 已生成 " + RootName + "，默认 Canvas.enabled = false。");
    }

    // ---------- 卡片 ----------

    static void BuildCard(RectTransform parent, string name, float offsetX)
    {
        // 卡片本体用边框色，内部再叠一层深色 Frame，做出图上那种亮蓝描边
        var card = NewImage(name, parent, ColCardEdge);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(CardW, CardH);
        card.anchoredPosition = new Vector2(offsetX, 0f);

        // Frame 只是背景层，不在 Lua 的 Find 路径里，纯装饰
        var frame = NewImage("Frame", card, ColCardFill);
        Stretch(frame, 3f);

        NewText("Name", card, "装备名称", 20, ColTitle, TextAlignmentOptions.Center,
            new Vector2(0f, 178f), new Vector2(CardW - 20f, 30f));

        NewText("Rarity", card, "【普通】", 15, ColMuted, TextAlignmentOptions.Center,
            new Vector2(0f, 152f), new Vector2(CardW - 20f, 24f));

        var icon = NewImage("Icon", card, Color.white);
        icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(76f, 76f);
        icon.anchoredPosition = new Vector2(0f, 100f);
        icon.GetComponent<Image>().preserveAspect = true;

        NewText("Level", card, "Lv0", 15, ColBody, TextAlignmentOptions.Center,
            new Vector2(0f, 48f), new Vector2(CardW - 20f, 24f));

        // Power：该栏自身战力。设计图上左栏这个位置画的是「当前装备」灰条，右栏是战力差值，
        // 但 Lua 侧两栏都要 Power 节点，所以统一放在这里，底衬一条灰条。
        var strip = NewImage("PowerStrip", card, ColStrip);
        strip.anchorMin = strip.anchorMax = new Vector2(0.5f, 0.5f);
        strip.sizeDelta = new Vector2(CardW - 32f, 26f);
        strip.anchoredPosition = new Vector2(0f, 20f);

        NewText("Power", card, "0", 15, ColBody, TextAlignmentOptions.Center,
            new Vector2(0f, 20f), new Vector2(CardW - 36f, 24f));

        BuildAttributes(card);
    }

    static void BuildAttributes(RectTransform card)
    {
        var attributes = NewRect("Attributes", card);
        attributes.anchorMin = attributes.anchorMax = new Vector2(0.5f, 0.5f);
        attributes.pivot = new Vector2(0.5f, 1f);
        attributes.sizeDelta = new Vector2(CardW - 32f, 196f);
        attributes.anchoredPosition = new Vector2(0f, 2f);

        var layout = attributes.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        foreach (int attrIndex in AttrDisplayOrder)
        {
            // 节点名必须是 Attribute<AttributeType 数值>，Lua 是按 i=1..7 拼路径取的
            var row = NewRect("Attribute" + attrIndex, attributes);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            NewText("Label", row, AttrLabels[attrIndex - 1], 15, ColMuted,
                TextAlignmentOptions.Left, Vector2.zero, Vector2.zero, stretch: true,
                padLeft: 6f, padRight: 90f);

            NewText("Value", row, "0", 15, ColBody,
                TextAlignmentOptions.Right, Vector2.zero, Vector2.zero, stretch: true,
                padLeft: CardW - 130f, padRight: 6f);
        }
    }

    // ---------- 卡片之外的元素（Lua 侧都挂在 Background 下）----------

    static void BuildPowerDelta(RectTransform background)
    {
        NewText("PowerDelta", background, "+0", 19, new Color(0.2f, 1f, 0.35f, 1f),
            TextAlignmentOptions.Center, new Vector2(CardX, 20f), new Vector2(CardW - 36f, 26f));
    }

    static void BuildButtons(RectTransform background)
    {
        NewButton("DecomposeButton", background, "分解", ColDecompose,
            new Vector2(CardX - 58f, -186f));
        NewButton("EquipButton", background, "替换", ColEquip,
            new Vector2(CardX + 58f, -186f));
    }

    static void NewButton(string name, RectTransform parent, string label, Color color, Vector2 pos)
    {
        var rect = NewImage(name, parent, color, raycast: true);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(104f, 42f);
        rect.anchoredPosition = pos;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();

        NewText("Label", rect, label, 17, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, stretch: true);
    }

    // ---------- 基础构件 ----------

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = UILayer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>raycast 只对需要接收点击的元件打开，纯装饰图形保持关闭，避免挡住下层。</summary>
    static RectTransform NewImage(string name, Transform parent, Color color, bool raycast = false)
    {
        var rect = NewRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return rect;
    }

    static RectTransform NewText(string name, Transform parent, string content, float size,
        Color color, TextAlignmentOptions align, Vector2 pos, Vector2 sizeDelta,
        bool stretch = false, float padLeft = 0f, float padRight = 0f)
    {
        var rect = NewRect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        if (stretch)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(padLeft, 0f);
            rect.offsetMax = new Vector2(-padRight, 0f);
        }
        else
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = pos;
        }
        return rect;
    }

    static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
