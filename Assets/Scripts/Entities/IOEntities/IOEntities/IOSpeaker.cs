using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IOSpeaker")]
[RequireComponent(typeof(AudioSource))]
public class IOSpeaker : IOEntity {
    public bool StartOn = true;
    public float StartingVolume = 1f;
    public DigitalState TurnedOn;
    public AnalogState Volume;
    public AudioSource source;

    override protected void Startup() {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.Play();
        source.enabled = false;
        if (!IsServer) {
            // Set up output state callbacks for clients
            TurnedOn.OnReceiveNetworkValue = Switch;
            Volume.OnReceiveNetworkValue = SetVolume;
        }
        // Set up the initial output state on the server
        else {
            Switch(StartOn);
            SetVolume(StartingVolume);
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