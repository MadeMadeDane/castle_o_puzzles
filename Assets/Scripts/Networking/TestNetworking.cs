using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Components;
using MLAPI.Transports.UNET;
using UnityEngine.Networking;

// #if UNITY_WEBGL
// namespace MLAPI.Transports.UNET {
//     public class UnetTransportWebGlFix : UnetTransport, IUDPTransport {
//         public new void Connect(string address, int port, object settings, out byte error) {
//             Debug.Log("WE GOT HERE TOO!");
//             serverHostId = NetworkTransport.AddHost(new HostTopology((ConnectionConfig)settings, 1), 0);
//             serverConnectionId = NetworkTransport.Connect(serverHostId, address, port, 0, out error);
//         }
//     }
// }
// #endif

public class TestNetworking : NetworkedBehaviour {
    public GameObject MapPrefab;
    public List<GameObject> destroyList;

    void Start() {
        // Add WebGL for server
        // UnetTransportWebGlFix.ServerTransports[0].Port = 7778;
        // UnetTransportWebGlFix.ServerTransports[0].Websockets = true;
#if UNITY_WEBGL
        // Add WebGL for client
        // Debug.Log("WE GOT HERE!");
        //NetworkingManager.singleton.NetworkConfig.Transport = MLAPI.Transports.DefaultTransport.Custom;
        //NetworkingManager.singleton.NetworkConfig.NetworkTransport = new UnetTransportWebGlFix();
        //NetworkingManager.singleton.NetworkConfig.ConnectPort = 7778;
#endif
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
        Destroy(transform.parent.gameObject, 0.1f);
        Destroy(gameObject);
        foreach (GameObject obj in destroyList) Destroy(obj);

        NetworkingManager.Singleton.StartHost();
        Debug.Log("Starting map...");
        SceneSwitchProgress ssp = NetworkSceneManager.SwitchScene("Workspace");
        ssp.OnComplete += (bool err) => {
            if (err) {
                Debug.Log("We timed out :(");
                return;
            }
            // GameObject Map = Instantiate(MapPrefab);
            // Map.transform.position = new Vector3(30f, 0, 0);
            // Map.GetComponent<NetworkedObject>().Spawn();
        };
    }

    private void SubmitName(string ip) {
        NetworkingManager.Singleton.GetComponent<UnetTransport>().ConnectAddress = ip;
        NetworkingManager.Singleton.StartClient();
        Destroy(transform.parent.gameObject, 0.1f);
        Destroy(gameObject);
        foreach (GameObject obj in destroyList) Destroy(obj);
    }
}
