using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

[AddComponentMenu("PhysicsProps/Pushable")]
[RequireComponent(typeof(Rigidbody))]
public class Pushable : PhysicsProp {
    (Vector3, bool)? lastPush = null;
    public void Push(Vector3 force, bool impulse = false) {
        lastPush = (force, impulse);
    }

    [ServerRPC(RequireOwnership = false)]
    private void rpc_PushOnServer(Vector3 push_intent) {
        //Debug.Log("Got push_intent: " + push_intent.ToString());
        //Debug.Log("Current rigidbody velocity: " + rigidbody.velocity.ToString());
        Push(push_intent - rigidbody.velocity, true);
    }

    public void PushOnServer(Vector3 push_intent) {
        InvokeServerRpc(rpc_PushOnServer, push_intent);
    }

    private void FixedUpdate() {
        if (!IsServer) return;

        if (lastPush != null) {
            (Vector3 force, bool impulse) = lastPush.Value;
            if (impulse) rigidbody.AddForce(force, ForceMode.VelocityChange);
            else rigidbody.AddForce(force);
            lastPush = null;
        }

    }
}
