using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Editor tool: 为每个敌人/boss 预计算其所有动画帧中最低的视觉底边偏移 bottomY。
///
/// 读取 Resources/AnimatorControllers 下的 AnimatorController，取出每个 state 的 AnimationClip，
/// 再取 clip 中 m_Sprite 曲线引用的所有 Sprite，用 Sprite.bounds.min.y 得到该 sprite 相对 pivot 的底边偏移，
/// 最后取所有帧中的最小值（最靠下的那一帧）作为该敌人的 bottomY。
///
/// 输出 Config.Generated.EnemyVisualBounds，供 EnemySpawner 在生成时直接查表做脚底对齐，
/// 不需要在运行时去逼 Animator 出 sprite。
/// </summary>
public static class EnemyVisualBoundsGenerator
{
    private const string ControllersDir = "Assets/Resources/AnimatorControllers";
    private const string OutputPath = "Assets/Resources/Lua/Config/Generated/EnemyVisualBounds.lua.txt";

    [MenuItem("Tools/Generate Enemy Visual Bounds")]
    public static void Generate()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string fullControllersDir = Path.Combine(projectRoot,
            ControllersDir.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(fullControllersDir))
        {
            Debug.LogError($"[EnemyVisualBounds] Directory not found: {ControllersDir}");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("local EnemyVisualBounds = {");
        sb.AppendLine();

        string[] guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { ControllersDir });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string enemyId = Path.GetFileNameWithoutExtension(path);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) continue;

            float minBottomY = float.MaxValue;
            bool hasSprite = false;

            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                CollectMinBottomY(layer.stateMachine, ref minBottomY, ref hasSprite);
            }

            if (!hasSprite)
            {
                Debug.LogWarning($"[EnemyVisualBounds] No sprite found in controller: {enemyId}");
                continue;
            }

            sb.AppendLine($"    [\"{enemyId}\"] = {{ bottomY = {minBottomY:0.####} }},");
            count++;
        }

        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return EnemyVisualBounds");

        string fullOutputPath = Path.Combine(projectRoot,
            OutputPath.Replace('/', Path.DirectorySeparatorChar));
        string outputDir = Path.GetDirectoryName(fullOutputPath);
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        File.WriteAllText(fullOutputPath, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"[EnemyVisualBounds] Generated {count} entries. Output: {OutputPath}");
    }

    private static void CollectMinBottomY(AnimatorStateMachine sm, ref float minBottomY, ref bool hasSprite)
    {
        foreach (ChildAnimatorState childState in sm.states)
        {
            AnimatorState state = childState.state;
            if (state == null || state.motion == null) continue;

            AnimationClip clip = state.motion as AnimationClip;
            if (clip == null) continue;

            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.propertyName != "m_Sprite") continue;

                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                foreach (ObjectReferenceKeyframe keyframe in keyframes)
                {
                    Sprite sprite = keyframe.value as Sprite;
                    if (sprite == null) continue;

                    hasSprite = true;
                    float bottomY = sprite.bounds.min.y;
                    if (bottomY < minBottomY) minBottomY = bottomY;
                }
            }
        }

        foreach (ChildAnimatorStateMachine child in sm.stateMachines)
        {
            CollectMinBottomY(child.stateMachine, ref minBottomY, ref hasSprite);
        }
    }
}
