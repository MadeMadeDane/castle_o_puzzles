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
    private bool isSprinting;
    //private bool isClimbing;
    private bool isSliding;
    //private bool isRolling;
    public float runMinSpeed = 5;
    public float sprintMinSpeed = 12;

    // Use this for initialization
    void Start ()
    {
        isWalking = false;
        //isSideStepping = false;
        isJumping = false;
        isRunning = false;
        isSprinting = false;
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
            isSprinting = false;
            isJumping = false;
        }
        else
        {
            isSliding = false;
            if (pc.OnGround() && velocity_mag > .2 && velocity_mag < runMinSpeed)
            {
                isWalking = true;
            }
            else
            {
                isWalking = false;
            }
            if(pc.OnGround() && velocity_mag > runMinSpeed && velocity_mag < sprintMinSpeed)
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;
            }
            if (pc.OnGround() && velocity_mag > sprintMinSpeed)
            {
                isSprinting = true;
            }
            else
            {
                isSprinting = false;
            }
            if (!pc.OnGround())
            {
                isJumping = true;
            }
            else
            {
                isJumping = false;
            }
        }
        //Add more animation states here

        animator.SetBool("isSprinting", isSprinting);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isSliding", isSliding);
    }
}
