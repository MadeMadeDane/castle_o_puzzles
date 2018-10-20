using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentPlugin
{
    private MonoBehaviour context;
    public ComponentPlugin(MonoBehaviour context) {
        this.context = context;
    }

    public virtual void Awake() {}

    public virtual void Start() {}

    public virtual void Update() {}

    public virtual void LateUpdate() {}

    public virtual void FixedUpdate() {}

    public virtual void OnTriggerEnter(Collider other) {}

    public virtual void OnTriggerStay(Collider other) {}

    public virtual void OnTriggerExit(Collider other) {}

    public virtual void OnCollisionEnter(Collision other) {}

    public virtual void OnCollisionStay(Collision other) {}

    public virtual void OnCollisionExit(Collision other) {}

    public virtual void OnControllerColliderHit(ControllerColliderHit hit) {}
}
