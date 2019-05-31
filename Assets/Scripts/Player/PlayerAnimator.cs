using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using System.IO;
using MLAPI.Serialization;

public class PlayerAnimator : NetworkedBehaviour {
    public Animator animator;
    private CharacterController cc;
    private PlayerController pc;
    private MovingPlayer netptransform;
    private bool isWalking;
    //private bool isSideStepping;
    private bool isJumping;
    private bool isRunning;
    private bool isHanging;
    //private bool isClimbing;
    private bool isSliding;
    //private bool isRolling;
    public float walkMaxSpeedMult = 12f;
    public float runMinSpeedMult = 16f;

    // For networked players
    public float FixedSendsPerSecond = 10f;
    private const string ANIM_CHANNEL = "MLAPI_DEFAULT_MESSAGE";
    private Vector3 previous_position;
    private bool lastGroundState;
    private bool lastHangState;
    private bool lastSentGroundState;
    private bool lastSentHangState;
    private FloatBuffer averageVelocity = new FloatBuffer(5);

    // Use this for initialization
    void Start() {
        cc = GetComponent<CharacterController>();
        pc = GetComponent<PlayerController>();

        isWalking = false;
        //isSideStepping = false;
        isJumping = false;
        isRunning = false;
        isHanging = false;
        //isClimbing = false;
        isSliding = false;
        //isRolling = false;
    }

    public override void NetworkStart() {
        netptransform = GetComponent<MovingPlayer>();
        if (IsOwner) {
            InvokeRepeating("TransmitAnimationStates", 0f, (1f / FixedSendsPerSecond));
        }
    }

    // Update is called once per frame
    void Update() {
        if (IsOwner && (cc == null || pc == null)) {
            cc = GetComponent<CharacterController>();
            pc = GetComponent<PlayerController>();
            return;
        }
        HandleAnimations();
    }

    private void HandleAnimations() {
        float velocity_mag;
        bool on_ground;
        bool is_hanging;
        if (IsOwner) {
            velocity_mag = Vector3.ProjectOnPlane(cc.velocity, Physics.gravity).magnitude;
            on_ground = lastGroundState = pc.OnGround();
            is_hanging = lastHangState = pc.IsHanging();
        }
        else {
            on_ground = lastGroundState;
            is_hanging = lastHangState;
            velocity_mag = Vector3.ProjectOnPlane(netptransform.velocity, Physics.gravity).magnitude;
            previous_position = transform.position;
        }
        // NOTE: This makes animations framerate dependent. We may want to move this into fixed update in the future.
        velocity_mag = averageVelocity.Accumulate(velocity_mag) / averageVelocity.Size();
        if (Input.GetKey(KeyCode.Q)) {
            isSliding = true;
            isWalking = false;
            isRunning = false;
            isJumping = false;
        }
        else {
            isSliding = false;
            if (on_ground && velocity_mag < 0.2f) {
                isWalking = false;
                isRunning = false;
            }
            else if (on_ground && velocity_mag < (walkMaxSpeedMult * cc.radius)) {
                isWalking = true;
                isRunning = false;
            }
            else if (on_ground && velocity_mag > (runMinSpeedMult * cc.radius)) {
                isRunning = true;
                isWalking = false;
            }
            else if (on_ground && !isWalking && !isRunning) isWalking = true;

            if (!on_ground) {
                isJumping = true;
                isWalking = false;
                isRunning = false;
            }
            else {
                isJumping = false;
            }
            if (is_hanging) {
                isJumping = false;
                isWalking = false;
                isRunning = false;
                isHanging = true;
            }
            else {
                isHanging = false;
            }
        }
        //Add more animation states here
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isHanging", isHanging);
        animator.SetBool("isSliding", isSliding);
    }

    private void TransmitAnimationStates() {
        lastSentGroundState = lastGroundState;
        lastSentHangState = lastHangState;

        using (PooledBitStream stream = PooledBitStream.Get()) {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream)) {
                AnimationPacket.WritePacket(lastGroundState, lastHangState, writer);

                if (IsServer)
                    InvokeClientRpcOnEveryoneExceptPerformance(ApplyAnimationState, OwnerClientId, stream, channel: ANIM_CHANNEL);
                else
                    InvokeServerRpcPerformance(SubmitAnimationState, stream, channel: ANIM_CHANNEL);
            }
        }
    }

    [ClientRPC]
    private void ApplyAnimationState(ulong clientId, Stream stream) {
        if (!enabled) return;
        AnimationPacket received_animation = AnimationPacket.FromStream(stream);
        lastGroundState = received_animation.ground_state;
        lastHangState = received_animation.hang_state;
    }

    [ServerRPC]
    private void SubmitAnimationState(ulong clientId, Stream stream) {
        if (!enabled) return;
        AnimationPacket received_animation = AnimationPacket.FromStream(stream);

        using (PooledBitStream writeStream = PooledBitStream.Get()) {
            using (PooledBitWriter writer = PooledBitWriter.Get(writeStream)) {
                received_animation.Write(writer);
                InvokeClientRpcOnEveryoneExceptPerformance(ApplyAnimationState, OwnerClientId, writeStream, channel: ANIM_CHANNEL);
            }
        }
    }

}

public struct AnimationPacket {
    public bool ground_state;
    public bool hang_state;

    public AnimationPacket(bool ground_state, bool hang_state) {
        this.ground_state = ground_state;
        this.hang_state = hang_state;
    }

    public static AnimationPacket FromStream(Stream stream) {
        using (PooledBitReader reader = PooledBitReader.Get(stream)) {
            bool groundstate = reader.ReadBool();
            bool hangstate = reader.ReadBool();

            return new AnimationPacket(groundstate, hangstate);
        }
    }

    public void Write(PooledBitWriter writer) {
        writer.WriteBool(ground_state);
        writer.WriteBool(hang_state);
    }

    public static void WritePacket(bool ground_state, bool hang_state, PooledBitWriter writer) {
        new AnimationPacket(ground_state, hang_state).Write(writer);
    }
}
