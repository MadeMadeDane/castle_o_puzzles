using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Components;
using MLAPI;

public class LoadArea : NetworkedBehaviour
{

    public string scene = "SampleScene";
    void OnTriggerEnter(Collider col)
    {
        Debug.Log("Collision happened");
        InvokeServerRpc(ChangeSceneRPC, scene);
    }
    [ServerRPC]
    private void ChangeSceneRPC(string sceneName)
    {
        NetworkSceneManager.SwitchScene(sceneName);
        Debug.Log("Running on the server");
    }
}
