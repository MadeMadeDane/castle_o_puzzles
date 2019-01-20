using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
using MLAPI.Serialization;

public class NetworkInventory : Inventory<NetworkUseItem>, IBitWritable {
    public void Read(Stream stream) {
        using (PooledBitReader reader = PooledBitReader.Get(stream)) {

            int item_count = reader.ReadInt32();
            for (int i = 0; i < item_count; i++) {
                string item_name = reader.ReadString().ToString();
                int count = reader.ReadInt32();
                Debug.Log("RPG Here is the item Name: " + item_name + " Count: " + count.ToString());
                AddItemStack(item_name, new NetworkUseItem(item_name), count);
            }
        }
    }
    public void Write(Stream stream) {
        using (PooledBitWriter writer = PooledBitWriter.Get(stream)) {
            writer.WriteInt32(stacks.Count);
            foreach (KeyValuePair<string, InventoryItem> kvp in stacks) {
                InventoryItem curInvItem = kvp.Value;
                if (curInvItem.Value.name != kvp.Key) {
                    Debug.LogWarning("NetworkUseItem name did not match " + kvp.Key + " != " + curInvItem.Value.name);
                }
                writer.WriteString(kvp.Key);
                writer.WriteInt32(curInvItem.Count);
            }
        }
    }
}
public class Inventory<T_ITEM> {
    protected Dictionary<string, InventoryItem> stacks;
    protected Dictionary<int, List<InventoryItem>> owner_list;
    public uint numSlots = 0;

    // Use this for initialization
    public Inventory() {
        stacks = new Dictionary<string, InventoryItem>();
        owner_list = new Dictionary<int, List<InventoryItem>>();
    }

    public T_ITEM this[string item_name] {
        get {
            return GetItem(item_name);
        }
        set {
            UnlockItem(item_name, value);
        }
    }

    public bool AddItem(string addItem, T_ITEM item) {
        Debug.Log("In Bad Add Item");
        return AddItemStack(addItem, item, 1);
    }

    public bool AddItemStack(string addItem, T_ITEM item, int numAdded) {
        InventoryItem foundItem;
        if (!stacks.TryGetValue(addItem, out foundItem)) {
            if (numSlots > 0 && numAdded + stacks.Count > numSlots) {
                return true;
            }
            foundItem = new InventoryItem(item);
            stacks[addItem] = foundItem;
            numAdded -= 1;
        }
        foundItem.Count += numAdded;
        return false;
    }

    public bool AddItem(string addItem, T_ITEM item, int owner) {
        Debug.Log("In Add Item");
        return AddItemStack(addItem, item, owner, 1);
    }

    public bool AddItemStack(string addItem, T_ITEM item, int owner, int numAdded) {
        InventoryItem foundItem;
        if (!stacks.TryGetValue(addItem, out foundItem)) {
            if (numSlots > 0 && numAdded + stacks.Count > numSlots) {
                return true;
            }
            Debug.Log("Adding single item stack");
            foundItem = new InventoryItem(item);
            stacks[addItem] = foundItem;
            foundItem.Count = numAdded;
        }
        else {
            foundItem.Count += numAdded;
        }
        return AddReference(foundItem, owner, numAdded);
    }

    public bool RemoveItem(string remItem) {
        return RemoveItemStack(remItem, 1);
    }

    public bool RemoveItemStack(string remItem, int num) {
        InventoryItem foundItem;
        if (stacks.TryGetValue(remItem, out foundItem) &&
            foundItem.Count >= num && foundItem.GetTotalRefs() > 0) {
            foundItem.Count -= num;
            if (foundItem.Count > 0) {
                stacks.Remove(remItem);
            }
            return false;
        }
        return true;
    }

    public bool RemoveItem(string remItem, int owner) {
        return RemoveItemStack(remItem, owner, 1);
    }

    public bool RemoveItemStack(string remItem, int owner, int num) {
        InventoryItem foundItem;
        if (stacks.TryGetValue(remItem, out foundItem) && foundItem.Count >= num) {
            if (foundItem.RemoveReference(owner, num)) {
                return true;
            }
            foundItem.Count -= num;
            if (foundItem.Count > 0) {
                UpdateOwnerList(owner, foundItem);
                stacks.Remove(remItem);
            }
        }
        return true;
    }


    public bool RemoveWholeItemStack(string remItem) {
        InventoryItem foundItem;
        if (stacks.TryGetValue(remItem, out foundItem) &&
            foundItem.Count > 0 && foundItem.GetTotalRefs() > 0) {
            foundItem.Count = 0;
            stacks.Remove(remItem);
            return false;
        }
        return true;
    }

    public bool RemoveWholeItemStack(string remItem, int owner) {
        InventoryItem foundItem;
        if (stacks.TryGetValue(remItem, out foundItem) &&
            foundItem.Count > 0 && foundItem.RemoveReference(owner, foundItem.Count)) {
            foundItem.Count = 0;
            UpdateOwnerList(owner, foundItem);
            stacks.Remove(remItem);
            return false;
        }
        return true;
    }

    public void UnlockItem(string item_name, T_ITEM item) {
        if (!stacks.ContainsKey(item_name)) {
            stacks[item_name] = new InventoryItem(item);
        }
    }

