using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class SharedItem : Item {
    protected InputManager im;
    public override void Start() {
        base.Start();
        im = InputManager.Instance;  
    }
    public static bool isSharedItem(Item item) {
        return item.type == typeof(SharedItem);
    }
    public SharedItem(NetworkedBehaviour context = null) : base(context) {
        type = typeof(SharedItem);
    }
    protected bool SharedItemButtonPress() {
        bool ret = im.GetSharedItem();
        return ret;
    }
}
public class NetworkSharedItem {
    public string name;

    public NetworkSharedItem(string name) {
        this.name = name;
    }
}