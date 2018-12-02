using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI;
using MLAPI.Serialization;
using System.IO;

public class InventoryManager : NetworkedBehaviour {
    public string INVMANG_CHANNEL = "MLAPI_INTERNAL";
    public CameraController cam_controller;
    public ActionSlots actionSlots;

    public Inventory<NetworkUseItem> netowrkInv;
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
        actionSlots = gameObject.AddComponent<ActionSlots>();
        actionSlots.mh = GetComponent<MenuHandler>();
        cam_controller = GetComponentInChildren<CameraController>();
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
        Debug.Log("BeforeStuff");
        if (UseItem.isUseItem(shipped_item)) {
            Debug.Log("Stuff");
            if (!isServer) {
                InvokeServerRpc(RPC_AddAndEquipUseItem, NetworkingManager.singleton.LocalClientId, request.item_name, 1, channel: INVMANG_CHANNEL);
            }
            else {
                string item_name = request.item_name;
                uint clientId = NetworkingManager.singleton.LocalClientId;
                NetworkUseItem netItem = new NetworkUseItem(item_name);
                Debug.Log("Server: Swapping Use Item");
                if (!netowrkInv.AddItemStack(item_name, netItem, (int) clientId, 1)) {
                    RPC_SwapUseItem(item_name);
                }
            }
        }
        if (AbilityItem.isAbilityItem(shipped_item)) {
            actionSlots.ability_items[shipped_item.GetName()] = (AbilityItem) shipped_item;
            actionSlots.ChangeAbilityItem(actionSlots.ability_items.GetStackCount(), shipped_item.GetName());
        }
        GameObject.Destroy(request.gameObject);
    }
    
    [ServerRPC]
    private void RPC_AddAndEquipUseItem (uint clientId, string item_name, int num)
    {
        NetworkUseItem netItem = new NetworkUseItem(item_name);
        Debug.Log("Swapping Use Item");
        if (!netowrkInv.AddItemStack(item_name, netItem, (int) clientId, num)) {
            InvokeClientRpcOnEveryone(RPC_SwapUseItem, item_name, channel: INVMANG_CHANNEL);
        }
    }
    [ClientRPC]
    private void RPC_SwapUseItem (string item_name)
    {
        UseItem shipped_item = (UseItem) amazon.RequestItem(item_name);
        shipped_item.ctx = this;
        shipped_item.menu_form = image;
        Debug.Log("Swapping Use Item");
        actionSlots.ChangeUseItem(shipped_item);
    }
}