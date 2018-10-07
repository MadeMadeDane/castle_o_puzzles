using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory<T>
{
    public bool enable_logs = true;

    public Dictionary<string, T> contents;
    public uint numSlots = 1;

    // Use this for initialization
    public Inventory()
    {
        contents = new Dictionary<string, T>();
    }
    

    public T AddItem(string addItem, T item)
    {
        if (contents.Keys.Count < numSlots) {
            if (enable_logs) {
                Debug.Log("Picking up " + addItem);
            }
            contents[addItem] = item;
            return item;
        }
        return default(T);
    }

    public void RemoveItem(string remItem)
    {

        if (enable_logs) {
            Debug.Log("Dropping " + remItem);
        }
        contents.Remove(remItem);
    }
}