    public bool UnlockItem(string item_name, T_ITEM item, int owner) {
        InventoryItem foundItem;

        if (!stacks.TryGetValue(item_name, out foundItem)) {
            foundItem = new InventoryItem(item);
        }
        return AddReference(foundItem, owner);
    }

    public bool RequestItem(string item_name, int owner, out T_ITEM item, bool all = false) {
        InventoryItem foundItem;
        item = default(T_ITEM);
        if (stacks.TryGetValue(item_name, out foundItem) && foundItem.Count > 0) {
            bool result = AddReference(foundItem, owner, all ? foundItem.Count : 1);
            if (!result) {
                item = foundItem.Value;
            }
            return result;
        }
        return true;
    }

    public bool RequestItem(string item_name, int owner, out T_ITEM item, int num) {
        InventoryItem foundItem;
        item = default(T_ITEM);
        if (stacks.TryGetValue(item_name, out foundItem) && foundItem.Count >= num) {
            bool result = AddReference(foundItem, owner, num);
            if (!result) {
                item = foundItem.Value;
            }
            return result;
        }
        return true;
    }

    public bool RevokeItem(string item_name, int owner, bool all = false) {
        InventoryItem foundItem;
        if (stacks.TryGetValue(item_name, out foundItem)) {
            int num = all ? foundItem.GetRefCount(owner) : 1;
            bool result = foundItem.RemoveReference(owner, num);
            UpdateOwnerList(owner, foundItem);
            return result;
        }
        return true;
    }

    public bool RevokeItem(string item_name, int owner, int num) {
        InventoryItem foundItem;
        if (stacks.TryGetValue(item_name, out foundItem)) {
            bool result = foundItem.RemoveReference(owner, num);
            UpdateOwnerList(owner, foundItem);
            return result;
        }
        return true;
    }

    public T_ITEM GetItem(string item_name) {
        InventoryItem item;
        stacks.TryGetValue(item_name, out item);
        return item == null ? item.Value : default(T_ITEM);
    }

    public int GetStackCount() {
        return stacks.Count;
    }

    public (T_ITEM, int) GetFirstOwnedItem(int owner) {
        List<InventoryItem> itemList;
        owner_list.TryGetValue(owner, out itemList);
        if (itemList == null) {
            return (default(T_ITEM), 0);
        }
        else {
            return itemList.Count > 0 ? (itemList[0].Value, itemList[0].GetRefCount(owner)) : (default(T_ITEM), 0);
        }
    }
    public (string, T_ITEM, int) GetStackAtIndex(int index) {
        InventoryItem indexItem = stacks.Values.ElementAtOrDefault(index);
        if (indexItem != null) {
            return (stacks.Keys.ElementAtOrDefault(index), indexItem.Value, indexItem.Count);
        }
        return (null, default(T_ITEM), -1);
    }
    private bool AddReference(InventoryItem item, int owner, int count = 1) {
        Debug.Log("Making Reference to Item");
        bool result;
        int ref_count = item.GetRefCount(owner);
        result = item.SetReference(owner, ref_count + count);
        UpdateOwnerList(owner, item);
        return result;
    }



    private void UpdateOwnerList(int owner, InventoryItem item) {
        List<InventoryItem> itemList;
        Debug.Log("Updating Owner List");
        if (owner_list.TryGetValue(owner, out itemList)) {
            int index = itemList.FindIndex(a => a == item);
            if (index >= 0 && (item.Count > 0 || item.GetRefCount(owner) > 0)) {
                itemList.RemoveAt(index);
            }
            else if (item.Count > 0 && item.GetRefCount(owner) > 0) {
                itemList.Add(item);
            }
        }
        else {
            Debug.Log("Not in owner list ");
            itemList = new List<InventoryItem>();
            if (item.Count > 0 && item.GetRefCount(owner) > 0) {
                Debug.Log("Adding to owner list");
                itemList.Add(item);
                owner_list[owner] = itemList;
            }
        }
    }

    protected class InventoryItem {
        public T_ITEM Value;
        public int Count;
        private Dictionary<int, int> ref_list;
        public bool enable_logs = true;

        // Use this for initialization
        public InventoryItem(T_ITEM Value) {
            this.Value = Value;
            this.Count = 1;
            this.ref_list = new Dictionary<int, int>();
        }
        public int GetRefCount(int owner) {
            int i;
            if (ref_list.TryGetValue(owner, out i)) {
                return i;
            }
            return 0;
        }
        public int GetTotalRefs() {
            return ref_list.Count;
        }

        public bool SetReference(int owner, int count) {
            int new_total = count + ref_list.Where(reff => { return !reff.Key.Equals(owner); }).Select((reff) => reff.Value).Sum();
            Debug.Log("Count for item " + this.Count + " new total " + new_total);
            if (new_total <= this.Count) {
                ref_list[owner] = count;
                return false;
            }
            return true;
        }

        public bool SetReference(int owner) {
            return SetReference(owner, 1);
        }



        public bool RemoveReference(int owner, int count) {
            if (ref_list[owner] >= count) {
                ref_list[owner] -= count;
                if (ref_list[owner] <= 0) {
                    ref_list.Remove(owner);
                }
                return false;
            }
            return true;
        }

        public bool RemoveReference(int owner) {
            return RemoveReference(owner, 1);
        }
    }
}