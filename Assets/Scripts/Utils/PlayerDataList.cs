
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

// Currently only one player is supported per platform.
// This will need to be turned into a list if splitscreen is desired.
public class PlayerDataList : UnitySingleton<PlayerDataList> {
    public Dictionary<Type, Component> player_data;
    public GameObject currentPlayer = null;
    private void Awake() {
        Debug.Log("INIT PLAYERDATA");
        player_data = new Dictionary<Type, Component>();
    }
    public T get<T>() where T:Component {
        if (!player_data.ContainsKey(typeof(T))) {
            if (currentPlayer == null)
                currentPlayer = NetworkingManager.singleton.ConnectedClientsList.Where(x => x.ClientId == NetworkingManager.singleton.LocalClientId).FirstOrDefault().PlayerObject.gameObject;
            player_data[typeof(T)] = currentPlayer?.GetComponentInChildren(typeof(T));
        }
        return (T) player_data[typeof(T)];
    }
}