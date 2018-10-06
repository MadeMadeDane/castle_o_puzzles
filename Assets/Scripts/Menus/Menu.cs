using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour {

    public CameraController cam_controller = null;
    public bool is_open = false;
    public GameObject prefab;
    public GameObject ui_instance;
    public delegate bool getbutton();
    public int hierarchy;
    
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void open()
    {
        cam_controller.enabled = false;
        ui_instance = Instantiate(prefab);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }
    public void close()
    {
        cam_controller.enabled = true;
        Destroy(ui_instance);
        ui_instance = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
