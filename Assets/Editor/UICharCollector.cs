using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 一次性扫描项目中所有源文件，提取全部 UI 用字（ASCII 可打印 + 标点符号 + CJK 汉字），
/// 去重排序后导出到单个 txt 文件，供 TMP Font Asset Creator 生成 Atlas。
/// </summary>
public static class UICharCollector
{
    const string OutputPath = "Assets/Fonts/AllChineseChars.txt";

    static readonly string ProjectRoot = Directory.GetParent(Application.dataPath).FullName;

    [MenuItem("Tools/UI/导出全部中文字符集")]
    public static void ExportAll()
    {
        var chars = new HashSet<char>();
        var log = new StringBuilder();
        int totalFiles = 0;

        // 始终包含 ASCII 可打印字符（0x20-0x7E）：空格、符号、0-9、A-Z、a-z
        for (char c = (char)0x20; c <= 0x7E; c++)
            chars.Add(c);

        // Lua 脚本（.lua.txt）
        totalFiles += ScanAndLog(
            Path.Combine(Application.dataPath, "Resources", "Lua"),
            new[] { ".lua.txt" }, "Lua 脚本", chars, log);

        // CSV 配置表
        totalFiles += ScanAndLog(
            Path.Combine(ProjectRoot, "Config", "Excel"),
            new[] { ".csv" }, "CSV 配置表", chars, log);

        // C# 编辑器脚本（builder 里有硬编码中文）
        totalFiles += ScanAndLog(
            Path.Combine(Application.dataPath, "Editor"),
            new[] { ".cs" }, "C# 脚本", chars, log);

        // 预制体
        totalFiles += ScanAndLog(
            Application.dataPath,
            new[] { ".prefab" }, "预制体", chars, log);

        // 场景
        totalFiles += ScanAndLog(
            Application.dataPath,
            new[] { ".unity" }, "场景", chars, log);

        // JSON / XML 配置
        totalFiles += ScanAndLog(
            Path.Combine(Application.dataPath, "Resources"),
            new[] { ".json", ".xml" }, "JSON/XML 配置", chars, log);

        // 当前打开的场景中所有 TMP / UI.Text 组件的文本
        int sceneTextCount = ScanLoadedScene(chars);
        log.AppendLine($"  场景内文本组件: {sceneTextCount} 个");

        // 排序导出
        var sorted = new List<char>(chars);
        sorted.Sort();
        var result = new string(sorted.ToArray());

        string fullOutputPath = Path.Combine(Application.dataPath, "Fonts", "AllChineseChars.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
        File.WriteAllText(fullOutputPath, result);
        AssetDatabase.Refresh();

        Debug.Log($"[UICharCollector] 扫描 {totalFiles} 个文件，共 {sorted.Count} 个字符:\n{log}\n字符集:\n{result}");

        EditorUtility.DisplayDialog("完成",
            $"扫描 {totalFiles} 个文件，共收集 {sorted.Count} 个字符。\n" +
            $"已导出到 {OutputPath}，\n请贴进 TMP Font Asset Creator 重新生成 Atlas。", "确定");
    }

    static int ScanAndLog(string dir, string[] exts, string label,
        HashSet<char> chars, StringBuilder log)
    {
        int count = ScanDir(dir, exts, chars);
        log.AppendLine($"  {label}: {count} 个文件");
        return count;
    }

    static int ScanDir(string dir, string[] extensions, HashSet<char> chars)
    {
        if (!Directory.Exists(dir)) return 0;

        int count = 0;
        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            bool match = false;
            foreach (var ext in extensions)
            {
                if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    match = true;
                    break;
                }
            }
            if (!match) continue;

            count++;
            string content = File.ReadAllText(file);
            content = StripComments(file, content);
            foreach (var c in content)
            {
                if (ShouldCollect(c))
                    chars.Add(c);
            }
        }
        return count;
    }

    static bool ShouldCollect(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF) ||  // CJK 统一表意文字
               (c >= 0x3000 && c <= 0x303F) ||  // CJK 符号和标点（、。《》「」【】等）
               (c >= 0xFF00 && c <= 0xFFEF) ||  // 全角形式（！％（），．：；等）
               (c >= 0x2000 && c <= 0x206F);     // 通用标点（–—''""……等）
    }

    static int ScanLoadedScene(HashSet<char> chars)
    {
        int count = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                count += CollectText(root, chars);
            }
        }
        return count;
    }

    static int CollectText(GameObject go, HashSet<char> chars)
    {
        int count = 0;

        // TMP（UGUI 和 非UGUI 都走 TMP_Text 基类）
        var tmpTexts = go.GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in tmpTexts)
        {
            if (string.IsNullOrEmpty(tmp.text)) continue;
            count++;
            foreach (var c in tmp.text)
                if (ShouldCollect(c)) chars.Add(c);
        }

        // 兼容旧版 UI.Text
        var uiTexts = go.GetComponentsInChildren<Text>(true);
        foreach (var ui in uiTexts)
        {
            if (string.IsNullOrEmpty(ui.text)) continue;
            count++;
            foreach (var c in ui.text)
                if (ShouldCollect(c)) chars.Add(c);
        }

        return count;
    }

    static string StripComments(string file, string content)
    {
        if (file.EndsWith(".lua.txt", StringComparison.OrdinalIgnoreCase))
        {
            // Lua: 先去块注释 --[[ ... ]]，再去行注释 -- ...
            content = Regex.Replace(content, @"--\[\[.*?\]\]", "", RegexOptions.Singleline);
            content = Regex.Replace(content, @"--.*$", "", RegexOptions.Multiline);
        }
        else if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            // C#: 先去块注释 /* ... */，再去行注释 //...
            content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);
            content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);
        }
        return content;
    }
}
