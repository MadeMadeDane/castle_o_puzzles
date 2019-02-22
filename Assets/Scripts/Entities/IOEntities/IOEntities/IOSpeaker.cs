using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IOSpeaker")]
[RequireComponent(typeof(AudioSource))]
public class IOSpeaker : IOEntity {
    public bool StartOn;
    public DigitalState TurnedOn;
    public AnalogState Volume;
    public AudioSource source;

    protected override void Awake() {
        base.Awake();
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.Play();
        source.enabled = false;

        // Set up output state callbacks for clients
        if (!isServer) {
            TurnedOn.OnReceiveNetworkValue = Switch;
        }
        // Set up the initial output state on the server
        Switch(StartOn);
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
        source.enabled = state;
        TurnedOn.state = state;
    }
    public void SetVolume(AnalogState input) {
        SetVolume(input.state);
    }

    public void SetVolume(float input) {
        source.volume = input;
        Volume.state = input;
    }
}