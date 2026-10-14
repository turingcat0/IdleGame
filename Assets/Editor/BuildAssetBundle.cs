using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;


public class AssetBundleBuilder
{
    static string[] bundleDirs = {
        "Images",
        "Prefabs",
        "Products/ds1",
        "Products/jstar",
        "Products/s3g",
        "Lua",
    };
    static Encoding fileEncoding = new UTF8Encoding(false);
    static string buildDirectory = "assetbundles";
    static string targetDirectory = "Assets/StreamingAssets/assetbundles";
    static string buildInfoFile = "buildInfo.json";
    static string excludeFilePattern = @"(\.manifest|\.json)$";
    static string preAssetBundleMD5 = "";
    static MD5 md5 = MD5.Create();

    [MenuItem("Tools/BuildAssetBundle")]
    static void BuildAssetBundle()
    {
        Debug.Log("build start");
        if (!Directory.Exists(buildDirectory))
        {
            Directory.CreateDirectory(buildDirectory);
        }
        preAssetBundleMD5 = GetRootAssetBundleMD5();
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
        foreach (string dir in bundleDirs)
        {
            var build = BuildDir(dir);
            builds.Add(build);
        }
        BuildAssetBundleOptions opt = BuildAssetBundleOptions.UncompressedAssetBundle |
                                        BuildAssetBundleOptions.DisableWriteTypeTree;
        BuildPipeline.BuildAssetBundles(buildDirectory, builds.ToArray(), opt,
                                        BuildTarget.Android);
        Debug.Log("build finish");
        GenerateBuildInfoFile(buildDirectory, excludeFilePattern);
        Debug.Log("gen buildInfo finish");
        CopyAssetBundle();
        Debug.Log("copy finish");
    }
    [MenuItem("Tools/CopyAssetBundle")]
    static void CopyAssetBundle()
    {
        CopyDirectory(buildDirectory, targetDirectory, excludeFilePattern);
        AssetDatabase.Refresh();
    }
    static string GetRootAssetBundleMD5()
    {
        string filepath = Path.Combine(buildDirectory, buildDirectory);
        if (!File.Exists(filepath))
        {
            return "";
        }
        return ComputeMD5(filepath);
    }
    static void CopyDirectory(string sourceDir, string destinationDir, string excludePattern = "^$")
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        DirectoryInfo[] dirs = dir.GetDirectories();
        if (!Directory.Exists(destinationDir))
            Directory.CreateDirectory(destinationDir);
        foreach (FileInfo file in dir.GetFiles())
        {
            string fn = file.Name;
            if (fn != buildInfoFile && Regex.IsMatch(fn, excludePattern))
                continue;
            string targetFilePath = Path.Combine(destinationDir, fn);
            Debug.Log("copy:" + fn + "->" + targetFilePath);
            file.CopyTo(targetFilePath, true);
        }
        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, excludePattern);
        }
    }
    static AssetBundleBuild BuildDir(string dir)
    {
        string path = "Assets/" + dir;
        Debug.Log("build " + path);
        string[] files = Directory.GetFiles(path).Where(name => !name.EndsWith(".meta")).ToArray();
        var build = new AssetBundleBuild();
        build.assetBundleName = dir;
        build.assetNames = files;
        return build;
    }
    [MenuItem("Tools/GenerateBuildInfo")]
    static void GenerateBuildInfoFile()
    {
        GenerateBuildInfoFile(buildDirectory, excludeFilePattern);
    }
    static void GenerateBuildInfoFile(string path, string excludePattern = null)
    {
        int version = 1;
        string preMD5 = "";
        string buildInfoFilepath = Path.Combine(path, buildInfoFile);
        string rootBundle = buildDirectory+"/"+buildDirectory;
        if (File.Exists(buildInfoFilepath))
        {
            JObject jobj = JObject.Parse(File.ReadAllText(buildInfoFilepath));
            version = jobj["resVersion"].ToObject<int>();
            JArray array = (JArray)jobj["bundleList"];
            foreach (JObject obj in array)
            {
                if (obj["file"].ToString() == rootBundle)
                {
                    preMD5 = obj["md5"].ToString();
                    Debug.Log("root md5:"+preMD5);
                    Debug.Log("pret md5:"+preAssetBundleMD5);
                    MyAssert(preMD5 == preAssetBundleMD5, "root bundle md5 not match");
                }
            }
        }
        var bundleList = new List<Dictionary<string, string>>();
        WalkDirectory(path, bundleList, excludePattern);
        string nowMD5 = "";
        foreach (var item in bundleList)
        {
            if (item["file"] == rootBundle)
            {
                nowMD5 = item["md5"];
            }
        }
        Debug.Log(preMD5 + " " + nowMD5);
        MyAssert(nowMD5 != "", "root bundle not exists");
        if (preMD5 != "" && nowMD5 != preMD5)
        {
            version = version + 1;
        }
        var content = new Dictionary<string, object>();
        content["bundleList"] = bundleList;
        content["resVersion"] = version;
        string jsonStr = JsonConvert.SerializeObject(content, Formatting.Indented);
        // string jsonStr = MiniJSON.Json.Serialize(content);
        Debug.Log(jsonStr);
        File.WriteAllText(buildInfoFilepath, jsonStr, fileEncoding);
    }

    static void WalkDirectory(string dirName, List<Dictionary<string, string>> bundleList, string excludePattern = "^$")
    {
        var dirInfo = new DirectoryInfo(dirName);
        foreach (FileInfo file in dirInfo.GetFiles())
        {
            if (Regex.IsMatch(file.Name, excludePattern))
                continue;
            Debug.Log(file.Name);
            var info = new Dictionary<string, string>();
            info["file"] = dirName + "/" + file.Name;
            string md5 = ComputeMD5(file.FullName);
            info["md5"] = md5;
            bundleList.Add(info);
        }
        DirectoryInfo[] dirs = dirInfo.GetDirectories();
        foreach (DirectoryInfo subDir in dirs)
        {
            string subDirPath = dirName + "/" + subDir.Name;
            WalkDirectory(subDirPath, bundleList, excludePattern);
        }
    }

    static string ComputeMD5(string filepath)
    {
        byte[] bytes = File.ReadAllBytes(filepath);
        byte[] hashcode = md5.ComputeHash(bytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashcode)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    static void MyAssert(bool cond, string msg)
    {
        if (!cond)
            throw new Exception(msg);
    }
}
