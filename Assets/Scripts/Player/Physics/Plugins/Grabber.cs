using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grabber : PhysicsPlugin {
    private Coroutine grab_timeout_coroutine = null;
    private const string THROW_METER = "ThrowMeter";
    private const string THROW_PRESS = "ThrowPress";
    private Grabable grabbing = null;
    private Grabable grabbed = null;
    public float throw_base_strength_mult = 10f;
    public float throw_added_strength_mult = 10f;
    public float grab_timeout = 3f;

    public Grabber(PhysicsPropHandler context) : base(context) { }

    public override void NetworkStart() {
        base.NetworkStart();
        if (!IsOwner) return;
        // Register callbacks for networked functions
        Grabable.pickup_callback = PickupCallback;
        Grabable.throw_callback = ThrowCallback;
        utils.CreateTimer(THROW_PRESS, 0.1f);
        utils.CreateTimer(THROW_METER, 1f);
    }

    public override bool OnUse(PhysicsProp prop) {
        if (!IsOwner) return false;
        Grabable grabable = prop as Grabable;
        if (grabbed == null && grabbing == null) {
            Collider grabable_collider = grabable.GetComponent<Collider>();
            float grabable_height = grabable_collider.bounds.size.y;
            grabbing = grabable;
            if (IsServer) {
                bool success = grabable.Pickup(
                    grabber: context.gameObject,
                    grab_offset: Vector3.up * (0.2f + (player.cc.height / 2f) + (grabable_height / 2f)));
                PickupCallback(success);
            }
            else {
                grabable.PickupOnServer(
                    grabber_netId: networkId,
                    grabber_moId: moving_player.GetMovingObjectIndex(),
                    grab_offset: Vector3.up * (0.2f + (player.cc.height / 2f) + (grabable_height / 2f)));
                grab_timeout_coroutine = utils.WaitAndRun(
                    seconds: grab_timeout,
                    action: () => {
                        grabbing = null;
                    });
            }
        }
        return false;
    }

    private void PickupCallback(bool success) {
        if (success) {
            if (grabbing != null) {
                grabbing.SetPickupState(context.gameObject, disable_collision: !IsServer);
            }
            // Wait until the grab button is released to finish the pick up
            utils.WaitUntilCondition(
                check: () => {
                    return !input_manager.GetUseHold();
                },
                action: () => {
                    if (grabbed != null) return;
                    grabbed = grabbing;
                    grabbing = null;
                });
        }
        else {
            grabbing = null;
        }
        if (grab_timeout_coroutine != null) {
            utils.StopCoroutine(grab_timeout_coroutine);
            grab_timeout_coroutine = null;
        }
    }

    public override void Update() {
        if (!IsOwner) return;
        if (input_manager.GetUse() && grabbed) {
            utils.ResetTimer(THROW_METER);
        }
        if (input_manager.GetUseRelease() && grabbed) {
            utils.ResetTimer(THROW_PRESS);
        }
    }

    private void ThrowCallback(bool success) {
        if (grabbed != null) {
            grabbed.SetThrownState();
        }
        grabbed = null;
    }

    public override void FixedUpdate() {
        if (!IsOwner) return;
        if (!utils.CheckTimer(THROW_PRESS) && grabbed) {
            Vector3 local_velocity = player.transform.InverseTransformVector(player.GetWorldVelocity());
            float throw_power = ((throw_base_strength_mult + throw_added_strength_mult) * player.cc.radius * utils.GetTimerPercent(THROW_METER));
            if (IsServer) {
                bool success = grabbed.Throw(
                    velocity: local_velocity + throw_power * (Vector3.forward + Vector3.up));
                ThrowCallback(success);
            }
            else {
                grabbed.ThrowOnServer(
                    velocity: local_velocity + throw_power * (Vector3.forward + Vector3.up));
            }

            utils.SetTimerFinished(THROW_PRESS);
            utils.SetTimerFinished(THROW_METER);
        }
    }
}