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
    public float select_cone_threshold = Mathf.Sqrt(3f) / 2f;
    public float select_reach_dist = 5.0f;
    public bool enable_logs = false;
    private Utilities utils;
    private InputManager im;

    // Use this for initialization
    public void Setup(MenuHandler mh) {
        utils = Utilities.Instance;
        im = InputManager.Instance;
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
        WorldItem targetItem = HandleItemSelection();
        if (targetItem != null) {
            targetItem.Highlight();
        }
        if (im.GetUse() && targetItem != null) {
            HandleItemPickup(targetItem);
        }
        if (im.GetDropItem() && actionSlots.shared_item != null) {
            actionSlots.DropItem();
        }
    }

    private WorldItem HandleItemSelection() {
        if (cam_controller.GetViewMode() != ViewMode.Shooter) return null;
        Camera cam = cam_controller.controlled_camera;
        Vector3 cam_pos = cam.transform.position;
        Vector3 cam_forward = cam.transform.forward;
        HashSet<WorldItem> worldItems = WorldItemTracker.Instance.worldItems;

        IEnumerable<WorldItem> objInRange = worldItems.Where((item) => {
            Vector3 relative_pos = (item.transform.position - cam_pos);
            // Is the item close enough to be picked up?
            bool in_radius = relative_pos.magnitude <= select_reach_dist;
            // The angle of the cone is controlled by this dot product. Which items am I looking at? 
            bool in_cone = Vector3.Dot(relative_pos.normalized, cam_forward) >= select_cone_threshold;
            return in_radius && in_cone;
        });
        // The highest dot product gives the item that is closest to our viewing angle
        return objInRange.OrderBy((item) => Vector3.Dot((item.transform.position - cam_pos).normalized, cam_forward)).LastOrDefault();
    }

    private void HandleItemPickup(WorldItem targetItem) {
        Vector3 local_pos = targetItem.transform.position - cam_controller.transform.position;
        if (Physics.Raycast(cam_controller.transform.position, local_pos, out RaycastHit hit_info, local_pos.magnitude)) {
            if (!hit_info.transform.GetComponentsInChildren<WorldItem>().Contains(targetItem)) return;
        }
        AddItemToInventory(targetItem);
    }
    private bool NetworkSwapSharedItem(string item_name, int clientId, out NetworkSharedItem netItem, int count) {
        bool success = false;
        netItem = default(NetworkSharedItem);
        success = networkInv.RevokeItem(item_name, (int)clientId, true);
        if (!success) return false;
        return networkInv.RequestItem(item_name, (int)clientId, out netItem);
    }

    #region AddItemRPCs
    void AddItemToInventory(WorldItem request) {
        if (!isOwner) return;
        Item shipped_item = ItemCatalogue.RequestItem(request.item_name);
        if (SharedItem.isSharedItem(shipped_item)) {
            if (!isServer) {
                InvokeServerRpc(RPC_AddSharedItemNetwork, request.networkId, 1, channel: INVMANG_CHANNEL);
            }
            else {
                RPC_AddSharedItemNetwork(request.networkId, 1);
            }
        }
        else if (AbilityItem.isAbilityItem(shipped_item)) {
            shipped_item.context = this;
            shipped_item.menu_form = image;
            actionSlots.ability_items[shipped_item.name()] = (AbilityItem)shipped_item;
            actionSlots.ChangeAbilityItem(actionSlots.ability_items.GetStackCount(), shipped_item.name());
        }
    }


    [ServerRPC]
    private void RPC_AddSharedItemNetwork(uint itemNetowrkId, int num) {
        NetworkedObject nobj = GetNetworkedObject(itemNetowrkId);
        if (nobj == null) return;
        RPC_AddSharedItem(nobj.GetComponent<WorldItem>().item_name, num);
        Destroy(nobj.gameObject);
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
        Item shipped_item = ItemCatalogue.RequestItem(item_name);
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
            SharedItem shipped_item = (SharedItem)ItemCatalogue.RequestItem(item_name);
            shipped_item.context = this;
            shipped_item.menu_form = image;
            actionSlots.ChangeSharedItem(shipped_item);
        }
    }
    #endregion

    #region UnequipRPCs
    public void UnequipSharedItem(string item_name) {
        if (!isOwner) return;
        Item shipped_item = ItemCatalogue.RequestItem(item_name);
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

    #region UpdateNetworkInventoryRPCs
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
    #endregion

    private bool CheckForCameraController() {
        if (cam_controller == null) {
            cam_controller = networkedObject.GetComponentInChildren<CameraController>();
        }
        return cam_controller != null;
    }
}