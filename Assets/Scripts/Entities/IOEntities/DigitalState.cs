using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using MLAPI;
using MLAPI.NetworkedVar;

[System.Serializable]
public class DigitalStateChange : UnityEvent<DigitalState> { };

[System.Serializable]
public class DigitalState : NetworkedVarBool {
    public UnityEvent Trigger;
    public DigitalStateChange Change;

    public DigitalState() : base() {
        OnValueChanged = ValueChanged;
        Settings.WritePermission = NetworkedVarPermission.Everyone;
        Settings.ReadPermission = NetworkedVarPermission.Everyone;
        Settings.SendTickrate = 0;
    }

    private bool _state = false;
    public bool state {
        set {
            // Keep the networkvar Value in sync with our state
            Value = value;

            // Set state before invoking listeners
            bool prev_state = _state;
            _state = value;

            // Invoke UnityEvent on state change
            if (_state != prev_state) {
                if (_state == true) {
                    IOManager.Instance.IOTick(() => Trigger.Invoke());
                }
                IOManager.Instance.IOTick(() => Change.Invoke(this));
            }
        }
        get {
            return _state;
        }
    }

    public Action<bool> OnReceiveNetworkValue = (bool value) => { };
    public void ValueChanged(bool previousValue, bool newValue) {
        state = newValue;
        OnReceiveNetworkValue(newValue);
    }

    public void initialize(bool initial_state) {
        Value = initial_state;
        _state = initial_state;
    }

    public void trigger() {
        IOManager.Instance.IOTick(() => Trigger.Invoke());
    }

    public void impulse(bool impulse_state = true) {
        Value = !impulse_state;
        _state = !impulse_state;
        state = impulse_state;
        state = !impulse_state;
    }
}