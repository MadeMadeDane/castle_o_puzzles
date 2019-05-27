using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IOProximityDetector")]
[RequireComponent(typeof(Collider))]
public class IOProximityDetector : IOEntity {
    public DigitalState Detected;
    public float range = 10f;
    private string DETECTION_TIMER;

    protected override void Startup() {
        DETECTION_TIMER = $"ProximityDetection_{GetInstanceID()}";
        utils.CreateTimer(DETECTION_TIMER, 0.1f).setFinished();
    }

    private void FixedUpdate() {
        if (!IsServer) return;
        Detected.state = !utils.CheckTimer(DETECTION_TIMER);
    }

    private void OnTriggerStay(Collider other) {
        if (!IsServer) return;
        if (!other.GetComponent<MovingPlayer>() && !other.GetComponent<Detectable>()) return;
        if ((other.transform.position - transform.position).magnitude < range) {
            utils.ResetTimer(DETECTION_TIMER);
        }
    }
}

public class Detectable : Component { }