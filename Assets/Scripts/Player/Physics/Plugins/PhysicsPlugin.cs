using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI;

public class PhysicsPlugin : ComponentPlugin {
    protected PlayerController player;
    protected MovingPlayer moving_player;
    protected InputManager input_manager;
    protected Utilities utils;
    protected NetworkedObject networkedObject;
    protected ulong networkId;
    protected bool IsServer;
    protected bool IsOwner;
    public bool enabled = false;

    public PhysicsPlugin(PhysicsPropHandler context) : base(context) { }

    public override void Awake() {
        input_manager = InputManager.Instance;
        utils = Utilities.Instance;
    }

    public virtual void NetworkStart() {
        player = context.GetComponent<PlayerController>();
        moving_player = context.GetComponent<MovingPlayer>();

        IsOwner = (context as PhysicsPropHandler).IsOwner;
        networkId = (context as PhysicsPropHandler).NetworkId;
        IsServer = (context as PhysicsPropHandler).GetIsServer();
        networkedObject = (context as PhysicsPropHandler).NetworkedObject;
        enabled = true;
    }

    public virtual void OnTriggerEnter(Collider other, PhysicsProp prop) { }

    public virtual void OnTriggerStay(Collider other, PhysicsProp prop) { }

    public virtual void OnTriggerExit(Collider other, PhysicsProp prop) { }

    public virtual void OnCollisionEnter(Collision other, PhysicsProp prop) { }

    public virtual void OnCollisionStay(Collision other, PhysicsProp prop) { }

    public virtual void OnCollisionExit(Collision other, PhysicsProp prop) { }

    public virtual void OnControllerColliderHit(ControllerColliderHit hit, PhysicsProp prop) { }

    // A return value of true "hides" the regular use behavior of the player controller
    public virtual bool OnUse(PhysicsProp prop) { return false; }

    public T GetComponentInNetworkedChildren<T>() {
        T childcomp = context.GetComponentInChildren<T>();
        if (childcomp != null) return childcomp;
        return moving_player.networkChildren.Select((child) => child.GetComponentInChildren<T>()).FirstOrDefault();
    }
}
