using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeySharedItem : SharedItem {
    private Utilities utils;

    public float reach_distance = 5f; // TODO: Use camera as interface to scan
    public float explosion_radius = 1f; // TODO: Use camera as interface to scan
    // Use this for initialization

    public override void Start() {
        base.Start();
        utils = Utilities.Instance;
    }

    public override void Update() {
        if (SharedItemButtonPress()) {
            TryLockToggle();
        }
    }

    protected void TryLockToggle() {
        CameraController cam = utils.get<CameraController>();
        IOLock iolock = utils.RayCastExplosiveSelect<IOLock>(cam.transform.position, cam.transform.forward.normalized * reach_distance, explosion_radius);
        if (iolock != null) {
            iolock.RequestLockStateChange(!iolock.Locked.state);
        }
    }
}
