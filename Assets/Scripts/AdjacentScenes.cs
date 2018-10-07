using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdjacentScenes : MonoBehaviour {
    private SceneLoader sceneLoader;

    private void Awake() {
        sceneLoader = SceneLoader.Instance;
    }

    public List<LoadArea> loadAreas;
    private bool loadedAdjacentAreas = false;
    private void Update()
    {
        if (SceneManager.GetActiveScene() == gameObject.scene) {
            if (!loadedAdjacentAreas) {
                Debug.Log("Loading Areas");
                Dictionary<string, Scene> scenes = new Dictionary<string, Scene>();
                foreach (LoadArea loadArea in loadAreas) {
                    Debug.Log("Getting Scene: " + loadArea.scene);
                    scenes[loadArea.scene] = SceneManager.GetSceneByName(loadArea.scene);
                }
                sceneLoader.LoadAdjacentScenes(scenes);
                loadedAdjacentAreas = true;
            }
        } else {
            loadedAdjacentAreas = false;
        }
    }
}
