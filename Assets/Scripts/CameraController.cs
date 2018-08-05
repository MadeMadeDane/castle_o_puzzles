using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;



public delegate void CameraMovementFunction();

public class CameraController : MonoBehaviour {
    private static string ZOOM_TIMER = "CameraZoom";
    private static string IDLE_TIMER = "CameraIdle";

    enum ViewMode
    {
        Shooter,
        Third_Person
    }
    [Header("Linked Components")]
    public InputManager input_manager;
    public Camera controlled_camera;
    public GameObject home;
    [Header("Camera Settings")]
    public Vector3 target_follow_distance;
    public Vector3 target_follow_angle;
    public bool ManualCamera;
    [HideInInspector]
    public GameObject yaw_pivot;
    [HideInInspector]
    public GameObject pitch_pivot;

    // Camera state
    private ViewMode view_mode;
    private PlayerController current_player;

    private Vector2 mouseAccumulator = Vector2.zero;
    private Vector2 idleOrientation = Vector2.zero;
    private CameraMovementFunction handleCameraMove;
    private CameraMovementFunction handlePlayerRotate;

    // Utils
    private Utilities utils;

    // Timers
    private float WallHitTimeDelta;
    private float WallHitGracePeriod;

    // Other Settings
    private float transparency_divider;
    private float fully_translucent_threshold;
    private bool fade_texture_in_use;
    private Material opaque_material;
    public Material fade_material;
    private Mesh original_model;
    public Mesh headless_model;
    public bool show_model_in_inspection;

    // Use this for initialization
    void Start () {
        QualitySettings.vSyncCount = 0;
        // Application.targetFrameRate = 45;
        transparency_divider = 4;
        fully_translucent_threshold = 1;
        yaw_pivot = new GameObject("yaw_pivot");
        yaw_pivot.tag = "Player";
        pitch_pivot = new GameObject("pitch_pivot");
        pitch_pivot.transform.parent = yaw_pivot.transform;
        PlayerController player_home = home.GetComponent<PlayerController>();
        if (player_home == null)
        {
            throw new Exception("Failed initializing camera.");
        }
        utils = home.GetComponent<Utilities>();
        if (utils == null)
        {
            throw new Exception("Failed getting utilities.");
        }
        utils.CreateTimer(ZOOM_TIMER, 0.5f);
        utils.CreateTimer(IDLE_TIMER, 1.0f);
        //SetShooterVars(player_home);
        SetThirdPersonActionVars(player_home);
        opaque_material = home.GetComponentInChildren<SkinnedMeshRenderer>().material;
        original_model = home.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        // TODO: Move this mouse hiding logic somewhere else
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetShooterVars(PlayerController target)
    {
        view_mode = ViewMode.Shooter;
        handleCameraMove = FirstPersonCameraMove;
        handlePlayerRotate = FirstPersonPlayerRotate;
        target_follow_angle = Vector3.zero;
        target_follow_distance = new Vector3(0, target.GetHeadHeight(), 0);

        if (current_player != null)
        {
            current_player.player_camera = null;
        }
        yaw_pivot.transform.parent = target.transform;
        yaw_pivot.transform.localPosition = Vector3.zero;
        target.player_camera = this;
        current_player = target;

        // Attach the camera to the yaw_pivot and set the default distance/angles
        yaw_pivot.transform.localRotation = Quaternion.identity;
        transform.parent = yaw_pivot.transform;
        transform.localRotation = Quaternion.Euler(target_follow_angle);
    }

    private void SetThirdPersonActionVars(PlayerController target)
    {
        view_mode = ViewMode.Third_Person;
        handleCameraMove = ThirdPersonCameraMove;
        handlePlayerRotate = ThirdPersonPlayerRotate;
        target_follow_angle = new Vector3(14f, 0, 0);
        target_follow_distance = new Vector3(0, target.GetHeadHeight(), -target.cc.height*1.5f);

        if (current_player != null)
        {
            current_player.player_camera = null;
        }
        target.player_camera = this;
        current_player = target;
        current_player.RegisterJumpCallback(ThirdPersonJumpCallback);

        // Attach the camera to the yaw_pivot and set the default distance/angles
        yaw_pivot.transform.parent = null;
        transform.parent = pitch_pivot.transform;
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
    void LateUpdate ()
    {
        UpdateCameraAngles();
    }

    private void FixedUpdate()
    {
        handleViewToggle();
        hideHome();
        handlePlayerRotate();
        IncrementCounters();
    }

    private void handleViewToggle()
    {
        if (input_manager.GetToggleView()) {
            utils.ResetTimer(ZOOM_TIMER);
            if (view_mode == ViewMode.Shooter) {
                SetThirdPersonActionVars(current_player);
                if (original_model && show_model_in_inspection) {
                    SkinnedMeshRenderer[] renderers = home.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer render in renderers) {
                        render.sharedMesh = original_model;
                    }
                }
            } else if (view_mode == ViewMode.Third_Person) {
                SetShooterVars(current_player);
                if (headless_model && show_model_in_inspection) {
                    SkinnedMeshRenderer[] renderers = home.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer render in renderers) {
                        render.sharedMesh = headless_model;
                    }
                }
            }
        }
    }

