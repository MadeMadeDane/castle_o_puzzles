using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class CollisionRecovery : NetworkedBehaviour {
    public PlayerController player;

    // Use this for initialization
    void Start() {
        if (!isOwner) return;
        Collider my_collider = GetComponent<Collider>();
        foreach (Collider col in GetComponentsInParent<Collider>()) {
            Physics.IgnoreCollision(my_collider, col);
        }
        player = GetComponentInParent<PlayerController>();
    }

    private void OnTriggerStay(Collider other) {
        if (!isOwner) return;
        // Don't recover on collision with triggers because they won't constrain us
        if (other.isTrigger) return;
        // Don't recover on collision with networked players
        if (other.GetComponent<NetworkedPlayerTransform>()) return;

        player.Recover(other);
    }
}
