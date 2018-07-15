using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadArea : MonoBehaviour
{

    public string scene = "SampleScene";
    void OnTriggerEnter(Collider col)
    {
        SceneLoader sceneLoader = col.gameObject.GetComponent<SceneLoader>();
        if (sceneLoader != null) {
            sceneLoader.LoadNextScene(scene);
        }
    }
}
