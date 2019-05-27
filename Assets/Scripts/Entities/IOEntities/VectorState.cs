using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using MLAPI;
using MLAPI.NetworkedVar;

[System.Serializable]
public class VectorStateChange : UnityEvent<VectorState> { };

[System.Serializable]
public class VectorState : NetworkedVarVector3 {
    public VectorStateChange Change;

    public VectorState() : base() {
        OnValueChanged = ValueChanged;
        Settings.WritePermission = NetworkedVarPermission.Everyone;
        Settings.ReadPermission = NetworkedVarPermission.Everyone;
        Settings.SendTickrate = 0;
    }

    public Vector3 previous_state { private set; get; }
    private Vector3 _state = Vector3.zero;
    public Vector3 state {
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

    public Action<Vector3> OnReceiveNetworkValue = (Vector3 value) => { };
    public void ValueChanged(Vector3 previousValue, Vector3 newValue) {
        state = newValue;
        OnReceiveNetworkValue(newValue);
    }

    public void initialize(Vector3 initial_state) {
        Value = initial_state;
        _state = initial_state;
    }
}
