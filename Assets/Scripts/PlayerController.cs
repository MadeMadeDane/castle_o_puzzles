using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    [Header("Linked Components")]
    public InputManager input_manager;
    public CharacterController cc;
    public Camera player_camera;
    [Header("Movement constants")]
    public float maxSpeed;
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
    public float WallClimbLimit;
    public Vector3 StartPos;

    // Jumping state variables
    private float SlideGracePeriod;
    private float SlideTimeDelta;
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
    private Vector3 WallJumpReflect;
    private Vector3 PreviousWallNormal;

    // Physics state variables
    private Vector3 current_velocity;
    private Vector3 accel;
    private ControllerColliderHit lastHit;
    private ControllerColliderHit currentHit;
    private float GravityMult;

    // Use this for initialization
    private void Start () {
        // Movement values
        maxSpeed = 4;
        RunSpeed = 9;
        AirSpeed = 0.90f;
        GroundAcceleration = 20;
        AirAcceleration = 500;
        SpeedDamp = 10f;
        AirSpeedDamp = 0.01f;
        SlideSpeed = 12f;

        // Gravity modifiers
        DownGravityAdd = 0;
        ShortHopGravityAdd = 0;
        
        // Jump states/values
        JumpVelocity = 12;
        WallJumpThreshold = 5f;
        WallJumpBoost = 1.0f;
        WallRunLimit = 3f;
        WallClimbLimit = 6f;
        WallJumpReflect = Vector3.zero;
        PreviousWallNormal = Vector3.zero;
        isJumping = false;
        isFalling = false;
        willJump = false;
        // Timers
        jumpGracePeriod = 0.1f;
        LandingTimeDelta = jumpGracePeriod;
        BufferJumpGracePeriod = 0.1f;
        BufferJumpTimeDelta = BufferJumpGracePeriod;
        SlideGracePeriod = 0.2f;
        SlideTimeDelta = SlideGracePeriod;
        WallJumpGracePeriod = 0.2f;
        WallJumpTimeDelta = WallJumpGracePeriod;
        WallRunGracePeriod = 0.1f;
        WallRunTimeDelta = WallRunGracePeriod;

        // Initial state
        current_velocity = Vector3.zero;
        currentHit = new ControllerColliderHit();
        StartPos = transform.position;
    }

    // Fixed Update is called once per physics tick
    private void FixedUpdate () {
        // Get starting values
        GravityMult = 1;
        //Debug.Log("Current velocity: " + cc.velocity.magnitude.ToString());
        //Debug.Log("Velocity error: " + (current_velocity - cc.velocity).ToString());
        accel = Vector3.zero;
        
        ProcessHits();
        HandleMovement();
        HandleJumping();

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
        cc.Move(current_velocity * Time.deltaTime);

        // Increment timers
        LandingTimeDelta = Mathf.Clamp(LandingTimeDelta + Time.deltaTime, 0, 2 * jumpGracePeriod);
        SlideTimeDelta = Mathf.Clamp(SlideTimeDelta + Time.deltaTime, 0, 2 * SlideGracePeriod);
        BufferJumpTimeDelta = Mathf.Clamp(BufferJumpTimeDelta + Time.deltaTime, 0, 2 * BufferJumpGracePeriod);
        WallJumpTimeDelta = Mathf.Clamp(WallJumpTimeDelta + Time.deltaTime, 0, 2 * WallJumpGracePeriod);
        WallRunTimeDelta = Mathf.Clamp(WallRunTimeDelta + Time.deltaTime, 0, 2 * WallRunGracePeriod);
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
            StartCoroutine(DeferedTeleport(StartPos));
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
        PreviousWallNormal = Vector3.zero;
    }

    private void ProcessSlideHit()
    {
        // Slides
        PreviousWallNormal = Vector3.zero;
    }

    private void ProcessWallHit()
    {
        // Did we hit a wall hard enough to jump
        if (!OnGround() && Vector3.Dot(current_velocity, currentHit.normal) < -WallJumpThreshold)
        {
            // Are we jumping in a new direction (atleast 20 degrees difference)
            if (Vector3.Dot(PreviousWallNormal, currentHit.normal) < 0.94f)
            {
                WallJumpTimeDelta = 0;
                WallJumpReflect = Vector3.Reflect(current_velocity, currentHit.normal);
                if (BufferJumpTimeDelta < BufferJumpGracePeriod)
                {
                    // Buffer a jump
                    willJump = true;
                }
                PreviousWallNormal = currentHit.normal;
            }
        }
        // Wall running is broken. Unity can't handle collisions with walls very well
        // TODO: Use the trigger collider instead and convert this code
        // Start a wall run/climb if we are moving into the wall at a 30 degree angle or less 
        // (a buffered/frame perfect walljump will cancel this)
        if (!IsWallRunning() && !OnGround())
        {
            Vector3 wall_axis = Vector3.Cross(currentHit.normal, Physics.gravity).normalized;
            Vector3 along_wall_vel = Vector3.Dot(current_velocity, wall_axis) * wall_axis;
            Vector3 up_wall_vel = current_velocity - along_wall_vel;
            // First attempt a wall run if we pass the limit and are looking along the wall. 
            // If we don't try to wall climb instead if we are looking at the wall.
            if (along_wall_vel.magnitude > WallRunLimit && Mathf.Abs(Vector3.Dot(currentHit.normal, transform.forward)) < 0.5f)
            {
                WallRunTimeDelta = 0;
                PreviousWallNormal = currentHit.normal;
            }
            else if (Vector3.Dot(up_wall_vel, -Physics.gravity.normalized) > WallClimbLimit &&
                     Vector3.Dot(transform.forward, -currentHit.normal) > 0.7)
            {
                Debug.DrawRay(transform.position, currentHit.normal, Color.blue, 10);
            }
        }
    }

    private void ProcessCeilingHit()
    {
        // Overhang
        PreviousWallNormal = Vector3.zero;
    }

    // Apply movement forces from input (FAST edition)
    private void HandleMovement()
    {
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
            AccelerateTo(movVec, RunSpeed*movmag, GroundAcceleration);
            accel += -current_velocity * SpeedDamp;
        }
        // We are either in the air, buffering a jump, or sliding (recent contact with ground). Use air accel.
        else
        {
            AccelerateTo(movVec, AirSpeed*movmag, AirAcceleration);
            accel += -Vector3.ProjectOnPlane(current_velocity, transform.up) * AirSpeedDamp;
        }
    }

    // Try to accelerate to the desired speed in the direction specified
    private void AccelerateTo(Vector3 direction, float desiredSpeed, float acceleration)
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
            if (OnGround() || CanWallJump())
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
        return (WallRunTimeDelta < WallRunGracePeriod);
    }

    private bool CanWallJump()
    {
        return (WallJumpTimeDelta < WallJumpGracePeriod);
    }

    // Set the player to a jumping state
    private void DoJump()
    {
        // Wall jump if we need to
        if (CanWallJump() && WallJumpReflect.magnitude > 0)
        {
            current_velocity = WallJumpReflect * WallJumpBoost;
        }
        current_velocity.y = JumpVelocity;
        isJumping = true;
        willJump = false;

        // Intentionally set the timers over the limit
        BufferJumpTimeDelta = BufferJumpGracePeriod;
        WallJumpTimeDelta = WallJumpGracePeriod;
        WallRunTimeDelta = WallRunGracePeriod;
        LandingTimeDelta = jumpGracePeriod;
        WallJumpReflect = Vector3.zero;
    }

    // Handle collisions on player move
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        lastHit = hit;
    }

    // Teleport coroutine (needed due to bug in character controller teleport)
    IEnumerator DeferedTeleport(Vector3 position)
    {
        yield return new WaitForEndOfFrame();
        transform.position = position;
    }
}
