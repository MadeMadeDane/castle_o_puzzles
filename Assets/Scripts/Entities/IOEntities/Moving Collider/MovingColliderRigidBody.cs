using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingColliderRigidBody : MonoBehaviour {
    private string ATTACH_TIMER;
    private Utilities utils;
    private Transform platform_transform;
    private Rigidbody rb;
    private Vector3 original_eulers;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        original_eulers = transform.rotation.eulerAngles;
        utils = Utilities.Instance;
        ATTACH_TIMER = "RigidBodyAttach_" + gameObject.GetInstanceID().ToString();
        utils.CreateTimer(ATTACH_TIMER, 0.2f);
    }

    public void attach(GameObject platform) {
        platform_transform = platform.transform;
        transform.parent = platform_transform;
        utils.ResetTimer(ATTACH_TIMER);
    }

    private void FixedUpdate() {
        if (transform.parent == platform_transform && transform.parent != null && utils.CheckTimer(ATTACH_TIMER)) {
            transform.parent = null;
            if (rb.constraints.HasFlag(RigidbodyConstraints.FreezeRotationX)) {
                transform.rotation = Quaternion.Euler(
                    new Vector3(original_eulers.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z));
            }
            if (rb.constraints.HasFlag(RigidbodyConstraints.FreezeRotationZ)) {
                transform.rotation = Quaternion.Euler(
                    new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, original_eulers.z));
            }
        }
    }
}
