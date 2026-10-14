using System.IO;
using UnityEngine;

namespace xluaDemo
{
    /// <summary>
    /// C# bridge exposed to Lua for reading/writing save files.
    /// Files live in Application.persistentDataPath.
    /// Accessible from Lua via CS.xluaDemo.SaveManager.
    /// </summary>
    public static class SaveManager
    {
        private const string SaveFileName = "save.json";
        private const string BackupFileName = "save.json.bak";

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        private static string BackupPath => Path.Combine(Application.persistentDataPath, BackupFileName);

        /// <summary>
        /// Write the JSON save string to disk.
        /// </summary>
        public static void Write(string json)
        {
            File.WriteAllText(SavePath, json);
        }

        /// <summary>
        /// Read the JSON save string from disk. Returns null if no save exists.
        /// </summary>
        public static string Read()
        {
            return File.Exists(SavePath) ? File.ReadAllText(SavePath) : null;
        }

        /// <summary>
        /// Check whether a save file exists.
        /// </summary>
        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Delete the save file and its backup.
        /// </summary>
        public static void Delete()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
            if (File.Exists(BackupPath))
            {
                File.Delete(BackupPath);
            }
        }

        /// <summary>
        /// Write a backup copy of the current save (if any).
        /// </summary>
        public static void WriteBackup()
        {
            if (!File.Exists(SavePath))
            {
                return;
            }
            File.Copy(SavePath, BackupPath, overwrite: true);
        }

        /// <summary>
        /// Read the backup save string. Returns null if no backup exists.
        /// </summary>
        public static string ReadBackup()
        {
            return File.Exists(BackupPath) ? File.ReadAllText(BackupPath) : null;
        }

        /// <summary>
        /// Remove the "guide" section from the save file, keeping all other progress intact.
        /// </summary>
        public static void ClearGuideProgress()
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            string json = File.ReadAllText(SavePath);
            string updated = RemoveJsonKey(json, "guide");
            if (updated != null && updated != json)
            {
                WriteBackup();
                File.WriteAllText(SavePath, updated);
            }
        }

        /// <summary>
        /// Remove a top-level key (and its value) from a JSON object string.
        /// Returns null if the key was not found.
        /// </summary>
        private static string RemoveJsonKey(string json, string key)
        {
            string quotedKey = "\"" + key + "\"";
            int start = 0;
            while (true)
            {
                int keyIndex = json.IndexOf(quotedKey, start);
                if (keyIndex < 0)
                {
                    return null;
                }

                // Make sure this is a key, not a string value containing the same text.
                // For simplicity we look backwards for an opening brace or comma or the very start.
                char prev = keyIndex > 0 ? json[keyIndex - 1] : '{';
                if (prev == '{' || prev == ',')
                {
                    int colonIndex = json.IndexOf(':', keyIndex + quotedKey.Length);
                    if (colonIndex < 0)
                    {
                        return null;
                    }

                    int valueStart = colonIndex + 1;
                    while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                    {
                        valueStart++;
                    }

                    if (valueStart >= json.Length)
                    {
                        return null;
                    }

                    int valueEnd = FindValueEnd(json, valueStart);
                    if (valueEnd < 0)
                    {
                        return null;
                    }

                    // Remove from the key to the end of the value.
                    int removeStart = keyIndex;
                    int removeEnd = valueEnd;

                    // Drop a trailing comma if present and it's not the only key.
                    if (json[removeEnd] == ',')
                    {
                        removeEnd++;
                    }
                    else if (removeStart > 0 && json[removeStart - 1] == ',')
                    {
                        removeStart--;
                    }

                    return json.Substring(0, removeStart) + json.Substring(removeEnd);
                }

                start = keyIndex + quotedKey.Length;
            }
        }

        /// <summary>
        /// Find the index just past the end of the JSON value starting at start.
        /// </summary>
        private static int FindValueEnd(string json, int start)
        {
            char c = json[start];
            if (c == '{')
            {
                return FindMatching(json, start, '{', '}');
            }
            if (c == '[')
            {
                return FindMatching(json, start, '[', ']');
            }
            if (c == '"')
            {
                return FindStringEnd(json, start);
            }

            // Number / true / false / null : read until delimiter.
            int i = start;
            while (i < json.Length)
            {
                char ch = json[i];
                if (ch == ',' || ch == '}' || ch == ']')
                {
                    return i;
                }
                i++;
            }
            return json.Length;
        }

        private static int FindMatching(string json, int start, char open, char close)
        {
            int depth = 1;
            int i = start + 1;
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '"')
                {
                    i = FindStringEnd(json, i);
                    if (i < 0)
                    {
                        return -1;
                    }
                }
                else if (c == open)
                {
                    depth++;
                }
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i + 1;
                    }
                }
                i++;
            }
            return -1;
        }

        private static int FindStringEnd(string json, int start)
        {
            int i = start + 1;
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '\\')
                {
                    i += 2;
                }
                else if (c == '"')
                {
                    return i + 1;
                }
                else
                {
                    i++;
                }
            }
            return -1;
        }
    }
}
