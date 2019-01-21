using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class SharedItem : Item {

    public static bool isSharedItem(Item item) {
        return item.type == typeof(SharedItem);
    }
    public SharedItem(NetworkedBehaviour context = null) : base(context) {
        type = typeof(SharedItem);
    }
}
public class NetworkSharedItem {
    public string name;

    public NetworkSharedItem(string name) {
        this.name = name;
    }
}