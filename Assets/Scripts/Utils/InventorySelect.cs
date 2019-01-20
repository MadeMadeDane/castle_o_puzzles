using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InventorySelect : MonoBehaviour {
    public InventoryManager im;
    private bool init = false;

    private void Awake() {
        InventoryManager.updateUseItems_callback = update_callback;
    }

    // Start is called before the first frame update
    void Start() {
        init = false;
        ToggleGroup tg = GetComponentInChildren<ToggleGroup>();
        tg.SetAllTogglesOff();
        bool imThere = GetInventoryManager();
        if (imThere) {
            im.UpdateNetworkInventoryCache();
        }
    }

    void update_callback() {
        bool imThere = GetInventoryManager();
        ToggleGroup tg = GetComponentInChildren<ToggleGroup>();
        Toggle[] toggles = GetComponentsInChildren<Toggle>();
        int i = 0;
        foreach (Toggle toggle in toggles) {
            if (imThere) {
                string item_name;
                NetworkUseItem item;
                int count;
                (item_name, item, count) = InventoryManager.networkInv.GetStackAtIndex(i++);
                toggle.GetComponentInChildren<Text>().text = item_name;
                if (im.actionSlots.use_item != null && im.actionSlots.use_item.GetName() == item_name) {
                    Debug.Log("RPG The Check needs to be on this one " + item_name + " == " + im.actionSlots.use_item?.GetName());
                    toggle.isOn = true;
                }
            }
            else {
                toggle.GetComponentInChildren<Text>().text = "";
            }
        }
        init = true;
    }
    // Update is called once per frame
    void Update() {

    }
    public void ItemToggle(string toggleNum) {
        if (init) {
            ToggleGroup tg = GetComponent<ToggleGroup>();
            Toggle toggle = tg.ActiveToggles().FirstOrDefault();
            if (toggle != null && GetInventoryManager()) {
                string item_name = toggle.GetComponentInChildren<Text>().text;
                if (item_name != null && item_name != "") {
                    im.EquipUseItem(item_name);
                }
            }
            else {

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
