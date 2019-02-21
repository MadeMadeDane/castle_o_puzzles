using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingCollider : MovingCollider {
    public Vector3 AngularVelocity = Vector3.zero;

    protected override void Move() {
        base.Move();
        transform.Rotate(AngularVelocity * Time.deltaTime);
        //transform.RotateAround(transform.position, AngularVelocity.normalized, AngularVelocity.magnitude*Time.deltaTime);
    }

    protected override Vector3 CalculatePlayerVelocity() {
        MovingPlayer player = GetComponentInChildren<MovingPlayer>();
        Vector3 player_pos = Vector3.zero;
        if (player != null) {
            player_pos = player.transform.position - transform.position;
        }
        Vector3 rotating_velocity = Vector3.Cross(transform.TransformDirection(AngularVelocity), player_pos) * Mathf.Deg2Rad;
        return velocity + rotating_velocity;
    }
}
