using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("PhysicsProps/Breakable")]
[RequireComponent(typeof(Rigidbody))]
public class Breakable : PhysicsProp {
    public UnityEvent OnBreak;
    public float Health = 10f;
    public float VelocityThreshold = 20f;
    public float CollisionCoefficient = 1f;
    public bool broken = false;

    private void OnCollisionEnter(Collision other) {
        if (broken || !IsServer) return;

        Vector3 normalVel = Vector3.Project(other.relativeVelocity, other.contacts[0].normal);
        if (normalVel.magnitude > VelocityThreshold) {
            Health -= (normalVel.magnitude - VelocityThreshold) * CollisionCoefficient;
            if (Health <= 0) {
                OnBreak.Invoke();
                broken = true;
            }
        }
    }
}
