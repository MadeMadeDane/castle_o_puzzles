﻿using System.Collections.Generic;
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

    private Vector3 lerpStartPos;
    private Quaternion lerpStartRot;
    private Vector3 lerpEndPos;
    private Quaternion lerpEndRot;

    private float lastSendTime;
    private Vector3 lastSentPos;
    private Quaternion lastSentRot;

    private float lastRecieveTime;

    /// <summary>
    /// The curve to use to calculate the send rate
    /// </summary>
    public AnimationCurve DistanceSendrate = AnimationCurve.Constant(0, 500, 20);

    # region new vars
    private const string POS_CHANNEL = "MLAPI_DEFAULT_MESSAGE";
    private Vector3 posOnLastReceive;
    private Quaternion rotOnLastReceive;
    private Vector3 lastRecievedPosition;
    private InterpBuffer<Vector3> position_buffer = new InterpBuffer<Vector3>();
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

        lastSentRot = transform.rotation;
        lastSentPos = transform.position;

        lerpStartPos = transform.position;
        lerpStartRot = transform.rotation;

        lerpEndPos = transform.position;
        lerpEndRot = transform.rotation;

        if (isOwner) {
            InvokeRepeating("TransmitPosition", 0f, (1f / FixedSendsPerSecond));
        }
    }

    private void TransmitPosition() {
        lastSendTime = NetworkingManager.singleton.NetworkTime;
        lastSentPos = transform.position;
        lastSentRot = transform.rotation;
        using (PooledBitStream stream = PooledBitStream.Get()) {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream)) {
                TransformPacket.WritePacket(Time.time, transform.position, transform.rotation, writer);

                if (isServer)
                    InvokeClientRpcOnEveryoneExcept(ApplyTransform, OwnerClientId, stream, channel: POS_CHANNEL);
                else
                    InvokeServerRpc(SubmitTransform, stream, channel: POS_CHANNEL);
            }
        }
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
            velocity = (received_transform.position - lastRecievedPosition) / (received_transform.timestamp - lastRecieveClientTime);
            lastRecievedPosition = received_transform.position;
            lastRecieveClientTime = received_transform.timestamp;

            posOnLastReceive = transform.position;
            rotOnLastReceive = transform.rotation;

            latency = latency_buffer.Accumulate(lastRecieveTime - lastRecieveClientTime) / latency_buffer.Size();
            position_buffer.Insert(lastRecieveClientTime, received_transform.position);
            rotation_buffer.Insert(lastRecieveClientTime, received_transform.rotation);
        }
        else {
            transform.position = received_transform.position;
            transform.rotation = received_transform.rotation;
        }
    }

    [ServerRPC]
    private void SubmitTransform(uint clientId, Stream stream) {
        if (!enabled) return;
        TransformPacket received_transform = TransformPacket.FromStream(stream);

        if (IsMoveValidDelegate != null && !IsMoveValidDelegate(lerpEndPos, received_transform.position)) {
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
            lerpStartPos = position;
            lerpStartRot = rotation;
            lerpEndPos = position;
            lerpEndRot = rotation;
        }
    }
}

public struct TransformPacket {
    public float timestamp;
    public Vector3 position;
    public Quaternion rotation;

    public TransformPacket(float timestamp, Vector3 position, Quaternion rotation) {
        this.timestamp = timestamp;
        this.position = position;
        this.rotation = rotation;
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

            return new TransformPacket(time, new Vector3(xPos, yPos, zPos), Quaternion.Euler(xRot, yRot, zRot));
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
    }

    public static void WritePacket(float timestamp, Vector3 position, Quaternion rotation, PooledBitWriter writer) {
        new TransformPacket(timestamp, position, rotation).Write(writer);
    }
}

public class InterpBuffer<T> {
    private class BufferEntry {
        public float time;
        public T value;

        public BufferEntry(float time, T value) {
            this.time = time;
            this.value = value;
        }
    }

    private List<BufferEntry> buffer;
    public InterpBuffer() {
        buffer = new List<BufferEntry>();
    }

    public void Insert(float time, T value) {
        BufferEntry entry = new BufferEntry(time, value);
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
            return buffer.Last().value;
        }
        else if (upperboundidx == 0) {
            return buffer.First().value;
        }
        BufferEntry upper = buffer[upperboundidx];
        BufferEntry lower = buffer[upperboundidx - 1];
        return interp_function(lower.value, upper.value, (time - lower.time) / (upper.time - lower.time));
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
            return interp_function(value, entry.value, (entry.time - time) * rate);
        }
        // Otherwise interpolate between the two values surrounding the current value at the current time
        BufferEntry upper = buffer[upperboundidx];
        BufferEntry lower = buffer[upperboundidx - 1];
        return interp_function(lower.value, upper.value, (time - lower.time) / (upper.time - lower.time));
    }
}