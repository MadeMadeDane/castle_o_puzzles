using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class MovingColliderTrigger : NetworkedBehaviour {
    public GameObject Target;
    public float WaitTime = 1f;
    public bool HoldOnStay = false;

    private bool triggering;
    private bool staying;
    private float stay_polling_time = 2f;

    public override void NetworkStart() {
        triggering = false;
        staying = false;
    }

    private IEnumerator TriggerMove() {
        MovingCollider moving_collider = Target.GetComponent<MovingCollider>();
        if (moving_collider != null) {
            yield return new WaitForSeconds(WaitTime);
            yield return moving_collider.TriggerAsync();
            triggering = false;
        }
    }

    private IEnumerator Stay() {
        MovingCollider moving_collider = Target.GetComponent<MovingCollider>();
        if (moving_collider != null) {
            moving_collider.Stay();
            yield return new WaitForSeconds(stay_polling_time);
            staying = false;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsOwner) return;
        if (!other.GetComponent<MovingPlayer>()) return;
        if (!triggering) {
            triggering = true;
            StartCoroutine(TriggerMove());
        }
    }

    private void OnTriggerStay(Collider other) {
        if (!IsOwner) return;
        if (!other.GetComponent<MovingPlayer>()) return;
        if (HoldOnStay && !staying) {
            staying = true;
            StartCoroutine(Stay());
        }
    }
}
