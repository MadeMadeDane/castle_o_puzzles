using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tankard : SharedItem {
    public override string name() => "Tankard";
    private InputManager im;
    // Use this for initialization

    public override void Start() {
        im = InputManager.Instance;
    }

    public override void Update() {
        if (saycheck()) {
            log();
        }
    }

    bool saycheck() {
        bool ret = im.GetSharedItem();
        return ret;
    }

    void log() {
        Debug.Log("Bottoms UP!!!");
    }
}
