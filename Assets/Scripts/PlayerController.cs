using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

delegate void AccelerationFunction(Vector3 direction, float desiredSpeed, float acceleration);

public class PlayerController : MonoBehaviour {
    [Header("Linked Components")]
    public InputManager input_manager;
    public CharacterController cc;
    public Collider WallRunCollider;
    [HideInInspector]
    public Camera player_camera;
    [Header("Movement constants")]
    public float RunSpeed;
    public float AirSpeed;
    public float GroundAcceleration;
    public float AirAcceleration;
    public float SpeedDamp;
    public float AirSpeedDamp;
    public float SlideSpeed;
    public float DownGravityAdd;
    public float ShortHopGravityAdd;
    public float JumpVelocity;
    public float WallJumpThreshold;
    public float WallJumpBoost;
    public float WallRunLimit;
    public float WallJumpSpeed;
    public float WallRunJumpSpeed;
    public float WallRunJumpUpSpeed;
    public Vector3 StartPos;

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
    private bool conserveUpwardMomentum;
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
    private float LedgeClimbOffset;
    private float LedgeClimbBoost;
    private float WallDistanceThreshold;
    private bool wallRunEnabled;
    private bool wallJumpEnabled;
    private bool wallClimbEnabled;

    // Physics state variables
    private AccelerationFunction accelerate;
    private Vector3 moving_frame_velocity;
    private Vector3 current_velocity;
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

    // Use this for initialization
    private void Start () {
        // Movement values
        //SetThirdPersonActionVars();
        SetShooterVars();

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
        JumpMeterSize = 0.3f;
        JumpMeterThreshold = JumpMeterSize / 3;
        JumpMeter = JumpMeterSize;
        JumpMeterComputed = JumpMeter / JumpMeterSize;
        jumpGracePeriod = 0.1f;
        LandingTimeDelta = jumpGracePeriod;
        BufferJumpGracePeriod = 0.1f;
        BufferJumpTimeDelta = BufferJumpGracePeriod;
        SlideGracePeriod = 0.2f;
        SlideTimeDelta = SlideGracePeriod;
        WallJumpGracePeriod = 0.2f;
        WallJumpTimeDelta = WallJumpGracePeriod;
        WallRunGracePeriod = 0.2f;
        WallRunTimeDelta = WallRunGracePeriod;
        WallClimbGracePeriod = 0.2f;
        WallClimbTimeDelta = WallClimbGracePeriod;
        ReGrabGracePeriod = 0.5f;
        ReGrabTimeDelta = ReGrabGracePeriod;
        MovingColliderGracePeriod = 0.1f;
        MovingColliderTimeDelta = MovingColliderGracePeriod;
        MovingPlatformGracePeriod = 0.1f;
        MovingPlatformTimeDelta = MovingPlatformGracePeriod;
        StuckGracePeriod = 0.2f;
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

        Physics.IgnoreCollision(WallRunCollider, cc);
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
        ShortHopGravityAdd = 0;
        // Jump/Wall modifiers
        JumpVelocity = 12f;
        WallJumpThreshold = 8f;
        WallJumpBoost = 1.0f;
        WallJumpSpeed = 12f;
        WallRunLimit = 8f;
        WallRunJumpSpeed = 15f;
        WallRunJumpUpSpeed = 12f;
        WallRunImpulse = 0.0f;
        WallRunSpeed = 15.0f;
        conserveUpwardMomentum = true;
        wallJumpEnabled = true;
        wallRunEnabled = true;
        wallClimbEnabled = true;
        accelerate = AccelerateCPM;
    }

    private void SetThirdPersonActionVars()
    {
        // Movement modifiers
        RunSpeed = 15f;
        AirSpeed = 15f;
        GroundAcceleration = 100;
        AirAcceleration = 20;
        SpeedDamp = 5f;
        AirSpeedDamp = 0.01f;
        SlideSpeed = 18f;
        // Gravity modifiers
        DownGravityAdd = 0;
        ShortHopGravityAdd = 0;
        // Jump/Wall modifiers
        JumpVelocity = 16f;
        WallJumpThreshold = 8f;
        WallJumpBoost = 1.0f;
        WallJumpSpeed = 12f;
        WallRunLimit = 8f;
        WallRunJumpSpeed = 12f;
        WallRunJumpUpSpeed = 12f;
        WallRunImpulse = 0.0f;
        WallRunSpeed = 15f;
        conserveUpwardMomentum = false;
        wallJumpEnabled = true;
        wallRunEnabled = false;
        wallClimbEnabled = true;
        accelerate = AccelerateStandard;
    }

