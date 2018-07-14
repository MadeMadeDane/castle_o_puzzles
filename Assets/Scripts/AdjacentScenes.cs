using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdjacentScenes : MonoBehaviour {
    public List<LoadArea> loadAreas;
    private bool loadedAdjacentAreas = false;
    private void Update()
    {
        if (SceneManager.GetActiveScene() == gameObject.scene) {
            if (!loadedAdjacentAreas) {
                SceneLoader sceneLoader = GameObject.Find("StickPlayer").GetComponent<SceneLoader>();
                if (sceneLoader != null) {
                    Debug.Log("Loading Areas");
                    Dictionary<string, Scene> scenes = new Dictionary<string, Scene>();
                    foreach (LoadArea loadArea in loadAreas) {
                        Debug.Log("Getting Scene: " + loadArea.scene);
                        scenes[loadArea.scene] = SceneManager.GetSceneByName(loadArea.scene);
                    }
                    sceneLoader.LoadAdjacentScenes(scenes);
                    loadedAdjacentAreas = true;
                }
            }
        } else {
            loadedAdjacentAreas = false;
        }
    }
}
