using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-time helper: auto-regenerate Lua config files from Config/Excel CSVs
/// when Unity starts or when entering Play mode, if any CSV is newer than its
/// generated Lua counterpart.
/// </summary>
[InitializeOnLoad]
public static class ExcelToLuaAutoGenerator
{
    private const string ExcelDir = "Config/Excel";
    private const string OutputDir = "Assets/Resources/Lua/Config/Generated";
    private const string GeneratorPath = "Tools/ExcelToLua/generator.py";

    static ExcelToLuaAutoGenerator()
    {
        EditorApplication.delayCall += RunIfDirty;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/ExcelToLua/Generate Now")]
    public static void GenerateNow()
    {
        RunGenerator();
    }

    [MenuItem("Tools/ExcelToLua/Check & Generate if Dirty")]
    public static void RunIfDirty()
    {
        if (IsDirty())
        {
            RunGenerator();
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            RunIfDirty();
        }
    }

    private static bool IsDirty()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string excelPath = Path.Combine(projectRoot, ExcelDir);
        string outputPath = Path.Combine(projectRoot, OutputDir);

        if (!Directory.Exists(excelPath))
        {
            return false;
        }

        foreach (string csvPath in Directory.GetFiles(excelPath, "*.csv"))
        {
            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            string luaPath = Path.Combine(outputPath, fileName + ".lua.txt");

            if (!File.Exists(luaPath))
            {
                return true;
            }

            DateTime csvTime = File.GetLastWriteTimeUtc(csvPath);
            DateTime luaTime = File.GetLastWriteTimeUtc(luaPath);
            if (csvTime > luaTime)
            {
                return true;
            }
        }

        return false;
    }

    private static void RunGenerator()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string generatorFullPath = Path.Combine(projectRoot, GeneratorPath);

        if (!File.Exists(generatorFullPath))
        {
            UnityEngine.Debug.LogError($"[ExcelToLua] Generator not found: {generatorFullPath}");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Excel To Lua", "Regenerating Lua config files...", 0.5f);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{generatorFullPath}\"",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    UnityEngine.Debug.Log($"[ExcelToLua] {output.Trim()}");
                }

                if (process.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError($"[ExcelToLua] Generation failed.\n{error}");
                }
                else
                {
                    UnityEngine.Debug.Log("[ExcelToLua] Config files generated successfully.");
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        AssetDatabase.Refresh();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[ExcelToLua] Failed to run generator: {ex.Message}\n" +
                           "Make sure Python 3 is installed and available in PATH.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
