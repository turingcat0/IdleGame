using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click scene organizer. Groups all scattered root-level GameObjects
/// under two new empty parents: UIRoot (for screen-space Canvas/UI) and
/// WorldRoot (for world-space entity pools).
///
/// Player, Grid, Main Camera, EventSystem and GameManager stay at scene root.
///
/// Menu: Tools/Scene/整理场景根节点
/// </summary>
public static class SceneRootOrganizer
{
    public const string UIRootName     = "UIRoot";
    public const string WorldRootName  = "WorldRoot";

    // UI objects to move under UIRoot
    static readonly string[] UIObjects =
    {
        "UICanvas",
        "BossBar",
        "LevelProgressUI",
        "CombatStatsUI",
        "ServantUI",
        "ServantUIRoot",
        "EquipmentComparePanel",
        "ArmorPanel",
        "ProfileUI",
        "GuideCanvas",
    };

    // World-space objects to move under WorldRoot
    static readonly string[] WorldObjects =
    {
        "Enemies",
        "EnemySpawnpoint",
        "Servants",
        "ServantSpawnPoint",
        "Projectiles",
        "Vfx",
        "DamageHuds",
        "Loots",
    };

    [MenuItem("Tools/Scene/整理场景根节点")]
    public static void Organize()
    {
        var report = new StringBuilder();
        report.AppendLine("=== Scene Root Organizer ===\n");

        // --- UIRoot ---
        Transform uiRoot = CreateRoot(UIRootName);
        report.AppendLine($"[{UIRootName}] created/found");
        int uiCount = ReparentAll(UIObjects, uiRoot, report, "UI");
        report.AppendLine($"  → {uiCount}/{UIObjects.Length} UI objects reparented\n");

        // --- WorldRoot ---
        Transform worldRoot = CreateRoot(WorldRootName);
        report.AppendLine($"[{WorldRootName}] created/found");
        int worldCount = ReparentAll(WorldObjects, worldRoot, report, "World");
        report.AppendLine($"  → {worldCount}/{WorldObjects.Length} world objects reparented\n");

        // --- Summary ---
        report.AppendLine("Kept at scene root: Player, Grid, Main Camera, EventSystem, GameManager");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("场景整理完成", report.ToString(), "确定");
    }

    static Transform CreateRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
            return existing.transform;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go.transform;
    }

    static int ReparentAll(string[] names, Transform parent, StringBuilder report, string label)
    {
        int count = 0;
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                report.AppendLine($"  ⚠ {name}: not found (skipped)");
                continue;
            }

            // Skip if already under the correct parent
            if (go.transform.parent == parent)
            {
                report.AppendLine($"  ✓ {name}: already under {parent.name}");
                count++;
                continue;
            }

            Undo.SetTransformParent(go.transform, parent, $"Reparent {name}");
            // true = preserve world position
            go.transform.SetParent(parent, true);
            count++;
            report.AppendLine($"  ✓ {name}: moved under {parent.name}");
        }
        return count;
    }
}
