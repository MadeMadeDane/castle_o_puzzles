using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsPlugin : ComponentPlugin
{
    protected PlayerController player;
    protected InputManager input_manager;

    public PhysicsPlugin(MonoBehaviour context) : base(context) {}

    public override void Awake() {
        player = context.GetComponent<PlayerController>();
        if (player == null) {
            throw new Exception("Could not find player controller");
        }
        input_manager = InputManager.Instance;
    }

    public virtual void OnTriggerEnter(Collider other, PhysicsProp prop) {}

    public virtual void OnTriggerStay(Collider other, PhysicsProp prop) {}

    public virtual void OnTriggerExit(Collider other, PhysicsProp prop) {}

    public virtual void OnCollisionEnter(Collision other, PhysicsProp prop) {}

    public virtual void OnCollisionStay(Collision other, PhysicsProp prop) {}

    public virtual void OnCollisionExit(Collision other, PhysicsProp prop) {}

    public virtual void OnControllerColliderHit(ControllerColliderHit hit, PhysicsProp prop) {}
}
