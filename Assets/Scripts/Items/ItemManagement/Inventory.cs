using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
using MLAPI.Serialization;

public class NetworkInventory : Inventory<NetworkSharedItem>, IBitWritable {
    public void Read(Stream stream) {
        using (PooledBitReader reader = PooledBitReader.Get(stream)) {

            int item_count = reader.ReadInt32();
            for (int i = 0; i < item_count; i++) {
                string item_name = reader.ReadString().ToString();
                int count = reader.ReadInt32();
                AddItemStack(item_name, new NetworkSharedItem(item_name), count);
            }
        }
    }
    public void Write(Stream stream) {
        using (PooledBitWriter writer = PooledBitWriter.Get(stream)) {
            writer.WriteInt32(stacks.Count);
            foreach (KeyValuePair<string, InventoryItem> kvp in stacks) {
                InventoryItem curInvItem = kvp.Value;
                if (curInvItem.Value.name != kvp.Key) {
                    Debug.LogWarning("NetworkSharedItem name did not match " + kvp.Key + " != " + curInvItem.Value.name);
                }
                writer.WriteString(kvp.Key);
                writer.WriteInt32(curInvItem.Count);
            }
        }
    }
}
public class Inventory<T_ITEM> {
    protected Dictionary<string, InventoryItem> stacks;
    public uint numSlots = 0;

    // Use this for initialization
    public Inventory() {
        stacks = new Dictionary<string, InventoryItem>();
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
                return false;
            }
            foundItem = new InventoryItem(item);
            stacks[addItem] = foundItem;
            numAdded -= 1;
        }
        foundItem.Count += numAdded;
        return true;
    }

    public bool AddItem(string addItem, T_ITEM item, int owner) {
        Debug.Log("In Add Item");
        return AddItemStack(addItem, item, owner, 1);
    }

    public bool AddItemStack(string addItem, T_ITEM item, int owner, int numAdded) {
        InventoryItem foundItem;
        if (!stacks.TryGetValue(addItem, out foundItem)) {
            if (numSlots > 0 && numAdded + stacks.Count > numSlots) {
                return false;
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
        if (!stacks.TryGetValue(remItem, out foundItem)) return true;
        if (foundItem.Count < num || foundItem.GetTotalRefs() > 0) return false;
        foundItem.Count -= num;
        if (foundItem.Count <= 0) {
            return stacks.Remove(remItem);
        }
        return true;
    }

    public bool RemoveItem(string remItem, int owner) {
        return RemoveItemStack(remItem, owner, 1);
    }

    public bool RemoveItemStack(string remItem, int owner, int num) {
        InventoryItem foundItem;
        if (!stacks.TryGetValue(remItem, out foundItem)) return true;
        if (foundItem.Count < num) return false;
        if (!foundItem.RemoveReference(owner, num)) return false;
        foundItem.Count -= num;
        if (foundItem.Count <= 0) {
            return stacks.Remove(remItem);
        }
        return true;
    }


    public bool RemoveWholeItemStack(string remItem) {
        InventoryItem foundItem;
        if (!stacks.TryGetValue(remItem, out foundItem)) return true;
        if (foundItem.GetTotalRefs() > 0) return false;
        return stacks.Remove(remItem);
    }

    public bool RemoveWholeItemStack(string remItem, int owner) {
        InventoryItem foundItem;
        if (!stacks.TryGetValue(remItem, out foundItem)) return true;
        if (!foundItem.RemoveReference(owner, foundItem.Count)) return false;
        return stacks.Remove(remItem);
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
            stacks[item_name] = foundItem;
        }
        else if (GetOwnedItems(owner).Contains(foundItem)) return true;
        return AddReference(foundItem, owner);
    }

    public bool RequestItem(string item_name, int owner, out T_ITEM item, bool all = false) {
        InventoryItem foundItem;
        item = default(T_ITEM);
        if (stacks.TryGetValue(item_name, out foundItem)) {
            bool success = AddReference(foundItem, owner, all ? foundItem.Count : 1);
            if (success) item = foundItem.Value;
            return success;
        }
        return false;
    }

    public bool RequestItem(string item_name, int owner, out T_ITEM item, int num) {
        InventoryItem foundItem;
        item = default(T_ITEM);
        if (stacks.TryGetValue(item_name, out foundItem) && foundItem.Count >= num) {
            bool success = AddReference(foundItem, owner, num);
            if (success) item = foundItem.Value;
            return success;
        }
        return false;
    }

    public bool RevokeItem(string item_name, int owner, bool all = false) {
        InventoryItem foundItem;
        if (!stacks.TryGetValue(item_name, out foundItem)) return true;
        int num = all ? foundItem.GetRefCount(owner) : 1;
        return foundItem.RemoveReference(owner, num);
    }

    public bool RevokeItem(string item_name, int owner, int num) {
        InventoryItem foundItem;
        if (!stacks.TryGetValue(item_name, out foundItem)) return true;
        return foundItem.RemoveReference(owner, num);
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
        return GetOwnedItems(owner).Select((InventoryItem it) => (it.Value, it.GetRefCount(owner))).FirstOrDefault();
    }
    public List<string> GetAllItemNames() {
        return stacks.Keys.ToList();
    }
    private bool AddReference(InventoryItem item, int owner, int count = 1) {
        return item.SetReference(owner, item.GetRefCount(owner) + count);
    }



    private IEnumerable<InventoryItem> GetOwnedItems(int owner) {
        return stacks.Values.Where((InventoryItem it) => it.ref_list.ContainsKey(owner));
    }

    protected class InventoryItem {
        public T_ITEM Value;
        public int Count;
        public Dictionary<int, int> ref_list;

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
            int new_total = count + ref_list.Where(r => !r.Key.Equals(owner)).Select((r) => r.Value).Sum();
            if (new_total <= this.Count) {
                ref_list[owner] = count;
                return true;
            }
            return false;
        }

        public bool SetReference(int owner) {
            return SetReference(owner, 1);
        }

        public bool RemoveReference(int owner, int count) {
            if (!ref_list.ContainsKey(owner)) return true;
            if (ref_list[owner] >= count) {
                ref_list[owner] -= count;
                if (ref_list[owner] <= 0) {
                    return ref_list.Remove(owner);
                }
                return true;
            }
            return false;
        }

        public bool RemoveReference(int owner) {
            return RemoveReference(owner, 1);
        }
    }
}