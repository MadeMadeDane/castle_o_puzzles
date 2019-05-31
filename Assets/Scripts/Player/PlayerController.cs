using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;

delegate void AccelerationFunction(Vector3 direction, float desiredSpeed, float acceleration, bool grounded);

public class PlayerController : NetworkedBehaviour {
    #region DECLARATIONS
    [Header("Linked Components")]
    public GameObject player_container;
    public CharacterController cc;
    [HideInInspector]
    public CameraController player_camera;
    [Header("Movement constants")]
    public float RunSpeedMult;
    public float ClimbSpeedMult;
    public float AirSpeedMult;
    public float MaxAirSpeedMult;
    public float GroundAccelerationMult;
    public float AirAccelerationMult;
    public float SpeedDampMult;
    public float AirSpeedDampMult;
    public float ClimbSpeedDampMult;
    public float SlideSpeedMult;
    public float DownGravityAdd;
    public float JumpVelocityMult;
    public float WallJumpThresholdMult;
    public float WallJumpBoost;
    public float WallRunLimitMult;
    public float WallJumpSpeedMult;
    public float WallRunJumpBoostSpeedMult;
    public float WallRunJumpBoostAddMult;
    public float WallRunJumpSpeedMult;
    public float WallRunJumpUpSpeedMult;
    public float JumpBoostSpeedMult;
    public float JumpBoostRequiredSpeedMult;
    public float JumpBoostAddMult;
    public float GroundStepOffsetMult;
    public float AirStepOffsetMult;
    public float MoveAccumulatorMax;
    public float MoveAccumulatorScaleFactor;
    public Vector3 StartPos;
    [Header("Movement toggles")]
    public bool wallRunEnabled;
    public bool wallJumpEnabled;
    public bool wallClimbEnabled;
    public bool conserveUpwardMomentum;
    public bool ShortHopEnabled;
    public bool ShortHopTempDisable;
    public bool JumpBoostEnabled;
    public bool ToggleCrouch;
    private Vector3 accel;
    private Vector3 current_velocity;
    private bool enableMovement = true;
    private Vector3 moveAccumulator;
    public bool debug_mode = false;

    // Managers
    private InputManager input_manager;
    private Utilities utils;
    private PhysicsPropHandler physhandler;

    // Timers
    private string JUMP_METER = "JumpMeter";
    private string USE_TIMER = "UseTimer";
    private string LANDING_TIMER = "Landing";
    private string BUFFER_JUMP_TIMER = "BufferJump";
    private string SLIDE_TIMER = "Slide";
    private string WALL_JUMP_TIMER = "WallJump";
    private string WALL_RUN_TIMER = "WallRun";
    private string WALL_CLIMB_TIMER = "WallClimb";
    private string WALL_HANG_TIMER = "WallHang";
    private string WALL_HIT_TIMER = "WallHit";
    private string REGRAB_TIMER = "ReGrab";
    private string MOVING_COLLIDER_TIMER = "MovingCollider";
    private string MOVING_PLATFORM_TIMER = "MovingPlatform";
    private string MOVING_INTERIOR_TIMER = "MovingInterior";
    private string STUCK_TIMER = "Stuck";
    private string CROUCH_TIMER = "Crouch";

    // Jumping state variables
    private float JumpMeterThreshold;
    private float JumpMeterComputed;
    private bool isJumping;
    private bool isFalling;
    private bool isCrouching;
    private bool willJump;
    private float cc_standHeight;
    private float cc_standCenter;
    private float wr_standHeight;
    private float wr_standCenter;
    private float re_standHeight;
    private float re_standCenter;
    private CapsuleCollider wall_run_collider;
    private CapsuleCollider recovery_collider;
    private LayerMask DEFAULT_LAYER;
    public float GetStandingHeight() => cc_standHeight;

    // Wall related variables
    private Vector3 WallJumpReflect;
    private Vector3 PreviousWallJumpPos;
    private Vector3 LastHangingNormal;
    private Vector3 PreviousWallNormal;
    private Vector3 PreviousWallJumpNormal;
    private Vector3 WallAxis;
    private Vector3 AlongWallVel;
    private Vector3 UpWallVel;
    private float WallRunImpulseMult;
    private float WallRunSpeedMult;
    private float WallRunClimbCosAngle;
    private float WallScanDistanceMult;
    private float LedgeClimbOffsetMult;
    private float WallDistanceThresholdMult;

    // Physics state variables
    private AccelerationFunction accelerate;
    private Vector3 moving_frame_velocity;
    private ControllerColliderHit lastHit;
    private MovingGeneric lastMovingPlatform;
    private Collider lastMovingTrigger;
    private Collider lastTrigger;
    private Vector3 lastFloorHitNormal;
    private Vector3 currentHitNormal;
    private Vector3 currentHitPos;
    private float GravityMult;
    private float GravityTimeConstant;

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
    #endregion
    // Use this for initialization
    private void Start() {
        if (!IsOwner) return;
        Setup();
        // Movement values
        //SetShooterVars();
        SetThirdPersonActionVars();

        isJumping = false;
        isFalling = false;
        willJump = false;
        isCrouching = false;
        // Wall related vars
        WallAxis = Vector3.zero;
        AlongWallVel = Vector3.zero;
        UpWallVel = Vector3.zero;
        WallJumpReflect = Vector3.zero;
        PreviousWallJumpPos = Vector3.positiveInfinity;
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        LedgeClimbOffsetMult = 2f;
        WallScanDistanceMult = 3f;
        WallDistanceThresholdMult = 56f; // proportional to radius^2
        ShortHopTempDisable = false;

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
        accel = Vector3.zero;
        GravityMult = 1;
        GravityTimeConstant = 1 / cc.height;
        currentHitNormal = Vector3.up;
        lastFloorHitNormal = currentHitNormal;
        StartPos = transform.position;

        // TODO: Test below
        //cc.enableOverlapRecovery = false;
        physhandler = GetComponent<PhysicsPropHandler>();
        if (physhandler == null) {
            throw new Exception("Could not find physics prop handler");
        }
        Physics.IgnoreCollision(wall_run_collider, cc);
    }

    private void Update() {
        if (!IsOwner) return;
        // If the player does not have a camera, do nothing
        if (player_camera == null) {
            return;
        }
        if (input_manager.GetJump()) {
            utils.ResetTimer(BUFFER_JUMP_TIMER);
        }
        if (input_manager.GetUse()) {
            utils.ResetTimer(USE_TIMER);
        }
        if (input_manager.GetCrouch()) {
            utils.ResetTimer(CROUCH_TIMER);
        }
    }

    private void LateUpdate() {
        if (!IsOwner) return;
        // If the player does not have a camera, do nothing
        if (player_camera == null) {
            return;
        }
        Vector3 old_yaw_pivot_pos = player_camera.yaw_pivot.transform.position;
        player_container.transform.position = transform.position;
        transform.localPosition = Vector3.zero;
        player_container.transform.rotation = Quaternion.identity;
        player_camera.yaw_pivot.transform.position = old_yaw_pivot_pos;
    }

    // Fixed Update is called once per physics tick
    private void FixedUpdate() {
        if (!IsOwner) return;
        // If the player does not have a camera, do nothing
        if (player_camera == null) {
            return;
        }
        //RecoverSmart();
        HandleChangesFromLastState();
        ProcessHits();
        ProcessTriggers();
        HandleCrouch();
        HandleMovement();
        HandleClimbing();
        HandleJumping();
        HandleUse();
        HandleGravity();
        if (enableMovement) UpdatePlayerState();
        if (debug_mode) {
            IncrementCounters();
        }
    }

    private void OnTriggerStay(Collider other) {
        if (!IsOwner) return;
        if (!other.isTrigger) {
            lastTrigger = other;
        }
    }

    // Handle collisions on player move
    private void OnControllerColliderHit(ControllerColliderHit hit) {
        if (!IsOwner) return;
        lastHit = hit;
    }

