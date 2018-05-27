using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    [Header("Linked Components")]
    public InputManager input_manager;
    public Camera controlled_camera;
    public GameObject home;
    [Header("Camera Settings")]
    public Vector3 target_follow_distance;
    public Vector3 target_follow_angle;

    // Camera state
    private PlayerController current_player;
    private GameObject pivot;
    private Vector2 mouseAccumlator = Vector2.zero;

    // Use this for initialization
    void Start () { 
        //target_follow_distance = new Vector3(0f, 0.4f, -1f);
        //target_follow_angle = new Vector3(14f, 0f, 0f);
        pivot = new GameObject("pivot");
        PlayerController player_home = home.GetComponent<PlayerController>();
        if (player_home == null)
        {
            throw new Exception("Failed initializing camera.");
        }
        // Move pivot to target
        MovePivotToTarget(home);

        // Attach the camera to the pivot and set the default distance/angles
        transform.parent = pivot.transform;
        transform.localPosition = target_follow_distance;
        transform.localRotation = Quaternion.Euler(target_follow_angle);

        // TODO: Move this mouse hiding logic somewhere else
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
	
	// LateUpdate is called after update. Ensures we are operating on the latest transform changes.
	void LateUpdate () {
        UpdateCameraAngles();
	}

    // Rotate the camera
    private void UpdateCameraAngles()
    {
        // Accumulate the angle changes and ensure x revolves in (-360, 360) and y is clamped in (-90,90)
        mouseAccumlator += input_manager.GetMouseMotion();
        mouseAccumlator.x = mouseAccumlator.x % 360;
        mouseAccumlator.y = Mathf.Clamp(mouseAccumlator.y, -90, 90);
        // Set camera pitch
        transform.localRotation = Quaternion.AngleAxis(
            -mouseAccumlator.y, Vector3.right);
        // Set player yaw (and camera with it)
        pivot.transform.parent.transform.localRotation = Quaternion.AngleAxis(
            mouseAccumlator.x, pivot.transform.parent.transform.up);
    }

    // Jump to a game object that has a player controller
    private void MovePivotToTarget(GameObject target)
    {
        PlayerController player = target.GetComponent<PlayerController>();
        if (player == null) {
            Debug.Log("Failed to move to player");
            return;
        }
        if (current_player != null)
        {
            current_player.player_camera = null;
        }
        pivot.transform.parent = target.transform;
        pivot.transform.localPosition = Vector3.zero;
        player.player_camera = controlled_camera;
        current_player = player;
    }
}
