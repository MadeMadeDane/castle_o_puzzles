using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IODoor")]
[RequireComponent(typeof(MovingGeneric))]
[RequireComponent(typeof(Rigidbody))]
public class IODoor : IOEntity, IUsable {
    public DigitalState DoorOpen;
    public Rigidbody DoorRb;
    public bool Locked = false;
    public float OpenRotation = 90f;
    public float Springiness = 50f;
    public float Damping = 20f;
    private float RestRotation = 0f;
    private float DoorVelocity = 0f;

    protected override void Awake() {
        base.Awake();
        DoorRb = GetComponent<Rigidbody>();
        Open(false);
    }

    public void Lock() {
        Locked = true;
    }

    public void Unlock() {
        Locked = false;
    }

    public void Open(DigitalState input) {
        Open(input.state);
    }

    public void Open(bool state) {
        DoorOpen.state = state;
    }

    public void Use() {
        if (!Locked) DoorOpen.state = !DoorOpen.state;
    }

    private void MoveDoor() {
        if (DoorRb.isKinematic) {
            float current_rotation = transform.localEulerAngles.y;
            float computed_force = 0f;
            if (DoorOpen.state) {
                computed_force = (Mathf.DeltaAngle(current_rotation, OpenRotation) * Springiness) - DoorVelocity * Damping;
            }
            else {
                computed_force = (Mathf.DeltaAngle(current_rotation, RestRotation) * Springiness) - DoorVelocity * Damping;
            }
            DoorVelocity += computed_force * Time.fixedDeltaTime;
            current_rotation += DoorVelocity * Time.fixedDeltaTime;
            transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, current_rotation, transform.localEulerAngles.z);
        }
        else {
            Debug.Log("non-kinematic rigidbodies not supported");
        }
    }

    private void FixedUpdate() {
        if (!isServer) return;
        MoveDoor();
    }
}