using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using MLAPI;

public class TestNetworking : NetworkedBehaviour {
    public GameObject MapPrefab;

    void Start() {

        InputField input = GetComponent<InputField>();
        InputField.SubmitEvent se = new InputField.SubmitEvent();
        se.AddListener(SubmitName);
        input.onEndEdit = se;

        UnityEngine.UI.Button button = transform.parent.GetComponentInChildren<UnityEngine.UI.Button>();
        button.onClick.AddListener(StartHost);

        //or simply use the line below, 
        //input.onEndEdit.AddListener(SubmitName);  // This also works
    }

    private void StartHost() {
        NetworkingManager.singleton.StartHost();
        Debug.Log("Starting map...");
        GameObject Map = Instantiate(MapPrefab);
        Map.transform.position = new Vector3(30f, 0, 0);
        Map.GetComponent<NetworkedObject>().Spawn();

        Destroy(transform.parent.gameObject, 0.1f);
        Destroy(gameObject);
    }

    private void SubmitName(string ip) {
        NetworkingManager.singleton.NetworkConfig.ConnectAddress = ip;
        NetworkingManager.singleton.StartClient();
        Destroy(transform.parent.gameObject, 0.1f);
        Destroy(gameObject);
    }
}
