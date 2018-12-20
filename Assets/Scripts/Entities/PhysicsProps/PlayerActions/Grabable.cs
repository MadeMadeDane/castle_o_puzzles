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
    private uint grabberId;
    private MovingGeneric moving_object;

    protected override void Awake() {
        base.Awake();
        moving_object = GetComponent<MovingGeneric>();
    }

    public bool Pickup(GameObject grabber, Vector3 grab_offset = default(Vector3), bool networked = false) {
        if (is_grabbed) {
            return false;
        }
        parent = grabber;
        if (!networked) {
            previous_transform = transform.parent;
            // Not a very good Back to the Future plot...
            if (parent.transform.parent.parent == transform) {
                parent.transform.parent.parent = transform.parent;
            }
            transform.parent = parent.transform;
            transform.localPosition = grab_offset;
            transform.localRotation = Quaternion.identity;
        }
        else {
            NetworkedObjectTransform networkedTransform = GetComponent<NetworkedObjectTransform>();
            NetworkedObjectTransform parentNetworkedTransform = parent.GetComponent<NetworkedObjectTransform>();
            if (networkedTransform != null && parentNetworkedTransform != null) {
                previous_network_transform = networkedTransform.networkParent;
                if (parentNetworkedTransform.networkParent == networkedTransform) {
                    parentNetworkedTransform.networkParent = networkedTransform.networkParent;
                }
                networkedTransform.networkParent = parentNetworkedTransform;
                networkedTransform.networkParentLocalPosition = grab_offset;
                networkedTransform.networkParentLocalRotation = Quaternion.identity;
            }
        }
        rigidbody.isKinematic = true;
        is_grabbed = true;
        return true;
    }

    public static Action<bool> pickup_callback = (bool success) => { };

    [ClientRPC]
    private void rpc_PickupCallback(bool success) {
        pickup_callback(success);
    }

    [ServerRPC(RequireOwnership = false)]
    private void rpc_PickupOnServer(uint grabber_netId, int grabber_moId, Vector3 grab_offset) {
        MovingGeneric target = MovingGeneric.GetMovingObjectAt(grabber_netId, grabber_moId);
        if (target == null) return;

        bool success = Pickup(target.gameObject, grab_offset, true);
        if (success) grabberId = ExecutingRpcSender;
        InvokeClientRpcOnClient(rpc_PickupCallback, ExecutingRpcSender, success);
    }

    public void PickupOnServer(uint grabber_netId, int grabber_moId, Vector3 grab_offset = default(Vector3)) {
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

        rigidbody.isKinematic = false;
        rigidbody.AddForce(velocity, ForceMode.VelocityChange);
        is_grabbed = false;
        parent = null;
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
        if (success) grabberId = 0;

        InvokeClientRpcOnClient(rpc_ThrowCallback, ExecutingRpcSender, success);
    }

    public void ThrowOnServer(Vector3 velocity, bool local = true) {
        InvokeServerRpc(rpc_ThrowOnServer, velocity, local);
    }

    public void Use() {
        return;
    }

    private void FixedUpdate() {
        // If the block loses it's parent, reset it
        if (parent == null) {
            rigidbody.isKinematic = false;
            is_grabbed = false;
            grabberId = 0;
        }
    }
}
