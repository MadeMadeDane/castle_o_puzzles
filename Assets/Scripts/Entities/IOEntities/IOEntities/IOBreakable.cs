using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("IOEntities/IOBreakable")]
[RequireComponent(typeof(Breakable))]
public class IOBreakable : IOEntity {
    public Breakable breakable;
    public DigitalState Broken;

    protected override void Startup() {
        breakable = GetComponent<Breakable>();
        Broken.initialize(false);
        if (IsServer) {
            breakable.OnBreak.AddListener(OnBreakCallback);
        }
        Broken.OnReceiveNetworkValue = BrokenCallback;
    }

    private void OnBreakCallback() {
        Broken.state = true;
    }

    private void BrokenCallback(bool broken) {
        if (broken) Debug.Log("It's borked");
    }
}
