using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsProp : MonoBehaviour
{
    new protected Rigidbody rigidbody;

    private void Awake() {
        rigidbody = GetComponent<Rigidbody>();
    }
}
