using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonFunctions : MonoBehaviour {
    private SceneLoader sceneLoader;

    private void Awake() {
        sceneLoader = SceneLoader.Instance;
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void QuitApplictation()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        string scene = "ItemTest";
        CameraController cc = GameObject.Find("PlayerContainer").GetComponentInChildren<CameraController>();
        cc.enabled = true;
        sceneLoader.LoadNextScene(scene);
    }
}
