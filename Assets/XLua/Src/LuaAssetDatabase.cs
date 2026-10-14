
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using XLua;

[LuaCallCSharp]
public class LuaAssetDatabase
{
    public static Object LoadAssetAtPath(string path, System.Type type)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath(path, type);
#else
        return null;
#endif
    }

    public static Object[] LoadAll(string path, System.Type type)
    {
#if UNITY_EDITOR
        List<string> resFiles = new List<string>();
        foreach (string file in Directory.GetFiles(path))
        {
            if (Path.GetExtension(file) != ".meta")
            {
                // Debug.Log("文件: " + file);
                resFiles.Add(file);
            }
        }
        List<Object> ret = new List<Object>();
        foreach (var file in resFiles)
        {
            Object resObj = AssetDatabase.LoadAssetAtPath(file, type);
            ret.Add(resObj);
        }
        return ret.ToArray();
#else
        return new Object[]{};
#endif
        
    }
}
