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

    public virtual void open() {
        ui_instance = Instantiate(prefab);
    }

    public virtual void close() {
        Destroy(ui_instance);
    }
}
