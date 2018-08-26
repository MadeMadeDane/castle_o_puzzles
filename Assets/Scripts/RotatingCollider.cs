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

    protected override Vector3 CalculatePlayerVelocity()
    {
        PlayerController player = GetComponentInChildren<PlayerController>();
        Vector3 player_pos = Vector3.zero;
        if (player != null) {
            player_pos = player.transform.position - transform.position;
        }
        Debug.DrawRay(transform.position, player_pos, Color.green);
        Vector3 rotating_velocity = Vector3.Cross(transform.TransformDirection(AngularVelocity), player_pos)*Mathf.Deg2Rad;
        Debug.DrawRay(transform.position, rotating_velocity, Color.blue);
        return velocity + rotating_velocity;
    }
}
