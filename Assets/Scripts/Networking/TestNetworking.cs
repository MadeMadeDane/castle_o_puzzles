using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using MLAPI;

public class TestNetworking : NetworkedBehaviour {
    public bool makemehost;

    void Start() {
        if (makemehost) {
            NetworkingManager.singleton.StartHost();
            Destroy(transform.parent.gameObject, 1f);
            Destroy(gameObject);
        }
        var input = gameObject.GetComponent<InputField>();
        var se = new InputField.SubmitEvent();
        se.AddListener(SubmitName);
        input.onEndEdit = se;

        //or simply use the line below, 
        //input.onEndEdit.AddListener(SubmitName);  // This also works
    }

    private void SubmitName(string ip) {
        NetworkingManager.singleton.NetworkConfig.ConnectAddress = ip;
        NetworkingManager.singleton.StartClient();
        Destroy(transform.parent.gameObject, 1f);
        Destroy(gameObject);
    }
}
