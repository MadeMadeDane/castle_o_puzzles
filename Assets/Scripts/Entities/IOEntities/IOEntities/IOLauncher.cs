using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MLAPI;

[AddComponentMenu("IOEntities/IOLauncher")]
[RequireComponent(typeof(Launcher))]
public class IOLauncher : IOEntity {
    [Header("Constant forces")]
    public VectorState force;
    public DigitalState activated;
    public Launcher launcher;

    protected override void Startup() {
        // Set up the initial output state on the server
        launcher = gameObject.GetComponent<Launcher>();
        // Set up output state callbacks for clients
        if (!IsServer) {
            activated.OnReceiveNetworkValue = Switch;
            force.OnReceiveNetworkValue = SetForce;
        }
        else {
            Switch(launcher.activated);
            SetForce(launcher.force);
        }
    }

    public void TurnOn() {
        Switch(true);
    }

    public void TurnOff() {
        Switch(false);
    }

    public void Switch(DigitalState input) {
        Switch(input.state);
    }

    public void Switch(bool state) {
        launcher.activated = state;
        activated.state = state;
    }

    public void SetForce(VectorState input) {
        SetForce(input.state);
    }

    public void SetForce(Vector3 state) {
        launcher.force = state;
        force.state = state;
    }
}