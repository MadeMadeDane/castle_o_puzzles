using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartSceneLoader : MonoBehaviour {
    public string scene = "Just Platforms";
	// Use this for initialization
	void Start (){
        SceneLoader sceneLoader = GameObject.Find("StickPlayer").GetComponent<SceneLoader>();
        sceneLoader.LoadNextScene(scene);
    }
}
