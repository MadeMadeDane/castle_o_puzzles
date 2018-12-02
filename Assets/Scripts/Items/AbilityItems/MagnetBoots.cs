using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetBoots : AbilityItem {

    private InputManager im;
    public static string item_name = "MagnetBoots";
    // Use this for initialization
        
    void toggle_mute(string buttons)
    {
        AudioSource audio = physical_obj.gameObject.GetComponent<AudioSource>();
        audio.mute = !audio.mute;
    }

    public override void Start()
    {
        im = InputManager.Instance;
        outputLogs("Added Actions to Magnet Boots");
    }

    public override void Update()
    {
        if (saycheck()) {
            log();
        }
    }

    bool saycheck()
    {
        bool ret = im.GetUseItem();
        return ret;
    }

    void log()
    {
        Debug.Log("Stuff to print from the item: I am the Gereudo King");
    }

    public override string GetName()
    {
        return MagnetBoots.item_name;
    }
}
