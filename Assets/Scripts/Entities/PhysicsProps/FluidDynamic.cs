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
    public Wind current_wind;
    private string WIND_TIMER;

    protected override void Awake() {
        base.Awake();
        WIND_TIMER = "FDWindTimer_" + gameObject.GetInstanceID().ToString();
        utils.CreateTimer(WIND_TIMER, 0.1f).setFinished();
    }

    public Vector3 CalculateAirFriction(Vector3 velocity) {
        Vector3 computedAirFriction = airFriction;
        Vector3 relativeVelocity = velocity - GetWindVelocity();
        if (fallingFrictionOnly && relativeVelocity.y > 0f) computedAirFriction.y = 0f;
        return -transform.TransformDirection(Vector3.Scale(transform.InverseTransformDirection(relativeVelocity), computedAirFriction));
    }

    public Vector3 CalculateBuoyantForce() {
        return -Physics.gravity * effectiveVolume * density_air;
    }

    public void SetWind(Wind wind) {
        current_wind = wind;
        utils.ResetTimer(WIND_TIMER);
    }

    private Vector3 GetWindVelocity() {
        if (current_wind != null) {
            return current_wind.GetWindVelocityAtPosition(transform.position);
        }
        return Vector3.zero;
    }

    private void HandleWind() {
        if (utils.CheckTimer(WIND_TIMER)) current_wind = null;
    }

    private void ApplyForces() {
        rigidbody.AddForce(CalculateAirFriction(rigidbody.velocity));
        rigidbody.AddForce(CalculateBuoyantForce());
    }

    private void FixedUpdate() {
        ApplyForces();
        HandleWind();
    }
}