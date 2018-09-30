using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseItem : Item {
    
    public static bool isUseItem(Item item)
    {
        return item.type == typeof(UseItem);
    }
}
