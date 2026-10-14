using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

public class PublishWindow : EditorWindow
{
    static public string publishDir = "";
    static public string inputPubDir = ""; 
    static public string buildDirectory = "assetbundles";
    static public string configFile = buildDirectory + "/publish.json";
    static public Dictionary<string, string> fileToMD5 = new Dictionary<string, string>();

    // Add menu item
    [MenuItem("Tools/Publish")]
    static void Init()
    {
        if (File.Exists(configFile))
        {
            string jsonStr = File.ReadAllText(configFile);
            Debug.Log(jsonStr);
            var conf = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonStr);
            publishDir = conf["publishDir"];
            inputPubDir = publishDir;
        }
        EditorWindow.GetWindow(typeof(PublishWindow));
    }

    Rect buttonRect;
    void OnGUI()
    {
        GUILayout.Label("Set Publish Directory", EditorStyles.boldLabel);
        inputPubDir = EditorGUILayout.TextField("发布路径", inputPubDir);
        if (GUILayout.Button("Publish", GUILayout.Width(200)))
        {
            if (inputPubDir == "")
            {
                EditorUtility.DisplayDialog("error", "no publish dir config", "ok");
                return;
            }
            else
            {
                if (!Directory.Exists(inputPubDir))
                {
                    EditorUtility.DisplayDialog("error", "publish dir not exists", "ok");
                    return;
                }
                WritePulishConfig();
                Publish();
                Debug.Log("publish finish");

            }
        }
        // if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
    }

    void WritePulishConfig()
    {
        if (inputPubDir == publishDir)
            return;
        publishDir = inputPubDir;
        JObject jobj = new JObject();
        jobj["publishDir"] = publishDir;
        string jstr = JsonConvert.SerializeObject(jobj, Formatting.Indented);
        File.WriteAllText(configFile, jstr);
    }

    void Publish()
    {
        string buildJsonStr = File.ReadAllText(GenPath(buildDirectory, "buildInfo.json"));
        JObject buildJobj = JObject.Parse(buildJsonStr);
        JToken bundleList = buildJobj["bundleList"];
        JObject verObj = new JObject();
        verObj["bundleList"] = bundleList;
        verObj["resVersion"] = buildJobj["resVersion"].ToObject<int>();
        string verJstr = JsonConvert.SerializeObject(verObj, Formatting.Indented);
        File.WriteAllText(GenPath(publishDir, "version.json"), verJstr);
        fileToMD5.Clear();
        foreach (JObject obj in bundleList)
        {
            fileToMD5.Add(obj["file"].ToString(), obj["md5"].ToString());
        }
        CopyTo(buildDirectory, GenPath(publishDir, buildDirectory));
    }
    void CopyTo(string srcDir, string dstDir)
    {

        var src = new DirectoryInfo(srcDir);
        if (!Directory.Exists(dstDir))
            Directory.CreateDirectory(dstDir);
        foreach (FileInfo finfo in src.GetFiles())
        {
            string fn = finfo.Name;
            string srcPath = GenPath(srcDir, fn);
            if (!fileToMD5.ContainsKey(srcPath))
                continue;
            fn = string.Format("{0}_{1}", fn, fileToMD5[srcPath]);
            string targetPath = Path.Combine(dstDir, fn);
            Debug.Log(srcPath + "->" + targetPath);
            finfo.CopyTo(targetPath, true);
        }
        foreach (var subDir in src.GetDirectories())
        {
            string subSrcDir = GenPath(srcDir, subDir.Name);
            string subDstDir = GenPath(dstDir, subDir.Name);
            CopyTo(subSrcDir, subDstDir);
        }
    }

    void GenerateVersion()
    {

    }

    string GenPath(params string[] l)
    {
        return string.Join("/", l);
    }


}
