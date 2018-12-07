using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class PlayerBuilder : NetworkedBehaviour {
    public GameObject camera_prefab;
    public GameObject recover_prefab;
    public GameObject hud_prefab;
    public GameObject startmenu_prefab;
    public Sprite ExampleImage;

    public override void NetworkStart() {
        if (!isOwner) return;

        gameObject.AddComponent<PlayerController>();
        gameObject.AddComponent<PhysicsPropHandler>();
        GameObject camera = Instantiate(camera_prefab);
        camera.transform.parent = transform;
        GameObject recover = Instantiate(recover_prefab);
        recover.transform.parent = transform;

        GameObject parent = transform.parent.gameObject;
        MenuHandler mh = parent.AddComponent<MenuHandler>();
        mh.hud_obj = hud_prefab;
        mh.start_menu_obj = startmenu_prefab;
        gameObject.GetComponent<InventoryManager>().AddMenuHandler(mh);
    }
}