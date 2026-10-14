using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把 EnemyBase.prefab 的头顶血条 Canvas/HP 重建为 BossBar 同款结构：
/// HP(Border 外框) / Bg(track 底) / Ghost(黄色残影) / Fill(红色即时血量)。
/// 仅重建 HP 子层级，不动 SpriteRenderer/Collider/Rigidbody/LuaBehaviour/Canvas/BuffBar。
/// 配合 EnemyView.lua.txt 的 offsetMax 驱动（Fill 即时、Ghost 滞后）。
/// </summary>
public static class EnemyOverheadBarBuilder
{
    private const string PrefabPath = "Assets/Resources/Prefabs/EnemyBase.prefab";

    // 颜色与 BossBar 一致
    private static readonly Color ColBorder = new Color(0.10f, 0.10f, 0.10f, 0.90f);
    private static readonly Color ColBg     = new Color(0.04f, 0.04f, 0.04f, 0.95f);
    private static readonly Color ColFill   = new Color(0.80f, 0.15f, 0.15f, 1f);
    private static readonly Color ColGhost  = new Color(1.00f, 0.85f, 0.30f, 0.80f);

    [MenuItem("Tools/Prefab/重建敌人头顶血条 (BossBar 风格)")]
    public static void Rebuild()
    {
        if (!System.IO.File.Exists(PrefabPath))
        {
            EditorUtility.DisplayDialog("找不到 prefab", PrefabPath, "确定");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            EditorUtility.DisplayDialog("加载失败", $"无法加载 prefab: {PrefabPath}", "确定");
            return;
        }

        try
        {
            Transform hpTf = root.transform.Find("Canvas/HP");
            if (hpTf == null)
            {
                Debug.LogError("[EnemyOverheadBarBuilder] 找不到 Canvas/HP，已取消。");
                return;
            }

            // 清掉 HP 现有子节点（旧 CurrentHP 等）
            for (int i = hpTf.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(hpTf.GetChild(i).gameObject);

            // HP 作为 Border（外框），改色为 Simple。保留其 RectTransform（定位/尺寸不变）
            Image borderImg = hpTf.GetComponent<Image>();
            if (borderImg != null)
            {
                borderImg.color = ColBorder;
                borderImg.type = Image.Type.Simple;
                borderImg.raycastTarget = false;
            }

            // Bg：血条底（track），在 HP 内缩一点点边作为框
            GameObject bgGo = NewStretchImage("Bg", hpTf, ColBg);
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.offsetMin = new Vector2(0.001f, 0.0015f);
            bgRect.offsetMax = new Vector2(-0.001f, -0.0015f);

            // Ghost（黄色残影，先建 → 渲染在下层）
            NewStretchImage("Ghost", bgGo.transform, ColGhost);
            // Fill（红色即时血量，后建 → 渲染在 Ghost 之上）
            NewStretchImage("Fill", bgGo.transform, ColFill);
        }
        finally
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"已重建 {PrefabPath} 的头顶血条为 BossBar 风格（Border/Bg/Ghost/Fill）。\n" +
            "运行后请进 Play 模式确认小怪头顶血条即时跟随 + 黄色残影滞后。", "确定");
    }

    private static GameObject NewStretchImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        return go;
    }
}
