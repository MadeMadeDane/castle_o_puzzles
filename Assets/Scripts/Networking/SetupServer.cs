using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class SetupServer : NetworkedBehaviour {
    public GameObject platform_parent;
    public GameObject platform_prefab;
    public GameObject platform_home;

    public GameObject rot_parent;
    public GameObject rot_prefab;
    public GameObject rot_home;

    public override void NetworkStart() {
        if (isServer) {
            Setup();
        }
    }

    private void SubmitName(string ip) {
        NetworkingManager.singleton.NetworkConfig.ConnectAddress = ip;
        NetworkingManager.singleton.StartClient();
        Destroy(transform.parent.gameObject, 1f);
        Destroy(gameObject);
    }

    private void Setup() {
        Debug.Log("Setting up server");
        GameObject plat = Instantiate(platform_prefab);
        plat.transform.parent = platform_parent.transform;
        plat.transform.localPosition = new Vector3(0f, -11f, 0f);
        MovingCollider plat_col = plat.GetComponent<MovingCollider>();
        plat_col.nextTargetObject = platform_home;
        plat.GetComponent<NetworkedObject>().Spawn();

        GameObject rot = Instantiate(rot_prefab);
        rot.transform.parent = rot_parent.transform;
        rot.transform.localPosition = Vector3.zero;
        RotatingCollider rot_col = rot.GetComponent<RotatingCollider>();
        rot_col.nextTargetObject = rot_home;
        rot.GetComponent<NetworkedObject>().Spawn();
    }
}
