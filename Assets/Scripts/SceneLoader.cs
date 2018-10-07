using System.Collections;
using System.Collections.Generic;

using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneLoader : UnitySingleton<SceneLoader> {
    public void LoadNextScene(string scene)
    {
        Debug.Log("Scene Name: " + scene);
        StartCoroutine(LoadAsyncNewScene(scene));
    }
    IEnumerator LoadAsyncNewScene(string scene)
    {
        Scene newScene = SceneManager.GetSceneByName(scene);
        // The Application loads the Scene in the background at the same time as the current Scene.
        if (!newScene.IsValid()) {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);

            // Wait until the last operation fully loads to return anything
            while (!asyncLoad.isDone) {
                yield return null;
            }
            newScene = SceneManager.GetSceneByName(scene);
        }
        // Move the GameObject (you attach this in the Inspector) to the newly loaded Scene
        GameObject[] objs = GameObject.FindGameObjectsWithTag(Constants.TAG_PLAYER);
        foreach (GameObject obj in objs) {
            SceneManager.MoveGameObjectToScene(obj, newScene);
        }
        // Set the current Scene to be able to unload it later
        SceneManager.SetActiveScene(newScene);
    }
    public void  LoadAdjacentScenes(Dictionary<string, Scene> adjScenes)
    {
        Dictionary<string, Scene> desiredScenes = adjScenes;
        Scene currentScene = SceneManager.GetActiveScene();
        desiredScenes[currentScene.name] = currentScene;
        Debug.Log("Scene List: " + desiredScenes.ToString() + "\n Count: " + desiredScenes.Count.ToString());
        StartCoroutine(UpdateLoadedScenes(desiredScenes));
    }
    IEnumerator UpdateLoadedScenes (Dictionary<string, Scene> adjScenes)
    {

        // Set the current Scene to be able to unload it later
        Dictionary<string, Scene> scenesToKeep = new Dictionary<string, Scene>();
        Dictionary<string, Scene> scenesToRemove = new Dictionary<string, Scene>();
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++) {
            Scene currentlyIndexedScene = SceneManager.GetSceneAt(i);
            if (!adjScenes.ContainsKey(currentlyIndexedScene.name)) {
                Debug.Log("{Removing} Scene Name: " + currentlyIndexedScene.name + "\nIndex: " + i);
                scenesToRemove[currentlyIndexedScene.name] = currentlyIndexedScene;
            } else {
                Debug.Log("{Keeping} Scene Name: " + currentlyIndexedScene.name + "\nIndex: " + i);
                scenesToKeep[currentlyIndexedScene.name] = currentlyIndexedScene;
            }
        }
        foreach (KeyValuePair<string, Scene> sceneKVP in scenesToRemove) {
            Debug.Log("Unloading " + sceneKVP.Key);
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneKVP.Value);
            // Wait until the last operation fully loads to return anything
            while (!asyncUnload.isDone) {
                yield return null;
            }
        }
        foreach (KeyValuePair<string, Scene> sceneKVP in adjScenes) {
            if (!scenesToKeep.ContainsKey(sceneKVP.Key)) {
                Debug.Log("Loading " + sceneKVP.Key);
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneKVP.Key,LoadSceneMode.Additive);
                // Wait until the last operation fully loads to return anything
                while (!asyncLoad.isDone) {
                    yield return null;
                }
            }
        }
    }
}
