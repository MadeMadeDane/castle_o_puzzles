using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MLAPI;

[AddComponentMenu("PhysicsProps/Wind")]
public class Wind : PhysicsProp {
    [Header("Constant forces")]
    public bool isConstant = true;
    public bool isLocal = true;
    public bool keepPlayerInside = false;
    public Vector3 constantForceValue;
    public Vector3 constantVelocityValue;
    public AnimationCurve windCurve;
    private Collider windBox;
    private (float, float) curveRange;

    protected override void Awake() {
        base.Awake();
        windBox = GetComponent<Collider>();
        curveRange = (windCurve.keys.First().time, windCurve.keys.Last().time);
    }

    public Vector3 GetForceAtPosition(Vector3 position) {
        Vector3 calculatedForce = Vector3.zero;
        // Only constant wind velocity/force is supported for now
        if (isConstant) calculatedForce = constantForceValue;
        if (isLocal) calculatedForce = transform.TransformDirection(calculatedForce);
        return calculatedForce;
    }

    private Vector3 CalculateVelocityWithCurve(Vector3 base_velocity, Vector3 position) {
        float vertical_extent;
        float vertical_pos;
        Vector3 base_vel_dir = base_velocity.normalized;
        switch (windBox) {
            case BoxCollider bc:
                vertical_extent = Vector3.Dot(transform.up * bc.size.y, base_vel_dir);
                vertical_pos = Mathf.Clamp(Vector3.Dot(position - (bc.center - (transform.up * bc.size.y / 2f)), base_vel_dir), 0f, vertical_extent);
                break;
            case CapsuleCollider cc:
                vertical_extent = Vector3.Dot(transform.up * cc.height, base_vel_dir);
                vertical_pos = Mathf.Clamp(Vector3.Dot(position - (cc.center - (transform.up * cc.height / 2f)), base_vel_dir), 0f, vertical_extent);
                break;
            default:
                vertical_extent = Vector3.Dot(windBox.bounds.extents, base_vel_dir) * 2f;
                vertical_pos = Mathf.Clamp(Vector3.Dot(position - windBox.bounds.min, base_vel_dir), 0f, vertical_extent);
                break;
        }
        float curveVal = curveRange.Item1 + (curveRange.Item2 - curveRange.Item1) * (vertical_pos / vertical_extent);
        return base_velocity * windCurve.Evaluate(curveVal);
    }

    public Vector3 GetWindVelocityAtPosition(Vector3 position) {
        Vector3 calculatedVel = Vector3.zero;
        if (isConstant) calculatedVel = constantVelocityValue;
        if (isLocal) calculatedVel = transform.TransformDirection(calculatedVel);
        calculatedVel = CalculateVelocityWithCurve(base_velocity: calculatedVel, position: position);
        return calculatedVel;
    }

    private void OnTriggerStay(Collider other) {
        FluidDynamic flobj = other.GetComponent<FluidDynamic>();
        if (flobj != null) {
            flobj.SetWind(this);
        }
    }
}