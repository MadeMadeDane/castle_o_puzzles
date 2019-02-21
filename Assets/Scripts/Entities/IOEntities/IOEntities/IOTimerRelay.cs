using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IOTimerRelay")]
public class IOTimerRelay : IOEntity {
    public DigitalState Output;
    public float delay;
    public enum RelayMode { CONTINUOUS, SQUARE_WAVE, SINGLE, SINGLE_ACTIVE_LOW }
    public RelayMode mode;
    private bool running = false;

    public void StartRelay() {
        Relay(true);
    }

    public void Input(DigitalState input) {
        Relay(input.state);
    }

    public void Relay(bool input) {
        switch (mode) {
            case RelayMode.SINGLE:
                DoSingle(input);
                break;
            case RelayMode.SINGLE_ACTIVE_LOW:
                DoSingle(input, active_state: false);
                break;
            case RelayMode.CONTINUOUS:
                DoContinuous(input);
                break;
            case RelayMode.SQUARE_WAVE:
                DoSquare(input);
                break;
        }
    }

    private void DoContinuous(bool input) {
        utils.WaitAndRun(delay, () => {
            Output.state = input;
        });
    }

    private void DoSquare(bool input, bool active_state = true) {
        if (running || input != active_state) return;

        running = true;
        Output.initialize(!input);
        Output.state = input;
        utils.WaitAndRun(delay, () => {
            running = false;
            Output.state = !input;
        });
    }

    private void DoSingle(bool input, bool active_state = true) {
        if (running || input != active_state) return;

        running = true;
        utils.WaitAndRun(delay, () => {
            running = false;
            Output.impulse(active_state);
        });
    }
}
