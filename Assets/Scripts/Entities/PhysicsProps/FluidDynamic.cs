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
    // Enable the two bools below for good player movement with a fluid dynamic object
    public bool fallingFrictionOnly = false;
    public Vector3 airFriction = new Vector3(0f, 4f, 0f);
    public Vector3 windFriction = new Vector3(4f, 4f, 4f);
    public bool noPlayerHorizontalFriction = true;
    public Wind current_wind;
    private string WIND_TIMER;

    protected override void Awake() {
        base.Awake();
        WIND_TIMER = "FDWindTimer_" + gameObject.GetInstanceID().ToString();
        utils.CreateTimer(WIND_TIMER, 0.1f).setFinished();
    }

    public Vector3 CalculateAirFriction(Vector3 velocity, bool held_by_player = false) {
        Vector3 computedWindFriction = windFriction;
        Vector3 computedAirFriction = airFriction;
        Vector3 windVelocity = GetWindVelocity();
        Vector3 relativeVelocity = velocity - windVelocity;
        Vector3 relativeVelocityWindComponent = Vector3.Project(relativeVelocity, windVelocity.normalized);
        Vector3 relativeVelocityAirComponent = relativeVelocity - relativeVelocityWindComponent;
        Vector3 relativeLocalVelocityWindComponent = transform.InverseTransformDirection(relativeVelocityWindComponent);
        Vector3 relativeLocalVelocityAirComponent = transform.InverseTransformDirection(relativeVelocityAirComponent);

        if (fallingFrictionOnly && relativeLocalVelocityWindComponent.y > 0f) computedWindFriction.y = 0f;
        if (fallingFrictionOnly && relativeLocalVelocityAirComponent.y > 0f) computedAirFriction.y = 0f;
        if (noPlayerHorizontalFriction && held_by_player && !(current_wind != null && current_wind.keepPlayerInside)) {
            computedAirFriction.x = 0f;
            computedAirFriction.z = 0f;
        }

        Vector3 computedLocalFriction = Vector3.Scale(relativeLocalVelocityWindComponent, computedWindFriction) +
                                        Vector3.Scale(relativeLocalVelocityAirComponent, computedAirFriction);
        return -transform.TransformDirection(computedLocalFriction);
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
        if (IsServer) ApplyForces();
        HandleWind();
    }
}