using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InventorySelect : MonoBehaviour
{
    public InventoryManager im;
    private bool init = false;
    // Start is called before the first frame update
    void Start()
    {
        init = false;
        bool imThere = GetInventoryManager();
        ToggleGroup tg = GetComponentInChildren<ToggleGroup>();
        Toggle[] toggles = GetComponentsInChildren<Toggle>();
        tg.SetAllTogglesOff();
        int i = 0;
        foreach(Toggle toggle in toggles) {
            if (imThere) {
                string item_name;
                NetworkUseItem item;
                int count;
                (item_name, item, count) = InventoryManager.networkInv.GetStackAtIndex(i++);
                toggle.GetComponentInChildren<Text>().text = item_name;
            } else {
                toggle.GetComponentInChildren<Text>().text = "";
            }
        }
        init = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ItemToggle(string toggleNum) {
        if (init) {
            ToggleGroup tg = GetComponent<ToggleGroup>();
            Toggle toggle = tg.ActiveToggles().FirstOrDefault();
            if (toggle != null && GetInventoryManager()) {
                string item_name = toggle.GetComponentInChildren<Text>().text;
                im.EquipUseItem(item_name);
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
