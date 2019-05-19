using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI;

public class Pusher : PhysicsPlugin {
    private Vector3 lastpushsurface;
    private CameraController camera;
    private string PUSH_TIMER = "PushTimer";
    private string PUSH_START_TIMER = "PushStartTimer";

    public Pusher(PhysicsPropHandler context) : base(context) { }

    public override void NetworkStart() {
        base.NetworkStart();
        if (!IsOwner) return;
        utils.CreateTimer(PUSH_TIMER, 0.1f).setFinished();
        utils.CreateTimer(PUSH_START_TIMER, 0.3f);
        // disable this plugin until the camera is created
        enabled = false;
        utils.WaitUntilCondition(
            check: () => player.player_camera != null,
            action: () => {
                camera = player.player_camera;
                enabled = true;
            });
    }

    public override void OnTriggerStay(Collider other, PhysicsProp prop) {
        if (!IsOwner) return;
        Pushable pushable = prop as Pushable;
        Vector3 motion_vector = player.GetMoveVector();
        RaycastHit hit;
        if (Physics.Raycast(context.transform.position, motion_vector, out hit, player.cc.radius * 1.5f)) {
            // Avoid pushing if we are angled away from the surface
            if (Vector3.Dot(motion_vector, -hit.normal) < 0.8f) return;
            // Reset the push start buildup if we are no longer pushing
            if (utils.CheckTimer(PUSH_TIMER)) {
                utils.ResetTimer(PUSH_START_TIMER);
            }
            if (utils.CheckTimer(PUSH_START_TIMER)) {
                if (IsServer) {
                    pushable.Push(Vector3.Project(player.GetVelocity() - pushable.rigidbody.velocity, -hit.normal), true);
                }
                else {
                    pushable.PushOnServer(-hit.normal * 24f * player.cc.radius);
                }
            }
            lastpushsurface = hit.normal;
            // We are not pushing if the analog is not being moved
            float move_mag = input_manager.GetMove().magnitude;
            if (move_mag > 0.5f) {
                utils.ResetTimer(PUSH_TIMER);
            }
        }
    }

    public override void FixedUpdate() {
        if (!IsOwner) return;
        if (!utils.CheckTimer(PUSH_TIMER)) {
            if (camera.GetViewMode() == ViewMode.Third_Person) {
                camera.RotateCameraToward(direction: -lastpushsurface,
                                          lerp_factor: 0.03f);
            }
            player.SetVelocity(Vector3.Project(player.GetVelocity(), lastpushsurface) + Vector3.Project(player.GetVelocity(), Physics.gravity));
        }
    }
}