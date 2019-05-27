using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MLAPI;

[AddComponentMenu("PhysicsProps/Launcher")]
public class Launcher : PhysicsProp {
    [Header("Constant forces")]
    public Vector3 force;
    public bool isLocalForce;
    public bool isImpulse;
    public bool activated;
}