using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadArea : MonoBehaviour
{
    private SceneLoader sceneLoader;

    private void Awake() {
        sceneLoader = SceneLoader.Instance;
    }

    public string scene = "SampleScene";
    void OnTriggerEnter(Collider col)
    {
        sceneLoader.LoadNextScene(scene);
    }
}
