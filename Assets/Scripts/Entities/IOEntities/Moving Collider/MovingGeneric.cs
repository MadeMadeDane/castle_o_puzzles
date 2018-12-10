using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MovingGeneric : NetworkedObjectTransform {
    public Vector3 player_velocity;

    // Dictionary that maps a networked object id to a list of it's moving objects
    // Used so that networked users can refer to a moving platform using an index in the list

    // NOTE: These lists are expected to be equivalent across all clients. This is currently being done
    // by the MLAPI library to keep track of NetworkedBehaviors, however Unity does not document
    // the GetComponentsInChildren as being deterministic for a given prefab anywhere in it's docs. So
    // if this breaks, the entire networking library will also be broken.
    public static Dictionary<uint, List<MovingGeneric>> MovingObjectDict;

    public override void NetworkStart() {
        // Start indexing our moving platforms as soon as the networked object is spawned.
        IndexMovingObjects();
        base.NetworkStart();
    }

    private void IndexMovingObjects() {
        if (MovingObjectDict == null) {
            MovingObjectDict = new Dictionary<uint, List<MovingGeneric>>();
        }
        if (!MovingObjectDict.ContainsKey(networkId)) {
            MovingObjectDict[networkId] = new List<MovingGeneric>();
            MovingGeneric[] moving_objs = networkedObject.GetComponentsInChildren<MovingGeneric>();
            foreach (MovingGeneric moving_obj in moving_objs) {
                if (moving_obj.networkId == networkId) {
                    MovingObjectDict[networkId].Add(moving_obj);
                }
            }
        }
    }

    public int GetMovingObjectIndex() {
        return MovingObjectDict[networkId].IndexOf(this);
    }

    public static int GetMovingObjectIndex(MovingGeneric moving_object) {
        return MovingObjectDict[moving_object.networkId].IndexOf(moving_object);
    }

    public static MovingGeneric GetMovingObjectAt(uint target_networkId, int index) {
        return MovingObjectDict[target_networkId][index];
    }
}
