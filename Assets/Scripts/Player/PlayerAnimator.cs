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
    private NetworkedPlayerTransform netptransform;
    private bool isWalking;
    //private bool isSideStepping;
    private bool isJumping;
    private bool isRunning;
    private bool isHanging;
    //private bool isClimbing;
    private bool isSliding;
    //private bool isRolling;
    public float runMinSpeed = 4.5f;
    public float walkMaxSpeed = 4;
    public float sprintMinSpeed = 12;

    // For networked players
    public float FixedSendsPerSecond = 10f;
    private const string ANIM_CHANNEL = "MLAPI_DEFAULT_MESSAGE";
    private Vector3 previous_position;
    private bool lastGroundState;
    private bool lastHangState;
    private bool lastSentGroundState;
    private bool lastSentHangState;
    private FloatBuffer averageVelocity = new FloatBuffer(10);

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
        netptransform = GetComponent<NetworkedPlayerTransform>();
        if (isOwner) {
            InvokeRepeating("TransmitAnimationStates", 0f, (1f / FixedSendsPerSecond));
        }
    }

    // Update is called once per frame
    void Update() {
        if (isOwner && (cc == null || pc == null)) {
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
        if (isOwner) {
            velocity_mag = cc.velocity.magnitude;
            on_ground = lastGroundState = pc.OnGround();
            is_hanging = lastHangState = pc.IsHanging();
        }
        else {
            on_ground = lastGroundState;
            is_hanging = lastHangState;
            velocity_mag = netptransform.velocity.magnitude;
            previous_position = transform.position;
        }

        if (Input.GetKey(KeyCode.Q)) {
            isSliding = true;
            isWalking = false;
            isRunning = false;
            isJumping = false;
        }
        else {
            isSliding = false;
            if (on_ground && velocity_mag < .2) {
                isWalking = false;
                isRunning = false;
            }
            else if (on_ground && velocity_mag < walkMaxSpeed) {
                isWalking = true;
                isRunning = false;
            }
            else if (on_ground && velocity_mag > runMinSpeed) {
                isRunning = true;
                isWalking = false;
            }

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

                if (isServer)
                    InvokeClientRpcOnEveryoneExcept(ApplyAnimationState, OwnerClientId, stream, channel: ANIM_CHANNEL);
                else
                    InvokeServerRpc(SubmitAnimationState, stream, channel: ANIM_CHANNEL);
            }
        }
    }

    [ClientRPC]
    private void ApplyAnimationState(uint clientId, Stream stream) {
        if (!enabled) return;
        AnimationPacket received_animation = AnimationPacket.FromStream(stream);
        lastGroundState = received_animation.ground_state;
        lastHangState = received_animation.hang_state;
    }

    [ServerRPC]
    private void SubmitAnimationState(uint clientId, Stream stream) {
        if (!enabled) return;
        AnimationPacket received_animation = AnimationPacket.FromStream(stream);

        using (PooledBitStream writeStream = PooledBitStream.Get()) {
            using (PooledBitWriter writer = PooledBitWriter.Get(writeStream)) {
                received_animation.Write(writer);
                InvokeClientRpcOnEveryoneExcept(ApplyAnimationState, OwnerClientId, writeStream, channel: ANIM_CHANNEL);
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
