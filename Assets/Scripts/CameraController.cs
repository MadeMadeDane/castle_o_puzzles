using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    [Header("Parenting")]
    public Camera controlled_camera;
    public GameObject home;
    [Header("Camera Settings")]
    public Vector3 target_follow_distance;
    public Vector3 target_follow_angle;
    public Vector2 mouse_sensitivity;
    public float mouse_multiplier;
    public Vector2 controller_sensitivity;
    public float controller_multiplier;

    private PlayerController current_player;
    private GameObject pivot;

    private Queue<Vector2> mouseQueue;
    private Vector2 mouseQueueAvg = Vector2.zero;
    private int mouseQueueCount = 4;
    private Vector2 mouseAccumlator = Vector2.zero;

    // Use this for initialization
    void Start () { 
        mouseQueue = new Queue<Vector2>(Enumerable.Repeat<Vector2>(Vector2.zero, mouseQueueCount));
        mouse_multiplier = 100;
        controller_multiplier = 50;
        mouse_sensitivity = 2*Vector2.one;
        controller_sensitivity = new Vector2(6, 3);
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
        // Handle both mouse and gamepad at the same time
        Vector2 rotVecM = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")) * mouse_sensitivity * mouse_multiplier * Time.deltaTime;
        Vector2 rotVecC = new Vector2(
            Input.GetAxisRaw("Joy X"),
            Input.GetAxisRaw("Joy Y")) * controller_sensitivity * controller_multiplier * Time.deltaTime;
        Vector2 rotVec = rotVecM + rotVecC;

        // Use rolling average for mouse smoothing (unity sucks at mouse input)
        mouseQueueAvg = mouseQueueAvg + ((rotVec - mouseQueue.Dequeue()) / mouseQueueCount);
        mouseQueue.Enqueue(rotVec);

        // Accumulate the angle changes and ensure x revolves in (-360, 360) and y is clamped in (-90,90)
        mouseAccumlator += mouseQueueAvg;
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
