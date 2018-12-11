using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseItem : Item 
{
    
    public static bool isUseItem(Item item)
    {
        return item.type == typeof(UseItem);
    }
    public UseItem() : base()
    {
        type = typeof(UseItem);
    }
}
public class NetworkUseItem
{
    public string name;
    
    public NetworkUseItem (string name)
    {
        this.name = name;
    }
}