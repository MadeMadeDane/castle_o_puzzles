using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MovingGeneric : NetworkedObjectTransform {
    private Vector3 _player_velocity;
    public Vector3 player_velocity {
        get {
            return CalculatePlayerVelocity();
        }
        set {
            _player_velocity = value;
        }
    }

    // Dictionary that maps a networked object id to a list of it's moving objects
    // Used so that networked users can refer to a moving platform using an index in the list

    // NOTE: These lists are expected to be equivalent across all clients. This is currently being done
    // by the MLAPI library to keep track of NetworkedBehaviors, however Unity does not document
    // the GetComponentsInChildren as being deterministic for a given prefab anywhere in it's docs. So
    // if this breaks, the entire networking library will also be broken.
    public static Dictionary<ulong, List<MovingGeneric>> MovingObjectDict;

    public override void NetworkStart() {
        // Start indexing our moving platforms as soon as the networked object is spawned.
        IndexMovingObjects();
        base.NetworkStart();
        player_velocity = Vector3.zero;
    }

    private void IndexMovingObjects() {
        if (MovingObjectDict == null) {
            MovingObjectDict = new Dictionary<ulong, List<MovingGeneric>>();
        }
        if (!MovingObjectDict.ContainsKey(NetworkId)) {
            MovingObjectDict[NetworkId] = new List<MovingGeneric>();
            MovingGeneric[] moving_objs = NetworkedObject.GetComponentsInChildren<MovingGeneric>();
            foreach (MovingGeneric moving_obj in moving_objs) {
                if (moving_obj.NetworkId == NetworkId) {
                    MovingObjectDict[NetworkId].Add(moving_obj);
                }
            }
        }
    }

    public override void OnDestroyed() {
        MovingObjectDict[NetworkId].RemoveAt(GetMovingObjectIndex());
        if (MovingObjectDict[NetworkId].Count == 0) MovingObjectDict.Remove(NetworkId);
    }

    public int GetMovingObjectIndex() {
        return MovingObjectDict[NetworkId].IndexOf(this);
    }

    public static int GetMovingObjectIndex(MovingGeneric moving_object) {
        return MovingObjectDict[moving_object.NetworkId].IndexOf(moving_object);
    }

    public static MovingGeneric GetMovingObjectAt(ulong target_networkId, int index) {
        return MovingObjectDict[target_networkId][index];
    }

    protected virtual Vector3 CalculatePlayerVelocity() {
        return velocity;
    }

    private static void ShowDebugInfo() {
        foreach (var kvp in MovingObjectDict) {
            Debug.Log($"key: {kvp.Key}, value: {string.Join(", ", kvp.Value.Select((MovingGeneric k) => k.name))}");
        }
    }
}
