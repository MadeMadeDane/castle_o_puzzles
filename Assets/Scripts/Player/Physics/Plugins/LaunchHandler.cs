using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LaunchHandler : PhysicsPlugin {
    private Launcher CurrentLauncher;
    private string LAUNCH_TIMER = "LaunchTimer";
    private Vector3 InitialPlayerVelocity;
    public LaunchHandler(PhysicsPropHandler context) : base(context) { }

    public override void Awake() {
        base.Awake();
        utils.CreateTimer(LAUNCH_TIMER, 0.1f).setFinished();
    }

    public override void FixedUpdate() {
        if (!IsOwner) return;
        HandleLaunch();
    }

    private void HandleLaunch() {
        if (utils.CheckTimer(LAUNCH_TIMER)) CurrentLauncher = null;
        if (CurrentLauncher == null) return;

        player.ShortHopTempDisable = true;
        Vector3 force = CurrentLauncher.force;
        if (CurrentLauncher.isLocalForce) force = CurrentLauncher.transform.TransformVector(force);
        if (CurrentLauncher.isImpulse) {
            // Calculate the necessary force to accelerate the player to the desired velocity
            Vector3 relativeOffsetVelocity = Vector3.zero;
            // If we are already moving upward relative to the launcher, add the impulse onto that.
            // If we are moving downward, cancel all downward momentum and give the desired impulse.
            if (Vector3.Dot(InitialPlayerVelocity, force.normalized) > 0f) relativeOffsetVelocity = InitialPlayerVelocity;
            Vector3 velocityAlongLauncher = Vector3.Project(player.GetVelocity() - relativeOffsetVelocity, force.normalized);
            if (force.magnitude > velocityAlongLauncher.magnitude) force = (force - velocityAlongLauncher) / Time.fixedDeltaTime;
            else force = Vector3.zero;
        }
        player.Accelerate(force);
    }

    public override void OnTriggerStay(Collider other, PhysicsProp prop) {
        if (!IsOwner) return;
        CurrentLauncher = prop as Launcher;
        if (!CurrentLauncher.activated) return;
        utils.ResetTimer(LAUNCH_TIMER);
    }

    public override void OnTriggerEnter(Collider other, PhysicsProp prop) {
        if (!IsOwner) return;
        if (!(prop as Launcher).activated) return;
        if (!utils.CheckTimer(LAUNCH_TIMER)) return;
        InitialPlayerVelocity = player.GetVelocity();
    }
}
