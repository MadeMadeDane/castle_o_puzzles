using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

delegate void AccelerationFunction(Vector3 direction, float desiredSpeed, float acceleration, bool grounded);

public class PlayerController : MonoBehaviour {
    [Header("Linked Components")]
    public InputManager input_manager;
    public CharacterController cc;
    public Collider WallRunCollider;
    [HideInInspector]
    public CameraController player_camera;
    [Header("Movement constants")]
    public float RunSpeed;
    public float AirSpeed;
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

    // Jumping state variables
    private float JumpMeterSize;
    private float JumpMeterThreshold;
    private float JumpMeter;
    private float SlideGracePeriod;
    private float SlideTimeDelta;
    private bool isHanging;
    private bool isJumping;
    private bool isFalling;
    private bool willJump;
    private float LandingTimeDelta;
    private float jumpGracePeriod;
    private float BufferJumpTimeDelta;
    private float BufferJumpGracePeriod;
    private float WallJumpTimeDelta;
    private float WallJumpGracePeriod;
    private float WallRunTimeDelta;
    private float WallRunGracePeriod;
    private float WallClimbTimeDelta;
    private float WallClimbGracePeriod;
    private float ReGrabTimeDelta;
    private float ReGrabGracePeriod;
    private float MovingColliderTimeDelta;
    private float MovingColliderGracePeriod;
    private float MovingPlatformTimeDelta;
    private float MovingPlatformGracePeriod;
    private float StuckTimeDelta;
    private float StuckGracePeriod;

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
    private float JumpMeterComputed;
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
        //EnableDebug();
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
        LedgeClimbOffset = 1.0f;
        LedgeClimbBoost = Mathf.Sqrt(2 * cc.height * 1.1f * Physics.gravity.magnitude);
        WallDistanceThreshold = 14f;
        // Timers
        JumpMeter = JumpMeterSize;
        JumpMeterComputed = JumpMeter / JumpMeterSize;
        LandingTimeDelta = jumpGracePeriod;
        BufferJumpTimeDelta = BufferJumpGracePeriod;
        SlideTimeDelta = SlideGracePeriod;
        WallJumpTimeDelta = WallJumpGracePeriod;
        WallRunTimeDelta = WallRunGracePeriod;
        WallClimbTimeDelta = WallClimbGracePeriod;
        ReGrabTimeDelta = ReGrabGracePeriod;
        MovingColliderTimeDelta = MovingColliderGracePeriod;
        MovingPlatformTimeDelta = MovingPlatformGracePeriod;
        StuckTimeDelta = StuckGracePeriod;

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
        // Timings
        JumpMeterSize = 0.3f;
        JumpMeterThreshold = JumpMeterSize / 3;
        jumpGracePeriod = 0.1f;
        BufferJumpGracePeriod = 0.1f;
        SlideGracePeriod = 0.2f;
        WallJumpGracePeriod = 0.2f;
        WallRunGracePeriod = 0.2f;
        WallClimbGracePeriod = 0.2f;
        ReGrabGracePeriod = 0.5f;
        MovingColliderGracePeriod = 0.01f;
        MovingPlatformGracePeriod = 0.1f;
        StuckGracePeriod = 0.2f;
    }

    private void SetThirdPersonActionVars()
    {
        // Movement modifiers
        RunSpeed = 15f;
        AirSpeed = 15f;
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
        // Timings
        JumpMeterSize = 0.3f;
        JumpMeterThreshold = JumpMeterSize / 3;
        jumpGracePeriod = 0.1f;
        BufferJumpGracePeriod = 0.1f;
        SlideGracePeriod = 0.2f;
        WallJumpGracePeriod = 0.1f;
        WallRunGracePeriod = 0.2f;
        WallClimbGracePeriod = 0.2f;
        ReGrabGracePeriod = 0.5f;
        MovingColliderGracePeriod = 0.01f;
        MovingPlatformGracePeriod = 0.1f;
        StuckGracePeriod = 0.2f;
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
            JumpMeterComputed = JumpMeter / JumpMeterSize;
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
        IncrementCounters();
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
            cc.Move(current_velocity * Time.deltaTime);
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

    // TODO: Make a class/struct/macro for all of these
    private void IncrementCounters()
    {
        JumpMeter = Mathf.Clamp(JumpMeter + Time.deltaTime, 0, JumpMeterSize);
        LandingTimeDelta = Mathf.Clamp(LandingTimeDelta + Time.deltaTime, 0, 2 * jumpGracePeriod);
        SlideTimeDelta = Mathf.Clamp(SlideTimeDelta + Time.deltaTime, 0, 2 * SlideGracePeriod);
        BufferJumpTimeDelta = Mathf.Clamp(BufferJumpTimeDelta + Time.deltaTime, 0, 2 * BufferJumpGracePeriod);
        WallJumpTimeDelta = Mathf.Clamp(WallJumpTimeDelta + Time.deltaTime, 0, 2 * WallJumpGracePeriod);
        WallRunTimeDelta = Mathf.Clamp(WallRunTimeDelta + Time.deltaTime, 0, 2 * WallRunGracePeriod);
        WallClimbTimeDelta = Mathf.Clamp(WallClimbTimeDelta + Time.deltaTime, 0, 2 * WallClimbGracePeriod);
        ReGrabTimeDelta = Mathf.Clamp(ReGrabTimeDelta + Time.deltaTime, 0, 2 * ReGrabGracePeriod);
        MovingColliderTimeDelta = Mathf.Clamp(MovingColliderTimeDelta + Time.deltaTime, 0, 2 * MovingColliderGracePeriod);
        MovingPlatformTimeDelta = Mathf.Clamp(MovingPlatformTimeDelta + Time.deltaTime, 0, 2 * MovingPlatformGracePeriod);
        StuckTimeDelta = Mathf.Clamp(StuckTimeDelta + Time.deltaTime, 0, 2 * StuckGracePeriod);
        if (debugcanvas != null)
        {
            debugtext["JumpMeter"].text = "JumpMeter: " + JumpMeter.ToString("0.0000");
            debugtext["LandingTimeDelta"].text = "LandingTimeDelta: " + LandingTimeDelta.ToString("0.0000");
            debugtext["SlideTimeDelta"].text = "SlideTimeDelta: " + SlideTimeDelta.ToString("0.0000");
            debugtext["BufferJumpTimeDelta"].text = "BufferJumpTimeDelta: " + BufferJumpTimeDelta.ToString("0.0000");
            debugtext["WallJumpTimeDelta"].text = "WallJumpTimeDelta: " + WallJumpTimeDelta.ToString("0.0000");
            debugtext["WallRunTimeDelta"].text = "WallRunTimeDelta: " + WallRunTimeDelta.ToString("0.0000");
            debugtext["WallClimbTimeDelta"].text = "WallClimbTimeDelta: " + WallClimbTimeDelta.ToString("0.0000");
            debugtext["ReGrabTimeDelta"].text = "ReGrabTimeDelta: " + ReGrabTimeDelta.ToString("0.0000");
            debugtext["MovingColliderTimeDelta"].text = "MovingColliderTimeDelta: " + MovingColliderTimeDelta.ToString("0.0000");
            debugtext["MovingPlatformTimeDelta"].text = "MovingPlatformTimeDelta: " + MovingPlatformTimeDelta.ToString("0.0000");
            debugtext["StuckTimeDelta"].text = "StuckTimeDelta: " + StuckTimeDelta.ToString("0.0000");
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
            if (transform.parent != moving_obj.transform)
            {
                transform.parent = moving_obj.transform;
                // Keep custom velocity globally accurate
                current_velocity -= moving_obj.velocity;
            }
            lastMovingPlatform = moving_obj;
            MovingColliderTimeDelta = 0;
        }

        if (!OnGround())
        {
            RaycastHit hit;
            Boolean hit_wall = false;
            if (IsWallRunning())
            {
                // Scan toward the wall normal
                if (Physics.Raycast(transform.position, -PreviousWallNormal, out hit, 2.0f))
                {
                    hit_wall = true;
                }
            }
            else if (IsWallClimbing())
            {
                // Scan toward the wall normal at both head and stomach height
                if (Physics.Raycast(transform.position, -PreviousWallNormal, out hit, 2.0f))
                {
                    hit_wall = true;
                }
                else if (Physics.Raycast(transform.position + (transform.up * (cc.height / 2 - cc.radius)), -PreviousWallNormal, out hit, 2.0f))
                {
                    hit_wall = true;
                }
            }
            else
            {
                // Scan forward and sideways to find a wall
                if (Physics.Raycast(transform.position, transform.right, out hit, 2.0f))
                {
                    hit_wall = true;
                }
                else if (Physics.Raycast(transform.position, -transform.right, out hit, 2.0f))
                {
                    hit_wall = true;
                }
                else if (Physics.Raycast(transform.position + (transform.up * (cc.height / 2 - cc.radius)), transform.forward, out hit, 2.0f))
                {
                    hit_wall = true;
                }
            }
            // Update my current state based on my scan results
            if (hit_wall && hit.normal.y > -0.17f && hit.normal.y <= 0.34f) 
            {
                UpdateWallConditions(hit.normal);
            }
            if (IsWallClimbing() && !isHanging)
            {
                // Scan for ledge
                if (Physics.Raycast(transform.position + (transform.up * (cc.height / 2 - cc.radius)), -PreviousWallNormal, out hit, 2.0f))
                {
                    Vector3 LedgeScanPos = transform.position + (transform.up * cc.height / 2) + LedgeClimbOffset * transform.forward;
                    if (Physics.Raycast(LedgeScanPos, -transform.up, out hit, LedgeClimbOffset))
                    {
                        if (CanGrabLedge() && Vector3.Dot(hit.normal, Physics.gravity) < -0.866f)
                        {
                            isHanging = true;
                        }
                    }
                }

                // If all ledge climb conditions are met, climb it to the surface on top
                // and clear all wall conditions
            }
        }

        lastTrigger = null;
    }

    private void UpdateWallConditions(Vector3 wall_normal)
    {
        if (Vector3.Dot(GetGroundVelocity(), wall_normal) < -WallJumpThreshold)
        {
            // Are we jumping in a new direction (atleast 20 degrees difference)
            if (Vector3.Dot(PreviousWallJumpNormal, wall_normal) < 0.94f)
            {
                WallJumpTimeDelta = 0;
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
        if (AlongWallVel.magnitude > WallRunLimit && Mathf.Abs(Vector3.Dot(wall_normal, transform.forward)) < WallRunClimbCosAngle && Vector3.Dot(AlongWallVel, transform.forward) > 0)
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
                WallRunTimeDelta = 0;
            }
        }
        // If we fail the wall run try to wall climb instead if we are looking at the wall
        else if (isHanging || Vector3.Dot(transform.forward, -wall_normal) >= WallRunClimbCosAngle)
        {
            WallClimbTimeDelta = 0;
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
        LandingTimeDelta = 0;

        // Handle buffered jumps
        if (JumpBuffered())
        {
            // Buffer a jump
            willJump = true;
        }
        MovingGeneric moving_platform = lastHit.gameObject.GetComponent<MovingGeneric>();
        if (moving_platform != null)
        {
            if (transform.parent != moving_platform.transform)
            {
                transform.parent = moving_platform.transform;
                // Keep custom velocity globally accurate
                current_velocity -= moving_platform.velocity;
            }
            lastMovingPlatform = moving_platform;
            MovingPlatformTimeDelta = 0;
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
            SlideTimeDelta = 0;
        }
        // Normal ground behavior
        if (OnGround() && !willJump && (SlideTimeDelta >= SlideGracePeriod || planevelocity.magnitude < SlideSpeed))
        {
            // If we weren't fast enough we aren't going to slide
            SlideTimeDelta = SlideGracePeriod;
            movVec = Vector3.ProjectOnPlane(movVec, currentHit.normal);
            //Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), movVec, Color.green, Time.fixedDeltaTime);
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
        if (!InMovingCollision() && !OnMovingPlatform())
        {
            moving_frame_velocity = Vector3.zero;
            if (transform.parent != null)
            {
                transform.parent = null;
                // Inherit velocity from previous platform
                if (lastMovingPlatform != null)
                {
                    // Keep custom velocity globally accurate
                    current_velocity += lastMovingPlatform.velocity;
                }
            }
            lastMovingPlatform = null;
        }
        else if (InMovingCollision())
        {
            moving_frame_velocity = lastMovingPlatform.velocity;
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
        // Accelerate if we aren't at the desired speed
        if (Vector3.ProjectOnPlane(current_velocity + deltaVel, Physics.gravity).magnitude <= desiredSpeed)
        {
            current_velocity += deltaVel;
        }
        // If we are past the desired speed, subtract the deltaVel off and add it back in the direction we want
        else
        {
            /*if (grounded)
            {
                current_velocity += direction * desiredSpeed - Vector3.Project(current_velocity, direction);
            }*/
            if (!grounded)
            {
                current_velocity = current_velocity - (deltaVel.magnitude * GetGroundVelocity().normalized) + deltaVel;
            }
        }

        if (grounded)
        {
            accel += -Vector3.ProjectOnPlane(current_velocity + moving_frame_velocity, currentHit.normal) * SpeedDamp;
        }
        else
        {
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
        return (LandingTimeDelta < jumpGracePeriod);
    }

    public bool JumpBuffered()
    {
        return (BufferJumpTimeDelta < BufferJumpGracePeriod);
    }

    public bool IsWallRunning()
    {
        return wallRunEnabled && (WallRunTimeDelta < WallRunGracePeriod);
    }

    public bool IsWallClimbing()
    {
        return wallClimbEnabled && (WallClimbTimeDelta < WallClimbGracePeriod);
    }

    public bool CanGrabLedge()
    {
        return wallClimbEnabled && (ReGrabTimeDelta >= ReGrabGracePeriod);
    }

    public bool CanWallJump()
    {
        return wallJumpEnabled && (WallJumpTimeDelta < WallJumpGracePeriod);
    }

    public bool IsHanging()
    {
        return isHanging;
    }

    public bool InMovingCollision()
    {
        return (MovingColliderTimeDelta < MovingColliderGracePeriod);
    }

    public bool OnMovingPlatform()
    {
        return (MovingPlatformTimeDelta < MovingPlatformGracePeriod);
    }

    public bool IsStuck()
    {
        return (StuckTimeDelta < StuckGracePeriod);
    }

    public Vector3 GetLastWallNormal()
    {
        return PreviousWallNormal;
    }

    public Vector3 GetGroundVelocity()
    {
        return Vector3.ProjectOnPlane(current_velocity, Physics.gravity);
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
        if (willJump)
        {
            DoJump();
        }
        else if (input_manager.GetJump())
        {
            if (OnGround() || CanWallJump() || IsWallRunning() || isHanging)
            {
                DoJump();
            }
            else
            {
                BufferJumpTimeDelta = 0;
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
        if (!isHanging && JumpMeter > JumpMeterThreshold)
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
                JumpMeter = 0;
            }
            else if (IsWallRunning())
            {
                //Debug.Log("Wall Run Jump");
                current_velocity += PreviousWallNormal * WallRunJumpSpeed * JumpMeterComputed;
                float pathvel = Vector3.Dot(current_velocity, transform.forward);
                float newspeed = Mathf.Clamp(pathvel + (WallRunJumpBoostAdd * JumpMeterComputed), 0f, WallRunJumpBoostSpeed);
                current_velocity += transform.forward * (newspeed - pathvel);
                current_velocity.y = Math.Max(current_velocity.y, WallRunJumpUpSpeed * JumpMeterComputed);
                JumpMeter = 0;
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
        ReGrabTimeDelta = 0;
        isJumping = true;
        isFalling = false;
        willJump = false;
        isHanging = false;

        // Intentionally set the timers over the limit
        BufferJumpTimeDelta = 2*BufferJumpGracePeriod;
        WallJumpTimeDelta = 2*WallJumpGracePeriod;
        WallRunTimeDelta = 2*WallRunGracePeriod;
        WallClimbTimeDelta = 2*WallClimbGracePeriod;
        LandingTimeDelta = 2*jumpGracePeriod;
        MovingPlatformTimeDelta = 2*MovingPlatformGracePeriod;
        WallJumpReflect = Vector3.zero;
    }

    public void RegisterJumpCallback(Action callback)
    {
        jump_callback_table.Add(callback);
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
        StuckTimeDelta = 0;
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