    #region SETUP_CONFIGURATIONS
    private void Setup() {
        DEFAULT_LAYER = LayerMask.GetMask("Default");
        player_container = transform.parent.gameObject;
        input_manager = InputManager.Instance;
        utils = Utilities.Instance;
        cc = GetComponent<CharacterController>();
        wall_run_collider = gameObject.AddComponent<CapsuleCollider>();
        wall_run_collider.isTrigger = true;
        // x=0, y=1, z=2
        wall_run_collider.direction = 1;
        wall_run_collider.height = cc.height * 0.95f; // 5.5f originally
        wall_run_collider.radius = cc.radius * 1.5f; // 0.75f originally
        //wall_run_collider.center = cc.center;
        GameObject recovery_go = new GameObject("RecoveryTrigger");
        recovery_go.transform.parent = transform;
        recovery_go.layer = LayerMask.NameToLayer("Ignore Raycast");
        recovery_collider = recovery_go.AddComponent<CapsuleCollider>();
        recovery_collider.isTrigger = true;
        recovery_collider.direction = 1;
        recovery_collider.height = cc.height * 0.95f; // 5.5f originally
        recovery_collider.radius = cc.radius * 0.8f; // 0.4f originally
        recovery_collider.center = cc.center;
        Rigidbody recovery_rb = recovery_go.AddComponent<Rigidbody>();
        recovery_rb.isKinematic = true;
        recovery_rb.drag = 0f;
        recovery_rb.angularDrag = 0f;
        recovery_rb.useGravity = false;
        CollisionRecovery recovery_cr = recovery_go.AddComponent<CollisionRecovery>();
        recovery_cr.player = this;

        cc_standHeight = cc.height;
        cc_standCenter = cc.center.y;
        wr_standHeight = wall_run_collider.height;
        wr_standCenter = wall_run_collider.center.y;
        re_standHeight = recovery_collider.height;
        re_standCenter = recovery_collider.center.y;

        utils.CreateTimer(JUMP_METER, 0.3f);
        utils.CreateTimer(USE_TIMER, 0.1f);
        JumpMeterThreshold = utils.GetTimerPeriod(JUMP_METER) / 3;
        JumpMeterComputed = utils.GetTimerPercent(JUMP_METER);
        utils.CreateTimer(LANDING_TIMER, 0.1f).setFinished();
        utils.CreateTimer(BUFFER_JUMP_TIMER, 0.1f).setFinished();
        utils.CreateTimer(SLIDE_TIMER, 0.2f).setFinished();
        utils.CreateTimer(WALL_JUMP_TIMER, 0.2f).setFinished();
        utils.CreateTimer(WALL_RUN_TIMER, 0.2f).setFinished();
        utils.CreateTimer(WALL_CLIMB_TIMER, 0.2f).setFinished();
        utils.CreateTimer(WALL_HANG_TIMER, 0.1f).setFinished();
        utils.CreateTimer(WALL_HIT_TIMER, 0.2f).setFinished();
        utils.CreateTimer(REGRAB_TIMER, 0.5f).setFinished();
        utils.CreateTimer(MOVING_COLLIDER_TIMER, 0.01f).setFinished();
        utils.CreateTimer(MOVING_PLATFORM_TIMER, 0.1f).setFinished();
        utils.CreateTimer(MOVING_INTERIOR_TIMER, 0.1f).setFinished();
        utils.CreateTimer(STUCK_TIMER, 0.2f).setFinished();
        utils.CreateTimer(CROUCH_TIMER, 0.1f).setFinished();

        if (debug_mode) {
            EnableDebug();
        }
    }

    private void SetShooterVars() {
        // Movement modifiers
        RunSpeedMult = 30f; // proportional to radius
        ClimbSpeedMult = 60f; // proportional to radius
        AirSpeedMult = 1.8f; // proportional to radius
        MaxAirSpeedMult = Mathf.Infinity; // proportional to radius
        GroundAccelerationMult = 40; // proportional to radius
        AirAccelerationMult = 1000; // proportional to radius
        SpeedDampMult = 40f; // proportional to radius
        AirSpeedDampMult = 0.02f; // proportional to radius
        ClimbSpeedDampMult = 40f; // proportional to radius
        SlideSpeedMult = 36f; // proportional to radius
        // Gravity modifiers
        DownGravityAdd = 0;
        // Jump/Wall modifiers
        JumpVelocityMult = 3.5f; // proportional to height
        JumpBoostSpeedMult = 24f; // proportional to radius
        JumpBoostRequiredSpeedMult = 24f; // proportional to radius
        JumpBoostAddMult = 0f; // proportional to radius
        WallJumpThresholdMult = 16f; // proportional to radius
        WallJumpBoost = 1f;
        WallJumpSpeedMult = 7f; // proportional to height
        WallRunLimitMult = 16f; // proportional to radius
        WallRunJumpBoostSpeedMult = 12f; // proportional to radius
        WallRunJumpBoostAddMult = 0f; // proportional to radius
        WallRunJumpSpeedMult = 30f; // proportional to radius
        WallRunJumpUpSpeedMult = 3.5f; // proportional to height
        WallRunImpulseMult = 0f; // proportional to height
        WallRunSpeedMult = 30f; // proportional to radius
        WallRunClimbCosAngle = Mathf.Cos(Mathf.Deg2Rad * 30f);
        GroundStepOffsetMult = 0.2f; // proportional to height
        AirStepOffsetMult = 0.2f; // proportional to height
        MoveAccumulatorMax = 5f;
        MoveAccumulatorScaleFactor = 5f;
        // Toggles
        conserveUpwardMomentum = true;
        wallJumpEnabled = true;
        wallRunEnabled = true;
        wallClimbEnabled = true;
        ShortHopEnabled = false;
        JumpBoostEnabled = false;
        ToggleCrouch = true;
        // Delegates
        accelerate = AccelerateCPM;
    }

    private void SetThirdPersonActionVars() {
        // Movement modifiers
        RunSpeedMult = 30f; // proportional to radius
        ClimbSpeedMult = 60f; // proportional to radius
        AirSpeedMult = 30f; // proportional to radius
        MaxAirSpeedMult = 10f; // proportional to radius
        GroundAccelerationMult = 600f; // proportional to radius
        AirAccelerationMult = 60f; // proportional to radius
        SpeedDampMult = 40f; // proportional to radius
        AirSpeedDampMult = 0.02f; // proportional to radius
        ClimbSpeedDampMult = 40f; // proportional to radius
        SlideSpeedMult = 44f; // proportional to radius
        // Gravity modifiers
        DownGravityAdd = 0;
        // Jump/Wall modifiers
        JumpVelocityMult = 5.262f; // proportional to height
        JumpBoostSpeedMult = 34f; // proportional to radius
        JumpBoostRequiredSpeedMult = 24f; // proportional to radius
        JumpBoostAddMult = 20f; // proportional to radius
        WallJumpThresholdMult = 16f; // proportional to radius
        WallJumpBoost = 1f;
        WallJumpSpeedMult = 3.5f; // proportional to height
        WallRunLimitMult = 16f; // proportional to radius
        WallRunJumpBoostSpeedMult = 40f; // proportional to radius
        WallRunJumpBoostAddMult = 20f; // proportional to radius
        WallRunJumpSpeedMult = 24f; // proportional to radius
        WallRunJumpUpSpeedMult = 3.5f; // proportional to height
        WallRunImpulseMult = 0f; // proportional to height
        WallRunSpeedMult = 30f; // proportional to radius
        WallRunClimbCosAngle = Mathf.Cos(Mathf.Deg2Rad * 30f);
        GroundStepOffsetMult = 0.2f; // proportional to height
        AirStepOffsetMult = 1f; // proportional to height
        MoveAccumulatorMax = 5f;
        MoveAccumulatorScaleFactor = 6f;
        // Toggles
        conserveUpwardMomentum = false;
        wallJumpEnabled = true;
        wallRunEnabled = false;
        wallClimbEnabled = true;
        ShortHopEnabled = true;
        JumpBoostEnabled = true;
        ToggleCrouch = true;
        // Delegates
        accelerate = AccelerateStandard;
    }

