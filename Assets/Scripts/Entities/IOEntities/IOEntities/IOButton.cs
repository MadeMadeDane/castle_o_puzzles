using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("IOEntities/IOButton")]
public class IOButton : IOEntity, IUsable {
    public enum PressMode {
        HOLD,
        TOGGLE,
        PRESS
    }
    public PressMode mode = PressMode.PRESS;
    public DigitalState pressed;

    public void Use() {
        switch (mode) {
            case (PressMode.HOLD):
                if (!pressed.state) {
                    pressed.state = true;
                    Utilities.Instance.WaitUntilCondition(
                        check: () => { return !InputManager.Instance.GetPickUpHold(); },
                        action: () => { pressed.state = false; });
                }
                break;
            case (PressMode.PRESS):
                pressed.impulse();
                break;
            case (PressMode.TOGGLE):
                pressed.state = !pressed.state;
                break;
        }
    }
}
