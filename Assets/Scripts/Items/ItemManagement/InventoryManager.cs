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
        if (im.GetDropItem() && actionSlots.shared_item != null) {
            actionSlots.DropItem();
        }
    }

    private bool NetworkSwapSharedItem(string item_name, int clientId, out NetworkSharedItem netItem, int count) {
        bool success = false;
        netItem = default(NetworkSharedItem);
        success = networkInv.RevokeItem(item_name, (int)clientId, true);
        if (!success) return false;
        return networkInv.RequestItem(item_name, (int)clientId, out netItem);
    }

    #region AddItemRPCs
    void AddItemToInventory(ItemRequest request) {
        if (!isOwner) return;
        Item shipped_item = amazon.RequestItem(request.item_name);
        if (SharedItem.isSharedItem(shipped_item)) {
            if (!isServer) {
                InvokeServerRpc(RPC_AddSharedItem, request.item_name, 1, channel: INVMANG_CHANNEL);
            }
            else {
                string item_name = request.item_name;
                uint clientId = NetworkingManager.singleton.LocalClientId;
                NetworkSharedItem netItem = new NetworkSharedItem(item_name);
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
    private void RPC_AddSharedItem(string item_name, int num) {
        NetworkSharedItem netItem = new NetworkSharedItem(item_name);
        networkInv.AddItemStack(item_name, netItem, num);
    }
    #endregion

    #region EquipRPCs
    public void EquipSharedItem(string item_name) {
        if (!isOwner) return;
        Item shipped_item = amazon.RequestItem(item_name);
        if (SharedItem.isSharedItem(shipped_item)) {
            if (!isServer) {
                InvokeServerRpc(RPC_EquipSharedItem, item_name, 1, channel: INVMANG_CHANNEL);
            }
            else {
                uint clientId = NetworkingManager.singleton.LocalClientId;
                NetworkSharedItem netItem;
                if (NetworkSwapSharedItem(item_name, (int)clientId, out netItem, 1)) {
                    RPC_ClientEquipSharedItem(netItem.name);
                }
            }
        }
    }

    [ServerRPC]
    private void RPC_EquipSharedItem(string item_name, int num) {
        NetworkSharedItem netItem;
        if (NetworkSwapSharedItem(item_name, (int)ExecutingRpcSender, out netItem, num)) {
            InvokeClientRpcOnClient(RPC_ClientEquipSharedItem, ExecutingRpcSender, netItem.name, channel: INVMANG_CHANNEL);
        }
        else {
            InvokeClientRpcOnClient(RPC_ClientEquipSharedItem, ExecutingRpcSender, "", channel: INVMANG_CHANNEL);
        }
    }

    [ClientRPC]
    private void RPC_ClientEquipSharedItem(string item_name) {
        if (item_name != "") {
            SharedItem shipped_item = (SharedItem)amazon.RequestItem(item_name);
            shipped_item.context = this;
            shipped_item.menu_form = image;
            actionSlots.ChangeSharedItem(shipped_item);
        }
    }
    #endregion

    #region UnequipRPCs
    public void UnequipSharedItem(string item_name) {
        if (!isOwner) return;
        Item shipped_item = amazon.RequestItem(item_name);
        if (SharedItem.isSharedItem(shipped_item)) {
            if (!isServer) {
                InvokeServerRpc(RPC_UnequipSharedItem, item_name, 1, channel: INVMANG_CHANNEL);
            }
            else {
                uint clientId = NetworkingManager.singleton.LocalClientId;
                bool success = networkInv.RevokeItem(item_name, (int)clientId, 1);
                RPC_ClientUnequipSharedItem(success ? item_name : "");
            }
        }
    }

    [ServerRPC]
    private void RPC_UnequipSharedItem(string item_name, int num) {
        bool success = networkInv.RevokeItem(item_name, (int)ExecutingRpcSender, num);
        InvokeClientRpcOnClient(RPC_ClientUnequipSharedItem, ExecutingRpcSender, success ? item_name : "", channel: INVMANG_CHANNEL);
    }

    [ClientRPC]
    private void RPC_ClientUnequipSharedItem(string item_name) {
        if (item_name == actionSlots.shared_item?.name()) actionSlots.ChangeSharedItem(null);
    }
    #endregion

    public void UpdateNetworkInventoryCache() {
        if (!isServer) {
            InvokeServerRpc(RPC_GetUpdatedInventory, true, channel: INVMANG_CHANNEL);
        }
        else {
            updateSharedItems_callback();
        }
    }

    [ServerRPC]
    private void RPC_GetUpdatedInventory(bool success) {
        InvokeClientRpcOnClient(RPC_UpdateSharedItems, ExecutingRpcSender, networkInv, channel: INVMANG_CHANNEL);
    }

    [ClientRPC]
    private void RPC_UpdateSharedItems(NetworkInventory netInv) {
        networkInv = netInv;
        updateSharedItems_callback();
    }

    public static Action updateSharedItems_callback = () => { };

    private void UpdateSharedItemList(NetworkInventory netInv) {
    }
    private bool CheckForCameraController() {
        if (cam_controller == null) {
            cam_controller = networkedObject.GetComponentInChildren<CameraController>();
        }
        return cam_controller != null;
    }
}