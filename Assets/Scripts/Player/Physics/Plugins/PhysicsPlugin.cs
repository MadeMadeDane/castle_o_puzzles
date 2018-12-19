using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class PhysicsPlugin : ComponentPlugin {
    protected PlayerController player;
    protected MovingPlayer moving_player;
    protected InputManager input_manager;
    protected Utilities utils;
    protected uint networkId;
    protected bool isServer;
    protected bool isOwner;
    public bool enabled = false;

    public PhysicsPlugin(PhysicsPropHandler context) : base(context) { }

    public override void Awake() {
        input_manager = InputManager.Instance;
        utils = Utilities.Instance;
    }

    public virtual void NetworkStart() {
        player = context.GetComponent<PlayerController>();
        moving_player = context.GetComponent<MovingPlayer>();

        isOwner = (context as PhysicsPropHandler).isOwner;
        networkId = (context as PhysicsPropHandler).networkId;
        isServer = (context as PhysicsPropHandler).IsServer();
        enabled = true;
    }

    public virtual void OnTriggerEnter(Collider other, PhysicsProp prop) { }

    public virtual void OnTriggerStay(Collider other, PhysicsProp prop) { }

    public virtual void OnTriggerExit(Collider other, PhysicsProp prop) { }

    public virtual void OnCollisionEnter(Collision other, PhysicsProp prop) { }

    public virtual void OnCollisionStay(Collision other, PhysicsProp prop) { }

    public virtual void OnCollisionExit(Collision other, PhysicsProp prop) { }

    public virtual void OnControllerColliderHit(ControllerColliderHit hit, PhysicsProp prop) { }

    public virtual void OnUse(PhysicsProp prop) { }
}
