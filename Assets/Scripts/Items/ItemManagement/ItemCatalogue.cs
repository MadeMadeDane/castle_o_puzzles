using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCatalogue {
    Dictionary<string, ItemCatalogueEntry> items;
    public ItemCatalogue ()
    {
        items = new Dictionary<string, ItemCatalogueEntry>();
        items.Add(MagnetBoots.item_name, new ItemCatalogueEntry(typeof(MagnetBoots), typeof(AbilityItem), "", ""));
        items.Add(BoltCutters.item_name, new ItemCatalogueEntry(typeof(BoltCutters), typeof(UseItem), "", ""));
    
    }
    public Item RequestItem (string item_name)
    {
        Item requested_item = Activator.CreateInstance(items[item_name].type) as Item;
        //requested_item.physical_form = AssetBundle.LoadFromFile()
        return requested_item;
    }
}
public class ItemCatalogueEntry
{
    public Type type;
    public string physical_form_path = "";
    public string menu_form_path = "";
    public ItemCatalogueEntry (Type t, Type  ext, string phys, string menu)
    {
        type = t;
        physical_form_path = phys;
        menu_form_path = menu;
    }
}
