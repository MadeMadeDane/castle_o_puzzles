using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(MovingGeneric))]

public class WorldItem : NetworkedBehaviour {
    private string ATTACH_TIMER;
    private Utilities utils;
    public string item_name = "";


    private ParticleSystem.EmissionModule emitter;

    // Use this for initialization
    void Awake() {
        utils = Utilities.Instance;
        ATTACH_TIMER = "Highlight_" + gameObject.GetInstanceID().ToString();
        utils.CreateTimer(ATTACH_TIMER, 0.1f).setFinished();
        ParticleSystem partSys = GetComponent<ParticleSystem>();
        if (!partSys.isPlaying) {
            partSys.Play();
        }
        emitter = partSys.emission;
    }

    // Update is called once per frame
    void Update() {
        HandleHighlight();
    }

    public void Highlight() {
        utils.ResetTimer(ATTACH_TIMER);
    }
    void HandleHighlight() {
        emitter.enabled = !utils.CheckTimer(ATTACH_TIMER);
    }
}
