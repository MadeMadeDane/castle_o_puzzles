using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class AnalogStateChange: UnityEvent<AnalogState> {};

[System.Serializable]
public class AnalogState
{
    public AnalogStateChange Change;

    public float previous_state {private set; get;}

    private float _state = 0f;
    public float state {
        set {
            // Set state before invoking listeners
            previous_state = _state;
            _state = value;

            // Invoke UnityEvent on state change
            if (_state != previous_state) {
                Change.Invoke(this);
            }
        }
        get {
            return _state;
        }
    }
}
