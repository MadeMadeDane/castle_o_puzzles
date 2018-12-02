using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using MLAPI;
using MLAPI.Serialization;

/// <summary>
/// A slightly better prototype component for syncing transforms
/// </summary>
public class NetworkedObjectTransform : NetworkedBehaviour {

    /// <summary>
    /// The base amount of sends per seconds to use when range is disabled
    /// </summary>
    [Range(0, 120)]
    public float FixedSendsPerSecond = 20f;
    /// <summary>
    /// Is the sends per second assumed to be the same across all instances
    /// </summary>
    [Tooltip("This assumes that the SendsPerSecond is synced across clients")]
    public bool AssumeSyncedSends = true;
    /// <summary>
    /// Enable interpolation
    /// </summary>
    [Tooltip("This requires AssumeSyncedSends to be true")]
    public bool InterpolatePosition = true;
    /// <summary>
    /// The distance before snaping to the position
    /// </summary>
    [Tooltip("The transform will snap if the distance is greater than this distance")]
    public float SnapDistance = 10f;
    /// <summary>
    /// Should the server interpolate
    /// </summary>
    public bool InterpolateServer = true;
    /// <summary>
    /// The min meters to move before a send is sent
    /// </summary>
    public float MinMeters = 0.15f;
    /// <summary>
    /// The min degrees to rotate before a send it sent
    /// </summary>
    public float MinDegrees = 1.5f;
    /// <summary>
    /// The curve to use to calculate the send rate
    /// </summary>
    public AnimationCurve DistanceSendrate = AnimationCurve.Constant(0, 500, 20);

    # region new vars
    private const string POS_CHANNEL = "MLAPI_DEFAULT_MESSAGE";
    private Vector3 posOnLastReceive;
    private Quaternion rotOnLastReceive;
    private Vector3 lastReceivedPosition;
    private float lastRecieveTime;
    private uint lastRecievedParentId = 0;
    private InterpBuffer<Vector3> position_buffer = new InterpBuffer<Vector3>(transform_function: (Transform t, Vector3 v) => { return t.TransformPoint(v); });
    private InterpBuffer<Quaternion> rotation_buffer = new InterpBuffer<Quaternion>();
    private FloatBuffer latency_buffer = new FloatBuffer(20);
    private float latency = 0f;
    private float lastRecieveClientTime;
    [Tooltip("The delay in seconds buffered for interpolation")]
    public float interp_delay = 0.1f;
    public Vector3 velocity;
    # endregion

    /// <summary>
    /// The delegate used to check if a move is valid
    /// </summary>
    /// <param name="oldPos">The previous position</param>
    /// <param name="newPos">The new requested position</param>
    /// <returns>Returns wheter or not the move is valid</returns>
    public delegate bool MoveValidationDelegate(Vector3 oldPos, Vector3 newPos);
    /// <summary>
    /// If set, moves will only be accepted if the custom delegate returns true
    /// </summary>
    public MoveValidationDelegate IsMoveValidDelegate = null;

    private void OnValidate() {
        if (!AssumeSyncedSends && InterpolatePosition)
            InterpolatePosition = false;
        if (InterpolateServer && !InterpolatePosition)
            InterpolateServer = false;
        if (MinDegrees < 0)
            MinDegrees = 0;
        if (MinMeters < 0)
            MinMeters = 0;
    }

    private float GetTimeForLerp(Vector3 pos1, Vector3 pos2) {
        return 1f / DistanceSendrate.Evaluate(Vector3.Distance(pos1, pos2));
    }

    /// <summary>
    /// Registers message handlers
    /// </summary>
    public override void NetworkStart() {
        posOnLastReceive = transform.position;
        rotOnLastReceive = transform.rotation;

        if (isOwner) {
            InvokeRepeating("TransmitPosition", 0f, (1f / FixedSendsPerSecond));
        }
    }

