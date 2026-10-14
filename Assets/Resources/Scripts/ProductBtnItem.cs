using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mosframe;
using System;

public class ProductBtnItem : LuaBehaviour, IDynamicScrollViewItem
{
    private Action<int> luaOnUpdateItem;
    public int idx = 0;
    public Button button;

    protected override void Awake() {
        base.Awake();
        scriptEnv.Get("onUpdateItem", out luaOnUpdateItem);
    }

    public void onUpdateItem(int index)
    {
        luaOnUpdateItem(index);
    }
}
