using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(MovingGeneric))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkedObject))]
public class WorldItem : NetworkedBehaviour {
    private string ATTACH_TIMER;
    private Utilities utils;
    public string item_name = "";
    public new Collider collider;


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
        collider = GetComponent<Collider>();
        emitter = partSys.emission;
        RegisterWithWorldItemTracker();
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
    public override void OnDestroyed() {
        UnregisterWithWorldItemTracker();
    }
    void RegisterWithWorldItemTracker() {
        WorldItemTracker.Instance.RegisterItem(this);
    }
    void UnregisterWithWorldItemTracker() {
        if (WorldItemTracker.Instance == null) return;
        WorldItemTracker.Instance.UnregisterItem(this);
    }
}
