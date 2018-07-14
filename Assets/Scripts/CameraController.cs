using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public delegate void CameraMovementFunction();

public class CameraController : MonoBehaviour {
    [Header("Linked Components")]
    public InputManager input_manager;
    public Camera controlled_camera;
    public GameObject home;
    [Header("Camera Settings")]
    public Vector3 target_follow_distance;
    public Vector3 target_follow_angle;
    [HideInInspector]
    public GameObject yaw_pivot;
    public GameObject pitch_pivot;

    // Camera state
    private PlayerController current_player;

    private Vector2 mouseAccumlator = Vector2.zero;
    private CameraMovementFunction handleCameraMove;
    private CameraMovementFunction handlePlayerRotate;

    // Timers
    private float WallHitTimeDelta;
    private float WallHitGracePeriod;

    // Use this for initialization
    void Start () {
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 45;
        yaw_pivot = new GameObject("yaw_pivot");
        pitch_pivot = new GameObject("pitch_pivot");
        pitch_pivot.transform.parent = yaw_pivot.transform;
        PlayerController player_home = home.GetComponent<PlayerController>();
        if (player_home == null)
        {
            throw new Exception("Failed initializing camera.");
        }
        //SetShooterVars(player_home);
        SetThirdPersonActionVars(player_home);

        // TODO: Move this mouse hiding logic somewhere else
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetShooterVars(PlayerController target)
    {
        handleCameraMove = FirstPersonCameraMove;
        handlePlayerRotate = FirstPersonPlayerRotate;
        target_follow_angle = Vector3.zero;
        target_follow_distance = new Vector3(0, (target.cc.height / 2) - target.cc.radius, 0);

        if (current_player != null)
        {
            current_player.player_camera = null;
        }
        yaw_pivot.transform.parent = target.transform;
        yaw_pivot.transform.localPosition = Vector3.zero;
        target.player_camera = this;
        current_player = target;

        // Attach the camera to the yaw_pivot and set the default distance/angles
        transform.parent = yaw_pivot.transform;
        transform.localPosition = target_follow_distance;
        transform.localRotation = Quaternion.Euler(target_follow_angle);
    }

    private void SetThirdPersonActionVars(PlayerController target)
    {
        handleCameraMove = ThirdPersonCameraMove;
        handlePlayerRotate = ThirdPersonPlayerRotate;
        target_follow_angle = new Vector3(14f, 0, 0);
        target_follow_distance = new Vector3(0, (target.cc.height / 2), -target.cc.height*1.5f);

        if (current_player != null)
        {
            current_player.player_camera = null;
        }
        target.player_camera = this;
        current_player = target;
        current_player.RegisterJumpCallback(ThirdPersonJumpCallback);

        // Attach the camera to the yaw_pivot and set the default distance/angles
        transform.parent = pitch_pivot.transform;
        transform.localPosition = target_follow_distance;
        transform.localRotation = Quaternion.Euler(target_follow_angle);

        // Set timers
        WallHitGracePeriod = 0.0f;
        WallHitTimeDelta = WallHitGracePeriod;
    }

    public void ThirdPersonJumpCallback()
    {
        if ((current_player.IsWallRunning() || current_player.CanWallJump()) && !input_manager.GetCenterCameraHold())
        {
            current_player.transform.forward = Vector3.ProjectOnPlane(current_player.current_velocity, Physics.gravity).normalized;
        }
    }

    // LateUpdate is called after update. Ensures we are operating on the latest transform changes.
    void LateUpdate () {
        UpdateCameraAngles();
    }

    private void FixedUpdate()
    {
        handlePlayerRotate();
        IncrementCounters();
    }

    // TODO: Make a class for these
    private void IncrementCounters()
    {
        WallHitTimeDelta = Mathf.Clamp(WallHitTimeDelta + Time.deltaTime, 0, 2 * WallHitGracePeriod);
    }
    
    // Rotate the camera
    private void UpdateCameraAngles()
    {
        // Accumulate the angle changes and ensure x revolves in (-360, 360) and y is clamped in (-90,90)
        mouseAccumlator += input_manager.GetMouseMotion();
        mouseAccumlator.x = mouseAccumlator.x % 360;
        mouseAccumlator.y = Mathf.Clamp(mouseAccumlator.y, -90, 90);
        if (current_player != null)
        {
            handleCameraMove();
        }
    }

    private void FirstPersonCameraMove()
    {
        // Set camera pitch
        transform.localRotation = Quaternion.AngleAxis(
            -mouseAccumlator.y, Vector3.right);
        // Set player yaw (and camera with it)
        yaw_pivot.transform.parent.transform.localRotation = Quaternion.AngleAxis(
            mouseAccumlator.x, Vector3.up);
    }

    private void ThirdPersonCameraMove()
    {
        if (mouseAccumlator.x < 0)
        {
            mouseAccumlator.x = 360 + mouseAccumlator.x;
        }
        mouseAccumlator.y = Mathf.Clamp(mouseAccumlator.y, -65, 75);
        // Set camera pitch
        pitch_pivot.transform.localRotation = Quaternion.AngleAxis(
            -mouseAccumlator.y, Vector3.right);
        // Set player yaw (and camera with it)
        yaw_pivot.transform.localRotation = Quaternion.AngleAxis(
            mouseAccumlator.x, Vector3.up);

        if (input_manager.GetCenterCameraHold())
        {
            float pitch = current_player.transform.localEulerAngles.x;
            float yaw = current_player.transform.localEulerAngles.y;
            float adjusted_pitch = 360 - pitch < pitch ? 360 - pitch : -pitch;
            mouseAccumlator.x = Mathf.LerpAngle(mouseAccumlator.x, yaw, 0.1f);
            mouseAccumlator.y = Mathf.LerpAngle(mouseAccumlator.y, adjusted_pitch, 0.1f);
        }
        AvoidWalls();
    }

    private void AvoidWalls()
    {
        RaycastHit hit;
        Vector3 startpos = yaw_pivot.transform.position;
        Vector3 path = transform.position - startpos;
        if (Physics.Raycast(startpos, path.normalized, out hit, target_follow_distance.magnitude))
        {
            WallHitTimeDelta = 0;
            transform.localPosition = target_follow_distance.normalized * (hit.distance - 1f);
        }
        else if (!InWallCollision())
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, target_follow_distance, 0.1f);
        }
    }

    private bool InWallCollision()
    {
        return (WallHitTimeDelta < WallHitGracePeriod);
    }

    private void ThirdPersonShooterCameraMove()
    {
        // Set camera pitch
        pitch_pivot.transform.localRotation = Quaternion.AngleAxis(
            -mouseAccumlator.y, Vector3.right);
        // Set player yaw (and camera with it)
        yaw_pivot.transform.localRotation = Quaternion.AngleAxis(
            mouseAccumlator.x, Vector3.up);
        // Set the players yaw to match our velocity
        current_player.transform.rotation = Quaternion.Slerp(current_player.transform.rotation, yaw_pivot.transform.rotation, Mathf.Clamp(current_player.cc.velocity.magnitude / current_player.RunSpeed, 0, 1));
        yaw_pivot.transform.position = current_player.transform.position;
    }

    private void FirstPersonPlayerRotate()
    {
        return;
    }

    private void ThirdPersonPlayerRotate()
    {
        // Set the players yaw to match our velocity
        Vector3 move_vector = current_player.GetMoveVector();
        Vector3 ground_velocity = Vector3.ProjectOnPlane(current_player.cc.velocity, Physics.gravity);

        Vector3 desired_move = Vector3.zero;
        float interp_multiplier = 1f;
        //if (current_player.OnGround())
        //{
        if (ground_velocity.magnitude < current_player.RunSpeed / 3)
        {
            //Debug.Log("Controller move");
            desired_move = move_vector.normalized;
            interp_multiplier = 0.5f;
        }
        /*else if (current_player.IsWallClimbing() && current_player.CanGrabLedge())
        {
            //Debug.Log("Wall move");
            desired_move = -Vector3.ProjectOnPlane(current_player.GetLastWallNormal(), Physics.gravity).normalized;
        }*/
        else
        {
            //Debug.Log("Velocity move");
            //Vector3 new_forward = Vector3.RotateTowards(current_player.transform.forward, ground_velocity.normalized, 0.1f * current_player.cc.velocity.magnitude / current_player.RunSpeed, 1f).normalized;
            desired_move = Vector3.ProjectOnPlane(current_player.current_velocity, Physics.gravity).normalized;
        }
        //}
        if (current_player.IsWallClimbing() && current_player.CanGrabLedge())
        {
            desired_move = -Vector3.ProjectOnPlane(current_player.GetLastWallNormal(), Physics.gravity).normalized;
        }
        if (desired_move != Vector3.zero && !input_manager.GetCenterCameraHold())
        {
            current_player.transform.forward = Vector3.RotateTowards(current_player.transform.forward, desired_move, 0.1f * interp_multiplier, 1f);
        }
        yaw_pivot.transform.position = Vector3.Lerp(yaw_pivot.transform.position, current_player.transform.position, 0.025f);
    }


}
