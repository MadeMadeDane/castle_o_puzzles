using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoltCutters : SharedItem {
    public override string name() => "BoltCutters";
    // Use this for initialization

    public override void Start() {
        base.Start();
        outputLogs("Added Actions to Magnet Boots");
    }

    public override void Update() {
        if (SharedItemButtonPress()) {
            log();
        }
    }

    void log() {
        Debug.Log("Snip... Clip...");
    }
}
