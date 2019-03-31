using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

[AddComponentMenu("PhysicsProps/Grabable")]
[RequireComponent(typeof(Rigidbody))]
public class Grabable : PhysicsProp, IUsable {
    private GameObject parent;
    private Transform previous_transform;
    private NetworkedObjectTransform previous_network_transform;
    private bool is_grabbed = false;
    private ulong grabberId;
    private MovingGeneric moving_object;
    private Collider rb_collider;
    private int IGNORE_RAYCAST_LAYER;
    private int DEFAULT_LAYER = 0;

    protected override void Awake() {
        base.Awake();
        IGNORE_RAYCAST_LAYER = LayerMask.NameToLayer("Ignore Raycast");
        moving_object = GetComponent<MovingGeneric>();
        rb_collider = GetComponent<Collider>();
        SetThrownState();
    }

    public bool Pickup(GameObject grabber, Vector3 grab_offset = default(Vector3), bool networked = false) {
        if (is_grabbed) {
            return false;
        }
        if (!networked) {
            previous_transform = transform.parent;
            if (grabber.transform.parent.parent == transform) {
                grabber.transform.parent.parent = transform.parent;
            }
            transform.parent = grabber.transform;
            transform.localPosition = grab_offset;
            transform.localRotation = Quaternion.identity;
        }
        else {
            NetworkedObjectTransform networkedTransform = GetComponent<NetworkedObjectTransform>();
            NetworkedObjectTransform grabberNetworkedTransform = grabber.GetComponent<NetworkedObjectTransform>();
            if (networkedTransform != null && grabberNetworkedTransform != null) {
                previous_network_transform = networkedTransform.networkParent;
                if (grabberNetworkedTransform.networkParent == networkedTransform) {
                    grabberNetworkedTransform.networkParent = networkedTransform.networkParent;
                }
                networkedTransform.networkParent = grabberNetworkedTransform;
                networkedTransform.networkParentLocalPosition = grab_offset;
                networkedTransform.networkParentLocalRotation = Quaternion.identity;
            }
        }
        SetPickupState(grabber);
        return true;
    }

    public static Action<bool> pickup_callback = (bool success) => { };

    [ClientRPC]
    private void rpc_PickupCallback(bool success) {
        pickup_callback(success);
    }

    [ServerRPC(RequireOwnership = false)]
    private void rpc_PickupOnServer(ulong grabber_netId, int grabber_moId, Vector3 grab_offset) {
        MovingGeneric target = MovingGeneric.GetMovingObjectAt(grabber_netId, grabber_moId);
        if (target == null) return;

        bool success = Pickup(target.gameObject, grab_offset, true);
        if (success) grabberId = ExecutingRpcSender;
        InvokeClientRpcOnClient(rpc_PickupCallback, ExecutingRpcSender, success);
    }

    public void PickupOnServer(ulong grabber_netId, int grabber_moId, Vector3 grab_offset = default(Vector3)) {
        InvokeServerRpc(rpc_PickupOnServer, grabber_netId, grabber_moId, grab_offset);
    }


    public bool Throw(Vector3 velocity, bool local = true, bool networked = false) {
        if (!is_grabbed) {
            return false;
        }
        if (local) {
            velocity = parent.transform.TransformDirection(velocity);
        }

        if (!networked) {
            transform.parent = previous_transform;
        }
        else {
            NetworkedObjectTransform networkedTransform = GetComponent<NetworkedObjectTransform>();
            if (networkedTransform != null) networkedTransform.networkParent = previous_network_transform;
        }

        SetThrownState();
        rigidbody.AddForce(velocity, ForceMode.VelocityChange);
        return true;
    }

    public static Action<bool> throw_callback = (bool success) => { };

    [ClientRPC]
    private void rpc_ThrowCallback(bool success) {
        throw_callback(success);
    }

    [ServerRPC(RequireOwnership = false)]
    private void rpc_ThrowOnServer(Vector3 velocity, bool local) {
        bool success = false;
        if (grabberId == ExecutingRpcSender) {
            success = Throw(velocity, local, true);
        }
        InvokeClientRpcOnClient(rpc_ThrowCallback, ExecutingRpcSender, success);
    }

    public void ThrowOnServer(Vector3 velocity, bool local = true) {
        InvokeServerRpc(rpc_ThrowOnServer, velocity, local);
    }

    public void Use() { }

    public void SetThrownState() {
        rigidbody.isKinematic = !IsServer;
        rigidbody.useGravity = true;
        is_grabbed = false;
        parent = null;
        grabberId = 0;
        rb_collider.isTrigger = false;
        gameObject.layer = DEFAULT_LAYER;
    }

    public void SetPickupState(GameObject target_parent, bool disable_collision = false) {
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        is_grabbed = true;
        parent = target_parent;
        if (disable_collision) {
            rb_collider.isTrigger = true;
            gameObject.layer = IGNORE_RAYCAST_LAYER;
        }
    }

    private void FixedUpdate() {
        // If the block loses it's parent, reset it
        if (parent == null) {
            if (is_grabbed) SetThrownState();
        }
        // Make sure carried blocks are always kinematic
        else {
            if (!is_grabbed) SetPickupState(parent);
        }
    }
}
