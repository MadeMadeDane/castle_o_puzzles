using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidDynamicHandler : PhysicsPlugin {
    public FluidDynamicHandler(PhysicsPropHandler context) : base(context) { }

    private void HandleFluidDynamicChildren() {
        FluidDynamic flchild = GetComponentInNetworkedChildren<FluidDynamic>();
        if (flchild != null) {
            player.Accelerate(flchild.CalculateAirFriction(velocity: player.GetVelocity(),
                                                           held_by_player: true));
            player.Accelerate(flchild.CalculateBuoyantForce());
        }
    }

    public override void FixedUpdate() {
        if (!IsOwner) return;
        HandleFluidDynamicChildren();
    }
}
