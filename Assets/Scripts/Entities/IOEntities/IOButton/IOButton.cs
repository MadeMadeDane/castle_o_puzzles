using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("IOEntities/IOButton")]
public class IOButton : IOEntity, IUsable {
    public bool can_be_held = false;
    public DigitalState pressed;

    public void Use() {
        // Allow the button to be held until the player lets go of use
        if (can_be_held) {
            if (!pressed.state) {
                pressed.state = true;
                Utilities.Instance.WaitUntilCondition(
                    check: () => { return !InputManager.Instance.GetPickUpHold(); },
                    action: () => { pressed.state = false; });
            }
        }
        // Trigger an immediate impulse on use
        else {
            pressed.impulse();
        }
    }
}
