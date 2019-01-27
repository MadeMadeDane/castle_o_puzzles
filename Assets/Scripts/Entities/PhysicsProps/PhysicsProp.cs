using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class PhysicsProp : NetworkedBehaviour {
    new public Rigidbody rigidbody;
    protected Utilities utils;

    protected virtual void Awake() {
        utils = Utilities.Instance;
        rigidbody = GetComponent<Rigidbody>();
    }
}
