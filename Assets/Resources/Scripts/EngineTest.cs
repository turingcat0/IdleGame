using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[XLua.LuaCallCSharp]
public class EngineTest : MonoBehaviour
{
    // Start is called before the first frame update
    public string msg = "ver1";
    void Start()
    {
        // UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/MainInterface.prefab", typeof(GameObject));
        // Instantiate(obj);
        // UnityEngine.Object[] imgs = AssetDatabase.LoadAllAssetsAtPath("Assets/Products/jstar");
        // Debug.Log(imgs[0]);
        var c = this.gameObject.GetComponent<LuaBehaviour>();
        Debug.Log(c.GetType());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void info() {
        Debug.Log("engine test:" + RuntimePlatform.Android);
    }
}
