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
        if (!other.isTrigger) {
            player.Recover(other);
        }
    }
}
