﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionSlots : MonoBehaviour {
    public UseItem use_item = null;
    public AbilityItem ability_item = null;
    public Inventory<AbilityItem> ability_items;
    public MenuHandler mh;
    private InputManager im;
    private int active_slot;
    private ItemBar item_bar;

    private void Awake() {
        im = InputManager.Instance;
    }

    // Use this for initialization
    void Start () {
        ability_items = new Inventory<AbilityItem>();
        active_slot = 0;
        if(mh != null) {
            item_bar = mh.hud.ui_instance.GetComponentInChildren<ItemBar>();
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (im.GetAbilitySlot1()) {
            if(ability_item != null) {
                ability_item.Destroy();
            }
            active_slot = 0;
            (AbilityItem item, int item_count)  = ability_items.GetFirstOwnedItem(active_slot);
            ability_item = item;
            if(ability_item != null) {
                ability_item.Start();
            }
            UpdateSelectedSlot(active_slot);
        } else if (im.GetAbilitySlot2()) {
            if(ability_item != null) {
                ability_item.Destroy();
            }
            active_slot = 1;
            (AbilityItem item, int item_count)  = ability_items.GetFirstOwnedItem(active_slot);
            ability_item = item;
            if(ability_item != null) {
                ability_item.Start();
            }
            UpdateSelectedSlot(active_slot);
        } else if (im.GetAbilitySlot3()) {
            if(ability_item != null) {
                ability_item.Destroy();
            }
            active_slot = 2;
            (AbilityItem item, int item_count)  = ability_items.GetFirstOwnedItem(active_slot);
            ability_item = item;
            if(ability_item != null) {
                ability_item.Start();
            }
            UpdateSelectedSlot(active_slot);
        } else if (im.GetAbilitySlot4()) {
            if(ability_item != null) {
                ability_item.Destroy();
            }
            active_slot = 3;
            (AbilityItem item, int item_count)  = ability_items.GetFirstOwnedItem(active_slot);
            ability_item = item;
            if(ability_item != null) {
                ability_item.Start();
            }
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

    public void ChangeUseItem (UseItem item)
    {
        if (use_item != null) {
            use_item.Destroy();
        }
        use_item = item;
        use_item.Start();
        ItemBar item_bar = mh.hud.ui_instance.GetComponentInChildren<ItemBar>();
        if (item_bar != null) {
            item_bar.use_slot.sprite = item.menu_form;
        }
    }

    public void ChangeAbilityItem(int slot, string item_name)
    {
        if (slot < 4 && slot >= 0) {
        Debug.Log("Adding Ability Item " + slot );
            (AbilityItem aItem, int count) = ability_items.GetFirstOwnedItem(slot);
            if(count > 0)
            {
                if (slot == active_slot) {
                    aItem.Destroy();
                }
                ability_items.RevokeItem(aItem.GetName(),slot, true);
            }
            if (!ability_items.RequestItem(item_name, slot, out aItem)) {
                if (item_bar != null) {
                    item_bar.ability_slots[slot].sprite = aItem.menu_form;
                }
            }
            if (slot == active_slot) {
                Debug.Log("starting item");
                ability_item = aItem;
                ability_item.Start();
            }
        }
    }

    private void UpdateSelectedSlot(int slot)
    {
        for (int i = 0; i < 4; i++) {
            if (i == slot) {
                if(item_bar){
                    item_bar.ability_slots[i].transform.GetChild(0).GetComponent<Image>().enabled = true;
                }
                else {
                    if(mh != null) {
                        item_bar = mh.hud.ui_instance.GetComponentInChildren<ItemBar>();
                    }
                }
                
            } else {
                if (item_bar) {
                    item_bar.ability_slots[i].transform.GetChild(0).GetComponent<Image>().enabled = false;
                }
                else {
                    if(mh != null) {
                        item_bar = mh.hud.ui_instance.GetComponentInChildren<ItemBar>();
                    }
                }
            }
        }
        
    }
    public void DropItem()
    {
        use_item = null;
    }
}
