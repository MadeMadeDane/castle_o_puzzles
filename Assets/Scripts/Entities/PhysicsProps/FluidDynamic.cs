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
    public bool oneWayFriction = false;
    public Vector3 airFriction = new Vector3(0.1f, 1f, 0.1f);
    public Wind current_wind;
    private string WIND_TIMER;

    protected override void Awake() {
        base.Awake();
        WIND_TIMER = "FDWindTimer_" + gameObject.GetInstanceID().ToString();
        utils.CreateTimer(WIND_TIMER, 0.1f).setFinished();
    }

    public Vector3 CalculateAirFriction(Vector3 velocity, Vector3? desiredDirection = null) {
        if (desiredDirection == null) desiredDirection = Vector3.up;
        Vector3 computedAirFriction = airFriction;
        Vector3 windVelocity = GetWindVelocity();
        Vector3 relativeVelocity = velocity - windVelocity;
        Vector3 relativeLocalVelocity = transform.InverseTransformDirection(relativeVelocity);
        if (fallingFrictionOnly && relativeLocalVelocity.y > 0f) computedAirFriction.y = 0f;
        // If we have "one way friction" disable x/z friction if the following are true:
        //   * We are either moving faster than the wind, or are moving nearly perpendicular to it
        //   * We are attempting to move in the same direction as the wind / moving normally outside of wind
        if (oneWayFriction && (Vector3.Dot(relativeVelocity, windVelocity) >= 0 || Mathf.Abs(Vector3.Dot(desiredDirection.Value, windVelocity.normalized)) < 0.1f) && Vector3.Dot(relativeVelocity, desiredDirection.Value) >= 0f) {
            computedAirFriction.x = 0f;
            computedAirFriction.z = 0f;
        }
        return -transform.TransformDirection(Vector3.Scale(relativeLocalVelocity, computedAirFriction));
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