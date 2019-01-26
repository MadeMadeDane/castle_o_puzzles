using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidDynamicHandler : PhysicsPlugin {
    public FluidDynamicHandler(PhysicsPropHandler context) : base(context) { }

    private void ApplyAirFriction(Transform transform, Vector3 airFriction, bool fallingFrictionOnly) {
        Vector3 computedAirFriction = airFriction;
        if (fallingFrictionOnly && player.GetVelocity().y > 0f) computedAirFriction.y = 0f;
        player.Accelerate(-transform.TransformDirection(Vector3.Scale(transform.InverseTransformDirection(player.GetVelocity()), computedAirFriction)));
    }

    private void ApplyBuoyantForce(float effectiveVolume) {
        player.Accelerate(-Physics.gravity * effectiveVolume * FluidDynamic.density_air);
    }

    public override void FixedUpdate() {
        if (!isOwner) return;
        FluidDynamic flchild = GetComponentInNetworkedChildren<FluidDynamic>();
        if (flchild != null) {
            ApplyAirFriction(flchild.transform, flchild.airFriction, flchild.fallingFrictionOnly);
            ApplyBuoyantForce(flchild.effectiveVolume);
        }
    }
}
