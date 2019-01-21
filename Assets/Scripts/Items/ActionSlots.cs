using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionSlots : MonoBehaviour {
    public SharedItem shared_item = null;
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
    void Start() {
        ability_items = new Inventory<AbilityItem>();
        active_slot = 0;
        CheckForItemBar();
    }

    // Update is called once per frame
    void Update() {
        if (im.GetAbilitySlot1()) {
            if (ability_item != null) {
                ability_item.OnDestroy();
            }
            active_slot = 0;
            (AbilityItem item, int item_count) = ability_items.GetFirstOwnedItem(active_slot);
            ability_item = item;
            if (ability_item != null) {
                ability_item.Start();
            }
            UpdateSelectedSlotUI(active_slot);
        }
        else if (im.GetAbilitySlot2()) {
            if (ability_item != null) {
                ability_item.OnDestroy();
            }
            active_slot = 1;
            (AbilityItem item, int item_count) = ability_items.GetFirstOwnedItem(active_slot);
            ability_item = item;
            if (ability_item != null) {
                ability_item.Start();
            }
            UpdateSelectedSlotUI(active_slot);
        }
        else if (im.GetAbilitySlot3()) {
            if (ability_item != null) {
                ability_item.OnDestroy();
            }
            active_slot = 2;
            (AbilityItem item, int item_count) = ability_items.GetFirstOwnedItem(active_slot);
            ability_item = item;
            if (ability_item != null) {
                ability_item.Start();
            }
            UpdateSelectedSlotUI(active_slot);
        }
        else if (im.GetAbilitySlot4()) {
            if (ability_item != null) {
                ability_item.OnDestroy();
            }
            active_slot = 3;
            (AbilityItem item, int item_count) = ability_items.GetFirstOwnedItem(active_slot);
            ability_item = item;
            if (ability_item != null) {
                ability_item.Start();
            }
            UpdateSelectedSlotUI(active_slot);
        }
        if (shared_item != null) {
            shared_item.Update();
        }
        if (ability_item != null) {
            ability_item.Update();
        }
    }
    private void FixedUpdate() {

        if (shared_item != null) {
            shared_item.FixedUpdate();
        }

        if (ability_item != null) {
            ability_item.FixedUpdate();
        }
    }

    public void ChangeSharedItem(SharedItem item) {
        if (shared_item != null) {
            shared_item.OnDestroy();
        }
        shared_item = item;
        if (shared_item != null) shared_item.Start();
        ChangeSharedItemUI(item);
    }

    public void ChangeAbilityItem(int slot, string item_name) {
        if (slot < 4 && slot >= 0) {
            Debug.Log("Adding Ability Item " + slot);
            (AbilityItem aItem, int count) = ability_items.GetFirstOwnedItem(slot);
            if (count > 0) {
                if (slot == active_slot) {
                    aItem.OnDestroy();
                }
                ability_items.RevokeItem(aItem.name(), slot, true);
            }
            if (ability_items.RequestItem(item_name, slot, out aItem)) {
                ChangeAbilityItemUI(slot, aItem);
            }
            if (slot == active_slot) {
                Debug.Log("starting item");
                ability_item = aItem;
                ability_item.Start();
            }
        }
    }

    private void UpdateSelectedSlotUI(int slot) {
        if (!CheckForItemBar()) {
            return;
        }
        for (int i = 0; i < 4; i++) {
            if (i == slot) {
                item_bar.ability_slots[i].transform.GetChild(0).GetComponent<Image>().enabled = true;
            }
            else {
                item_bar.ability_slots[i].transform.GetChild(0).GetComponent<Image>().enabled = false;
            }
        }

    }

    private void ChangeAbilityItemUI(int slot, AbilityItem item) {
        if (CheckForItemBar()) {
            item_bar.ability_slots[slot].sprite = item.menu_form;
        }
    }
    private void ChangeSharedItemUI(SharedItem item) {
        if (CheckForItemBar()) {
            item_bar.use_slot.sprite = item?.menu_form;
        }
    }
    private bool CheckForItemBar() {
        if (mh != null && item_bar == null) {
            item_bar = mh.hud.ui_instance.GetComponentInChildren<ItemBar>();
        }
        return item_bar != null;
    }
    public void DropItem() {
        shared_item = null;
    }
}
