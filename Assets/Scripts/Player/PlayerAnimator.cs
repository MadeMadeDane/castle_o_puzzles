using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public CharacterController cc;
    public PlayerController pc;
    public Animator animator;
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

    // Use this for initialization
    void Start ()
    {
        isWalking = false;
        //isSideStepping = false;
        isJumping = false;
        isRunning = false;
        isHanging = false;
        //isClimbing = false;
        isSliding = false;
        //isRolling = false;
	}
	
	// Update is called once per frame
	void Update ()
    {
        HandleAnimations();
	}
    private void HandleAnimations ()
    {
        float velocity_mag = cc.velocity.magnitude;

        if (Input.GetKey(KeyCode.Q))
        {
            isSliding = true;
            isWalking = false;
            isRunning = false;
            isJumping = false;
        }
        else
        {
            isSliding = false;
            if(pc.OnGround() && velocity_mag < .2)
            {
                isWalking = false;
                isRunning = false;
            }
            else if (pc.OnGround() && velocity_mag < walkMaxSpeed)
            {
                isWalking = true;
                isRunning = false;
            }
            else if(pc.OnGround() && velocity_mag > runMinSpeed)
            {
                isRunning = true;
                isWalking = false;
            }

            if (!pc.OnGround())
            {
                isJumping = true;
                isWalking = false;
                isRunning = false;
            }
            else
            {
                isJumping = false;
            }
            if (pc.IsHanging()) {
                isJumping = false;
                isWalking = false;
                isRunning = false;
                isHanging = true;
            } else {
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
}
