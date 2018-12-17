using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI;

public class Pusher : PhysicsPlugin {
    private float pushtarget = 0f;
    private Vector3 lastpushsurface;
    private CameraController camera;
    private string PUSH_TIMER = "PushTimer";
    private string PUSH_START_TIMER = "PushStartTimer";

    public Pusher(PhysicsPropHandler context) : base(context) { }

    public override void Awake() {
        base.Awake();
        utils.CreateTimer(PUSH_TIMER, 0.1f).setFinished();
        utils.CreateTimer(PUSH_START_TIMER, 0.3f);
    }

    public override void Start() {
        utils.RunOnNextFrame(() => { camera = player.player_camera; });
    }

    public override void OnTriggerStay(Collider other, PhysicsProp prop) {
        Pushable pushable = prop as Pushable;
        Vector3 motion_vector = player.GetMoveVector();
        RaycastHit hit;
        if (Physics.Raycast(context.transform.position, motion_vector, out hit, player.cc.radius * 1.5f)) {
            // Reset the push start buildup if we are no longer pushing
            if (utils.CheckTimer(PUSH_TIMER)) {
                utils.ResetTimer(PUSH_START_TIMER);
            }
            if (utils.CheckTimer(PUSH_START_TIMER)) {
                pushtarget = 600f * ((player.cc.radius * 1.5f) - hit.distance) + 30f * Vector3.Dot(player.current_velocity - pushable.rigidbody.velocity, -hit.normal);
                if ((context as PhysicsPropHandler).IsServer()) {
                    //pushable.Push(pushtarget * Vector3.Project(motion_vector, hit.normal));
                    pushable.Push(Vector3.Project(player.current_velocity - pushable.rigidbody.velocity, -hit.normal), true);
                }
                else {
                    pushable.PushOnServer(-hit.normal * 12f);
                }
            }
            lastpushsurface = hit.normal;
            // We are not pushing if the analog is not being moved
            float move_mag = input_manager.GetMove().magnitude;
            if (move_mag == 0) {
                utils.SetTimerFinished(PUSH_TIMER);
            }
            else if (move_mag > 0.5f) {
                utils.ResetTimer(PUSH_TIMER);
            }
        }
    }

    public override void FixedUpdate() {
        if (!utils.CheckTimer(PUSH_TIMER)) {
            if (camera.GetViewMode() == ViewMode.Third_Person) {
                camera.RotateCameraToward(direction: -lastpushsurface,
                                          lerp_factor: 0.03f);
            }
            player.current_velocity = Vector3.Project(player.current_velocity, lastpushsurface) + Vector3.Project(player.current_velocity, Physics.gravity);
        }
    }
}