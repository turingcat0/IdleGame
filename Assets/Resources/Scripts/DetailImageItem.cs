using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mosframe;
using System;

public class DetailImageItem : LuaBehaviour, IDynamicScrollViewItem
{
    private Action<int> luaOnUpdateItem;
    public string productName = "";
    public Button button;
    public DynamicHScrollView scrollView;

    protected override void Awake() {
        base.Awake();
        scriptEnv.Get("onUpdateItem", out luaOnUpdateItem);
    }

    public void onUpdateItem(int index)
    {
        luaOnUpdateItem(index);
    }
}