    private void hideHome()
    {
        Color textureColor;
        SkinnedMeshRenderer[] renderers = home.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer render in renderers) {
            if (view_mode == ViewMode.Third_Person || !show_model_in_inspection) {
                float distance_to_head = (current_player.GetHeadHeight()*current_player.transform.up + current_player.transform.position - transform.position).magnitude;
                if (distance_to_head < transparency_divider) {
                    if (!fade_texture_in_use) {
                        fade_texture_in_use = true;
                        render.material = fade_material;
                    }
                    textureColor = render.material.color;
                    textureColor.a = distance_to_head < fully_translucent_threshold ? 0 : distance_to_head / transparency_divider;
                    render.material.color = textureColor;
                } else {
                    if (fade_texture_in_use) {
                        fade_texture_in_use = false;
                        render.material = opaque_material;
                    }
                }
            } else {
                fade_texture_in_use = false;
                render.material = opaque_material;
            }
        }
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
        Vector2 mouse_input = input_manager.GetMouseMotion();
        if (mouse_input != Vector2.zero)
        {
            utils.ResetTimer(IDLE_TIMER);
            idleOrientation = mouseAccumulator;
        }
        mouseAccumulator += mouse_input;
        mouseAccumulator.x = mouseAccumulator.x % 360;
        mouseAccumulator.y = Mathf.Clamp(mouseAccumulator.y, -90, 90);
        if (current_player != null)
        {
            handleCameraMove();
        }
    }

    private void FirstPersonCameraMove()
    {
        // Set camera pitch
        transform.localRotation = Quaternion.AngleAxis(
            -mouseAccumulator.y, Vector3.right);
        // Set player yaw (and camera with it)
        yaw_pivot.transform.parent.transform.localRotation = Quaternion.AngleAxis(
            mouseAccumulator.x, Vector3.up);
    }

    private void ThirdPersonCameraMove()
    {
        if (mouseAccumulator.x < 0)
        {
            mouseAccumulator.x = 360 + mouseAccumulator.x;
        }
        mouseAccumulator.y = Mathf.Clamp(mouseAccumulator.y, -65, 75);
        // set the pitch pivots pitch
        pitch_pivot.transform.localRotation = Quaternion.AngleAxis(
            -mouseAccumulator.y, Vector3.right);
        // set the yaw pivots yaw
        yaw_pivot.transform.localRotation = Quaternion.AngleAxis(
            mouseAccumulator.x, Vector3.up);

        if (input_manager.GetCenterCameraHold())
        {
            utils.ResetTimer(IDLE_TIMER);
            Vector2 orientation = EulerToMouseAccum(current_player.transform.eulerAngles);
            mouseAccumulator.x = Mathf.LerpAngle(mouseAccumulator.x, orientation.x, 0.1f);
            mouseAccumulator.y = Mathf.LerpAngle(mouseAccumulator.y, orientation.y, 0.1f);
            idleOrientation = mouseAccumulator;
        }
        else if (input_manager.GetCenterCameraRelease())
        {
            utils.SetTimerFinished(IDLE_TIMER);
            Vector2 orientation = EulerToMouseAccum(current_player.transform.eulerAngles);
            if (Mathf.Abs(Mathf.DeltaAngle(orientation.x, mouseAccumulator.x)) > 15f)
            {
                idleOrientation = mouseAccumulator = EulerToMouseAccum(current_player.transform.eulerAngles);
            }
        }

        FollowPlayerVelocity();
        AvoidWalls();
    }

    private void FollowPlayerVelocity()
    {
        Vector3 player_ground_vel = Vector3.ProjectOnPlane(current_player.cc.velocity, current_player.transform.up);
        if (player_ground_vel.normalized != Vector3.zero)
        {
            Quaternion velocity_angle = Quaternion.LookRotation(player_ground_vel.normalized, current_player.transform.up);
            idleOrientation = EulerToMouseAccum(velocity_angle.eulerAngles);
        }
    }

    private Vector2 EulerToMouseAccum(Vector3 euler_angle)
    {
        float pitch = euler_angle.x;
        float yaw = euler_angle.y;
        float adjusted_pitch = 360 - pitch < pitch ? 360 - pitch : -pitch;
        return new Vector2(yaw, adjusted_pitch);
    }

    private void AvoidWalls()
    {
        RaycastHit hit;
        Vector3 startpos = current_player.transform.position;
        Vector3 world_target_vec = transform.TransformVector(Quaternion.Inverse(transform.localRotation) * target_follow_distance);
        Vector3 path = (yaw_pivot.transform.position + world_target_vec - startpos);
        //Debug.DrawRay(startpos, path.normalized*(path.magnitude+1f), Color.green);
        
        if (Physics.Raycast(startpos, path.normalized, out hit, path.magnitude + 1f))
        {
            WallHitTimeDelta = 0;
            Vector3 pivot_hit = (hit.point - yaw_pivot.transform.position);
            // Ignore hits that are too far away
            if (pivot_hit.magnitude > target_follow_distance.magnitude + 1f)
            {
                //Debug.Log("Too far hit");
                transform.localPosition = Vector3.Lerp(transform.localPosition, target_follow_distance, 0.1f);
            }
            // Very close hits should move the camera to a predefined location
            else if (hit.distance < 1f)
            {
                //Debug.Log("Too close hit");
                if (pitch_pivot.transform.localRotation.x < 0)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, (Quaternion.Inverse(pitch_pivot.transform.localRotation) * Vector3.up * target_follow_distance.y), 0.1f);
                }
                else
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, (Vector3.up * target_follow_distance.y), 0.1f);
                }
            }
            // Otherwise move the camrea to where the hit is, minus an offset
            else
            {
                //Debug.Log("Wall hit");
                float horizontal_displacement = Vector3.Dot(pivot_hit, pitch_pivot.transform.forward);
                if (pitch_pivot.transform.localRotation.x < 0)
                {
                    transform.localPosition = (Mathf.Sign(horizontal_displacement) * (Mathf.Abs(horizontal_displacement) - 1f) * Vector3.forward) + (Quaternion.Inverse(pitch_pivot.transform.localRotation) * Vector3.up * target_follow_distance.y);
                }
                else
                {
                    transform.localPosition = (Mathf.Sign(horizontal_displacement) * (Mathf.Abs(horizontal_displacement) - 1f) * Vector3.forward) + (Vector3.up * target_follow_distance.y);
                }
                transform.localPosition += transform.InverseTransformDirection(hit.normal) * controlled_camera.rect.width / 2;
            }
        }
        else
        {
            //Debug.Log("No hit");
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
            -mouseAccumulator.y, Vector3.right);
        // Set player yaw (and camera with it)
        yaw_pivot.transform.localRotation = Quaternion.AngleAxis(
            mouseAccumulator.x, Vector3.up);
        // Set the players yaw to match our velocity
        current_player.transform.rotation = Quaternion.Slerp(current_player.transform.rotation, yaw_pivot.transform.rotation, Mathf.Clamp(current_player.cc.velocity.magnitude / current_player.RunSpeed, 0, 1));
        yaw_pivot.transform.position = current_player.transform.position;
    }

    private void FirstPersonPlayerRotate()
    {
        if (!utils.CheckTimer(ZOOM_TIMER))
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, target_follow_distance, utils.GetTimerPercent(ZOOM_TIMER));
        }
        else
        {
            transform.localPosition = target_follow_distance;
        }
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
        AvoidWalls();
        if (utils.CheckTimer(IDLE_TIMER))
        {
            RotateTowardIdleOrientation();
        }
    }

    private void RotateTowardIdleOrientation()
    {
        if (ManualCamera)
        {
            return;
        }
        if (!current_player.IsHanging())
        {
            Vector3 player_ground_vel = Vector3.ProjectOnPlane(current_player.cc.velocity, current_player.transform.up);
            float lerp_factor = Mathf.Max(player_ground_vel.magnitude / current_player.RunSpeed, 0.2f);
            mouseAccumulator.x = Mathf.LerpAngle(mouseAccumulator.x, idleOrientation.x, 0.005f * lerp_factor);
            mouseAccumulator.y = Mathf.LerpAngle(mouseAccumulator.y, idleOrientation.y, 0.005f * lerp_factor);
        }
        else
        {
            idleOrientation = mouseAccumulator;
        }
    }

}