    private void TransmitPosition() {
        uint parentId = GetParentNetworkedObjectId();

        using (PooledBitStream stream = PooledBitStream.Get()) {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream)) {
                TransformPacket.WritePacket(Time.time, GetRelativePosition(transform.position, parentId), transform.rotation, parentId, writer);

                if (isServer)
                    InvokeClientRpcOnEveryoneExcept(ApplyTransform, OwnerClientId, stream, channel: POS_CHANNEL);
                else
                    InvokeServerRpc(SubmitTransform, stream, channel: POS_CHANNEL);
            }
        }
    }

    private uint GetParentNetworkedObjectId() {
        Transform network_parent = networkedObject.transform.parent;
        if (network_parent != null) {
            // Get the networkId of any parent to our NetworkedObject
            NetworkedObject parent_netobj = network_parent.GetComponentInParent<NetworkedObject>();
            if (parent_netobj != null) {
                return parent_netobj.NetworkId;
            }
        }
        return 0;
    }

    private void Update() {
        if (!isOwner) {
            //If we are server and interpolation is turned on for server OR we are not server and interpolation is turned on
            if ((isServer && InterpolateServer && InterpolatePosition) || (!isServer && InterpolatePosition)) {
                float interp_time = Time.time - latency - interp_delay;
                Vector3 new_pos = position_buffer.Interpolate(interp_time, posOnLastReceive, FixedSendsPerSecond, Vector3.Lerp);
                if ((transform.position - new_pos).magnitude > 2f) {
                    Debug.Log("LARGE DIFF IN DISTANCE!");
                }
                transform.position = new_pos;
                transform.rotation = rotation_buffer.Interpolate(interp_time, rotOnLastReceive, FixedSendsPerSecond, Quaternion.Slerp);
            }
        }
    }

    [ClientRPC]
    private void ApplyTransform(uint clientId, Stream stream) {
        if (!enabled) return;
        TransformPacket received_transform = TransformPacket.FromStream(stream);

        if (InterpolatePosition) {
            lastRecieveTime = Time.time;
            if (received_transform.timestamp < lastRecieveClientTime) {
                Debug.Log("OUT OF ORDER PACKET DETECTED");
            }
            Vector3 world_pos = GetWorldPosition(received_transform.position, received_transform.parentId);
            Vector3 prev_world_pos = GetWorldPosition(lastReceivedPosition, lastRecievedParentId);
            // Use local velocity if we are on the same parent as the previous packet
            if (lastRecievedParentId == received_transform.parentId) {
                velocity = (received_transform.position - lastReceivedPosition) / (received_transform.timestamp - lastRecieveClientTime);
            }
            else {
                velocity = (world_pos - prev_world_pos) / (received_transform.timestamp - lastRecieveClientTime);
            }
            lastReceivedPosition = received_transform.position;
            lastRecieveClientTime = received_transform.timestamp;
            lastRecievedParentId = received_transform.parentId;

            posOnLastReceive = transform.position;
            rotOnLastReceive = transform.rotation;

            latency = latency_buffer.Accumulate(lastRecieveTime - lastRecieveClientTime) / latency_buffer.Size();
            Transform parent_transform = GetNetworkedObjectTransform(received_transform.parentId);
            position_buffer.Insert(lastRecieveClientTime, received_transform.position, parent_transform);
            rotation_buffer.Insert(lastRecieveClientTime, received_transform.rotation);
        }
        else {
            transform.position = received_transform.position;
            transform.rotation = received_transform.rotation;
        }
    }

    private Transform GetNetworkedObjectTransform(uint netId) {
        if (netId == 0) return null;
        NetworkedObject netobj = GetNetworkedObject(netId);
        if (netobj != null) {
            return netobj.transform;
        }
        return null;
    }

    private Vector3 GetWorldPosition(Vector3 relative_pos, uint parentId) {
        // 0 parentId indicates that there is no parent
        if (parentId == 0) return relative_pos;
        // try and get the world position converted from the parent networked objects local space
        NetworkedObject parent_netobj = GetNetworkedObject(parentId);
        if (parent_netobj != null) {
            return parent_netobj.transform.TransformPoint(relative_pos);
        }
        // if we fail, just return the relative position
        return relative_pos;
    }

    private Vector3 GetRelativePosition(Vector3 world_pos, uint parentId) {
        // 0 parentId indicates that there is no parent
        if (parentId == 0) return world_pos;
        // try and get the relative position to the parent networked object
        NetworkedObject parent_netobj = GetNetworkedObject(parentId);
        if (parent_netobj != null) {
            return parent_netobj.transform.InverseTransformPoint(world_pos);
        }
        // if we fail, just return the world position
        return world_pos;
    }

    [ServerRPC]
    private void SubmitTransform(uint clientId, Stream stream) {
        if (!enabled) return;
        TransformPacket received_transform = TransformPacket.FromStream(stream);

        if (IsMoveValidDelegate != null && !IsMoveValidDelegate(transform.position, received_transform.position)) {
            //Invalid move!
            //TODO: Add rubber band (just a message telling them to go back)
            return;
        }

        using (PooledBitStream writeStream = PooledBitStream.Get()) {
            using (PooledBitWriter writer = PooledBitWriter.Get(writeStream)) {
                received_transform.Write(writer);
                InvokeClientRpcOnEveryoneExcept(ApplyTransform, OwnerClientId, writeStream, channel: POS_CHANNEL);
            }
        }
    }

    /// <summary>
    /// Teleports the transform to the given position and rotation
    /// </summary>
    /// <param name="position">The position to teleport to</param>
    /// <param name="rotation">The rotation to teleport to</param>
    public void Teleport(Vector3 position, Quaternion rotation) {
        if (InterpolateServer && isServer || isClient) {
            // Implement when needed
        }
    }
}

