using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.UI;

public class InventorySelect : MonoBehaviour {
    public InventoryManager im;
    private bool init = false;

    private void Awake() {
        InventoryManager.updateSharedItems_callback = update_callback;
    }

    // Start is called before the first frame update
    public void InitInventory() {
        init = false;
        ToggleGroup tg = GetComponentInChildren<ToggleGroup>();
        if (GetInventoryManager()) {
            im.UpdateNetworkInventoryCache();
        }
    }

    void update_callback() {
        ToggleGroup tg = GetComponentInChildren<ToggleGroup>();
        Toggle[] toggles = GetComponentsInChildren<Toggle>();
        int i = 0;
        List<string> item_names = InventoryManager.networkInv.GetAllItemNames();
        foreach (Toggle toggle in toggles) {
            string cur_name = item_names.ElementAtOrDefault(i++);
            toggle.GetComponentInChildren<Text>().text = cur_name;
            if (im.actionSlots.shared_item != null && im.actionSlots.shared_item.name() == cur_name) {
                toggle.isOn = true;
            }
            else {
                toggle.isOn = false;
            }
        }
        init = true;
    }
    // Update is called once per frame
    void Update() {

    }
    public void ItemToggle(string toggleNum) {
        if (init) {
            Toggle toggle = transform.GetChild(Int32.Parse(toggleNum) - 1).GetComponentInChildren<Toggle>();
            if (toggle != null) {
                string item_name = toggle.GetComponentInChildren<Text>().text;
                if (item_name != null && item_name != "") {
                    if (toggle.isOn) im.EquipSharedItem(item_name);
                    else im.UnequipSharedItem(item_name);
                }
            }
        }
    }
    bool GetInventoryManager() {
        if (im == null) {
            im = GetComponentInParent<InventoryManager>();
        }
        return im != null;
    }
}
