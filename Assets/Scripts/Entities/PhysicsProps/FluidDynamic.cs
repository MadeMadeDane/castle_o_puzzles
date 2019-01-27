using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

[AddComponentMenu("PhysicsProps/FluidDynamic")]
[RequireComponent(typeof(Rigidbody))]
public class FluidDynamic : PhysicsProp {
    public const float density_air = 1f;
    public const float density_water = 800f;
    public float effectiveVolume = 0f;
    public bool fallingFrictionOnly = false;
    public Vector3 airFriction = new Vector3(0.1f, 1f, 0.1f);

    private void ApplyAirFriction() {
        Vector3 computedAirFriction = airFriction;
        if (fallingFrictionOnly && rigidbody.velocity.y > 0f) computedAirFriction.y = 0f;
        rigidbody.AddForce(-transform.TransformDirection(Vector3.Scale(transform.InverseTransformDirection(rigidbody.velocity), computedAirFriction)));
    }

    private void ApplyBuoyantForce() {
        rigidbody.AddForce(-Physics.gravity * effectiveVolume * density_air);
    }

    private void FixedUpdate() {
        ApplyAirFriction();
        ApplyBuoyantForce();
    }
}