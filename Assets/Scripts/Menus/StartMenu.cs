using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartMenu : Menu {

    public override void open() {
        base.open();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        InventorySelect invSel = ui_instance.GetComponentInChildren<InventorySelect>();
        invSel.InitInventory();
    }

    public override void close() {
        base.close();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
