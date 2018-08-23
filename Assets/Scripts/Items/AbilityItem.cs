using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityItem : Item {
    public static bool isAbilityItem(Item item)
    {
        return item.type == typeof(AbilityItem);
    }
    public AbilityItem() : base()
    {
        type = typeof(AbilityItem);
    }
}
