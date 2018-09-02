using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

delegate void AccelerationFunction(Vector3 direction, float desiredSpeed, float acceleration, bool grounded);

public class PlayerController : MonoBehaviour {
    [Header("Linked Components")]
    public GameObject player_container;
    public InputManager input_manager;
    public CharacterController cc;
    public Collider WallRunCollider;
    [HideInInspector]
    public CameraController player_camera;
    [Header("Movement constants")]
    public float RunSpeed;
    public float AirSpeed;
    public float MaxAirSpeed;
    public float GroundAcceleration;
    public float AirAcceleration;
    public float SpeedDamp;
    public float AirSpeedDamp;
    public float SlideSpeed;
    public float DownGravityAdd;
    public float JumpVelocity;
    public float WallJumpThreshold;
    public float WallJumpBoost;
    public float WallRunLimit;
    public float WallJumpSpeed;
    public float WallRunJumpBoostSpeed;
    public float WallRunJumpBoostAdd;
    public float WallRunJumpSpeed;
    public float WallRunJumpUpSpeed;
    public float JumpBoostSpeed;
    public float JumpBoostRequiredSpeed;
    public float JumpBoostAdd;
    public Vector3 StartPos;
    [Header("Movement toggles")]
    public bool wallRunEnabled;
    public bool wallJumpEnabled;
    public bool wallClimbEnabled;
    public bool conserveUpwardMomentum;
    public bool ShortHopEnabled;
    public bool JumpBoostEnabled;
    [HideInInspector]
    public Vector3 current_velocity;
    public bool debug_mode = false;

    // Utils
    private Utilities utils;

    // Timers
    private string JUMP_METER = "JumpMeter";
    private string LANDING_TIMER = "Landing";
    private string BUFFER_JUMP_TIMER = "BufferJump";
    private string SLIDE_TIMER = "Slide";
    private string WALL_JUMP_TIMER = "WallJump";
    private string WALL_RUN_TIMER = "WallRun";
    private string WALL_CLIMB_TIMER = "WallClimb";
    private string REGRAB_TIMER = "ReGrab";
    private string MOVING_COLLIDER_TIMER = "MovingCollider";
    private string MOVING_PLATFORM_TIMER = "MovingPlatform";
    private string MOVING_INTERIOR_TIMER = "MovingInterior";
    private string STUCK_TIMER = "Stuck";

    // Jumping state variables
    private float JumpMeterThreshold;
    private float JumpMeterComputed;
    private bool isHanging;
    private bool isJumping;
    private bool isFalling;
    private bool willJump;

    // Wall related variables
    private Vector3 WallJumpReflect;
    private Vector3 PreviousWallJumpPos;
    private Vector3 PreviousWallNormal;
    private Vector3 PreviousWallJumpNormal;
    private Vector3 WallAxis;
    private Vector3 AlongWallVel;
    private Vector3 UpWallVel;
    private float WallRunImpulse;
    private float WallRunSpeed;
    private float WallRunClimbCosAngle;
    private float WallScanDistance;
    private float LedgeClimbOffset;
    private float LedgeClimbBoost;
    private float WallDistanceThreshold;

    // Physics state variables
    private AccelerationFunction accelerate;
    private Vector3 moving_frame_velocity;
    private Vector3 accel;
    private ControllerColliderHit lastHit;
    private MovingGeneric lastMovingPlatform;
    private Collider lastMovingTrigger;
    private Collider lastTrigger;
    private ControllerColliderHit currentHit;
    private float GravityMult;

    // Other variables
    private Queue<float> error_accum;
    private float total_error;
    private int error_accum_size;
    private int error_bucket;
    private int error_threshold;
    private int error_stage;
    private int position_history_size;
    private LinkedList<Vector3> position_history;
    private static HashSet<Action> jump_callback_table = new HashSet<Action>();

    private GameObject debugcanvas;
    private Dictionary<string, Text> debugtext;

    // Use this for initialization
    private void Start () {
        utils = GetComponent<Utilities>();
        if (utils == null)
        {
            throw new Exception("Failed getting utilities.");
        }
        if (debug_mode)
        {
            EnableDebug();
        }
        // Movement values
        //SetShooterVars();
        SetThirdPersonActionVars();

        isJumping = false;
        isFalling = false;
        willJump = false;
        // Wall related vars
        WallAxis = Vector3.zero;
        AlongWallVel = Vector3.zero;
        UpWallVel = Vector3.zero;
        WallJumpReflect = Vector3.zero;
        PreviousWallJumpPos = Vector3.positiveInfinity;
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        LedgeClimbOffset = 0.5f;
        WallScanDistance = 1.5f;
        LedgeClimbBoost = Mathf.Sqrt(2 * cc.height * 1.1f * Physics.gravity.magnitude);
        WallDistanceThreshold = 14f;

        // Timers
        utils.CreateTimer(JUMP_METER, 0.3f);
        JumpMeterThreshold = utils.GetTimerPeriod(JUMP_METER) / 3;
        JumpMeterComputed = utils.GetTimerPercent(JUMP_METER);
        utils.CreateTimer(LANDING_TIMER, 0.1f).setFinished();
        utils.CreateTimer(BUFFER_JUMP_TIMER, 0.1f).setFinished();
        utils.CreateTimer(SLIDE_TIMER, 0.2f).setFinished();
        utils.CreateTimer(WALL_JUMP_TIMER, 0.2f).setFinished();
        utils.CreateTimer(WALL_RUN_TIMER, 0.2f).setFinished();
        utils.CreateTimer(WALL_CLIMB_TIMER, 0.2f).setFinished();
        utils.CreateTimer(REGRAB_TIMER, 0.5f).setFinished();
        utils.CreateTimer(MOVING_COLLIDER_TIMER, 0.01f).setFinished();
        utils.CreateTimer(MOVING_PLATFORM_TIMER, 0.1f).setFinished();
        utils.CreateTimer(MOVING_INTERIOR_TIMER, 0.1f).setFinished();
        utils.CreateTimer(STUCK_TIMER, 0.2f).setFinished();

        // Initial state
        position_history_size = 50;
        position_history = new LinkedList<Vector3>();
        total_error = 0f;
        error_threshold = 50;
        error_stage = 0;
        error_bucket = 0;
        error_accum_size = 10;
        error_accum = new Queue<float>(Enumerable.Repeat<float>(0f, error_accum_size));
        moving_frame_velocity = Vector3.zero;
        current_velocity = Vector3.zero;
        currentHit = new ControllerColliderHit();
        StartPos = transform.position;

        // TODO: Test below
        //cc.enableOverlapRecovery = false;
        Physics.IgnoreCollision(WallRunCollider, cc);
    }

