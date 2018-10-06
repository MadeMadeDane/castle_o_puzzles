using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuHandler : MonoBehaviour {
    public InputManager im;
    public CameraController cam_controller;
    public HeadsUpDisplay hud;
    public GameObject hud_obj;
    public StartMenu start_menu;
    public GameObject start_menu_obj;

    // Use this for initialization
    void Awake () {
        if (gameObject.GetCompoent<HeadsUpDisplay>() == null){
            hud = gameObject.AddComponent<HeadsUpDisplay>();
        }
        if (gameObject.GetComponent<StartMenu>() == null){
            start_menu = gameObject.AddComponent<StartMenu>();
        }
    }
    void Start () {
        hud.cam_controller = cam_controller;
        hud.prefab = hud_obj;
        hud.open();
        hud.ui_instance.transform.SetParent(transform, false);

        start_menu.cam_controller = cam_controller;
        start_menu.prefab = start_menu_obj;
    }
	
	// Update is called once per frame
	void Update () {
        if (im.GetStart()) {
            start_menu.is_open = !start_menu.is_open;
            if(start_menu.is_open) {
                start_menu.open();
            } else {
                start_menu.close();
            }
        }
    }
}
