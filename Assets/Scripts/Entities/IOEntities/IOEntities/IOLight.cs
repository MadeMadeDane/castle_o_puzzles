using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IOLight")]
[RequireComponent(typeof(Light))]
public class IOLight : IOEntity {
    public DigitalState TurnedOn;
    public DigitalState ActiveColorOn;
    public AnalogState Intensity;
    public Light controlled_light;
    public bool StartOn;
    public bool UseColorToggles;
    public Color ActiveColor;
    public Color InactiveColor;

    protected override void Startup() {
        controlled_light = GetComponent<Light>();
        if (UseColorToggles) controlled_light.color = InactiveColor;

        // Set up output state callbacks for clients
        if (!IsServer) {
            TurnedOn.OnReceiveNetworkValue = Switch;
            ActiveColorOn.OnReceiveNetworkValue = SwitchToActiveColor;
            Intensity.OnReceiveNetworkValue = SetIntensity;
        }
        // Set up the initial output state on the server
        else {
            SetIntensity(1f);
            Switch(StartOn);
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
        controlled_light.enabled = state;
        TurnedOn.state = state;
    }

    public void SwitchToActiveColor(DigitalState input) {
        SwitchToActiveColor(input.state);
    }

    public void SwitchToActiveColor(bool active) {
        if (!UseColorToggles) return;
        controlled_light.color = active ? ActiveColor : InactiveColor;
        ActiveColorOn.state = active;
    }

    public void SetIntensity(AnalogState input) {
        SetIntensity(input.state);
    }

    public void SetIntensity(float input) {
        controlled_light.intensity = input;
        Intensity.state = input;
    }
}