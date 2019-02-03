using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IOAndGate")]
public class IOAndGate : IOEntity {
    public DigitalState Output;

    public void Input(DigitalState input) {
        Output.state = ConnectedDigitalInputs.All((dState) => dState.state);
    }
}
