using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tankard : SharedItem {
    public override string name() => "Tankard";
    // Use this for initialization

    public override void Start() {
        im = InputManager.Instance;
    }

    public override void Update() {
        if (SharedItemButtonPress()) {
            log();
        }
    }

    void log() {
        Debug.Log("Bottoms UP!!!");
    }
}
