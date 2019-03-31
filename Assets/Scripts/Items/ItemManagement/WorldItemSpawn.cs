using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class WorldItemSpawn : NetworkedBehaviour {
    public string item_name = "";

    // Use this for initialization
    public override void NetworkStart() {
        if (!IsServer) return;
        Item item = ItemCatalogue.RequestItem(item_name);
        item.physical_form = Instantiate(item.physical_form);
        item.physical_form.transform.position = transform.position;
        item.physical_form.GetComponent<NetworkedObject>().Spawn();
    }

    // Update is called once per frame
    void Update() {
    }
}
