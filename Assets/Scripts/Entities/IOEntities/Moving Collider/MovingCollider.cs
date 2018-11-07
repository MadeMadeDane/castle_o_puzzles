using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MovingCollider : MovingGeneric {
    public GameObject nextTargetObject;
    public bool Automatic = false;
    public bool ResetToHome = true;
    public float HomeResetTime = 10f;

    private Vector3 target_velocity;
    private float distanceThreshold;
    private MotionTarget current_target;
    private GameObject home;
    private bool moving;
    private bool at_home;
    private Utilities utils;

    // States
    public AnalogState a_Velocity;
    public DigitalState d_AtHome;
    public DigitalState d_Moving;

    // Timers
    private string HOME_TIMER;

    // Use this for initialization
    private void Start() {
        utils = Utilities.Instance;
        HOME_TIMER = "MovingColliderHomeReset_" + gameObject.GetInstanceID().ToString();
        utils.CreateTimer(HOME_TIMER, HomeResetTime);

        velocity = Vector3.zero;
        player_velocity = Vector3.zero;
        target_velocity = Vector3.zero;
        moving = false;
        at_home = true;
        distanceThreshold = 0.1f;
        current_target = null;
        // Save the original target as the home
        home = nextTargetObject;

        // Set IOStates
        d_Moving.initialize(moving);
        d_AtHome.initialize(at_home);

        StartCoroutine(MoveToNextTarget());
    }

    private void FixedUpdate() {
        Move();
        player_velocity = CalculatePlayerVelocity();
        if (ResetToHome && !Automatic) {
            CheckHomeResetTimer();
        }
        d_Moving.state = moving;
        d_AtHome.state = !moving && at_home;
    }

    protected virtual void Move() {
        if (current_target != null) {
            Vector3 path = current_target.transform.position - transform.position;
            if (path.magnitude > distanceThreshold) {
                target_velocity = path.normalized * current_target.speed;
            }
            else {
                target_velocity = path / distanceThreshold;
            }
        }
        else {
            target_velocity = Vector3.zero;
        }
        velocity = Vector3.Lerp(velocity, target_velocity, 0.1f);
        transform.Translate(velocity * Time.deltaTime, Space.World);
        a_Velocity.state = velocity.magnitude;
    }

    protected virtual Vector3 CalculatePlayerVelocity() {
        return velocity;
    }

    private void CheckHomeResetTimer() {
        if (moving) {
            utils.ResetTimer(HOME_TIMER);
        }
        else if (!at_home && utils.CheckTimer(HOME_TIMER)) {
            GoHome();
        }
    }

    public void Stay() {
        utils.ResetTimer(HOME_TIMER);
    }

    public void GoHome() {
        if (!moving) {
            nextTargetObject = home;
            StartCoroutine(MoveToNextTarget());
        }
        else {
            MotionTarget home_target = home.GetComponent<MotionTarget>();
            if (home_target != null) {
                current_target = home_target;
            }
        }
    }

    public void Trigger() {
        StartCoroutine(MoveToNextTarget());
    }

    public Coroutine TriggerAsync() {
        return StartCoroutine(MoveToNextTarget());
    }

    private IEnumerator MoveToNextTarget() {
        // Basic lock for coroutines. Do not allow other coroutines to run when moving
        if (moving) {
            yield break;
        }
        moving = true;

        if (nextTargetObject == null) {
            // Wait for nextTargetObject to bet set
            yield return new WaitUntil(() => nextTargetObject != null);
        }
        if (nextTargetObject != home) {
            at_home = false;
        }
        MotionTarget nextTarget = nextTargetObject.GetComponent<MotionTarget>();
        // Reset to home if we fail to find a next target
        if (nextTarget == null) {
            Debug.Log("Target was not a MotionTarget");
            nextTargetObject = home;
            yield return new WaitForSeconds(1f);
            moving = false;
            StartCoroutine(MoveToNextTarget());
            yield break;
        }

        current_target = nextTarget;
        while ((current_target.transform.position - transform.position).magnitude > distanceThreshold) {
            //target_velocity = (current_target.transform.position - transform.position).normalized * current_target.speed;
            // Synchronize with fixed update
            yield return new WaitForFixedUpdate();
        }

        // Put the coroutine to sleep while we wait
        target_velocity = Vector3.zero;
        if (current_target.waitTime > 0f) {
            yield return new WaitForSeconds(current_target.waitTime);
        }
        nextTargetObject = current_target.nextTargetObject;

        moving = false;
        if (current_target.gameObject == home) {
            at_home = true;
        }

        if (Automatic) {
            StartCoroutine(MoveToNextTarget());
        }
    }

    private void OnCollisionStay(Collision collision) {
        MovingColliderRigidBody other = collision.gameObject.GetComponent<MovingColliderRigidBody>();
        if (other != null) {
            other.attach(gameObject);
        }
    }
}
