/*
 * Tencent is pleased to support the open source community by making xLua available.
 * Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
 * http://opensource.org/licenses/MIT
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XLua;
using System;
using System.IO;

[System.Serializable]
public class Injection
{
    public string name;
    public GameObject value;
}

[LuaCallCSharp]
public class LuaBehaviour : MonoBehaviour
{
    public TextAsset luaScript;
    public Injection[] injections;

    internal static bool isLuaEnvInit = false;
    public static LuaEnv luaEnv = new LuaEnv(); //all lua behaviour shared one luaenv only!
    internal static float lastGCTime = 0;
    internal const float GCInterval = 1;//1 second 

    private Action luaStart;
    private Action luaUpdate;
    private Action luaOnDestroy;
    private Action<Collider2D> luaOnTriggerEnter2D;

    public LuaTable scriptEnv;

    public byte[] MyLoader(ref string filepath) {
        string filename = String.Format("Lua/{0}.lua", filepath.Replace('.', '/'));
        TextAsset file = Resources.Load<TextAsset>(filename);
        if (file == null) {
            return null;
        }
        return file.bytes;
    }

     public void luaEnvInit()
    {
        luaEnv.AddLoader(MyLoader);
        isLuaEnvInit = true;
    }

    protected virtual void Awake()
    {
        if (!isLuaEnvInit) 
        {
            luaEnvInit();
        }

        scriptEnv = luaEnv.NewTable();

        // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
        LuaTable meta = luaEnv.NewTable();
        meta.Set("__index", luaEnv.Global);
        scriptEnv.SetMetaTable(meta);
        meta.Dispose();

        scriptEnv.Set("self", this);
        foreach (var injection in injections)
        {
            scriptEnv.Set(injection.name, injection.value);
        }
        #if UNITY_EDITOR
        string luaPath = UnityEditor.AssetDatabase
            .GetAssetPath(luaScript)
            .Replace("\\", "/");

        // Assets/Resources/Lua/Login.lua.txt
        // 转成 Assets/Resources/Lua/Login
        const string suffix = ".lua.txt";
        if (luaPath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            luaPath = luaPath.Substring(0, luaPath.Length - suffix.Length);
        }

        //Debug.Log("Lua chunkName: @" + luaPath);
        luaEnv.DoString(luaScript.bytes, "@" + luaPath, scriptEnv);
        #else
        luaEnv.DoString(luaScript.bytes, luaScript.name, scriptEnv);
        #endif


        Action luaAwake = scriptEnv.Get<Action>("Awake");
        scriptEnv.Get("Start", out luaStart);
        scriptEnv.Get("Update", out luaUpdate);
        scriptEnv.Get("OnDestroy", out luaOnDestroy);
        scriptEnv.Get("OnTriggerEnter2D", out luaOnTriggerEnter2D);

        if (luaAwake != null)
        {
            luaAwake();
        }
    }

    public LuaTable GetLuaObject(string name)
    {
    if (scriptEnv == null)
        return null;

    return scriptEnv.Get<LuaTable>(name);
    }

    public LuaTable GetScriptEnv()
    {
        return scriptEnv;
    }

    // Use this for initialization
    void Start()
    {
        if (luaStart != null)
        {
            luaStart();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (luaUpdate != null)
        {
            luaUpdate();
        }
        if (Time.time - LuaBehaviour.lastGCTime > GCInterval)
        {
            luaEnv.Tick();
            LuaBehaviour.lastGCTime = Time.time;
        }
    }

    void OnDestroy()
    {
        if (luaOnDestroy != null)
        {
            luaOnDestroy();
        }
        luaOnDestroy = null;
        luaUpdate = null;
        luaStart = null;
        scriptEnv.Dispose();
        injections = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (luaOnTriggerEnter2D != null)
        {
            luaOnTriggerEnter2D(other);
        }
    }
}
