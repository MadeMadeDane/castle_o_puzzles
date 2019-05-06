using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[AddComponentMenu("IOEntities/IODoor")]
[RequireComponent(typeof(MovingGeneric))]
[RequireComponent(typeof(Rigidbody))]
public class IODoor : IOEntity, IUsable {
    public DigitalState DoorOpen;
    public DigitalState Unlocked;
    public Rigidbody DoorRb;
    public bool StartUnlocked = true;
    public float OpenRotation = 90f;
    public float Springiness = 50f;
    public float Damping = 20f;
    private float RestRotation = 0f;
    private float DoorVelocity = 0f;

    protected override void Startup() {
        DoorRb = GetComponent<Rigidbody>();
        SetUnlocked(StartUnlocked);
        Open(false);
    }

    public void Lock() {
        SetUnlocked(false);
    }

    public void Unlock() {
        SetUnlocked(true);
    }

    public void SetUnlocked(DigitalState input) {
        SetUnlocked(input.state);
    }

    public void SetUnlocked(bool unlocked) {
        Unlocked.state = unlocked;
    }

    public void Open(DigitalState input) {
        Open(input.state);
    }

    public void Open(bool state) {
        DoorOpen.state = state;
    }

    public void Use() {
        if (Unlocked.state) DoorOpen.state = !DoorOpen.state;
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
        if (!IsServer) return;
        MoveDoor();
    }
}