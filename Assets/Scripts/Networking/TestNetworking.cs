using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class TestNetworking : NetworkedBehaviour {
    public bool makemehost;

    private void Start() {
        if (makemehost) {
            NetworkingManager.singleton.StartHost();
        }
        else {
            NetworkingManager.singleton.StartClient();
        }
    }
}
