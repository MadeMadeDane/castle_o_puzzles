using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public bool enable_logs = false;

    public Dictionary<string, Item> contents;
    public uint numSlots = 1;

    // Use this for initialization
    void Start()
    {
        contents = new Dictionary<string, Item>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddItem(string addItem, Item item)
    {
        if (contents.Keys.Count < numSlots) {
            if (enable_logs) {
                foreach (string s in item.ActionList.Keys) {
                    Debug.Log(s);
                }
                Debug.Log("Picking up " + addItem);
            }
            contents[addItem] = item;
        }
    }

    public void RemoveItem(string remItem)
    {

        if (enable_logs) {
            Debug.Log("Dropping " + remItem);
        }
        contents.Remove(remItem);
    }
}