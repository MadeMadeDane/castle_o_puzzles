using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public bool enable_logs = false;

    public Dictionary<string, Item> contents;
    public uint numItems;
    public uint numSlots = 1;

    // Use this for initialization
    void Start()
    {
        contents = new Dictionary<string, Item>();
        numItems = 0;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddItem(string addItem, Item item)
    {
        if (numItems < numSlots) {
            if (enable_logs) {
                foreach (string s in item.ActionList.Keys) {
                    Debug.Log(s);
                }
            }
            contents.Add(addItem, item);
            numItems++;
        }
    }

    public void RemoveItem(string remItem)
    {
        contents.Remove(remItem);
        numItems--;
    } 
}
