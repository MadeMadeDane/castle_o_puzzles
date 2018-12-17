using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grabber : PhysicsPlugin {
    private const string THROW_METER = "ThrowMeter";
    private const string THROW_PRESS = "ThrowPress";
    private Grabable grabbed;
    private bool will_grab = false;
    public float throw_base_strength = 5f;
    public float throw_added_strength = 5f;

    public Grabber(PhysicsPropHandler context) : base(context) { }

    public override void Awake() {
        base.Awake();
        utils.CreateTimer(THROW_PRESS, 0.1f);
        utils.CreateTimer(THROW_METER, 1f);
    }

    public override void OnUse(PhysicsProp prop) {
        Grabable grabable = prop as Grabable;
        if (grabbed == null && !will_grab) {
            Collider grabable_collider = grabable.GetComponent<Collider>();
            float grabable_height = grabable_collider.bounds.size.y;
            bool success = grabable.Pickup(
                grabber: context.gameObject,
                grab_offset: Vector3.up * (0.2f + (player.cc.height / 2f) + (grabable_height / 2f)));
            if (success) {
                will_grab = true;
                // Wait until the grab button is released to finish the pick up
                utils.WaitUntilCondition(
                    check: () => {
                        return !input_manager.GetPickUpHold();
                    },
                    action: () => {
                        grabbed = grabable;
                        will_grab = false;
                    });
            }
        }
    }

    public override void Update() {
        if (input_manager.GetPickUp() && grabbed) {
            utils.ResetTimer(THROW_METER);
        }
        if (input_manager.GetPickUpRelease() && grabbed) {
            utils.ResetTimer(THROW_PRESS);
        }
    }

    public override void FixedUpdate() {
        if (!utils.CheckTimer(THROW_PRESS) && grabbed) {
            Vector3 local_velocity = player.transform.InverseTransformVector(player.GetWorldVelocity());
            float throw_power = (throw_base_strength + throw_added_strength * utils.GetTimerPercent(THROW_METER));
            bool success = grabbed.Throw(
                velocity: local_velocity + throw_power * (Vector3.forward + Vector3.up));
            if (success) {
                grabbed = null;
            }
            utils.SetTimerFinished(THROW_PRESS);
            utils.SetTimerFinished(THROW_METER);
        }
    }
}