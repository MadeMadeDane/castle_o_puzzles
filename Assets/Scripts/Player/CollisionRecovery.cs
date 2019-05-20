using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class CollisionRecovery : NetworkedBehaviour {
    public PlayerController player;

    // Use this for initialization
    void Start() {
        if (!IsOwner) return;
        Collider my_collider = GetComponent<Collider>();
        foreach (Collider col in GetComponentsInParent<Collider>()) {
            Physics.IgnoreCollision(my_collider, col);
        }
    }

    private void OnTriggerStay(Collider other) {
        if (!IsOwner) return;
        // Don't recover on collision with triggers because they won't constrain us
        if (other.isTrigger) return;
        if (player != null) {
            if (other.GetComponent<MovingGeneric>()) {
                Debug.Log("Safe recovering...");
                player.RecoverSafe(other);
            }
            else {
                Debug.Log("Recovering...");
                player.Recover(other);
            }
        }
    }
}
