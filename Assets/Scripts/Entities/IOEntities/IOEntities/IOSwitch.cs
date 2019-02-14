using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("IOEntities/IOSwitch")]
public class IOSwitch : IOEntity, IUsable {
    public DigitalState Output;
    public enum SwitchMode { TOGGLE, HOLD, PRESS }
    public SwitchMode mode = SwitchMode.PRESS;

    public void Input(DigitalState input) {
        if (input.state) Use();
    }

    public void Use() {
        switch (mode) {
            case (SwitchMode.HOLD):
                if (!Output.state) {
                    Output.state = true;
                    Utilities.Instance.WaitUntilCondition(
                        check: () => { return !InputManager.Instance.GetUseHold(); },
                        action: () => { Output.state = false; });
                }
                break;
            case (SwitchMode.PRESS):
                Output.impulse();
                break;
            case (SwitchMode.TOGGLE):
                Output.state = !Output.state;
                break;
        }
    }
}
