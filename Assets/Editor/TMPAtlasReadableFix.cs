using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// 一键将 TMP 字体 Atlas 纹理设为 Readable，消除运行时
/// "Please make the texture readable" 警告。
/// 原因：字体 Atlas 里缺少某个字符（如新加的"副本"），TMP 尝试运行时
/// 动态补字，但纹理未开启 Read/Write 导致写入失败。
/// </summary>
public static class TMPAtlasReadableFix
{
    const string FontPath = "Assets/Fonts/SourceHanSansSC-Normal SDF.asset";

    [MenuItem("Tools/Fix TMP Atlas Readable")]
    public static void Fix()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("找不到字体", $"找不到: {FontPath}", "确定");
            return;
        }

        int count = 0;
        foreach (var atlas in font.atlasTextures)
        {
            if (atlas == null) continue;

            // Texture2D.isReadable 是只读属性，必须通过 SerializedObject 修改 m_IsReadable 字段
            var so = new SerializedObject(atlas);
            var prop = so.FindProperty("m_IsReadable");
            if (prop != null && !prop.boolValue)
            {
                prop.boolValue = true;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(atlas);
                count++;
            }
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("完成",
            count > 0
                ? $"已将 {count} 张 Atlas 纹理设为 Readable。\n运行时动态补字警告应消失。"
                : "所有 Atlas 纹理已经是 Readable，无需修改。",
            "确定");
    }
}
