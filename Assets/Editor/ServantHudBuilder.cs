using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 生成随从 HUD 场景结构：ServantHUD 根节点 + 空的 Card1/Card2/Card3 容器。
/// 运行时由 ServantHUD.lua.txt 把 Prefabs/ServantHUD（单张卡片模板）实例化到这些容器里。
/// </summary>
public static class ServantHudBuilder
{
    const string RootName = "ServantHUD";
    const string LuaPath = "Assets/Resources/Lua/LuaBehavior/UI/ServantHUD.lua.txt";

    static readonly int UILayer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5;

    [MenuItem("Tools/UI/生成 ServantHUD")]
    public static void Build()
    {
        var luaScript = AssetDatabase.LoadAssetAtPath<TextAsset>(LuaPath);
        if (luaScript == null)
        {
            EditorUtility.DisplayDialog("缺少 Lua 脚本", $"找不到: {LuaPath}", "确定");
            return;
        }

        // 删掉旧的同名根节点
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("已存在同名对象",
                $"场景里已经有 {RootName}。\n重新生成会删掉它。", "删掉重建", "取消");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        // 找一个合适的父节点：优先挂在 ServantUI Canvas 下，否则挂在 UIRoot 下
        Transform parent = FindServantUIParent();
        bool hasCanvasParent = parent != null;
        if (!hasCanvasParent)
            parent = UIRootHelper.GetOrCreateUIRoot();

        // 创建 ServantHUD 根节点
        var rootComponents = hasCanvasParent
            ? new System.Type[] { typeof(RectTransform), typeof(LuaBehaviour) }
            : new System.Type[] { typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LuaBehaviour) };
        var root = new GameObject(RootName, rootComponents);
        Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
        root.layer = UILayer;
        root.transform.SetParent(parent, false);

        if (!hasCanvasParent)
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
        }

        var rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        var lb = root.GetComponent<LuaBehaviour>();
        lb.luaScript = luaScript;
        lb.injections = new Injection[0];

        // 创建 3 个空的卡片容器
        for (int i = 1; i <= 3; i++)
        {
            var card = new GameObject("Card" + i, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(card, "Create Card" + i);
            card.layer = UILayer;
            card.transform.SetParent(root.transform, false);

            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0f, 1f);
            cardRect.anchorMax = new Vector2(0f, 1f);
            cardRect.pivot = new Vector2(0f, 1f);
            cardRect.sizeDelta = new Vector2(180f, 80f);
            cardRect.anchoredPosition = new Vector2(20f, -20f - (i - 1) * 90f);
        }

        Selection.activeGameObject = root;
        EditorSceneManager.MarkAllScenesDirty();
    }

    static Transform FindServantUIParent()
    {
        var servantUI = GameObject.Find("ServantUI");
        if (servantUI == null) return null;

        var canvas = servantUI.GetComponent<Canvas>();
        return canvas != null ? servantUI.transform : null;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
