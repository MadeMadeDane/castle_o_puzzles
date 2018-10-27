using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pusher : PhysicsPlugin {
    private FloatBuffer pushbuffer = new FloatBuffer((int)(1 / UnityEngine.Time.fixedDeltaTime));
    private float pushtarget = 0f;
    private float pushforce = 0f;

    public Pusher(MonoBehaviour context) : base(context) { }

    public override void OnTriggerStay(Collider other, PhysicsProp prop) {
        Pushable pushable = prop as Pushable;
        Vector3 motion_vector = player.GetMoveVector();
        RaycastHit hit;
        if (Physics.Raycast(context.transform.position, motion_vector, out hit, player.cc.radius * 1.5f)) {
            pushtarget = 150f;
            pushable.Push(pushforce * Vector3.Project(motion_vector, hit.normal));
        }
        //Debug.DrawRay(context.transform.position, motion_vector, Color.green, 100f);
        //Debug.DrawRay(context.transform.position, hit.normal, Color.red, 100f);
        //Debug.DrawRay(context.transform.position, 20f*Vector3.Project(motion_vector, hit.normal), Color.blue, 100f);
    }

    public override void FixedUpdate() {
        if (player.GetMoveVector().magnitude == 0) {
            pushbuffer.Clear();
        }
        pushforce = pushbuffer.Accumulate(pushtarget * Time.deltaTime);
        pushtarget = 0f;
    }
}