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
    public Vector3 StartPos;

    // Jumping state variables
    private float SlideGracePeriod;
    private float SlideTimeDelta;
    private bool isJumping;
    private bool isFalling;
    private bool canJump;
    private bool willJump;
    private float LandingTimeDelta;
    private float jumpGracePeriod;
    private float BufferJumpTimeDelta;
    private float BufferJumpGracePeriod;

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
        SlideGracePeriod = 0.2f;
        SlideTimeDelta = SlideGracePeriod;
        SlideSpeed = 12f;

        // Gravity modifiers
        DownGravityAdd = 0;
        ShortHopGravityAdd = 0;
        
        // Jump states/values
        JumpVelocity = 12;
        isJumping = false;
        isFalling = false;
        canJump = false;
        willJump = false;
        // Jump timers for early/late jumps
        jumpGracePeriod = 0.1f;
        LandingTimeDelta = 0;
        BufferJumpGracePeriod = 0.1f;
        BufferJumpTimeDelta = BufferJumpGracePeriod;

        // Initial state
        current_velocity = Vector3.zero;
        currentHit = new ControllerColliderHit();
        StartPos = transform.position;
    }

    // Fixed Update is called once per physics tick
    private void FixedUpdate () {
        // Get starting values
        GravityMult = 1;
        //Debug.Log("Current velocity: " + Vector3.ProjectOnPlane(current_velocity, transform.up).magnitude.ToString());
        Debug.Log("Velocity error: " + (current_velocity - cc.velocity).ToString());
        accel = Vector3.zero;

        ProcessHits();
        HandleMovement();
        HandleJumping();

        // Update character state based on desired movement
        accel += Physics.gravity * GravityMult;
        current_velocity += accel * Time.deltaTime;
        cc.Move(current_velocity * Time.deltaTime);

        // Increment timers
        LandingTimeDelta = Mathf.Clamp(LandingTimeDelta + Time.deltaTime, 0, 2 * jumpGracePeriod);
        SlideTimeDelta = Mathf.Clamp(SlideTimeDelta + Time.deltaTime, 0, 2 * SlideGracePeriod);
        BufferJumpTimeDelta = Mathf.Clamp(BufferJumpTimeDelta + Time.deltaTime, 0, 2 * BufferJumpGracePeriod);
    }

    private void ProcessHits()
    {
        if (lastHit == null)
        {
            return;
        }
        // isGrounded doesn't work properly on slopes, replace with this.
        if (lastHit.normal.y > 0.6)
        {
            Debug.Log("On the ground");
            //Debug.DrawRay(transform.position, hit.normal, Color.red, 100);
            canJump = true;
            LandingTimeDelta = 0;

            // Handle buffered jumps
            if (BufferJumpTimeDelta < BufferJumpGracePeriod)
            {
                // Defer the jump so that it happens in update
                willJump = true;
            }
        }
        // Use this for detecting slopes to slide down
        else
        {
            Debug.Log("On a slide");
            if (Vector3.Dot(current_velocity, lastHit.normal) < 0)
            {
                // Conserve velocity along plane, zero it out on the normal
                current_velocity = Vector3.ProjectOnPlane(current_velocity, lastHit.normal);
            }
        }
        if (lastHit.gameObject.tag == "Respawn")
        {
            StartCoroutine(DeferedTeleport(StartPos));
        }
        currentHit = lastHit;
        lastHit = null;
    }

    // Apply movement forces from input (FAST edition)
    private void HandleMovement()
    {
        Vector3 planevelocity;
        Vector3 movVec = (input_manager.GetMoveVertical() * transform.forward +
                          input_manager.GetMoveHorizontal() * transform.right);
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
            if (cc.isGrounded)
            {
                Debug.Log("We are on the ground");
                movVec = Vector3.ProjectOnPlane(movVec, currentHit.normal);
            }
            AccelerateTo(movVec, RunSpeed, GroundAcceleration);
            accel += -current_velocity * SpeedDamp;
        }
        // We are either in the air, buffering a jump, or sliding (recent contact with ground). Use air accel.
        else
        {
            AccelerateTo(movVec, AirSpeed, AirAcceleration);
            accel += -Vector3.ProjectOnPlane(current_velocity, transform.up) * AirSpeedDamp;
        }
        Debug.DrawRay(transform.position, Vector3.ProjectOnPlane(current_velocity, transform.up).normalized, Color.green, 0);
        Debug.DrawRay(transform.position, movVec.normalized, Color.blue, 0);
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
        if (input_manager.GetJump() || willJump)
        {
            BufferJumpTimeDelta = 0;
            if (OnGround())
            {
                DoJump();
            }
        }
        // Fall fast when we let go of jump (optional)
        if (isFalling || isJumping && !input_manager.GetJumpHold())
        {
            GravityMult += ShortHopGravityAdd;
            isFalling = true;
        }
    }

    // Double check if on ground using a separate canJump test
    private bool OnGround()
    {
        canJump = canJump && (LandingTimeDelta < jumpGracePeriod);
        return canJump;
    }

    // Set the player to a jumping state
    private void DoJump()
    {
        current_velocity.y = JumpVelocity;
        isJumping = true;
        canJump = false;
        willJump = false;

        // Intentionally set the timer over the limit
        BufferJumpTimeDelta = BufferJumpGracePeriod;
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
