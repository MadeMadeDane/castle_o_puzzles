using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IOPressurePlate")]
[RequireComponent(typeof(MovingEntity))]
[RequireComponent(typeof(Rigidbody))]
public class IOPressurePlate : IOEntity {
    public DigitalState Pressed;
    public AnalogState Weight;
    public Rigidbody plate_rb;
    public float StandardPressForce = 10f;
    public float MaxDisplacement = 0.5f;
    public float ActivationPoint = 0.4f;
    public float Springiness = 50f;
    public float Damping = 20f;
    private float RestDisplacement = 0f;
    private float plate_velocity = 0f;
    private float plate_force;
    private GameObject lastPresser;
    private string PRESS_TIMER;
    private string ACTIVE_TIMER;

    protected override void Awake() {
        base.Awake();
        PRESS_TIMER = $"PressurePlatePress_{GetInstanceID()}";
        ACTIVE_TIMER = $"PressurePlateActive_{GetInstanceID()}";
        utils.CreateTimer(PRESS_TIMER, 0.1f).setFinished();
        utils.CreateTimer(ACTIVE_TIMER, 0.1f).setFinished();
        plate_rb = GetComponent<Rigidbody>();
        RestDisplacement = transform.localPosition.y;
    }

    public void SetWeight(AnalogState input) {
        SetWeight(input.state);
    }

    public void SetWeight(float weight) {
        Weight.state = weight;
    }


    public void Press() {
        Press(StandardPressForce);
    }

    public void Press(AnalogState input) {
        Press(input.state);
    }

    public void Press(float force) {
        plate_force += force;
    }

    private void HandlePress() {
        if (!utils.CheckTimer(PRESS_TIMER) && lastPresser != null) {
            Rigidbody presser_rb = lastPresser.GetComponent<Rigidbody>();
            if (presser_rb != null) {
                float applied_force = presser_rb.mass * Physics.gravity.magnitude;
                SetWeight(applied_force);
                Press(applied_force);
            }
            else {
                Debug.Log("No rigidbody found");
            }
        }
        else {
            lastPresser = null;
            SetWeight(0f);
        }
    }

    private void MovePlate() {
        if (plate_rb.isKinematic) {
            float current_displacement = transform.localPosition.y;
            float computed_force = -plate_force + ((RestDisplacement - transform.localPosition.y) * Springiness) - plate_velocity * Damping;
            plate_velocity += computed_force * Time.fixedDeltaTime;
            current_displacement += plate_velocity * Time.fixedDeltaTime;
            if (Mathf.Abs(current_displacement - RestDisplacement) >= MaxDisplacement) {
                current_displacement = Mathf.Clamp(current_displacement,
                                                   Mathf.Min(RestDisplacement - MaxDisplacement, RestDisplacement + MaxDisplacement),
                                                   Mathf.Max(RestDisplacement - MaxDisplacement, RestDisplacement + MaxDisplacement));
                plate_velocity = 0f;
            }
            transform.localPosition = new Vector3(transform.localPosition.x, current_displacement, transform.localPosition.z);
            if (RestDisplacement - current_displacement > ActivationPoint) {
                utils.ResetTimer(ACTIVE_TIMER);
            }
        }
        else {
            Debug.Log("non-kinematic rigidbodies not supported");
        }
        plate_force = 0f;
    }

    private void FixedUpdate() {
        if (!isServer) return;
        HandlePress();
        MovePlate();
        Pressed.state = !utils.CheckTimer(ACTIVE_TIMER);
    }

    private void OnCollisionStay(Collision other) {
        if (!isServer) return;
        if (other.contacts[0].normal.y > -0.9f) return;
        utils.ResetTimer(PRESS_TIMER);
        lastPresser = other.gameObject;
    }

    private void OnCollisionEnter(Collision other) {
        if (!isServer) return;
        Press(0.1f * Mathf.Abs(other.relativeVelocity.y) / Time.fixedDeltaTime);
    }
}