using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class AbilityItem : Item {
    public static bool isAbilityItem(Item item) {
        return item.type == typeof(AbilityItem);
    }
    public AbilityItem(NetworkedBehaviour context = null) : base(context) {
        type = typeof(AbilityItem);
    }
}
