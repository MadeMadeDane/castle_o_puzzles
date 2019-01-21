using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoltCutters : UseItem {
    public override string name() => "BoltCutters";
    private InputManager im;
    // Use this for initialization

    public override void Start() {
        im = InputManager.Instance;
        outputLogs("Added Actions to Magnet Boots");
    }

    public override void Update() {
        if (saycheck()) {
            log();
        }
    }

    bool saycheck() {
        bool ret = im.GetUseItem();
        return ret;
    }

    void log() {
        Debug.Log("Snip... Clip...");
    }
}
