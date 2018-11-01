using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grabber : PhysicsPlugin {
    private const string GRAB_PRESS_TIMER = "GrabPressTimer";
    private Grabable grabbed;

    public Grabber(MonoBehaviour context) : base(context) { }

    public override void Awake() {
        base.Awake();
        utils.CreateTimer(GRAB_PRESS_TIMER, 0.1f);
    }

    public override void OnUse(PhysicsProp prop) {
        Grabable grabable = prop as Grabable;
        if (grabbed == null) {
            Collider grabable_collider = grabable.GetComponent<Collider>();
            float grabable_height = grabable_collider.bounds.size.y;
            bool success = grabable.Pickup(
                grabber: context.gameObject,
                grab_offset: Vector3.up * (0.2f + (player.cc.height / 2f) + (grabable_height / 2f)));
            if (success) {
                utils.RunOnNextFrame(() => grabbed = grabable);
            }
        }
    }

    public override void Update() {
        if (input_manager.GetPickUp() && grabbed) {
            utils.ResetTimer(GRAB_PRESS_TIMER);
        }
    }

    public override void FixedUpdate() {
        if (!utils.CheckTimer(GRAB_PRESS_TIMER) && grabbed) {
            Vector3 local_velocity = player.transform.InverseTransformVector(player.cc.velocity);
            bool success = grabbed.Throw(velocity: local_velocity + 5f * (Vector3.forward + Vector3.up));
            if (success) {
                grabbed = null;
            }
            utils.SetTimerFinished(GRAB_PRESS_TIMER);
        }
    }
}