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

    protected override void Awake() {
        base.Awake();
        controlled_light = GetComponent<Light>();
        Switch(StartOn);
        if (UseColorToggles) controlled_light.color = InactiveColor;
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
    }

    public void SetIntensity(AnalogState input) {
        SetIntensity(input.state);
    }

    public void SetIntensity(float input) {
        controlled_light.intensity = input;
        Intensity.state = input;
    }
}