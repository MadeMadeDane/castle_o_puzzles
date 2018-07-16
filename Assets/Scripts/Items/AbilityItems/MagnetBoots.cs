using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetBoots : Item {

    public bool enable_logs = false; 
	// Use this for initialization
	void Start () {
        enable_logs = true;
        this.ActionList = new Dictionary<string, PerformAction>();
        this.ActionList.Add("use", use);
        this.ActionList.Add("say", log);
        this.ActionList.Add("toggle_mute", toggle_mute);
        foreach(string s in this.ActionList.Keys) {
            outputLogs(s);
        }
        outputLogs("Added Actions to Magnet Boots");
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    void use (string buttons) {
        log(buttons);
        toggle_mute(buttons);
    }
    void log (string buttons) {
        outputLogs("Stuff to print from the item: I am the Gereudo King");
    }
    void toggle_mute(string buttons)
    {
        AudioSource audio = gameObject.GetComponent<AudioSource>();
        audio.mute = !audio.mute;
    }
    void outputLogs (string msg) {
        if (enable_logs)
            Debug.Log(msg);
    }
}
