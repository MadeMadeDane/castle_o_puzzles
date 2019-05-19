using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class PlayerBuilder : NetworkedBehaviour {
    public GameObject camera_prefab;
    public GameObject recover_prefab;
    public GameObject hud_prefab;
    public GameObject startmenu_prefab;

    public override void NetworkStart() {
        if (!IsOwner) return;

        gameObject.AddComponent<PlayerController>();
        GameObject camera = Instantiate(camera_prefab);
        camera.transform.parent = transform;

        GameObject parent = transform.parent.gameObject;
        MenuHandler mh = parent.AddComponent<MenuHandler>();
        CameraController cam_controller = camera.GetComponent<CameraController>();
        mh.hud.cam_controller = cam_controller;
        mh.hud.prefab = hud_prefab;
        mh.hud.create();
        mh.hud.ui_instance.transform.SetParent(transform, false);
        mh.start_menu.cam_controller = cam_controller;
        mh.start_menu.prefab = startmenu_prefab;
        mh.start_menu.create();
        mh.start_menu.close();
        mh.start_menu.ui_instance.transform.SetParent(transform, false);
        InventoryManager im = parent.GetComponentInChildren<InventoryManager>();
        if (im != null) {
            im.Setup(mh);
        }
    }
}