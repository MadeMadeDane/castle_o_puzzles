using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pusher : PhysicsPlugin
{
    public Pusher(MonoBehaviour context) : base(context) {}

    public override void OnControllerColliderHit(ControllerColliderHit hit, PhysicsProp prop) {
        Pushable pushable = prop as Pushable;
        Vector3 motion_vector = player.GetMoveVector();
        //Debug.DrawRay(context.transform.position, motion_vector, Color.green, 100f);
        //Debug.DrawRay(context.transform.position, hit.normal, Color.red, 100f);
        //Debug.DrawRay(context.transform.position, 20f*Vector3.Project(motion_vector, hit.normal), Color.blue, 100f);
        pushable.Push(20f*Vector3.Project(motion_vector, hit.normal));
    }
}