    // Fixed Update is called once per physics tick
    private void FixedUpdate () {
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
        if (error_bucket >= error_threshold)
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
        if (Vector3.Dot(Vector3.ProjectOnPlane(current_velocity, Physics.gravity), wall_normal) < -WallJumpThreshold)
        {
            // Are we jumping in a new direction (atleast 20 degrees difference)
            if (Vector3.Dot(PreviousWallJumpNormal, wall_normal) < 0.94f)
            {
                WallJumpTimeDelta = 0;
                WallJumpReflect = Vector3.Reflect(current_velocity, wall_normal);
                if (BufferJumpTimeDelta < BufferJumpGracePeriod)
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
        if (AlongWallVel.magnitude > WallRunLimit && Mathf.Abs(Vector3.Dot(wall_normal, transform.forward)) < 0.866f && Vector3.Dot(AlongWallVel, transform.forward) > 0)
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
        else if (isHanging || Vector3.Dot(transform.forward, -wall_normal) >= 0.866f)
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
        if (BufferJumpTimeDelta < BufferJumpGracePeriod)
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

        Vector3 planevelocity;
        Vector3 movVec = (input_manager.GetMoveVertical() * transform.forward +
                          input_manager.GetMoveHorizontal() * transform.right);
        float movmag = movVec.magnitude < 0.8f ? movVec.magnitude : 1f;
        // Do this first so we cancel out incremented time from update before checking it
        if (!OnGround())
        {
            // We are in the air (for atleast LandingGracePeriod). We will slide on landing if moving fast enough.
            SlideTimeDelta = 0;
            planevelocity = current_velocity;
        }
        else
        {
            planevelocity = Vector3.ProjectOnPlane(current_velocity, currentHit.normal);
        }
        // Normal ground behavior
        if (OnGround() && !willJump && (SlideTimeDelta >= SlideGracePeriod || planevelocity.magnitude < SlideSpeed))
        {
            // If we weren't fast enough we aren't going to slide
            SlideTimeDelta = SlideGracePeriod;
            // Use character controller grounded check to be certain we are actually on the ground
            movVec = Vector3.ProjectOnPlane(movVec, currentHit.normal);
            accelerate(movVec, RunSpeed*movmag, GroundAcceleration);
            accel += -(current_velocity + moving_frame_velocity) * SpeedDamp;
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
            else
            {
                accelerate(movVec, AirSpeed * movmag, AirAcceleration);
            }
            accel += -Vector3.ProjectOnPlane(current_velocity + moving_frame_velocity, transform.up) * AirSpeedDamp;
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
    private void AccelerateCPM(Vector3 direction, float desiredSpeed, float acceleration)
    {
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
    }

    // Regular acceleration
    private void AccelerateStandard(Vector3 direction, float desiredSpeed, float acceleration)
    {
        Vector3 deltaVel = direction.normalized * acceleration * Time.deltaTime;
        // Accelerate if we aren't at the desired speed
        if (Vector3.ProjectOnPlane(current_velocity + deltaVel, Physics.gravity).magnitude <= desiredSpeed)
        {
            current_velocity += deltaVel;
        }
        // If we are past the desired speed, subtract the deltaVel off and add it back in the direction we want
        else
        {
            current_velocity = current_velocity - (deltaVel.magnitude * Vector3.ProjectOnPlane(current_velocity, Physics.gravity).normalized) + deltaVel;
        }
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
        if (input_manager.GetJump())
        {
            BufferJumpTimeDelta = 0;
            if (OnGround() || CanWallJump() || IsWallRunning() || isHanging)
            {
                DoJump();
            }
        }
        if (willJump)
        {
            DoJump();
        }
        // Fall fast when we let go of jump (optional)
        if (isFalling || isJumping && !input_manager.GetJumpHold())
        {
            GravityMult += ShortHopGravityAdd;
            isFalling = true;
        }
    }

    // Double check if on ground using a separate test
    private bool OnGround()
    {
        return (LandingTimeDelta < jumpGracePeriod);
    }

    private bool IsWallRunning()
    {
        return wallRunEnabled && (WallRunTimeDelta < WallRunGracePeriod);
    }

    private bool IsWallClimbing()
    {
        return wallClimbEnabled && (WallClimbTimeDelta < WallClimbGracePeriod);
    }

    private bool CanGrabLedge()
    {
        return wallClimbEnabled && (ReGrabTimeDelta >= ReGrabGracePeriod);
    }

    private bool CanWallJump()
    {
        return wallJumpEnabled && (WallJumpTimeDelta < WallJumpGracePeriod);
    }

    private bool InMovingCollision()
    {
        return (MovingColliderTimeDelta < MovingColliderGracePeriod);
    }

    private bool OnMovingPlatform()
    {
        return (MovingPlatformTimeDelta < MovingPlatformGracePeriod);
    }

    private bool IsStuck()
    {
        return (StuckTimeDelta < StuckGracePeriod);
    }

    private bool WallDistanceCheck()
    {
        float horizontal_distance_sqr = Vector3.ProjectOnPlane(PreviousWallJumpPos - transform.position, Physics.gravity).sqrMagnitude;
        return float.IsNaN(horizontal_distance_sqr) || horizontal_distance_sqr > WallDistanceThreshold;
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
        }
        else if (isHanging)
        {
            current_velocity.y = LedgeClimbBoost;
        }
        ReGrabTimeDelta = 0;
        isJumping = true;
        willJump = false;
        isHanging = false;

        // Intentionally set the timers over the limit
        BufferJumpTimeDelta = BufferJumpGracePeriod;
        WallJumpTimeDelta = WallJumpGracePeriod;
        WallRunTimeDelta = WallRunGracePeriod;
        WallClimbTimeDelta = WallClimbGracePeriod;
        LandingTimeDelta = jumpGracePeriod;
        MovingPlatformTimeDelta = MovingPlatformGracePeriod;
        WallJumpReflect = Vector3.zero;
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
        Debug.Log("Attempting to recover from stuck collision");
        StuckTimeDelta = 0;
        isHanging = false;
        if (position_history.Count > 0)
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
