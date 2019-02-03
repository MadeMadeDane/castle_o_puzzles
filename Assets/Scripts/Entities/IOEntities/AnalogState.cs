using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using MLAPI;
using MLAPI.NetworkedVar;

[System.Serializable]
public class AnalogStateChange : UnityEvent<AnalogState> { };

[System.Serializable]
public class AnalogState : NetworkedVarFloat {
    public AnalogStateChange Change;

    public AnalogState() : base() {
        OnValueChanged = ValueChanged;
        Settings.WritePermission = NetworkedVarPermission.Everyone;
        Settings.ReadPermission = NetworkedVarPermission.Everyone;
        Settings.SendTickrate = 0;
    }

    public float previous_state { private set; get; }
    private float _state = 0f;
    public float state {
        set {
            // Keep the networkvar Value in sync with our state
            Value = value;

            // Set state before invoking listeners
            previous_state = _state;
            _state = value;

            // Invoke UnityEvent on state change
            if (_state != previous_state) {
                IOManager.Instance.IOTick(() => Change.Invoke(this));
            }
        }
        get {
            return _state;
        }
    }

    public Action<float> OnReceiveNetworkValue = (float value) => { };
    public void ValueChanged(float previousValue, float newValue) {
        state = newValue;
        OnReceiveNetworkValue(newValue);
    }

    public void initialize(float initial_state) {
        Value = initial_state;
        _state = initial_state;
    }
}
