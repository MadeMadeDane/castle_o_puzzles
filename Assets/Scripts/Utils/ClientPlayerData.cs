
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

// Currently only one player is supported per platform.
// This will need to be turned into a list if splitscreen is desired.
public class PlayerDataList : UnitySingleton<PlayerDataList> {
    public Dictionary<string, Component> player_data;
    public GameObject currentPlayer = null; 
    private Dictionary<string, Type> component_types = new Dictionary<string, Type>() {
        {"InventoryManager", typeof(InventoryManager)},
        {"CameraController", typeof(CameraController)},
        {"InventoryManager", typeof(InventoryManager)}
        };
    private void Awake() {
        Debug.Log("INIT PLAYERDATA");
        player_data = new Dictionary<string, Component>();
    }
    public Component this[string component_name] {
        get {
            if (!player_data.ContainsKey(component_name)) {
                GameObject.FindWithTag("Player");
                if (currentPlayer == null)
                    currentPlayer = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.CompareTag("Player") && (x.GetComponent<NetworkedObject>().OwnerClientId == NetworkingManager.singleton.LocalClientId)).FirstOrDefault();
                player_data[component_name] = currentPlayer?.GetComponentInChildren(component_types[component_name]);
            }
            return player_data[component_name];
        }
    }
}