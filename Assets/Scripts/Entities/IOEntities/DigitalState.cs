using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DigitalStateChange: UnityEvent<DigitalState> {};

[System.Serializable]
public class DigitalState
{
    public UnityEvent Trigger;
    public DigitalStateChange Change;

    private bool _state = false;
    public bool state {
        set {
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

    public void initialize(bool initial_state) {
        _state = initial_state;
    }

    public void trigger() {
        IOManager.Instance.IOTick(() => Trigger.Invoke());
    }

    public void impulse(bool impulse_state=true) {
        _state = !impulse_state;
        state = impulse_state;
        state = !impulse_state;
    }
}