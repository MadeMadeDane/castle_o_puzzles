using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldItem : MonoBehaviour {
    private string ATTACH_TIMER;
    private Utilities utils;
    public string item_name = "";

    private ParticleSystem.EmissionModule emitter;


    // Use this for initialization
    void Start() {
        utils = Utilities.Instance;
        ATTACH_TIMER = "Highlight_" + gameObject.GetInstanceID().ToString();
        utils.CreateTimer(ATTACH_TIMER, 0.5f).setFinished();
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

    #region Hightliting
    public void Highlight() {
        utils.ResetTimer(ATTACH_TIMER);
    }
    void HandleHighlight() {
        emitter.enabled = !utils.CheckTimer(ATTACH_TIMER);
    }
    #endregion
}
