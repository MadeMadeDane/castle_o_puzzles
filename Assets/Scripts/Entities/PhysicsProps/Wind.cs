using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

[AddComponentMenu("PhysicsProps/Wind")]
public class Wind : PhysicsProp {
    [Header("Constant forces")]
    public bool isConstant;
    public bool isLocal;
    public Vector3 constantForceValue;
    public Vector3 constantVelocityValue;

    public Vector3 GetForceAtPosition(Vector3 position) {
        Vector3 calculatedForce = Vector3.zero;
        // Only constant wind velocity/force is supported for now
        if (isConstant) calculatedForce = constantForceValue;
        if (isLocal) calculatedForce = transform.TransformDirection(calculatedForce);
        return calculatedForce;
    }

    public Vector3 GetWindVelocityAtPosition(Vector3 position) {
        Vector3 calculatedVel = Vector3.zero;
        // Only constant wind velocity/force is supported for now
        if (isConstant) calculatedVel = constantVelocityValue;
        if (isLocal) calculatedVel = transform.TransformDirection(calculatedVel);
        return calculatedVel;
    }

    private void OnTriggerStay(Collider other) {
        FluidDynamic flobj = other.GetComponent<FluidDynamic>();
        if (flobj != null) {
            flobj.SetWind(this);
        }
    }
}