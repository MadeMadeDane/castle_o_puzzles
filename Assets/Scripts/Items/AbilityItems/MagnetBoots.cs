using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetBoots : AbilityItem {

    public override string name() => "MagnetBoots";

    private InputManager im;
    // Use this for initialization

    void toggle_mute(string buttons) {
        AudioSource audio = physical_obj.gameObject.GetComponent<AudioSource>();
        audio.mute = !audio.mute;
    }

    public override void Start() {
        im = InputManager.Instance;
        outputLogs("Added Actions to Magnet Boots");
    }

    public override void Update() {
        if (saycheck()) {
            log();
        }
    }

    bool saycheck() {
        bool ret = im.GetSharedItem();
        return ret;
    }

    void log() {
        Debug.Log("Stuff to print from the item: I am the Gereudo King");
    }
}
