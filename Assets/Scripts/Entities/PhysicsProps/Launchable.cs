using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MLAPI;

[AddComponentMenu("PhysicsProps/Launchable")]
[RequireComponent(typeof(Rigidbody))]
public class Launchable : PhysicsProp {
    private Launcher CurrentLauncher;
    private string LAUNCH_TIMER;
    private Vector3 InitialVelocity;

    protected override void Awake() {
        base.Awake();
        LAUNCH_TIMER = $"LaunchTimer_{GetInstanceID()}";
        utils.CreateTimer(LAUNCH_TIMER, 0.1f).setFinished();
    }

    private void FixedUpdate() {
        if (!IsServer) return;
        HandleLaunch();
    }

    private void HandleLaunch() {
        if (utils.CheckTimer(LAUNCH_TIMER)) CurrentLauncher = null;
        if (CurrentLauncher == null) return;

        Vector3 force = CurrentLauncher.force;
        if (CurrentLauncher.isLocalForce) force = CurrentLauncher.transform.TransformVector(force);
        if (CurrentLauncher.isImpulse) {
            // Calculate the necessary force to accelerate the player to the desired velocity
            Vector3 relativeOffsetVelocity = Vector3.zero;
            // If we are already moving upward relative to the launcher, add the impulse onto that.
            // If we are moving downward, cancel all downward momentum and give the desired impulse.
            if (Vector3.Dot(InitialVelocity, force.normalized) > 0f) relativeOffsetVelocity = InitialVelocity;
            Vector3 velocityAlongLauncher = Vector3.Project(rigidbody.velocity - relativeOffsetVelocity, force.normalized);
            if (force.magnitude > velocityAlongLauncher.magnitude) force = (force - velocityAlongLauncher);
            else force = Vector3.zero;

            rigidbody.AddForce(force, ForceMode.VelocityChange);
        }
        else {
            rigidbody.AddForce(force);
        }
    }

    private void OnTriggerStay(Collider other) {
        if (!IsServer) return;
        Launcher newLauncher = other.GetComponent<Launcher>();
        if (!newLauncher || !newLauncher.activated) return;
        CurrentLauncher = newLauncher;
        utils.ResetTimer(LAUNCH_TIMER);
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer) return;
        if (!utils.CheckTimer(LAUNCH_TIMER)) return;
        Launcher newLauncher = other.GetComponent<Launcher>();
        if (!newLauncher || !newLauncher.activated) return;
        InitialVelocity = rigidbody.velocity;
    }
}