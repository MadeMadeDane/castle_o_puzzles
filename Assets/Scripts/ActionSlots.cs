using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionSlots : MonoBehaviour {
    public UseItem use_item = null;
    public AbilityItem ability_item = null;
    public Inventory<AbilityItem> ability_items;
    public InputManager im;
    public MenuHandler mh;
    private int active_slot;
    private ItemBar item_bar;

    // Use this for initialization
    void Start () {
        ability_items = new Inventory<AbilityItem>();
        ability_items.numSlots = 4;
        active_slot = 1;
        item_bar = mh.hud.ui_instance.GetComponentInChildren<ItemBar>();
    }
	
	// Update is called once per frame
	void Update () {
        if (im.GetAbilitySlot1()) {
            active_slot = 1;
            ability_items.contents.TryGetValue("slot" + active_slot, out ability_item);
            UpdateSelectedSlot(active_slot);
        } else if (im.GetAbilitySlot2()) {
            active_slot = 2;
            ability_items.contents.TryGetValue("slot" + active_slot, out ability_item);
            UpdateSelectedSlot(active_slot);
        } else if (im.GetAbilitySlot3()) {
            active_slot = 3;
            ability_items.contents.TryGetValue("slot" + active_slot, out ability_item);
            UpdateSelectedSlot(active_slot);
        } else if (im.GetAbilitySlot4()) {
            active_slot = 4;
            ability_items.contents.TryGetValue("slot" + active_slot, out ability_item);
            UpdateSelectedSlot(active_slot);
        }
        if (use_item != null) {
            use_item.Update();
        }
        if (ability_item != null) {
            ability_item.Update();
        }
    }
    private void FixedUpdate()
    {

        if (use_item != null) {
            use_item.FixedUpdate();
        }

        if (ability_item != null) {
            ability_item.FixedUpdate();
        }
    }

    public void AddUseItem (UseItem item)
    {
        if (use_item == null) {
            use_item = item;
            ItemBar item_bar = mh.hud.ui_instance.GetComponentInChildren<ItemBar>();
            if (item_bar != null) {
                item_bar.use_slot.sprite = item.menu_form;
            }
        }
    }

    public void AddAbilityItem(int slot, AbilityItem item)
    {
        Debug.Log("Adding Item to Slot" + slot);
        if (slot < 5 && slot > 0) {
            Debug.Log(item.menu_form);
            AbilityItem added_item = ability_items.AddItem("slot" + slot, item);
            Debug.Log(item.menu_form);
            if (added_item != null) {
                if (item_bar != null) {
                    item_bar.ability_slots[slot - 1].sprite = added_item.menu_form;
                }
            }
            if (slot == active_slot) {
                ability_item = added_item;
            }
        }
    }

    private void UpdateSelectedSlot(int slot)
    {
        for (int i = 0; i < 4; i++) {
            if (i == (slot - 1)) {
                item_bar.ability_slots[i].transform.GetChild(0).GetComponent<Image>().enabled = true;
            } else {
                item_bar.ability_slots[i].transform.GetChild(0).GetComponent<Image>().enabled = false;
            }
        }
        GetComponentInChildren<Image>().enabled = true;
        
    }
    public void DropItem()
    {
        use_item = null;
    }
}
