using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class PhysicsProp : NetworkedBehaviour {
    new public Rigidbody rigidbody;

    private void Awake() {
        rigidbody = GetComponent<Rigidbody>();
    }
}
