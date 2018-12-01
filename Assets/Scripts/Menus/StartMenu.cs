using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartMenu : Menu {

    public override void open() {
        base.open();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public override void close() {
        base.close();
        ui_instance = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
