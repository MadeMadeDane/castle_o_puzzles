using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class WorldItemTracker : UnitySingleton<WorldItemTracker> {
    public HashSet<WorldItem> worldItems;
    void Awake() {
        worldItems = new HashSet<WorldItem>();
    }
    public void RegisterItem(WorldItem item) {
        if (item != null) {
            worldItems.Add(item);
        }
    }
    public void UnregisterItem(WorldItem item) {
        if (item != null) {
            worldItems.Remove(item);
        }
    }
}
