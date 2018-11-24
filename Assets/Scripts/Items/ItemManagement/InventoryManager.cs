using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI;

public class InventoryManager : NetworkedBehaviour {
    public CameraController cam_controller;
    public ActionSlots actionSlots;
    public Sprite image;
    public float explosive_rad = 1.0f;
    public float select_reach_dist = 5.0f;
    public bool enable_logs = false;
    private Utilities utils;
    private InputManager im;
    private ItemCatalogue amazon;


    private ItemRequest prevItem = null;
    // Use this for initialization
    private void Setup() {
        utils = Utilities.Instance;
        im = InputManager.Instance;
        amazon = new ItemCatalogue();
    }

    void Start() {
        if (!isOwner) return;
        Setup();
    }

    // Update is called once per frame
    void Update() {
        if (!isOwner) return;
        ItemRequest targetItem = null;
        if (cam_controller.GetViewMode() == ViewMode.Shooter) {
            Camera cam = cam_controller.controlled_camera;
            targetItem = utils.RayCastExplosiveSelect<ItemRequest>(origin: cam.transform.position,
                                                                   path: cam.transform.forward * select_reach_dist,
                                                                   radius: explosive_rad);
        }
        if (prevItem != null && prevItem != targetItem) {
            //prevItem.gameObject.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.EmissionModule emitter = prevItem.gameObject.GetComponent<ParticleSystem>().emission;
            emitter.enabled = false;
            prevItem = null;
        }
        if (targetItem != null) {
            if (!targetItem.gameObject.GetComponent<ParticleSystem>().isPlaying) {
                targetItem.gameObject.GetComponent<ParticleSystem>().Play();
            }
            ParticleSystem.EmissionModule emitter = targetItem.gameObject.GetComponent<ParticleSystem>().emission;
            emitter.enabled = true;
            prevItem = targetItem;
        }
        if (im.GetPickUp() && targetItem != null) {
            AddItemToInventory(targetItem);
        }
        if (im.GetDropItem() && actionSlots.use_item != null) {
            actionSlots.DropItem();
        }
    }

    void AddItemToInventory(ItemRequest request) {
        if (!isOwner) return;
        Item shipped_item = amazon.RequestItem(request.item_name);

        shipped_item.ctx = this;
        shipped_item.Start();
        shipped_item.menu_form = image;
        if (UseItem.isUseItem(shipped_item)) {
            actionSlots.AddUseItem((UseItem)shipped_item);
        }
        if (AbilityItem.isAbilityItem(shipped_item)) {
            actionSlots.AddAbilityItem(actionSlots.ability_items.contents.Count, (AbilityItem)shipped_item);
        }
        GameObject.Destroy(request.gameObject);
    }
}