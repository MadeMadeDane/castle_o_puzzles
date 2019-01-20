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

    public static Inventory<NetworkUseItem> networkInv;
    public Sprite image;
    public float explosive_rad = 1.0f;
    public float select_reach_dist = 5.0f;
    public bool enable_logs = false;
    private Utilities utils;
    private InputManager im;
    private ItemCatalogue amazon;


    private ItemRequest prevItem = null;
    // Use this for initialization
    public void Setup(MenuHandler mh) {
        utils = Utilities.Instance;
        im = InputManager.Instance;
        amazon = new ItemCatalogue();
        actionSlots = gameObject.AddComponent<ActionSlots>();
        cam_controller = GetComponentInChildren<CameraController>();
        networkInv = new Inventory<NetworkUseItem>();
        actionSlots.mh = mh;
    }

    void Start() {
        if (!isOwner) return;
    }

    // Update is called once per frame
    void Update() {
        if(!CheckForCameraController()) return;
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
                Debug.Log("RPC to server");
                InvokeServerRpc(RPC_AddAndEquipUseItem, request.item_name, 1, channel: INVMANG_CHANNEL);
            }
            else {
                string item_name = request.item_name;
                uint clientId = NetworkingManager.singleton.LocalClientId;
                NetworkUseItem netItem = new NetworkUseItem(item_name);
                Debug.Log("Server: Swapping Use Item");
                if (!networkInv.AddItemStack(item_name, netItem, (int) clientId, 1)) {
                    RPC_SwapUseItem(item_name);
                }
            }
        }
        if (AbilityItem.isAbilityItem(shipped_item)) {
            shipped_item.ctx = this;
            shipped_item.menu_form = image;
            actionSlots.ability_items[shipped_item.GetName()] = (AbilityItem) shipped_item;
            actionSlots.ChangeAbilityItem(actionSlots.ability_items.GetStackCount(), shipped_item.GetName());
        }
        GameObject.Destroy(request.gameObject);
    }
    
    public void EquipUseItem(string item_name) {
        if (!isOwner) return;
        Item shipped_item = amazon.RequestItem(item_name);
        if (UseItem.isUseItem(shipped_item)) {
            if (!isServer) {
                InvokeServerRpc(RPC_EquipUseItem, item_name, 1, channel: INVMANG_CHANNEL);
            }
            else {
                uint clientId = NetworkingManager.singleton.LocalClientId;
                NetworkUseItem netItem;
                if (!networkInv.RequestItem(item_name, (int) clientId, out netItem, 1)) {
                    InvokeClientRpcOnEveryone(RPC_UpdateUseItems, networkInv, channel: INVMANG_CHANNEL);
                    RPC_SwapUseItem(netItem.name);
                }
            }
        }
    }
    [ServerRPC]
    private void RPC_EquipUseItem (string item_name, int num)
    {
        NetworkUseItem netItem;
        if (!networkInv.RequestItem(item_name, (int) ExecutingRpcSender, out netItem, num)) {
            InvokeClientRpcOnEveryone(RPC_UpdateUseItems, networkInv, channel: INVMANG_CHANNEL);
            InvokeClientRpcOnClient(RPC_SwapUseItem, ExecutingRpcSender, netItem.name, channel: INVMANG_CHANNEL);
        }
    }
    
    [ServerRPC]
    private void RPC_AddAndEquipUseItem (string item_name, int num)
    {
        NetworkUseItem netItem = new NetworkUseItem(item_name);
        if (!networkInv.AddItemStack(item_name, netItem, (int) ExecutingRpcSender, num)) {
            InvokeClientRpcOnEveryone(RPC_UpdateUseItems, networkInv,  channel: INVMANG_CHANNEL);
            InvokeClientRpcOnClient(RPC_SwapUseItem, ExecutingRpcSender, item_name, channel: INVMANG_CHANNEL);
        }
    }
    [ClientRPC]
    private void RPC_SwapUseItem (string item_name)
    {
        UseItem shipped_item = (UseItem) amazon.RequestItem(item_name);
        shipped_item.ctx = this;
        shipped_item.menu_form = image;
        actionSlots.ChangeUseItem(shipped_item);
    }
    [ClientRPC]
    private void RPC_UpdateUseItems (Inventory<NetworkUseItem> netInv)
    {
        Debug.Log("RPG Updating UseItems");
        networkInv = netInv;
    }
    private void UpdateUseItemList( Inventory<NetworkUseItem> netInv) {
    }
    private bool CheckForCameraController() {
        if (cam_controller == null){
            cam_controller = networkedObject.GetComponentInChildren<CameraController>();
        }
        return cam_controller != null;
    }
}