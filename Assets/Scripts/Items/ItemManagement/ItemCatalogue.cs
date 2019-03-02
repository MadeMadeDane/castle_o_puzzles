using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class ItemCatalogue {
    private static Dictionary<string, ItemCatalogueEntry> items = new Dictionary<string, ItemCatalogueEntry>() {
        {new MagnetBoots().name(), new ItemCatalogueEntry(typeof(MagnetBoots), typeof(AbilityItem), "", "")},
        {new BoltCutters().name(), new ItemCatalogueEntry(typeof(BoltCutters), typeof(SharedItem), "", "")},
        {new GoldKey().name(), new ItemCatalogueEntry(typeof(GoldKey), typeof(SharedItem), "", "")},
        {new Tankard().name(), new ItemCatalogueEntry(typeof(Tankard), typeof(SharedItem), "", "")}
    };
    public static Item RequestItem(string item_name) {
        Item requested_item = Activator.CreateInstance(items[item_name].type) as Item;
        requested_item.physical_form = Resources.Load<GameObject>(item_name);
        return requested_item;
    }
}
public class ItemCatalogueEntry {
    public Type type;
    public string physical_form_path = "";
    public string menu_form_path = "";
    public ItemCatalogueEntry(Type t, Type ext, string phys, string menu) {
        type = t;
        physical_form_path = phys;
        menu_form_path = menu;
    }
}
