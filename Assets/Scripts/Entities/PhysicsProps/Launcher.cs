using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MLAPI;

[AddComponentMenu("PhysicsProps/Launcher")]
public class Launcher : PhysicsProp {
    [Header("Constant forces")]
    public Vector3 Force;
    public bool isLocalForce;
    public bool isImpulse;
}