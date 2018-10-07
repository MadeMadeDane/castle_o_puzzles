using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartSceneLoader : MonoBehaviour {
    public string scene = "Just Platforms";
    private SceneLoader sceneLoader;
	// Use this for initialization
    private void Awake() {
        sceneLoader = SceneLoader.Instance;
    }

	void Start (){
        sceneLoader.LoadNextScene(scene);
    }
}
