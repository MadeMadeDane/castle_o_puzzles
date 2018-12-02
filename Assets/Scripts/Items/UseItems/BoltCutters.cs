using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoltCutters : UseItem {

    private InputManager im;
    public static string item_name = "BoltCutters";
    // Use this for initialization

    public override void Start()
    {
        im = InputManager.Instance;
        outputLogs("Added Actions to Magnet Boots");
    }

    public override void Update()
    {
        if (saycheck()) {
            log();
        }
    }

    bool saycheck()
    {
        bool ret = im.GetUseItem();
        return ret;
    }

    void log()
    {
        Debug.Log("Snip... Clip...");
    }

    public override string GetName()
    {
        return BoltCutters.item_name;
    }
}