    private void EnableDebug() {
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
        foreach (KeyValuePair<string, Text> item in debugtext) {
            item.Value.gameObject.name = item.Key;
            RectTransform rect = item.Value.gameObject.GetComponent<RectTransform>();
            rect.SetParent(debugcanvas.transform);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 220f);
            rect.anchoredPosition3D = new Vector3(-250f, -15f * idx, 0);
            item.Value.font = ArialFont;
            idx++;
        }
    }
    #endregion

    #region HANDLE_STATE
    private void UpdatePlayerState() {
        // Update character state based on desired movement
        current_velocity += accel * Time.deltaTime;
        accel = Vector3.zero;

        Vector3 previous_position = transform.position;
        bool failed_move = false;
        try {
            cc.Move(ComputeMove(current_velocity * Time.deltaTime));
        }
        catch (Exception ex) {
            Debug.Log("Failed to move: " + ex.ToString());
            failed_move = true;
        }

        float error = cc.velocity.magnitude - current_velocity.magnitude;
        total_error += error - error_accum.Dequeue();
        error_accum.Enqueue(error);
        // We are too far off from the real velocity. This mainly happens when trying to move into colliders.
        // Nothing will significantly change if we reset the velocity here, so use this time to resync it.
        if (total_error < -50.0f) {
            current_velocity = cc.velocity;
            //Debug.Log("Total error: " + total_error.ToString());
        }

        // Unity is too far off from what the velocity should be
        // This is bandaid-ing a bug in the unity character controller where moving into certain
        // edges will cause the character to teleport extreme distances, sometimes crashing the game.
        if (error > 100.0f || failed_move) {
            Debug.Log("Unity error: " + error.ToString());
            cc.SimpleMove(-cc.velocity);
            Teleport(previous_position);
            error_bucket++;
        }
        else {
            if (error_bucket > 0) {
                error_bucket--;
                // If the frame didn't have an error, we are probably safe now. Reset to no error stage.
                error_stage = 0;
            }
        }

        // If we have a large number of errors in a row, we are probably stuck.
        // Try to resolve this in a series of stages.
        if (error_bucket >= error_threshold && !IsStuck()) {
            Debug.Log("Lots of error!");
            Debug.Log("Previous position: " + previous_position.ToString());
            Debug.Log("Current position: " + transform.position.ToString());
            Debug.Log("Current cc velocity: " + cc.velocity.magnitude.ToString());
            Debug.Log("Current velocity: " + current_velocity.magnitude.ToString());
            Debug.Log("Velocity error: " + (current_velocity - cc.velocity).ToString());
            error_stage++;
            Debug.Log("Attempting error resolution stage " + error_stage.ToString());
            switch (error_stage) {
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
        if (!IsStuck()) {
            if (position_history.Count == position_history_size) {
                position_history.RemoveLast();
            }
            position_history.AddFirst(transform.position);
        }
    }

    private Vector3 ComputeMove(Vector3 desired_move) {
        // Save a bit on perfomance by returning early if we don't need to raycast
        HandleStairs(desired_move, stepMaxHeight: cc_standHeight / 3f, stepMaxDepth: cc_standHeight / 3f);
        if (!OverlapPlayerCheck(desired_move)) {
            return desired_move;
        }
        Vector3 ground_move = Vector3.ProjectOnPlane(desired_move, transform.up);
        if (ground_move.normalized == Vector3.zero) {
            return desired_move;
        }
        ground_move = ground_move.normalized * cc.radius;
        Vector3 ground_move_alt = Vector3.Cross(ground_move, transform.up);

        Vector3 SkinPos = transform.position + (ground_move.normalized * (cc.radius + cc.skinWidth));
        Vector3 SkinPosAlt = transform.position + (ground_move_alt.normalized * (cc.radius + cc.skinWidth));
        Vector3 SkinPosAltN = transform.position + (-ground_move_alt.normalized * (cc.radius + cc.skinWidth));
        Ray[] scanrays = new Ray[9] {
            new Ray(SkinPos, ground_move),
            new Ray(SkinPosAlt, ground_move_alt),
            new Ray(SkinPosAltN, -ground_move_alt),
            new Ray(SkinPos + cc.center + transform.up*cc.height/4, ground_move),
            new Ray(SkinPosAlt + cc.center + transform.up*cc.height/4, ground_move_alt),
            new Ray(SkinPosAltN + cc.center + transform.up*cc.height/4, -ground_move_alt),
            new Ray(SkinPos + cc.center - transform.up*cc.height/4, ground_move),
            new Ray(SkinPosAlt + cc.center - transform.up*cc.height/4, ground_move_alt),
            new Ray(SkinPosAltN + cc.center - transform.up*cc.height/4, -ground_move_alt)
        };
        RaycastHit hit;

        //Debug.DrawRay(SkinPos + transform.up * cc.height / 2, ground_move, Color.blue);
        //Debug.DrawRay(SkinPosAlt + transform.up * cc.height / 2, ground_move_alt, Color.blue);
        //Debug.DrawRay(SkinPosAltN + transform.up * cc.height / 2, -ground_move_alt, Color.blue);
        foreach (Ray scanray in scanrays) {
            if (Physics.Raycast(scanray, out hit, ground_move.magnitude)) {
                //Debug.DrawRay(SkinPos + transform.up * cc.height / 2, Vector3.ProjectOnPlane(ground_move, hit.normal).normalized, Color.green);
                float cosanglehit = Vector3.Dot(hit.normal, scanray.direction.normalized);
                if (Vector3.Dot(desired_move, hit.normal) < 0 && ((hit.distance + cc.radius) * Mathf.Abs(cosanglehit) < cc.radius * 1.1f)) {
                    //Debug.DrawRay(SkinPos + transform.up * cc.height / 2, hit.normal, Color.red);
                    if (IsWall(hit.normal)) {
                        if (!OnGround()) {
                            UpdateWallConditions(hit.normal);
                        }
                        utils.ResetTimer(WALL_HIT_TIMER);
                        current_velocity = Vector3.ProjectOnPlane(current_velocity, hit.normal);
                        return Vector3.ProjectOnPlane(desired_move, hit.normal);
                    }
                }
            }
        }
        return desired_move;
    }

    private void HandleStairs(Vector3 desiredMove, float stepMaxHeight = 0.5f, float stepMaxDepth = 0.5f) {
        // If we are in the air, handle running into the edge of the top of a wall
        if (!OnGround()) {
            desiredMove = GetMoveVector();
            if (desiredMove.magnitude < 0.2f) return;
            // Make sure we are running into a wall
            if (!IsOnWall() || !IsWall(currentHitNormal)) return;
            // Make sure we are moving along the wall normal axis
            if (Vector3.Dot(desiredMove.normalized, currentHitNormal) > -0.3f) {
                // if (Vector3.Dot(desiredMove.normalized, currentHitNormal) > 0.3f) {
                //     player_camera.RotatePlayerToward(Vector3.ProjectOnPlane(-currentHitNormal, transform.up), 1.0f);
                // }
                return;
            }
            // Make sure the wall hit is near our feet
            if ((currentHitPos - GetFootPosition()).magnitude > cc.radius * 1.5f) return;
            // If we hit a wall at our torso, abort
            if (Physics.Raycast(transform.position, -currentHitNormal, 2f * cc.radius)) return;
            // Check if we can fit on the surface above wall we are running into
            if (CapsuleCastPlayer(transform.position + (transform.up * cc.radius * 1.2f) - (desiredMove.normalized * cc.radius * 0.1f), desiredMove, out RaycastHit hitinfo, cc.radius)) {
                if (!IsFloor(hitinfo.normal) && !IsSlide(hitinfo.normal)) return;
            }
            // If we pass all the above tests, give the player enough upward velocity to move upward one radius of the capsule
            current_velocity.y = Mathf.Max(Mathf.Sqrt(2 * cc.radius * Physics.gravity.magnitude * cc_standHeight * GravityTimeConstant), current_velocity.y);
            return;
        }
        // Scan for a step infront of us
        float stepExtraHeight = cc_standHeight / 60f;
        Vector3 stepScanOrigin = transform.position + cc.center + desiredMove + (transform.up * stepMaxHeight);
        float scanDepth = stepMaxHeight + stepMaxDepth;
        if (!CapsuleCastPlayer(origin: stepScanOrigin, direction: -transform.up, out RaycastHit stepSurfaceHit, maxDistance: scanDepth)) return;
        //if (!IsFloor(stepSurfaceHit.normal)) return;

        Vector3 ledgeOffset = Vector3.Project(stepSurfaceHit.point - GetFootPosition(), transform.up);
        // Make sure the step isn't too high
        if (ledgeOffset.magnitude > (GroundStepOffsetMult * cc_standHeight) || ledgeOffset.magnitude < (cc.radius / 3f)) return;
        //Debug.DrawRay(stepSurfaceHit.point, ledgeOffset, Color.green, 10f);
        //Debug.Log($"ledgeOff: {ledgeOffset.magnitude}");

        Vector3 surfaceProbeOrigin = stepSurfaceHit.point + Vector3.ProjectOnPlane(desiredMove.normalized * cc.radius, stepSurfaceHit.normal) + transform.up * stepExtraHeight;
        //Debug.DrawRay(surfaceProbeOrigin, -transform.up, Color.red, 10f);
        if (!Physics.Raycast(surfaceProbeOrigin, -transform.up, out RaycastHit surfaceProbe, cc_standHeight / 12f)) return;
        //Debug.DrawRay(surfaceProbe.point, surfaceProbe.normal, Color.blue, 10f);
        // If neither the original surface or the probe are floors, then this is definitely not a staircase or slope tip
        if (!IsFloor(surfaceProbe.normal) && !IsFloor(stepSurfaceHit.normal)) return;

        Vector3 desiredPlayerPos = ledgeOffset + transform.position + (ledgeOffset.normalized * stepExtraHeight);
        Teleport(desiredPlayerPos);
        if (IsFloor(surfaceProbe.normal)) {
            currentHitNormal = lastFloorHitNormal = surfaceProbe.normal;
            current_velocity = Vector3.ProjectOnPlane(current_velocity, lastFloorHitNormal);
        }
    }

    private void IncrementCounters() {
        if (debugcanvas != null) {
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

    private void HandleChangesFromLastState() {
        recovery_collider.height = cc.height * 0.95f;
        recovery_collider.radius = cc.radius * 0.8f;
        wall_run_collider.height = cc.height * 0.95f;
        wall_run_collider.radius = cc.radius * 1.5f;
        if (WallDistanceCheck()) {
            JumpMeterComputed = utils.GetTimerPercent(JUMP_METER);
        }
        else {
            JumpMeterComputed = 0;
        }
    }
    #endregion

    #region PROCESS_COLLISIONS
    private void ProcessTriggers() {
        if (lastTrigger == null) {
            return;
        }

        // Use MovingCollider for parenting on collisions
        MovingCollider moving_obj = lastTrigger.GetComponent<MovingCollider>();
        if (moving_obj != null) {
            if (player_container.transform.parent != moving_obj.transform) {
                player_container.transform.parent = moving_obj.transform;
                // Keep custom velocity globally accurate
                current_velocity -= moving_obj.player_velocity;
            }
            lastMovingPlatform = moving_obj;
            utils.ResetTimer(MOVING_COLLIDER_TIMER);
        }

        if (!OnGround()) {
            RaycastHit hit;
            Boolean hit_wall = false;
            if (IsWallRunning()) {
                // Scan toward the wall normal
                if (Physics.Raycast(transform.position, -PreviousWallNormal, out hit, cc.radius * WallScanDistanceMult)) {
                    hit_wall = true;
                }
            }
            else if (IsWallClimbing()) {
                // Scan toward the wall normal at both head and stomach height
                if (Physics.Raycast(transform.position, -PreviousWallNormal, out hit, cc.radius * WallScanDistanceMult)) {
                    hit_wall = true;
                }
                else if (Physics.Raycast(transform.position + (transform.up * GetHeadHeight()), -PreviousWallNormal, out hit, cc.radius * WallScanDistanceMult)) {
                    hit_wall = true;
                }
            }
            else {
                // Scan forward and sideways to find all walls
                if (Physics.Raycast(transform.position, transform.right, out hit, cc.radius * WallScanDistanceMult)) {
                    if (IsWall(hit.normal)) {
                        UpdateWallConditions(hit.normal);
                    }
                }
                if (Physics.Raycast(transform.position, -transform.right, out hit, cc.radius * WallScanDistanceMult)) {
                    if (IsWall(hit.normal)) {
                        UpdateWallConditions(hit.normal);
                    }
                }
                if (Physics.Raycast(transform.position, transform.forward, out hit, cc.radius * WallScanDistanceMult)) {
                    if (IsWall(hit.normal)) {
                        UpdateWallConditions(hit.normal);
                    }
                }
                if (Physics.Raycast(transform.position + (transform.up * GetHeadHeight()), transform.forward, out hit, cc.radius * WallScanDistanceMult)) {
                    if (IsWall(hit.normal)) {
                        UpdateWallConditions(hit.normal);
                    }
                }
            }

            if (hit_wall && IsWall(hit.normal)) {
                UpdateWallConditions(hit.normal);
            }

            if (IsWallClimbing() && !IsHanging()) {
                HandleLedgeHang();
            }
        }

        lastTrigger = null;
    }

    private void HandleLedgeHang() {
        // Make sure our head is against a wall
        if (!Physics.Raycast(transform.position + (transform.up * GetHeadHeight()), transform.forward, out RaycastHit WallHit, cc.radius * WallScanDistanceMult)) return;

        Vector3 LedgeScanVerticalPos = transform.position + transform.up * (GetHeadHeight() + cc.radius);
        Vector3 LedgeScanHorizontalVector = (cc.radius * LedgeClimbOffsetMult) * transform.forward;
        // Make sure we don't hit a wall at the ledge height
        if (Physics.Raycast(origin: LedgeScanVerticalPos, direction: LedgeScanHorizontalVector.normalized, maxDistance: LedgeScanHorizontalVector.magnitude)) return;
        if (Physics.Raycast(origin: LedgeScanVerticalPos - (transform.up * cc.height / 60f), direction: LedgeScanHorizontalVector.normalized, maxDistance: LedgeScanHorizontalVector.magnitude)) return;

        // Scan down for a ledge
        Vector3 LedgeScanPos = LedgeScanVerticalPos + LedgeScanHorizontalVector;
        if (!Physics.Raycast(LedgeScanPos, -transform.up, out RaycastHit LedgeHit, cc.radius * LedgeClimbOffsetMult)) return;

        // Make sure the ledge is not too steep
        if (CanGrabLedge() && Vector3.Dot(LedgeHit.normal, Physics.gravity.normalized) < -0.866f) {
            // The surface of the ledge should always be behind the ledge wall
            if (Vector3.Dot(WallHit.point - LedgeHit.point, WallHit.normal) < 0) return;
            LastHangingNormal = WallHit.normal;
            utils.ResetTimer(WALL_HANG_TIMER);
        }
    }

    private bool IsCeiling(Vector3 normal) {
        return normal.y <= -0.17f;
    }

    private bool IsWall(Vector3 normal) {
        return normal.y > -0.17f && normal.y <= 0.34f;
    }

    private bool IsSlide(Vector3 normal) {
        return normal.y > 0.34f && normal.y <= 0.6f;
    }

    private bool IsFloor(Vector3 normal) {
        return normal.y > 0.6f;
    }

    private void UpdateWallConditions(Vector3 wall_normal) {
        utils.ResetTimer(WALL_HIT_TIMER);
        if (Vector3.Dot(GetGroundVelocity(), wall_normal) < -(WallJumpThresholdMult * cc.radius)) {
            // Are we jumping in a new direction (atleast 20 degrees difference)
            if (Vector3.Dot(PreviousWallJumpNormal, wall_normal) < 0.94f) {
                utils.ResetTimer(WALL_JUMP_TIMER);
                WallJumpReflect = Vector3.Reflect(current_velocity, wall_normal);
                if (JumpBuffered()) {
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
        if (wallRunEnabled && AlongWallVel.magnitude > (WallRunLimitMult * cc.radius) && Mathf.Abs(Vector3.Dot(wall_normal, transform.forward)) < WallRunClimbCosAngle && Vector3.Dot(AlongWallVel, transform.forward) > 0) {
            if (IsWallRunning() || CanWallRun(PreviousWallJumpPos, PreviousWallNormal, transform.position, wall_normal)) {
                // Get a small boost on new wall runs. Also prevent spamming wall boosts
                if (!IsWallRunning() && WallDistanceCheck()) {
                    if (AlongWallVel.magnitude < (WallRunSpeedMult * cc.radius)) {
                        current_velocity = UpWallVel + Mathf.Sign(Vector3.Dot(current_velocity, WallAxis)) * (WallRunSpeedMult * cc.radius) * WallAxis;
                    }
                    current_velocity.y = Math.Max(current_velocity.y + (WallRunImpulseMult * cc_standHeight), WallRunImpulseMult * cc_standHeight);
                }
                utils.ResetTimer(WALL_RUN_TIMER);
            }
        }
        // If we fail the wall run try to wall climb instead if we are looking at the wall
        else if (Vector3.Dot(transform.forward, -wall_normal) >= WallRunClimbCosAngle) {
            utils.ResetTimer(WALL_CLIMB_TIMER);
        }
        PreviousWallNormal = wall_normal;
    }

    private bool CanWallRun(Vector3 old_wall_pos, Vector3 old_wall_normal, Vector3 new_wall_pos, Vector3 new_wall_normal) {
        if (!wallRunEnabled) {
            return false;
        }
        bool wall_normal_check = Vector3.Dot(old_wall_normal, new_wall_normal) < 0.94f;
        if (old_wall_pos == Vector3.positiveInfinity) {
            return wall_normal_check;
        }
        // Allow wall running on the same normal if we move to a new position not along the wall
        else {
            return wall_normal_check || (WallDistanceCheck() && Mathf.Abs(Vector3.Dot((new_wall_pos - old_wall_pos).normalized, old_wall_normal)) > 0.34f);
        }
    }

    private bool WallDistanceCheck() {
        float horizontal_distance_sqr = Vector3.ProjectOnPlane(PreviousWallJumpPos - transform.position, Physics.gravity).sqrMagnitude;
        return float.IsNaN(horizontal_distance_sqr) || horizontal_distance_sqr > (Mathf.Pow(cc.radius, 2) * WallDistanceThresholdMult);
    }

    private void ProcessHits() {
        if (lastHit == null) {
            return;
        }
        // Save the most recent last hit
        currentHitNormal = lastHit.normal;
        currentHitPos = lastHit.point;

        if (IsFloor(currentHitNormal)) {
            ProcessFloorHit();
        }
        else if (IsSlide(currentHitNormal)) {
            ProcessSlideHit();
        }
        else if (IsWall(currentHitNormal)) {
            ProcessWallHit();
        }
        else {
            ProcessCeilingHit();
        }
        // Keep velocity in the direction of the plane if the plane is not a ceiling
        // Or if it is a ceiling only cancel out the velocity if we are moving fast enough into its normal
        if (Vector3.Dot(currentHitNormal, Physics.gravity) < 0 || Vector3.Dot(current_velocity, currentHitNormal) < -(cc.radius * 2f)) {
            current_velocity = Vector3.ProjectOnPlane(current_velocity, currentHitNormal);
        }

        // Set last hit null so we don't process it again
        lastHit = null;
    }

    private void ProcessFloorHit() {
        // On the ground
        utils.ResetTimer(LANDING_TIMER);

        // Handle buffered jumps
        if (JumpBuffered()) {
            // Buffer a jump
            willJump = true;
        }
        // Use MovingEntity for parenting on moving platforms
        MovingEntity moving_platform = lastHit.gameObject.GetComponent<MovingEntity>();
        if (moving_platform != null) {
            if (player_container.transform.parent != moving_platform.transform) {
                player_container.transform.parent = moving_platform.transform;
                // Keep custom velocity globally accurate
                current_velocity -= moving_platform.player_velocity;
            }
            lastMovingPlatform = moving_platform;
            utils.ResetTimer(MOVING_PLATFORM_TIMER);
        }

        if (currentHitNormal.y < 0.8f && IsSliding()) utils.ResetTimer(SLIDE_TIMER);
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        PreviousWallJumpPos = Vector3.positiveInfinity;
        lastFloorHitNormal = currentHitNormal;
    }

    private void ProcessSlideHit() {
        // Slides
        if (IsSliding()) utils.ResetTimer(SLIDE_TIMER);
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        PreviousWallJumpPos = Vector3.positiveInfinity;
    }

    private void ProcessWallHit() {
        if (!OnGround()) {
            UpdateWallConditions(currentHitNormal);
        }
    }

    private void ProcessCeilingHit() {
        // Overhang
        PreviousWallNormal = Vector3.zero;
        PreviousWallJumpNormal = Vector3.zero;
        PreviousWallJumpPos = Vector3.positiveInfinity;
    }
    #endregion

    #region HANDLE_GRAVITY
    private void HandleGravity() {
        if (!OnGround()) {
            accel += Physics.gravity * GravityMult * cc_standHeight * GravityTimeConstant;
        }
        else {
            // Push the character controller into the normal of the surface
            // This should trigger ground detection
            accel += -Mathf.Sign(lastFloorHitNormal.y) * Physics.gravity.magnitude * lastFloorHitNormal;
        }
        GravityMult = 1;
    }
    #endregion

    #region HANDLE_CROUCHING
    private void HandleCrouch() {
        if (ToggleCrouch) {
            if (!utils.CheckTimer(CROUCH_TIMER)) {
                if (!isCrouching) {
                    isCrouching = true;
                    SetCrouchHeight();
                }
                else if (CheckIfCanStand()) {
                    isCrouching = false;
                    SetStandHeight();
                }
                utils.SetTimerFinished(CROUCH_TIMER);
            }
        }
        else if (!isCrouching && input_manager.GetCrouchHold()) {
            isCrouching = true;
            SetCrouchHeight();
            utils.WaitUntilCondition(
                check: () => {
                    return (!input_manager.GetCrouchHold() && CheckIfCanStand());
                },
                action: () => {
                    isCrouching = false;
                    SetStandHeight();
                });
        }
    }

    private bool CheckIfCanStand() {
        Vector3 stand_center = transform.position + cc.center + transform.up * (cc_standCenter - cc.center.y);
        float radius = cc.radius;
        Vector3 start = stand_center - transform.up * (cc_standHeight / 2 - radius);
        Vector3 end = stand_center + transform.up * (cc_standHeight / 2 - radius);
        return !Physics.CheckCapsule(start, end, radius);
    }

    private void SetCrouchHeight() {
        cc.height = cc_standHeight / 2;
        cc.center = new Vector3(cc.center.x, cc_standCenter - (cc.height / 2), cc.center.z);
        wall_run_collider.height = wr_standHeight / 2;
        wall_run_collider.center = new Vector3(wall_run_collider.center.x, wr_standCenter - (wall_run_collider.height / 2), wall_run_collider.center.z);
        recovery_collider.height = re_standHeight / 2;
        recovery_collider.center = new Vector3(recovery_collider.center.x, re_standCenter - (recovery_collider.height / 2), recovery_collider.center.z);
    }

    private void SetStandHeight() {
        cc.height = cc_standHeight;
        cc.center = new Vector3(cc.center.x, cc_standCenter, cc.center.z);
        wall_run_collider.height = wr_standHeight;
        wall_run_collider.center = new Vector3(wall_run_collider.center.x, wr_standCenter, wall_run_collider.center.z);
        recovery_collider.height = re_standHeight;
        recovery_collider.center = new Vector3(recovery_collider.center.x, re_standCenter, recovery_collider.center.z);
    }
    #endregion

    #region HANDLE_MOVEMENT
    // Apply movement forces from input (FAST edition)
    private void HandleMovement() {
        // Handle moving collisions
        HandleMovingCollisions();
        HandleMovementAccumulator();

        // If we are hanging stay still
        if (IsHanging()) {
            current_velocity = Vector3.zero;
            GravityMult = 0;
            return;
        }

        Vector3 planevelocity = Vector3.ProjectOnPlane(current_velocity, lastFloorHitNormal);
        Vector3 movVec = GetMoveVector();
        // Force a 20% cutoff region where only the player is rotated but not moved
        float movmag = movVec.magnitude < 0.2f ? 0f : movVec.magnitude;
        // Do this first so we cancel out incremented time from update before checking it
        if (!OnGround()) {
            // We are in the air (for atleast LandingGracePeriod). We will slide on landing if moving fast enough.
            utils.ResetTimer(SLIDE_TIMER);
        }
        // Normal ground behavior
        if (OnGround() && !willJump && (!IsSliding() || planevelocity.magnitude < (SlideSpeedMult * cc.radius))) {
            // If we weren't fast enough we aren't going to slide
            utils.SetTimerFinished(SLIDE_TIMER);
            // dot(new_movVec, normal) = 0 --> dot(movVec, normal) + dot(up, normal)*k = 0 --> k = -dot(movVec, normal)/dot(up, normal)
            float slope_correction = -Vector3.Dot(movVec, lastFloorHitNormal) / Vector3.Dot(transform.up, lastFloorHitNormal);
            //if (slope_correction < 0f) {
            movVec += slope_correction * transform.up;
            //}
            // Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), movVec, Color.green);
            accelerate(movVec, RunSpeedMult * cc.radius * movmag, (GroundAccelerationMult * cc.radius), true);
        }
        // We are either in the air, buffering a jump, or sliding (recent contact with ground). Use air accel.
        else {
            // Handle wall movement
            if (IsWallRunning()) {
                float away_from_wall_speed = Vector3.Dot(current_velocity, PreviousWallNormal);
                // Only remove velocity if we are attempting to move away from the wall
                if (away_from_wall_speed > 0) {
                    // Remove the component of the wall normal velocity that is along the gravity axis
                    float gravity_resist = Vector3.Dot(away_from_wall_speed * PreviousWallNormal, Physics.gravity.normalized);
                    float previous_velocity_mag = current_velocity.magnitude;
                    current_velocity -= (away_from_wall_speed * PreviousWallNormal - gravity_resist * Physics.gravity.normalized);
                    // consider adding a portion of the lost velocity back along the wall axis
                    current_velocity += WallAxis * Mathf.Sign(Vector3.Dot(current_velocity, WallAxis)) * (previous_velocity_mag - current_velocity.magnitude);
                }
                if (Vector3.Dot(UpWallVel, Physics.gravity) >= 0) {
                    GravityMult = 0.25f;
                }
            }
            accelerate(movVec, AirSpeedMult * cc.radius * movmag, (AirAccelerationMult * cc.radius), false);
        }
    }

    private void HandleMovingCollisions() {
        if (!InMovingCollision() && !OnMovingPlatform() && !InMovingInterior()) {
            moving_frame_velocity = Vector3.zero;
            if (player_container.transform.parent != null) {
                // Inherit velocity from previous platform
                if (lastMovingPlatform != null) {
                    // Keep custom velocity globally accurate
                    current_velocity += lastMovingPlatform.player_velocity;
                }
                player_container.transform.parent = null;
            }
            lastMovingPlatform = null;
        }
        else if (InMovingCollision() && !OnMovingPlatform() && !InMovingInterior() && lastMovingPlatform != null) {
            moving_frame_velocity = lastMovingPlatform.player_velocity;
        }
        else if (InMovingInterior() || OnMovingPlatform()) {
            moving_frame_velocity = Vector3.zero;
        }
    }

    private void HandleMovementAccumulator() {
        if (!OnGround()) {
            moveAccumulator = moveAccumulator.normalized * Mathf.Clamp(moveAccumulator.magnitude - 1f + GetMoveVector().magnitude, 0f, MoveAccumulatorMax);
            return;
        }

        Vector3 move = Vector3.ProjectOnPlane(current_velocity, lastFloorHitNormal) * Mathf.Pow(GetMoveVector().magnitude, 2);
        move = Vector3.ProjectOnPlane(move, Physics.gravity).normalized * move.magnitude;
        Vector3 move_percent_vec = moveAccumulator / MoveAccumulatorMax;
        Vector3 delta = MoveAccumulatorScaleFactor * (move - (0.8f * RunSpeedMult * cc.radius * move_percent_vec)) * Time.fixedDeltaTime;
        if (move.magnitude > 0.1f * RunSpeedMult * cc.radius) {
            moveAccumulator = Vector3.ClampMagnitude(moveAccumulator + delta, MoveAccumulatorMax);
        }
        else {
            moveAccumulator = Vector3.zero;
        }
        // if (move_percent_vec.magnitude > 0.9f) Debug.DrawRay(transform.position, moveAccumulator, Color.green);
        // else Debug.DrawRay(transform.position, moveAccumulator, Color.red);
    }

    // Try to accelerate to the desired speed in the direction specified
    private void AccelerateCPM(Vector3 direction, float desiredSpeed, float acceleration, bool grounded) {
        if (!grounded && IsWallRunning()) {
            return;
        }
        direction.Normalize();
        float moveAxisSpeed = Vector3.Dot(current_velocity, direction);
        float deltaSpeed = desiredSpeed - moveAxisSpeed;
        if (deltaSpeed < 0) {
            // Gotta go fast
            return;
        }

        // Scale acceleration by speed because we want to go fast
        deltaSpeed = Mathf.Clamp(acceleration * Time.deltaTime * desiredSpeed, 0, deltaSpeed);
        current_velocity += deltaSpeed * direction;

        if (grounded) {
            accel += -(current_velocity + moving_frame_velocity) * SpeedDampMult * cc.radius;
        }
        else {
            accel += -Vector3.ProjectOnPlane(current_velocity + moving_frame_velocity, Physics.gravity) * AirSpeedDampMult * cc.radius;
        }

    }

    // Regular acceleration
    private void AccelerateStandard(Vector3 direction, float desiredSpeed, float acceleration, bool grounded) {
        if (!grounded && IsWallRunning()) {
            return;
        }
        direction.Normalize();
        /*float turn_constant = 1f;
        if (grounded)
        {
            turn_constant = 0.55f + Mathf.Sign(Vector3.Dot(current_velocity.normalized, direction))*Mathf.Pow(Vector3.Dot(current_velocity.normalized, direction), 2f) * 0.45f;
        }*/
        Vector3 deltaVel = direction * acceleration * Time.deltaTime;
        if (grounded) {
            // Accelerate if we aren't at the desired speed
            Vector3 plane_normal;
            bool onRotatingPlatform = OnMovingPlatform() && (lastMovingPlatform is RotatingCollider);
            if (onRotatingPlatform) plane_normal = Physics.gravity;
            else plane_normal = lastFloorHitNormal;
            Vector3 new_plane_velocity = Vector3.ProjectOnPlane(current_velocity + deltaVel, plane_normal);
            Vector3 plane_velocity = Vector3.ProjectOnPlane(current_velocity, plane_normal);
            if (new_plane_velocity.magnitude <= desiredSpeed) {
                current_velocity += deltaVel;
            }
            else if (plane_velocity.magnitude <= desiredSpeed && !onRotatingPlatform) {
                current_velocity = Vector3.ClampMagnitude(new_plane_velocity, desiredSpeed) + (current_velocity - plane_velocity);
            }
            /*else if (desiredSpeed > GetGroundVelocity().magnitude) {
                current_velocity = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(current_velocity + deltaVel, Physics.gravity), desiredSpeed) + (current_velocity + Vector3.Project(deltaVel, Physics.gravity) - GetGroundVelocity());
            }*/
            accel += -Vector3.ProjectOnPlane(current_velocity + moving_frame_velocity, lastFloorHitNormal) * SpeedDampMult * cc.radius;
        }
        else {
            if (Vector3.ProjectOnPlane(current_velocity + deltaVel, Physics.gravity).magnitude <= Mathf.Max(GetGroundVelocity().magnitude, MaxAirSpeedMult * cc.radius)) {
                current_velocity += deltaVel;
            }
            else {
                current_velocity = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(current_velocity + deltaVel, Physics.gravity), GetGroundVelocity().magnitude) + (current_velocity - GetGroundVelocity());
                //current_velocity = current_velocity - (deltaVel.magnitude * GetGroundVelocity().normalized) + deltaVel;
            }
            accel += -Vector3.ProjectOnPlane(current_velocity + moving_frame_velocity, Physics.gravity) * AirSpeedDampMult * cc.radius;
        }
        //Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), deltaVel, Color.cyan, Time.fixedDeltaTime);
        //Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), current_velocity, Color.red, Time.fixedDeltaTime);
        //Debug.DrawRay(transform.position + transform.up * (cc.height / 2 + 1f), accel, Color.blue, Time.fixedDeltaTime);
    }
    #endregion

    #region HANDLE_CLIMBING
    private void HandleClimbing() {
        // HandleClimbableSurfaces();
    }

    private void HandleClimbableSurfaces() {
        if (!OnGround()) {
            // Make sure we are running into a wall
            if (!IsOnWall()) return;
            GravityMult = 0f;
            Accelerate(-PreviousWallNormal * cc.radius * 20f);
            if (Vector3.Dot(current_velocity, PreviousWallNormal) > 0) {
                current_velocity = Vector3.ProjectOnPlane(current_velocity, PreviousWallNormal);
            }
            Accelerate(-Vector3.ProjectOnPlane(current_velocity, PreviousWallNormal) * ClimbSpeedDampMult * cc.radius);
            float wallAxisMove = Vector3.Dot(GetMoveVector(), -PreviousWallNormal);
            Debug.DrawRay(transform.position, GetMoveVector(), Color.green);
            Debug.DrawRay(transform.position, transform.up * wallAxisMove, Color.red);
            Accelerate(transform.up * wallAxisMove * ClimbSpeedMult * cc.radius);
            player_camera.RotatePlayerToward(Vector3.ProjectOnPlane(-PreviousWallNormal, transform.up), 0.5f);
            utils.ResetTimer(REGRAB_TIMER);
            Debug.DrawRay(currentHitPos, currentHitNormal, Color.blue, 10f);
        }
        else {
            if (IsOnWall()) Debug.Log("On wall on ground");
        }
    }

    private IEnumerator SeekTarget(Vector3 target) {
        bool seeking = true;
        float seek_time = 0.5f;
        float speed = (target - transform.position).magnitude / seek_time;
        SetCollision(false);
        utils.WaitAndRun(seek_time, () => { seeking = false; });
        while (seeking) {
            transform.position = transform.position + ((target - transform.position).normalized * speed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        SetCollision(true);
    }

    private bool CapsuleCastPlayer(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance) {
        float radius = cc.radius;
        Vector3 start = origin - transform.up * (cc.height / 2 - radius);
        Vector3 end = origin + transform.up * (cc.height / 2 - radius);
        return Physics.CapsuleCast(start, end, radius, direction, out hitInfo, maxDistance);
    }

    private bool ScanForNearestStep(Vector3 scanDir, float scanReach, out RaycastHit stepSurface, out RaycastHit stepWall, float scanDepthOffset = 0f) {
        // This constant will change with character size
        float stepLipOffset = 0.2f;
        stepSurface = default(RaycastHit);
        stepWall = default(RaycastHit);
        scanDir.Normalize();

        // Scan for a step infront of us
        Vector3 stepScanOrigin = transform.position + cc.center + (scanDir * scanReach) + (transform.up * scanDepthOffset);
        float scanDepth = cc.height * 0.5f + scanDepthOffset;
        if (!Physics.Raycast(origin: stepScanOrigin, direction: -transform.up, out RaycastHit stepSurfaceHit, maxDistance: scanDepth)) return false;
        if (!IsFloor(stepSurfaceHit.normal)) return false;

        // Scan for the wall of the step
        float stepWallDistance = Vector3.Project(stepSurfaceHit.point - transform.position, scanDir).magnitude;
        Vector3 stepGapScanOrigin = transform.position + Vector3.Project(stepSurfaceHit.point - transform.position, transform.up) + (transform.up * stepLipOffset);
        // First make sure there is no wall above the step
        if (Physics.Raycast(origin: stepGapScanOrigin, direction: scanDir, maxDistance: stepWallDistance)) return false;
        Vector3 stepWallScanOrigin = transform.position + Vector3.Project(stepSurfaceHit.point - transform.position, transform.up) - (transform.up * stepLipOffset);
        if (!CapsuleCastPlayer(origin: stepWallScanOrigin, direction: scanDir, out RaycastHit stepWallHit, maxDistance: stepWallDistance)) return false;
        if (!IsWall(stepWallHit.normal)) return false;

        // Make sure the wall is in front of the step
        if (Vector3.Dot((stepWallHit.point - stepSurfaceHit.point), stepWallHit.normal) < 0f) return false;

        // The step surface is a valid step, so we can return it
        stepSurface = stepSurfaceHit;
        stepWall = stepWallHit;
        return true;
    }
    #endregion

    #region HANDLE_JUMPING
    // Handle jumping
    private void HandleJumping() {
        // Ground detection for friction and jump state
        if (OnGround() || IsHanging()) {
            isJumping = false;
            isFalling = false;
            ShortHopTempDisable = false;
        }

        // Add additional gravity when going down (optional)
        if (current_velocity.y < 0) {
            GravityMult += DownGravityAdd;
        }

        // Handle jumping and falling
        if (JumpBuffered()) {
            if (OnGround() || CanWallJump() || IsWallRunning() || IsHanging()) {
                DoJump();
            }
        }
        // Fall fast when we let go of jump (optional)
        if (!isFalling && isJumping && !input_manager.GetJumpHold()) {
            if (ShortHopEnabled && !ShortHopTempDisable && Vector3.Dot(current_velocity, Physics.gravity.normalized) < 0) {
                current_velocity -= Vector3.Project(current_velocity, Physics.gravity.normalized) / 2;
            }
            isFalling = true;
        }


    }

    // Set the player to a jumping state
    private void DoJump() {
        if (CanWallJump() || IsWallRunning()) {
            PreviousWallJumpPos = transform.position;
            PreviousWallJumpNormal = PreviousWallNormal;
        }
        if (!IsHanging() && utils.GetTimerTime(JUMP_METER) > JumpMeterThreshold) {
            if (JumpBoostEnabled && CanJumpBoost()) {
                Vector3 movvec = GetMoveVector().normalized;
                float pathvel = Vector3.Dot(current_velocity, movvec);
                float newspeed = Mathf.Clamp(pathvel + (JumpBoostAddMult * cc.radius), 0f, JumpBoostSpeedMult * cc.radius);
                current_velocity += (Mathf.Max(newspeed, pathvel) - pathvel) * movvec;
            }
            if (!OnGround() && CanWallJump() && WallJumpReflect.magnitude > 0) {
                //Debug.Log("Wall Jump");
                current_velocity += (WallJumpReflect - current_velocity) * WallJumpBoost * JumpMeterComputed;
                if (conserveUpwardMomentum) {
                    current_velocity.y = Math.Max(current_velocity.y + (WallJumpSpeedMult * cc_standHeight * JumpMeterComputed), WallJumpSpeedMult * cc_standHeight * JumpMeterComputed);
                }
                else {
                    current_velocity.y = Math.Max(current_velocity.y, WallJumpSpeedMult * cc_standHeight * JumpMeterComputed);
                }
                utils.ResetTimer(JUMP_METER);
                utils.SetTimerFinished(WALL_HIT_TIMER);
            }
            else if (!OnGround() && IsWallRunning()) {
                //Debug.Log("Wall Run Jump");
                current_velocity += PreviousWallNormal * WallRunJumpSpeedMult * cc.radius * JumpMeterComputed;
                float pathvel = Vector3.Dot(current_velocity, transform.forward);
                float newspeed = Mathf.Clamp(pathvel + (WallRunJumpBoostAddMult * cc.radius * JumpMeterComputed), 0f, WallRunJumpBoostSpeedMult * cc.radius);
                current_velocity += transform.forward * (newspeed - pathvel);
                current_velocity.y = Math.Max(current_velocity.y, WallRunJumpUpSpeedMult * cc_standHeight * JumpMeterComputed);
                utils.ResetTimer(JUMP_METER);
                utils.SetTimerFinished(WALL_HIT_TIMER);
            }
            else if (OnGround()) {
                //Debug.Log("Upward Jump");
                if (conserveUpwardMomentum) {
                    current_velocity.y = Math.Max(current_velocity.y + (JumpVelocityMult * cc_standHeight * JumpMeterComputed), JumpVelocityMult * cc_standHeight * JumpMeterComputed);
                }
                else {
                    current_velocity.y = Math.Max(current_velocity.y, JumpVelocityMult * cc_standHeight * JumpMeterComputed);
                }
            }
            foreach (Action callback in jump_callback_table) {
                callback();
            }
        }
        else if (IsHanging()) {
            current_velocity.y = Mathf.Sqrt(2 * cc.height * 1.1f * Physics.gravity.magnitude * cc_standHeight * GravityTimeConstant);
            PreviousWallNormal = Vector3.zero;
            PreviousWallJumpNormal = Vector3.zero;
            PreviousWallJumpPos = Vector3.positiveInfinity;
        }
        utils.ResetTimer(REGRAB_TIMER);
        isJumping = true;
        isFalling = false;
        willJump = false;

        // Intentionally set the timers over the limit
        utils.SetTimerFinished(BUFFER_JUMP_TIMER);
        utils.SetTimerFinished(LANDING_TIMER);
        utils.SetTimerFinished(WALL_JUMP_TIMER);
        utils.SetTimerFinished(WALL_RUN_TIMER);
        utils.SetTimerFinished(WALL_CLIMB_TIMER);
        utils.SetTimerFinished(WALL_HANG_TIMER);
        utils.SetTimerFinished(MOVING_PLATFORM_TIMER);

        WallJumpReflect = Vector3.zero;
    }

    public void RegisterJumpCallback(Action callback) {
        jump_callback_table.Add(callback);
    }

    public void UnregisterJumpCallback(Action callback) {
        jump_callback_table.Remove(callback);
    }
    #endregion

    #region HANDLE_USE
    private void HandleUse() {
        if (!utils.CheckTimer(USE_TIMER)) {
            GameObject usable_object;
            IUsable[] usables = utils.RayCastExplosiveSelectAll<IUsable>(
                origin: transform.position,
                path: transform.forward * 2f * cc.radius,
                radius: cc.radius * 3f,
                gameObject: out usable_object);
            // Let the physprophandler try and handle the use first. If it returns false, handle it here.
            if (usables.Any() && !physhandler.HandleUse(usable_object)) {
                foreach (IUsable usable in usables) {
                    usable.Use();
                }
            }
            utils.SetTimerFinished(USE_TIMER);
        }
    }
    #endregion

    #region PUBLIC_OUTPUT_INTERFACE
    public Vector3 GetMoveVector() {
        if (player_camera == null) return Vector3.zero;
        Vector3 movVec = (input_manager.GetMoveVertical() * player_camera.yaw_pivot.transform.forward +
                          input_manager.GetMoveHorizontal() * player_camera.yaw_pivot.transform.right);
        return Vector3.ClampMagnitude(movVec, 1f);
    }

    // Double check if on ground using a separate test
    public bool OnGround() {
        return IsOwner && !utils.CheckTimer(LANDING_TIMER);
    }

    public bool JumpBuffered() {
        return !utils.CheckTimer(BUFFER_JUMP_TIMER);
    }

    public bool IsSliding() {
        return !utils.CheckTimer(SLIDE_TIMER);
    }

    public bool CanWallJump() {
        return wallJumpEnabled && !utils.CheckTimer(WALL_JUMP_TIMER);
    }

    public bool IsWallRunning() {
        return wallRunEnabled && !utils.CheckTimer(WALL_RUN_TIMER);
    }

    public bool IsWallClimbing() {
        return wallClimbEnabled && !utils.CheckTimer(WALL_CLIMB_TIMER);
    }

    public bool IsOnWall() {
        return !utils.CheckTimer(WALL_HIT_TIMER);
    }

    public bool CanGrabLedge() {
        return wallClimbEnabled && utils.CheckTimer(REGRAB_TIMER);
    }

    public bool IsHanging() {
        return !utils.CheckTimer(WALL_HANG_TIMER);
    }

    public bool IsCrouching() {
        return isCrouching;
    }

    public bool InMovingCollision() {
        return !utils.CheckTimer(MOVING_COLLIDER_TIMER);
    }

    public bool OnMovingPlatform() {
        return !utils.CheckTimer(MOVING_PLATFORM_TIMER);
    }

    public bool InMovingInterior() {
        return !utils.CheckTimer(MOVING_INTERIOR_TIMER);
    }

    public void StayInMovingInterior() {
        utils.ResetTimer(MOVING_INTERIOR_TIMER);
    }

    public void ExitMovingInterior() {
        utils.SetTimerFinished(MOVING_INTERIOR_TIMER);
    }

    public bool IsStuck() {
        return !utils.CheckTimer(STUCK_TIMER);
    }

    public Vector3 GetLastWallNormal() {
        return PreviousWallNormal;
    }

    public Vector3 GetLastHangingNormal() {
        return LastHangingNormal;
    }

    public Vector3 GetGroundVelocity(bool use_cc = false) {
        if (use_cc) return Vector3.ProjectOnPlane(cc.velocity, Physics.gravity);
        return Vector3.ProjectOnPlane(current_velocity, Physics.gravity);
    }

    public Vector3 GetPlaneVelocity(bool use_cc = false) {
        if (use_cc) return Vector3.ProjectOnPlane(cc.velocity, lastFloorHitNormal);
        return Vector3.ProjectOnPlane(current_velocity, lastFloorHitNormal);
    }

    public Vector3 GetWorldVelocity() {
        if (OnMovingPlatform() && lastMovingPlatform != null) {
            // Keep custom velocity globally accurate
            return current_velocity + lastMovingPlatform.player_velocity;
        }
        return current_velocity + moving_frame_velocity;
    }

    public float GetHeadHeight() {
        return ((cc.height / 2) - cc.radius + cc.center.y);
    }

    public Vector3 GetHeadPosition() {
        return transform.position + cc.center + ((cc.height / 2 - cc.radius) * transform.up);
    }

    public Vector3 GetFootPosition() {
        return transform.position + cc.center - (cc.height / 2 * transform.up);
    }

    public bool CanJumpBoost() {
        bool can_jump_boost = true;
        // Ground behavior
        if (OnGround()) {
            Vector3 ground_vel = GetGroundVelocity();
            Vector3 plane_vel = Vector3.ProjectOnPlane(current_velocity, lastFloorHitNormal);
            can_jump_boost &= (plane_vel.magnitude > (JumpBoostRequiredSpeedMult * cc.radius));
            can_jump_boost &= (Vector3.Dot(GetMoveVector(), ground_vel.normalized) > 0.7f);
            can_jump_boost &= (moveAccumulator.magnitude / MoveAccumulatorMax > 0.9f);
        }
        else {
            can_jump_boost = false;
        }
        return can_jump_boost;
    }

    public Vector3 GetVelocity() => current_velocity;

    public Vector3 GetAcceleration() => accel;
    #endregion

    #region PUBLIC_INPUT_INTERFACE
    public Vector3 Accelerate(Vector3 acceleration) {
        accel += acceleration;
        return accel;
    }

    public void SetVelocity(Vector3 velocity) {
        current_velocity = velocity;
    }

    public void Recover(Collider other) {
        if (!IsOwner) return;
        utils.ResetTimer(STUCK_TIMER);
        utils.SetTimerFinished(WALL_HANG_TIMER);

        Vector3 closest_point = other.ClosestPointOnBounds(transform.position);
        Vector3 path_to_point = closest_point - transform.position;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, path_to_point, out hit, path_to_point.magnitude * 1.5f)) {
            Teleport(transform.position + hit.normal * cc.radius);
        }
        else if (Physics.Raycast(transform.position + 2 * path_to_point, -path_to_point, out hit, path_to_point.magnitude * 1.5f)) {
            Teleport(transform.position + hit.normal * cc.radius);
        }
        else if (position_history.Count > 0) {
            Teleport(position_history.First.Value);
            position_history.RemoveFirst();
        }
        else {
            Teleport(StartPos);
        }
    }

    public void RecoverSafe(Collider other) {
        if (!IsOwner) return;
        utils.ResetTimer(STUCK_TIMER);
        utils.SetTimerFinished(WALL_HANG_TIMER);

        Vector3 path_from_center = Vector3.ProjectOnPlane(transform.position - other.bounds.center, Physics.gravity);
        Teleport(transform.position + path_from_center.normalized * cc.radius * 0.25f);
    }

    private bool OverlapPlayerCheck() {
        return OverlapPlayerCheck(Vector3.zero);
    }

    private bool OverlapPlayerCheck(Vector3 offset) {
        float radius = cc.radius;
        Vector3 origin = transform.position + cc.center + offset;
        Vector3 start = origin - (transform.up * (cc.height / 2 - radius));
        Vector3 end = origin + (transform.up * (cc.height / 2 - radius));
        return Physics.CheckCapsule(start, end, radius, DEFAULT_LAYER);
    }

    public void RecoverSmart() {
        float radius = cc.radius;
        Vector3 start = transform.position - transform.up * (cc.height / 2 - radius);
        Vector3 end = transform.position + transform.up * (cc.height / 2 - radius);
        Collider[] cols = Physics.OverlapCapsule(start, end, radius, DEFAULT_LAYER);
        Collider cc_col = GetComponent<Collider>();
        if (cols.Length > 0) SetCollision(false);
        foreach (Collider col in cols) {
            Debug.Log("smart recovering");
            Debug.Log($"CC_COL {cc_col.name}");
            Debug.Log($"CC_COL t: {cc_col.isTrigger}");
            Debug.Log($"cols: {string.Join(", ", cols.Select((c) => c.name))}");
            if (Physics.ComputePenetration(cc_col, cc_col.transform.position, cc_col.transform.rotation,
                                          col, col.transform.position, col.transform.rotation,
                                          out Vector3 dir, out float dist)) {
                transform.position += (dir * dist);
            }
        }
        if (cols.Length > 0) SetCollision(true);
    }

    private void SetCollision(bool on) {
        if (on) {
            foreach (Collider col in GetComponents<Collider>()) {
                col.enabled = true;
            }
            enableMovement = true;
        }
        else {
            foreach (Collider col in GetComponents<Collider>()) {
                col.enabled = false;
            }
            enableMovement = false;
        }
    }

    private void Teleport(Vector3 position) {
        SetCollision(false);
        transform.position = position;
        SetCollision(true);
    }
    #endregion
}
