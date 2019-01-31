using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IOAndGate")]
public class IOAndGate : IOEntity {
    public DigitalState output;

    public void input(DigitalState input) {
        output.state = ConnectedDigitalInputs.All((dState) => dState.state);
    }
}