    private void EnableDebug()
    {
        debugcanvas = new GameObject("Canvas", typeof(Canvas));
        debugcanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        debugcanvas.AddComponent<CanvasScaler>();
        debugcanvas.AddComponent<GraphicRaycaster>();
        debugtext = new Dictionary<string, Text>()
        {
            {"JumpMeter", new GameObject().AddComponent<Text>() },
            {"LandingTimeDelta", new GameObject().AddComponent<Text>() },
            {"BufferJumpTimeDelta", new GameObject().AddComponent<Text>() },
            {"SlideTimeDelta", new GameObject().AddComponent<Text>() },
            {"WallJumpTimeDelta", new GameObject().AddComponent<Text>() },
            {"WallRunTimeDelta", new GameObject().AddComponent<Text>() },
            {"WallClimbTimeDelta", new GameObject().AddComponent<Text>() },
            {"ReGrabTimeDelta", new GameObject().AddComponent<Text>() },
            {"MovingColliderTimeDelta", new GameObject().AddComponent<Text>() },
            {"MovingPlatformTimeDelta", new GameObject().AddComponent<Text>() },
            {"StuckTimeDelta", new GameObject().AddComponent<Text>() },
            {"Current Velocity", new GameObject().AddComponent<Text>() }
        };
        int idx = 0;
        Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        foreach (KeyValuePair<string, Text> item in debugtext)
        {
            item.Value.gameObject.name = item.Key;
            RectTransform rect = item.Value.gameObject.GetComponent<RectTransform>();
            rect.SetParent(debugcanvas.transform);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 220f);
            rect.anchoredPosition3D = new Vector3(-250f, -15f * idx, 0);
            item.Value.font = ArialFont;
            idx++;
        }
    }

    private void SetShooterVars()
    {
        // Movement modifiers
        RunSpeed = 15f;
        AirSpeed = 0.90f;
        MaxAirSpeed = Mathf.Infinity;
        GroundAcceleration = 20;
        AirAcceleration = 500;
        SpeedDamp = 10f;
        AirSpeedDamp = 0.01f;
        SlideSpeed = 18f;
        // Gravity modifiers
        DownGravityAdd = 0;
        // Jump/Wall modifiers
        JumpVelocity = 12f;
        JumpBoostSpeed = 12f;
        JumpBoostRequiredSpeed = 12f;
        JumpBoostAdd = 0f;
        WallJumpThreshold = 8f;
        WallJumpBoost = 1.0f;
        WallJumpSpeed = 12f;
        WallRunLimit = 8f;
        WallRunJumpBoostSpeed = 12f;
        WallRunJumpBoostAdd = 0f;
        WallRunJumpSpeed = 15f;
        WallRunJumpUpSpeed = 12f;
        WallRunImpulse = 0.0f;
        WallRunSpeed = 15.0f;
        WallRunClimbCosAngle = Mathf.Cos(Mathf.Deg2Rad*30f);
        // Toggles
        conserveUpwardMomentum = true;
        wallJumpEnabled = true;
        wallRunEnabled = true;
        wallClimbEnabled = true;
        ShortHopEnabled = false;
        JumpBoostEnabled = false;
        // Delegates
        accelerate = AccelerateCPM;
    }

    private void SetThirdPersonActionVars()
    {
        // Movement modifiers
        RunSpeed = 15f;
        AirSpeed = 15f;
        MaxAirSpeed = 5f;
        GroundAcceleration = 300f;
        AirAcceleration = 30f;
        SpeedDamp = 10f;
        AirSpeedDamp = 0.01f;
        SlideSpeed = 22f;
        // Gravity modifiers
        DownGravityAdd = 0;
        // Jump/Wall modifiers
        JumpVelocity = 16f;
        JumpBoostSpeed = 18f;
        JumpBoostRequiredSpeed = 12f;
        JumpBoostAdd = 10f;
        WallJumpThreshold = 8f;
        WallJumpBoost = 1.0f;
        WallJumpSpeed = 12f;
        WallRunLimit = 8f;
        WallRunJumpBoostSpeed = 20f;
        WallRunJumpBoostAdd = 10f;
        WallRunJumpSpeed = 12f;
        WallRunJumpUpSpeed = 12f;
        WallRunImpulse = 0.0f;
        WallRunSpeed = 15f;
        WallRunClimbCosAngle = Mathf.Cos(Mathf.Deg2Rad*30f);
        // Toggles
        conserveUpwardMomentum = false;
        wallJumpEnabled = true;
        wallRunEnabled = false;
        wallClimbEnabled = true;
        ShortHopEnabled = true;
        JumpBoostEnabled = true;
        // Delegates
        accelerate = AccelerateStandard;
    }

    private void Update()
    {
        if (input_manager.GetJump())
        {
            utils.ResetTimer(BUFFER_JUMP_TIMER);
        }
    }

    private void LateUpdate()
    {
        Vector3 old_yaw_pivot_pos = player_camera.yaw_pivot.transform.position;
        player_container.transform.position = transform.position;
        transform.localPosition = Vector3.zero;
        player_container.transform.rotation = Quaternion.identity;
        player_camera.yaw_pivot.transform.position = old_yaw_pivot_pos;
    }

    // Fixed Update is called once per physics tick
    private void FixedUpdate () {
        // If the player does not have a camera, do nothing
        if (player_camera == null)
        {
            return;
        }
        // Get starting values
        GravityMult = 1;
        accel = Vector3.zero;
        if (WallDistanceCheck())
        {
            JumpMeterComputed = utils.GetTimerPercent(JUMP_METER);
        }
        else
        {
            JumpMeterComputed = 0;
        }

        ProcessHits();
        ProcessTriggers();
        HandleMovement();
        HandleJumping();
        UpdatePlayerState();
        if (debug_mode)
        {
            IncrementCounters();
        }
    }

    private void UpdatePlayerState()
    {
        // Update character state based on desired movement
        if (!OnGround())
        {
            accel += Physics.gravity * GravityMult;
        }
        else
        {
            // Push the character controller into the normal of the surface
            // This should trigger ground detection
            accel += -Mathf.Sign(currentHit.normal.y) * Physics.gravity.magnitude * currentHit.normal;
        }

        current_velocity += accel * Time.deltaTime;

        Vector3 previous_position = transform.position;
        bool failed_move = false;
        try
        {
            cc.Move(ComputeMove(current_velocity * Time.deltaTime));
        }
        catch (Exception ex)
        {
            Debug.Log("Failed to move: " + ex.ToString());
            failed_move = true;
        }

        float error = cc.velocity.magnitude - current_velocity.magnitude;
        total_error += error - error_accum.Dequeue();
        error_accum.Enqueue(error);
        // We are too far off from the real velocity. This mainly happens when trying to move into colliders.
        // Nothing will significantly change if we reset the velocity here, so use this time to resync it.
        if (total_error < -50.0f)
        {
            current_velocity = cc.velocity;
            //Debug.Log("Total error: " + total_error.ToString());
        }

        // Unity is too far off from what the velocity should be
        // This is bandaid-ing a bug in the unity character controller where moving into certain
        // edges will cause the character to teleport extreme distances, sometimes crashing the game.
        if (error > 100.0f || failed_move)
        {
            Debug.Log("Unity error: " + error.ToString());
            cc.SimpleMove(-cc.velocity);
            Teleport(previous_position);
            error_bucket++;
        }
        else
        {
            if (error_bucket > 0)
            {
                error_bucket--;
                // If the frame didn't have an error, we are probably safe now. Reset to no error stage.
                error_stage = 0;
            }
        }

        // If we have a large number of errors in a row, we are probably stuck.
        // Try to resolve this in a series of stages.
        if (error_bucket >= error_threshold && !IsStuck())
        {
            Debug.Log("Lots of error!");
            Debug.Log("Previous position: " + previous_position.ToString());
            Debug.Log("Current position: " + transform.position.ToString());
            Debug.Log("Current cc velocity: " + cc.velocity.magnitude.ToString());
            Debug.Log("Current velocity: " + current_velocity.magnitude.ToString());
            Debug.Log("Velocity error: " + (current_velocity - cc.velocity).ToString());
            error_stage++;
            Debug.Log("Attempting error resolution stage " + error_stage.ToString());
            switch (error_stage)
            {
                case 1:
                    Teleport(previous_position - (cc.velocity * Time.deltaTime));
                    break;
                case 2:
                    Debug.Log("Last resort!");
                    error_stage = 0;
                    break;
            }
            error_bucket = 0;
        }

        // Add to history of not stuck positions
        if (!IsStuck())
        {
            if (position_history.Count == position_history_size)
            {
                position_history.RemoveLast();
            }
            position_history.AddFirst(transform.position);
        }
    }

    private Vector3 ComputeMove(Vector3 desired_move)
    {
        Vector3 ground_move = Vector3.ProjectOnPlane(desired_move, transform.up);
        if (ground_move.normalized == Vector3.zero)
        {
            return desired_move;
        }
        ground_move = ground_move.normalized * cc.radius;
        Vector3 ground_move_alt = Vector3.Cross(ground_move, transform.up);

        Vector3 SkinPos = transform.position + (ground_move.normalized * (cc.radius + cc.skinWidth));
        Vector3 SkinPosAlt = transform.position + (ground_move_alt.normalized * (cc.radius + cc.skinWidth));
        Vector3 SkinPosAltN = transform.position + (-ground_move_alt.normalized * (cc.radius + cc.skinWidth));
        Ray[] scanrays = new Ray[12] {
            new Ray(SkinPos, ground_move),
            new Ray(SkinPosAlt, ground_move_alt),
            new Ray(SkinPosAltN, -ground_move_alt),
            new Ray(SkinPos + transform.up*GetHeadHeight(), ground_move),
            new Ray(SkinPosAlt + transform.up*GetHeadHeight(), ground_move_alt),
            new Ray(SkinPosAltN + transform.up*GetHeadHeight(), -ground_move_alt),
            new Ray(SkinPos + transform.up*cc.height/4, ground_move),
            new Ray(SkinPosAlt + transform.up*cc.height/4, ground_move_alt),
            new Ray(SkinPosAltN + transform.up*cc.height/4, -ground_move_alt),
            new Ray(SkinPos - transform.up*cc.height/4, ground_move),
            new Ray(SkinPosAlt - transform.up*cc.height/4, ground_move_alt),
            new Ray(SkinPosAltN - transform.up*cc.height/4, -ground_move_alt)
        };
        RaycastHit hit;

        //Debug.DrawRay(SkinPos + transform.up * cc.height / 2, ground_move, Color.blue);
        //Debug.DrawRay(SkinPosAlt + transform.up * cc.height / 2, ground_move_alt, Color.blue);
        //Debug.DrawRay(SkinPosAltN + transform.up * cc.height / 2, -ground_move_alt, Color.blue);
        foreach (Ray scanray in scanrays) {
            if (Physics.Raycast(scanray, out hit, ground_move.magnitude))
            {
                //Debug.DrawRay(SkinPos + transform.up * cc.height / 2, Vector3.ProjectOnPlane(ground_move, hit.normal).normalized, Color.green);
                float cosanglehit = Vector3.Dot(hit.normal, scanray.direction.normalized);
                if (Vector3.Dot(desired_move, hit.normal) < 0 && ((hit.distance + cc.radius) * Mathf.Abs(cosanglehit) < cc.radius * 1.1f))
                {
                    //Debug.DrawRay(SkinPos + transform.up * cc.height / 2, hit.normal, Color.red);
                    current_velocity = Vector3.ProjectOnPlane(current_velocity, hit.normal);
                    if (!OnGround() && IsWall(hit.normal)) {
                        UpdateWallConditions(hit.normal);
                    }
                    return Vector3.ProjectOnPlane(desired_move, hit.normal);
                }
            }
        }
        return desired_move;
    }
    
    private void IncrementCounters()
    {
        if (debugcanvas != null)
        {
            debugtext["JumpMeter"].text = "JumpMeter: " + utils.GetTimerTime(JUMP_METER).ToString("0.0000");
            debugtext["LandingTimeDelta"].text = "LandingTimeDelta: " + utils.GetTimerTime(LANDING_TIMER).ToString("0.0000");
            debugtext["BufferJumpTimeDelta"].text = "BufferJumpTimeDelta: " + utils.GetTimerTime(BUFFER_JUMP_TIMER).ToString("0.0000");
            debugtext["SlideTimeDelta"].text = "SlideTimeDelta: " + utils.GetTimerTime(SLIDE_TIMER).ToString("0.0000");
            debugtext["WallJumpTimeDelta"].text = "WallJumpTimeDelta: " + utils.GetTimerTime(WALL_JUMP_TIMER).ToString("0.0000");
            debugtext["WallRunTimeDelta"].text = "WallRunTimeDelta: " + utils.GetTimerTime(WALL_RUN_TIMER).ToString("0.0000");
            debugtext["WallClimbTimeDelta"].text = "WallClimbTimeDelta: " + utils.GetTimerTime(WALL_CLIMB_TIMER).ToString("0.0000");
            debugtext["ReGrabTimeDelta"].text = "ReGrabTimeDelta: " + utils.GetTimerTime(REGRAB_TIMER).ToString("0.0000");
            debugtext["MovingColliderTimeDelta"].text = "MovingColliderTimeDelta: " + utils.GetTimerTime(MOVING_COLLIDER_TIMER).ToString("0.0000");
            debugtext["MovingPlatformTimeDelta"].text = "MovingPlatformTimeDelta: " + utils.GetTimerTime(MOVING_PLATFORM_TIMER).ToString("0.0000");
            debugtext["StuckTimeDelta"].text = "StuckTimeDelta: " + utils.GetTimerTime(STUCK_TIMER).ToString("0.0000");
            debugtext["Current Velocity"].text = "Current Velocity: " + current_velocity.ToString("0.00");

        }
    }

    private void ProcessTriggers()
    {
        if (lastTrigger == null)
        {
            return;
        }

        MovingGeneric moving_obj = lastTrigger.GetComponent<MovingCollider>();
        if (moving_obj != null)
        {
            if (player_container.transform.parent != moving_obj.transform)
            {
                player_container.transform.parent = moving_obj.transform;
                // Keep custom velocity globally accurate
                current_velocity -= moving_obj.player_velocity;
            }
            lastMovingPlatform = moving_obj;
            utils.ResetTimer(MOVING_COLLIDER_TIMER);
        }

        if (!OnGround())
        {
            RaycastHit hit;
            Boolean hit_wall = false;
            if (IsWallRunning())
            {
                // Scan toward the wall normal
                if (Physics.Raycast(transform.position, -PreviousWallNormal, out hit, cc.radius + WallScanDistance))
                {
                    hit_wall = true;
                }
            }
            else if (IsWallClimbing())
            {
                // Scan toward the wall normal at both head and stomach height
                if (Physics.Raycast(transform.position, -PreviousWallNormal, out hit, cc.radius + WallScanDistance))
                {
                    hit_wall = true;
                }
                else if (Physics.Raycast(transform.position + (transform.up * GetHeadHeight()), -PreviousWallNormal, out hit, cc.radius + WallScanDistance))
                {
                    hit_wall = true;
                }
            }
            else
            {
                // Scan forward and sideways to find all walls
                if (Physics.Raycast(transform.position, transform.right, out hit, cc.radius + WallScanDistance))
                {
                    if (IsWall(hit.normal))
                    {
                        UpdateWallConditions(hit.normal);
                    }
                }
                if (Physics.Raycast(transform.position, -transform.right, out hit, cc.radius + WallScanDistance))
                {
                    if (IsWall(hit.normal))
                    {
                        UpdateWallConditions(hit.normal);
                    }
                }
                if (Physics.Raycast(transform.position, transform.forward, out hit, cc.radius + WallScanDistance))
                {
                    if (IsWall(hit.normal))
                    {
                        UpdateWallConditions(hit.normal);
                    }
                }
                if (Physics.Raycast(transform.position + (transform.up * GetHeadHeight()), transform.forward, out hit, cc.radius + WallScanDistance))
                {
                    if (IsWall(hit.normal))
                    {
                        UpdateWallConditions(hit.normal);
                    }
                }
            }

            if (hit_wall && IsWall(hit.normal)) 
            {
                UpdateWallConditions(hit.normal);
            }

            if (IsWallClimbing() && !isHanging)
            {
                // Make sure our head is against a wall
                if (Physics.Raycast(transform.position + (transform.up * GetHeadHeight()), -PreviousWallNormal, out hit, cc.radius + WallScanDistance))
                {
                    Vector3 LedgeScanVerticalPos = transform.position + (transform.up * cc.height / 2);
                    Vector3 LedgeScanHorizontalVector = (cc.radius + LedgeClimbOffset) * transform.forward;
                    // Make sure we don't hit a wall at the ledge height
                    if (!Physics.Raycast(origin: LedgeScanVerticalPos, direction: LedgeScanHorizontalVector.normalized, maxDistance: LedgeScanHorizontalVector.magnitude))
                    {
                        Vector3 LedgeScanPos = LedgeScanVerticalPos + LedgeScanHorizontalVector;
                        // Scan down for a ledge
                        if (Physics.Raycast(LedgeScanPos, -transform.up, out hit, cc.radius + LedgeClimbOffset))
                        {
                            if (CanGrabLedge() && Vector3.Dot(hit.normal, Physics.gravity) < -0.866f)
                            {
                                isHanging = true;
                            }
                        }
                    }
                }
            }
        }

        lastTrigger = null;
    }

    private bool IsWall(Vector3 normal)
    {
        return normal.y > -0.17f && normal.y <= 0.34f;
    }

    private void UpdateWallConditions(Vector3 wall_normal)
    {
        if (Vector3.Dot(GetGroundVelocity(), wall_normal) < -WallJumpThreshold)
        {
            // Are we jumping in a new direction (atleast 20 degrees difference)
            if (Vector3.Dot(PreviousWallJumpNormal, wall_normal) < 0.94f)
            {
                utils.ResetTimer(WALL_JUMP_TIMER);
                WallJumpReflect = Vector3.Reflect(current_velocity, wall_normal);
                if (JumpBuffered())
                {
                    // Buffer a jump
                    willJump = true;
                }
            }
        }
        WallAxis = Vector3.Cross(wall_normal, Physics.gravity).normalized;
        // Use cc.velocity for velocity along wall for higher accuracy
        AlongWallVel = Vector3.Dot(cc.velocity, WallAxis) * WallAxis;
        UpWallVel = current_velocity - (Vector3.Dot(current_velocity, WallAxis) * WallAxis);
        // First attempt a wall run if we pass the limit and are looking along the wall
        if (wallRunEnabled && AlongWallVel.magnitude > WallRunLimit && Mathf.Abs(Vector3.Dot(wall_normal, transform.forward)) < WallRunClimbCosAngle && Vector3.Dot(AlongWallVel, transform.forward) > 0)
        {
            if (IsWallRunning() || CanWallRun(PreviousWallJumpPos, PreviousWallNormal, transform.position, wall_normal))
            {
                // Get a small boost on new wall runs. Also prevent spamming wall boosts
                if (!IsWallRunning() && WallDistanceCheck())
                {
                    if (AlongWallVel.magnitude < WallRunSpeed)
                    {
                        current_velocity = UpWallVel + Mathf.Sign(Vector3.Dot(current_velocity, WallAxis)) * WallRunSpeed * WallAxis;
                    }
                    current_velocity.y = Math.Max(current_velocity.y + WallRunImpulse, WallRunImpulse);
                }
                utils.ResetTimer(WALL_RUN_TIMER);
            }
        }
        // If we fail the wall run try to wall climb instead if we are looking at the wall
        else if (isHanging || Vector3.Dot(transform.forward, -wall_normal) >= WallRunClimbCosAngle)
        {
            utils.ResetTimer(WALL_CLIMB_TIMER);
        }
        PreviousWallNormal = wall_normal;
    }

    private bool CanWallRun(Vector3 old_wall_pos, Vector3 old_wall_normal, Vector3 new_wall_pos, Vector3 new_wall_normal)
    {
        if (!wallRunEnabled)
        {
            return false;
        }
        bool wall_normal_check = Vector3.Dot(old_wall_normal, new_wall_normal) < 0.94f;
        if (old_wall_pos == Vector3.positiveInfinity)
        {
            return wall_normal_check;
        }
        // Allow wall running on the same normal if we move to a new position not along the wall
        else
        {
            return wall_normal_check || (WallDistanceCheck() && Mathf.Abs(Vector3.Dot((new_wall_pos - old_wall_pos).normalized, old_wall_normal)) > 0.34f);
        }
    }

    private bool WallDistanceCheck()
    {
        float horizontal_distance_sqr = Vector3.ProjectOnPlane(PreviousWallJumpPos - transform.position, Physics.gravity).sqrMagnitude;
        return float.IsNaN(horizontal_distance_sqr) || horizontal_distance_sqr > WallDistanceThreshold;
    }

    private void ProcessHits()
    {
        if (lastHit == null)
        {
            return;
        }
        // Save the most recent last hit
        currentHit = lastHit;

        if (currentHit.normal.y > 0.6f)
        {
            ProcessFloorHit();
        }
        else if (currentHit.normal.y > 0.34f)
        {
            ProcessSlideHit();
        }
        else if (currentHit.normal.y > -0.17f)
        {
            ProcessWallHit();
        }
        else
        {
            ProcessCeilingHit();
        }
        // Keep velocity in the direction of the plane if the plane is not a ceiling
        // Or if it is a ceiling only cancel out the velocity if we are moving fast enough into its normal
        if (Vector3.Dot(currentHit.normal, Physics.gravity) < 0 || Vector3.Dot(current_velocity, currentHit.normal) < -1f)
        {
            current_velocity = Vector3.ProjectOnPlane(current_velocity, currentHit.normal);
        }

        if (currentHit.gameObject.tag == "Respawn")
        {
            Teleport(StartPos);
        }
        // Set last hit null so we don't process it again
        lastHit = null;
    }

    private void ProcessFloorHit()
    {
        // On the ground
        utils.ResetTimer(LANDING_TIMER);

        // Handle buffered jumps
        if (JumpBuffered())
        {
            // Buffer a jump
            willJump = true;
        }
        MovingGeneric moving_platform = lastHit.gameObject.GetComponent<MovingGeneric>();
        if (moving_platform != null)
        {
            if (player_container.transform.parent != moving_platform.transform)
            {
                player_container.transform.parent = moving_platform.transform;
                // Keep custom velocity globally accurate
                current_velocity -= moving_platform.player_velocity;
            }
            lastMovingPlatform = moving_platform;
            utils.ResetTimer(MOVING_PLATFORM_TIMER);
        }
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        PreviousWallJumpPos = Vector3.positiveInfinity;
    }

    private void ProcessSlideHit()
    {
        // Slides
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        PreviousWallJumpPos = Vector3.positiveInfinity;
    }

    private void ProcessWallHit()
    {
        if (!OnGround())
        {
            UpdateWallConditions(currentHit.normal);
        }
    }

    private void ProcessCeilingHit()
    {
        // Overhang
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        PreviousWallJumpPos = Vector3.positiveInfinity;
    }

    // Apply movement forces from input (FAST edition)
    private void HandleMovement()
    {
        // Check if we can still be hanging
        if (!IsWallClimbing())
        {
            isHanging = false;
        }
        // Handle moving collisions
        HandleMovingCollisions();

        // If we are hanging stay still
        if (isHanging)
        {
            current_velocity = Vector3.zero;
            GravityMult = 0;
            return;
        }

        Vector3 planevelocity = Vector3.ProjectOnPlane(current_velocity, currentHit.normal);
        Vector3 movVec = GetMoveVector();
        float movmag = movVec.magnitude < 0.8f ? movVec.magnitude < 0.2f ? 0f : movVec.magnitude : 1f;
        movmag = Mathf.Pow(movmag, 2f);
        // Do this first so we cancel out incremented time from update before checking it
        if (!OnGround())
        {
            // We are in the air (for atleast LandingGracePeriod). We will slide on landing if moving fast enough.
            utils.ResetTimer(SLIDE_TIMER);
        }
        // Normal ground behavior
        if (OnGround() && !willJump && (!IsSliding() || planevelocity.magnitude < SlideSpeed))
        {
            // If we weren't fast enough we aren't going to slide
            utils.SetTimerFinished(SLIDE_TIMER);
            // dot(new_movVec, normal) = 0 --> dot(movVec, normal) + dot(up, normal)*k = 0 --> k = -dot(movVec, normal)/dot(up, normal)
            float slope_correction = -Vector3.Dot(movVec, currentHit.normal) / Vector3.Dot(transform.up, currentHit.normal);
            if (slope_correction < 0f)
            {
                movVec += slope_correction * transform.up;
            }
            // Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), movVec, Color.green);
            accelerate(movVec, RunSpeed*movmag, GroundAcceleration, true);
        }
        // We are either in the air, buffering a jump, or sliding (recent contact with ground). Use air accel.
        else
        {
            // Handle wall movement
            if (IsWallRunning())
            {
                float away_from_wall_speed = Vector3.Dot(current_velocity, PreviousWallNormal);
                // Only remove velocity if we are attempting to move away from the wall
                if (away_from_wall_speed > 0)
                {
                    // Remove the component of the wall normal velocity that is along the gravity axis
                    float gravity_resist = Vector3.Dot(away_from_wall_speed * PreviousWallNormal, Physics.gravity.normalized);
                    float previous_velocity_mag = current_velocity.magnitude;
                    current_velocity -= (away_from_wall_speed * PreviousWallNormal - gravity_resist * Physics.gravity.normalized);
                    // consider adding a portion of the lost velocity back along the wall axis
                    current_velocity += WallAxis * Mathf.Sign(Vector3.Dot(current_velocity, WallAxis)) * (previous_velocity_mag - current_velocity.magnitude);
                }
                if (Vector3.Dot(UpWallVel, Physics.gravity) >= 0)
                {
                    GravityMult = 0.25f;
                }
            }
            accelerate(movVec, AirSpeed * movmag, AirAcceleration, false);
        }
    }

    private void HandleMovingCollisions()
    {
        if (!InMovingCollision() && !OnMovingPlatform() && !InMovingInterior())
        {
            moving_frame_velocity = Vector3.zero;
            if (player_container.transform.parent != null)
            {
                player_container.transform.parent = null;

                // Inherit velocity from previous platform
                if (lastMovingPlatform != null)
                {
                    // Keep custom velocity globally accurate
                    current_velocity += lastMovingPlatform.player_velocity;
                }
            }
            lastMovingPlatform = null;
        }
        else if (InMovingCollision() && !OnMovingPlatform() && !InMovingInterior())
        {
            moving_frame_velocity = lastMovingPlatform.player_velocity;
        }
        else if (InMovingInterior() || OnMovingPlatform())
        {
            moving_frame_velocity = Vector3.zero;
        }
    }

    // Try to accelerate to the desired speed in the direction specified
    private void AccelerateCPM(Vector3 direction, float desiredSpeed, float acceleration, bool grounded)
    {
        if (!grounded && IsWallRunning())
        {
            return;
        }
        direction.Normalize();
        float moveAxisSpeed = Vector3.Dot(current_velocity, direction);
        float deltaSpeed = desiredSpeed - moveAxisSpeed;
        if (deltaSpeed < 0)
        {
            // Gotta go fast
            return;
        }

        // Scale acceleration by speed because we want to go fast
        deltaSpeed = Mathf.Clamp(acceleration * Time.deltaTime * desiredSpeed, 0, deltaSpeed);
        current_velocity += deltaSpeed * direction;

        if (grounded)
        {
            accel += -(current_velocity + moving_frame_velocity) * SpeedDamp;
        }
        else
        {
            accel += -Vector3.ProjectOnPlane(current_velocity + moving_frame_velocity, Physics.gravity) * AirSpeedDamp;
        }

    }

    // Regular acceleration
    private void AccelerateStandard(Vector3 direction, float desiredSpeed, float acceleration, bool grounded)
    {
        if (!grounded && IsWallRunning())
        {
            return;
        }
        direction.Normalize();
        /*float turn_constant = 1f;
        if (grounded)
        {
            turn_constant = 0.55f + Mathf.Sign(Vector3.Dot(current_velocity.normalized, direction))*Mathf.Pow(Vector3.Dot(current_velocity.normalized, direction), 2f) * 0.45f;
        }*/
        Vector3 deltaVel = direction * acceleration * Time.deltaTime;
        if (grounded)
        {
            // Accelerate if we aren't at the desired speed
            if (Vector3.ProjectOnPlane(current_velocity + deltaVel, Physics.gravity).magnitude <= desiredSpeed)
            {
                current_velocity += deltaVel;
            }
            accel += -Vector3.ProjectOnPlane(current_velocity + moving_frame_velocity, currentHit.normal) * SpeedDamp;
        }
        else
        {
            if (Vector3.ProjectOnPlane(current_velocity + deltaVel, Physics.gravity).magnitude <= Mathf.Max(GetGroundVelocity().magnitude, MaxAirSpeed))
            {
                current_velocity += deltaVel;
            }
            else
            {
                current_velocity = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(current_velocity + deltaVel, Physics.gravity), GetGroundVelocity().magnitude) + (current_velocity - GetGroundVelocity());
                //current_velocity = current_velocity - (deltaVel.magnitude * GetGroundVelocity().normalized) + deltaVel;
            }
            accel += -Vector3.ProjectOnPlane(current_velocity + moving_frame_velocity, Physics.gravity) * AirSpeedDamp;
        }
        //Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), deltaVel, Color.cyan, Time.fixedDeltaTime);
        //Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), current_velocity, Color.red, Time.fixedDeltaTime);
        //Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), accel, Color.blue, Time.fixedDeltaTime);
    }

    public Vector3 GetMoveVector()
    {
        return (input_manager.GetMoveVertical() * player_camera.yaw_pivot.transform.forward +
                input_manager.GetMoveHorizontal() * player_camera.yaw_pivot.transform.right);
    }

    // Double check if on ground using a separate test
    public bool OnGround()
    {
        return !utils.CheckTimer(LANDING_TIMER);
    }

    public bool JumpBuffered()
    {
        return !utils.CheckTimer(BUFFER_JUMP_TIMER);
    }

    public bool IsSliding()
    {
        return !utils.CheckTimer(SLIDE_TIMER);
    }

    public bool CanWallJump()
    {
        return wallJumpEnabled && !utils.CheckTimer(WALL_JUMP_TIMER);
    }

    public bool IsWallRunning()
    {
        return wallRunEnabled && !utils.CheckTimer(WALL_RUN_TIMER);
    }

    public bool IsWallClimbing()
    {
        return wallClimbEnabled && !utils.CheckTimer(WALL_CLIMB_TIMER);
    }

    public bool CanGrabLedge()
    {
        return wallClimbEnabled && utils.CheckTimer(REGRAB_TIMER);
    }

    public bool IsHanging()
    {
        return isHanging;
    }

    public bool InMovingCollision()
    {
        return !utils.CheckTimer(MOVING_COLLIDER_TIMER);
    }

    public bool OnMovingPlatform()
    {
        return !utils.CheckTimer(MOVING_PLATFORM_TIMER);
    }

    public bool InMovingInterior()
    {
        return !utils.CheckTimer(MOVING_INTERIOR_TIMER);
    }

    public void StayInMovingInterior()
    {
        utils.ResetTimer(MOVING_INTERIOR_TIMER);
    }

    public void ExitMovingInterior()
    {
        utils.SetTimerFinished(MOVING_INTERIOR_TIMER);
    }

    public bool IsStuck()
    {
        return !utils.CheckTimer(STUCK_TIMER);
    }

    public Vector3 GetLastWallNormal()
    {
        return PreviousWallNormal;
    }

    public Vector3 GetGroundVelocity()
    {
        return Vector3.ProjectOnPlane(current_velocity, Physics.gravity);
    }

    public float GetHeadHeight()
    {
        return ((cc.height / 2) - cc.radius);
    }

    public bool CanJumpBoost()
    {
        bool can_jump_boost = true;
        // Ground behavior
        if (OnGround() || (JumpBuffered() && !CanWallJump()))
        {
            Vector3 ground_vel = GetGroundVelocity();
            can_jump_boost &= (ground_vel.magnitude > JumpBoostRequiredSpeed);
            can_jump_boost &= (Vector3.Dot(GetMoveVector(), ground_vel.normalized) > 0.7f);
        }
        else
        {
            can_jump_boost = false;
        }
        return can_jump_boost;
    }

    // Handle jumping
    private void HandleJumping()
    {
        // Ground detection for friction and jump state
        if (OnGround())
        {
            isJumping = false;
            isFalling = false;
        }

        // Add additional gravity when going down (optional)
        if (current_velocity.y < 0)
        {
            GravityMult += DownGravityAdd;
        }

        // Handle jumping and falling
        if (JumpBuffered())
        {
            if (OnGround() || CanWallJump() || IsWallRunning() || isHanging)
            {
                DoJump();
            }
        }
        // Fall fast when we let go of jump (optional)
        if (!isFalling && isJumping && !input_manager.GetJumpHold())
        {
            if (ShortHopEnabled && Vector3.Dot(current_velocity, Physics.gravity.normalized) < 0)
            {
                current_velocity -= Vector3.Project(current_velocity, Physics.gravity.normalized) / 2;
            }
            isFalling = true;
        }
    }

    // Set the player to a jumping state
    private void DoJump()
    {
        if (CanWallJump() || IsWallRunning())
        {
            PreviousWallJumpPos = transform.position;
            PreviousWallJumpNormal = PreviousWallNormal;
        }
        if (!isHanging && utils.GetTimerTime(JUMP_METER) > JumpMeterThreshold)
        {
            if (CanWallJump() && WallJumpReflect.magnitude > 0)
            {
                //Debug.Log("Wall Jump");
                current_velocity += (WallJumpReflect - current_velocity) * WallJumpBoost * JumpMeterComputed;
                if (conserveUpwardMomentum)
                {
                    current_velocity.y = Math.Max(current_velocity.y + WallJumpSpeed * JumpMeterComputed, WallJumpSpeed * JumpMeterComputed);
                }
                else
                {
                    current_velocity.y = Math.Max(current_velocity.y, WallJumpSpeed * JumpMeterComputed);
                }
                utils.ResetTimer(JUMP_METER);
            }
            else if (IsWallRunning())
            {
                //Debug.Log("Wall Run Jump");
                current_velocity += PreviousWallNormal * WallRunJumpSpeed * JumpMeterComputed;
                float pathvel = Vector3.Dot(current_velocity, transform.forward);
                float newspeed = Mathf.Clamp(pathvel + (WallRunJumpBoostAdd * JumpMeterComputed), 0f, WallRunJumpBoostSpeed);
                current_velocity += transform.forward * (newspeed - pathvel);
                current_velocity.y = Math.Max(current_velocity.y, WallRunJumpUpSpeed * JumpMeterComputed);
                utils.ResetTimer(JUMP_METER);
            }
            else if (OnGround())
            {
                //Debug.Log("Upward Jump");
                if (conserveUpwardMomentum)
                {
                    current_velocity.y = Math.Max(current_velocity.y + JumpVelocity * JumpMeterComputed, JumpVelocity * JumpMeterComputed);
                }
                else
                {
                    current_velocity.y = Math.Max(current_velocity.y, JumpVelocity * JumpMeterComputed);
                }
            }
            if (JumpBoostEnabled && CanJumpBoost())
            {
                Vector3 movvec = GetMoveVector().normalized;
                float pathvel = Vector3.Dot(current_velocity, movvec);
                float newspeed = Mathf.Clamp(pathvel + JumpBoostAdd, 0f, JumpBoostSpeed);
                current_velocity += (Mathf.Max(newspeed, pathvel) - pathvel) * movvec;
            }
            foreach (Action callback in jump_callback_table)
            {
                callback();
            }
        }
        else if (isHanging)
        {
            current_velocity.y = LedgeClimbBoost;
            PreviousWallNormal = Vector3.zero;
            PreviousWallJumpNormal = Vector3.zero;
            PreviousWallJumpPos = Vector3.positiveInfinity;
        }
        utils.ResetTimer(REGRAB_TIMER);
        isJumping = true;
        isFalling = false;
        willJump = false;
        isHanging = false;

        // Intentionally set the timers over the limit
        utils.SetTimerFinished(BUFFER_JUMP_TIMER);
        utils.SetTimerFinished(LANDING_TIMER);
        utils.SetTimerFinished(WALL_JUMP_TIMER);
        utils.SetTimerFinished(WALL_RUN_TIMER);
        utils.SetTimerFinished(WALL_CLIMB_TIMER);
        utils.SetTimerFinished(MOVING_PLATFORM_TIMER);

        WallJumpReflect = Vector3.zero;
    }

    public void RegisterJumpCallback(Action callback)
    {
        jump_callback_table.Add(callback);
    }

    public void UnregisterJumpCallback(Action callback)
    {
        jump_callback_table.Remove(callback);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.isTrigger)
        {
            lastTrigger = other;
        }
    }

    // Handle collisions on player move
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        lastHit = hit;
    }

    private void Teleport(Vector3 position)
    {
        foreach (Collider col in GetComponents<Collider>())
        {
            col.enabled = false;
        }
        transform.position = position;
        foreach (Collider col in GetComponents<Collider>())
        {
            col.enabled = true;
        }
    }

    public void Recover(Collider other)
    {
        utils.ResetTimer(STUCK_TIMER);
        isHanging = false;

        Vector3 closest_point = other.ClosestPointOnBounds(transform.position);
        Vector3 path_to_point = closest_point - transform.position;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, path_to_point, out hit, path_to_point.magnitude * 1.5f))
        {
            Teleport(transform.position + hit.normal * cc.radius);
        }
        else if (Physics.Raycast(transform.position + 2*path_to_point, -path_to_point, out hit, path_to_point.magnitude * 1.5f))
        {
            Teleport(transform.position + hit.normal * cc.radius);
        }
        else if (position_history.Count > 0)
        {
            Teleport(position_history.First.Value);
            position_history.RemoveFirst();
        }
        else
        {
            Teleport(StartPos);
        }
    }
}
