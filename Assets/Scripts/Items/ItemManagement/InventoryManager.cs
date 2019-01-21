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

    // The network inventory can ONLY be edited by the server
    // The client simply uses this for ouptuting visuals
    public static NetworkInventory networkInv;
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
        networkInv = new NetworkInventory();
        actionSlots.mh = mh;
    }

    void Start() {
        if (!isOwner) return;
    }

    // Update is called once per frame
    void Update() {
        if (!CheckForCameraController()) return;
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

    private bool NetworkSwapUseItem(string item_name, int clientId, out NetworkUseItem netItem, int count) {
        bool success = false;
        netItem = default(NetworkUseItem);
        success = networkInv.RevokeItem(item_name, (int)clientId, true);
        if (!success) return false;
        return networkInv.RequestItem(item_name, (int)clientId, out netItem);
    }

    #region AddItemRPCs
    void AddItemToInventory(ItemRequest request) {
        if (!isOwner) return;
        Item shipped_item = amazon.RequestItem(request.item_name);
        if (UseItem.isUseItem(shipped_item)) {
            if (!isServer) {
                InvokeServerRpc(RPC_AddUseItem, request.item_name, 1, channel: INVMANG_CHANNEL);
            }
            else {
                string item_name = request.item_name;
                uint clientId = NetworkingManager.singleton.LocalClientId;
                NetworkUseItem netItem = new NetworkUseItem(item_name);
                networkInv.AddItemStack(item_name, netItem, 1);
            }
        }
        if (AbilityItem.isAbilityItem(shipped_item)) {
            shipped_item.context = this;
            shipped_item.menu_form = image;
            actionSlots.ability_items[shipped_item.name()] = (AbilityItem)shipped_item;
            actionSlots.ChangeAbilityItem(actionSlots.ability_items.GetStackCount(), shipped_item.name());
        }
        GameObject.Destroy(request.gameObject);
    }


    [ServerRPC]
    private void RPC_AddUseItem(string item_name, int num) {
        NetworkUseItem netItem = new NetworkUseItem(item_name);
        networkInv.AddItemStack(item_name, netItem, num);
    }
    #endregion

    #region EquipRPCs
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
                if (NetworkSwapUseItem(item_name, (int)clientId, out netItem, 1)) {
                    RPC_ClientEquipUseItem(netItem.name);
                }
            }
        }
    }

    [ServerRPC]
    private void RPC_EquipUseItem(string item_name, int num) {
        NetworkUseItem netItem;
        if (NetworkSwapUseItem(item_name, (int)ExecutingRpcSender, out netItem, num)) {
            InvokeClientRpcOnClient(RPC_ClientEquipUseItem, ExecutingRpcSender, netItem.name, channel: INVMANG_CHANNEL);
        }
        else {
            InvokeClientRpcOnClient(RPC_ClientEquipUseItem, ExecutingRpcSender, "", channel: INVMANG_CHANNEL);
        }
    }

    [ClientRPC]
    private void RPC_ClientEquipUseItem(string item_name) {
        if (item_name != "") {
            UseItem shipped_item = (UseItem)amazon.RequestItem(item_name);
            shipped_item.context = this;
            shipped_item.menu_form = image;
            actionSlots.ChangeUseItem(shipped_item);
        }
    }
    #endregion

    #region UnequipRPCs
    public void UnequipUseItem(string item_name) {
        if (!isOwner) return;
        Item shipped_item = amazon.RequestItem(item_name);
        if (UseItem.isUseItem(shipped_item)) {
            if (!isServer) {
                InvokeServerRpc(RPC_UnequipUseItem, item_name, 1, channel: INVMANG_CHANNEL);
            }
            else {
                uint clientId = NetworkingManager.singleton.LocalClientId;
                bool success = networkInv.RevokeItem(item_name, (int)clientId, 1);
                RPC_ClientUnequipUseItem(success ? item_name : "");
            }
        }
    }

    [ServerRPC]
    private void RPC_UnequipUseItem(string item_name, int num) {
        bool success = networkInv.RevokeItem(item_name, (int)ExecutingRpcSender, num);
        InvokeClientRpcOnClient(RPC_ClientUnequipUseItem, ExecutingRpcSender, success ? item_name : "", channel: INVMANG_CHANNEL);
    }

    [ClientRPC]
    private void RPC_ClientUnequipUseItem(string item_name) {
        if (item_name == actionSlots.use_item?.name()) actionSlots.ChangeUseItem(null);
    }
    #endregion

    public void UpdateNetworkInventoryCache() {
        if (!isServer) {
            InvokeServerRpc(RPC_GetUpdatedInventory, true, channel: INVMANG_CHANNEL);
        }
        else {
            updateUseItems_callback();
        }
    }

    [ServerRPC]
    private void RPC_GetUpdatedInventory(bool success) {
        InvokeClientRpcOnClient(RPC_UpdateUseItems, ExecutingRpcSender, networkInv, channel: INVMANG_CHANNEL);
    }

    [ClientRPC]
    private void RPC_UpdateUseItems(NetworkInventory netInv) {
        networkInv = netInv;
        updateUseItems_callback();
    }

    public static Action updateUseItems_callback = () => { };

    private void UpdateUseItemList(NetworkInventory netInv) {
    }
    private bool CheckForCameraController() {
        if (cam_controller == null) {
            cam_controller = networkedObject.GetComponentInChildren<CameraController>();
        }
        return cam_controller != null;
    }
}