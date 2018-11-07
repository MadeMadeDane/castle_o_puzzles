using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NullPlugin : PhysicsPlugin {
    public NullPlugin(MonoBehaviour context) : base(context) { }

    public override void Awake() { }
}