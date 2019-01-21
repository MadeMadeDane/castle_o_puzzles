using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;


public class Item : ComponentPlugin {
    public Item(NetworkedBehaviour context) : base(context) { }
    public virtual string name() => "Item";
    public Type type = typeof(Item);
    public GameObject physical_form;
    public Sprite menu_form;
    public GameObject physical_obj = null;
    public GameObject menu_obj = null;

    public bool enable_logs = false;

    protected void outputLogs(string msg) {
        if (enable_logs)
            Debug.Log(msg);
    }
}