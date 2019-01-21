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

    protected virtual void OnOpen() {
    }

    protected virtual void OnClose() {
    }

    public virtual void create() {
        ui_instance = Instantiate(prefab);
    }

    public virtual void open() {
        ui_instance.SetActive(true);
        is_open = true;
        OnOpen();
    }

    public virtual void close() {
        ui_instance.SetActive(false);
        is_open = false;
        OnClose();
    }

    public virtual void destroy() {
        Destroy(ui_instance);
    }
}
