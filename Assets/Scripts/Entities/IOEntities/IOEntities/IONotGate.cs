using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IONotGate")]
public class IONotGate : IOEntity {
    public DigitalState Output;

    protected override void Startup() {
        if (!IsServer) return;
        Output.state = !ConnectedDigitalInputs.Any((dState) => dState.state);
    }

    public void Input(DigitalState input) {
        Output.state = !input.state;
    }
}
