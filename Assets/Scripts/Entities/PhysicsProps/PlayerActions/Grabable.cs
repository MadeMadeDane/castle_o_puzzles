using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("PhysicsProps/Grabable")]
[RequireComponent(typeof(Rigidbody))]
public class Grabable : PhysicsProp, IUsable {
    private GameObject parent;
    private Transform previous_transform;
    private bool is_grabbed = false;

    public bool Pickup(GameObject grabber, Vector3 grab_offset = default(Vector3)) {
        if (is_grabbed) {
            return false;
        }
        parent = grabber;
        previous_transform = transform.parent;
        // Not a very good Back to the Future plot...
        if (parent.transform.parent.parent == transform) {
            parent.transform.parent.parent = null;
        }
        transform.parent = parent.transform;
        transform.localPosition = grab_offset;
        transform.localRotation = Quaternion.identity;
        rigidbody.isKinematic = true;
        is_grabbed = true;
        return true;
    }

    public bool Throw(Vector3 velocity, bool local = true) {
        if (!is_grabbed) {
            return false;
        }
        rigidbody.isKinematic = false;
        if (local) {
            velocity = parent.transform.TransformDirection(velocity);
        }
        transform.parent = previous_transform;
        rigidbody.AddForce(velocity, ForceMode.VelocityChange);
        is_grabbed = false;
        return true;
    }

    public void Use() {
        return;
    }
}