public struct TransformPacket {
    public float timestamp;
    public Vector3 position;
    public Quaternion rotation;
    public uint parentId;

    public TransformPacket(float timestamp, Vector3 position, Quaternion rotation, uint parentId) {
        this.timestamp = timestamp;
        this.position = position;
        this.rotation = rotation;
        this.parentId = parentId;
    }

    public static TransformPacket FromStream(Stream stream) {
        using (PooledBitReader reader = PooledBitReader.Get(stream)) {
            float time = reader.ReadSinglePacked();

            float xPos = reader.ReadSinglePacked();
            float yPos = reader.ReadSinglePacked();
            float zPos = reader.ReadSinglePacked();

            float xRot = reader.ReadSinglePacked();
            float yRot = reader.ReadSinglePacked();
            float zRot = reader.ReadSinglePacked();

            uint pId = reader.ReadUInt32Packed();

            return new TransformPacket(time, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot), pId);
        }
    }

    public void Write(PooledBitWriter writer) {
        writer.WriteSinglePacked(timestamp);

        writer.WriteSinglePacked(position.x);
        writer.WriteSinglePacked(position.y);
        writer.WriteSinglePacked(position.z);

        writer.WriteSinglePacked(rotation.eulerAngles.x);
        writer.WriteSinglePacked(rotation.eulerAngles.y);
        writer.WriteSinglePacked(rotation.eulerAngles.z);

        writer.WriteUInt32Packed(parentId);
    }

    public static void WritePacket(float timestamp, Vector3 position, Quaternion rotation, uint parentId, PooledBitWriter writer) {
        new TransformPacket(timestamp, position, rotation, parentId).Write(writer);
    }
}

public class InterpBuffer<T> {
    private class BufferEntry {
        public float time;
        public T value;
        public Transform parent;

        public BufferEntry(float time, T value, Transform parent) {
            this.time = time;
            this.value = value;
            this.parent = parent;
        }
    }

    private List<BufferEntry> buffer;
    private Func<Transform, T, T> transform_function;
    public InterpBuffer(Func<Transform, T, T> transform_function = null) {
        buffer = new List<BufferEntry>();
        this.transform_function = transform_function;
    }

    public void Insert(float time, T value, Transform parent = null) {
        BufferEntry entry = new BufferEntry(time, value, parent);
        // Hardcode a limit on the buffer for now to prevent a leak
        if (buffer.Count > 20) {
            buffer.RemoveAt(0);
        }
        // Check the end to see if we should just append early
        if (buffer.Count == 0 || buffer.Last().time <= time) {
            buffer.Add(entry);
        }
        // Otherwise find where we need to insert this frame
        else {
            int idx = buffer.FindIndex((BufferEntry it) => { return it.time > time; });
            if (idx == -1) {
                buffer.Add(entry);
            }
            else {
                buffer.Insert(idx, entry);
            }
        }
    }

    public T Interpolate(float time, Func<T, T, float, T> interp_function) {
        if (buffer.Count == 0) {
            return default(T);
        }
        int upperboundidx = buffer.FindIndex((BufferEntry it) => { return it.time > time; });
        if (upperboundidx == -1) {
            return ComputeWorldValue(buffer.Last());
        }
        else if (upperboundidx == 0) {
            return ComputeWorldValue(buffer.First());
        }
        BufferEntry upper = buffer[upperboundidx];
        BufferEntry lower = buffer[upperboundidx - 1];
        return interp_function(ComputeWorldValue(lower), ComputeWorldValue(upper), (time - lower.time) / (upper.time - lower.time));
    }

    public T Interpolate(float time, T value, float rate, Func<T, T, float, T> interp_function) {
        // If the buffers empty, just return the current value
        if (buffer.Count == 0) {
            return value;
        }
        int upperboundidx = buffer.FindIndex((BufferEntry it) => { return it.time > time; });
        // If the current value occurs after all values in the buffer, return it
        if (upperboundidx == -1) {
            return value;
        }
        // If the current value occurs before all values in the buffer, interpolate
        // towards the first value from the current value using the expected rate.
        else if (upperboundidx == 0) {
            BufferEntry entry = buffer.First();
            return interp_function(value, ComputeWorldValue(entry), (entry.time - time) * rate);
        }
        // Otherwise interpolate between the two values surrounding the current value at the current time
        BufferEntry upper = buffer[upperboundidx];
        BufferEntry lower = buffer[upperboundidx - 1];
        return interp_function(ComputeWorldValue(lower), ComputeWorldValue(upper), (time - lower.time) / (upper.time - lower.time));
    }

    private T ComputeWorldValue(BufferEntry entry) {
        T world_value = entry.value;
        if (entry.parent != null && transform_function != null) {
            world_value = transform_function(entry.parent, world_value);
        }
        return world_value;
    }
}