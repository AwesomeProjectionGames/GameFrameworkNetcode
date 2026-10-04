using System;
using Unity.Netcode;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    [Serializable]
    public struct SerializableVector3 : INetworkSerializable
    {
        public float x, y, z;
        public SerializableVector3(Vector3 v) => (x, y, z) = (v.x, v.y, v.z);
        public Vector3 ToVector3() => new Vector3(x, y, z);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
            serializer.SerializeValue(ref z);
        }
    }

    [Serializable]
    public struct SerializableQuaternion : INetworkSerializable
    {
        public float x, y, z, w;
        public SerializableQuaternion(Quaternion q) => (x, y, z, w) = (q.x, q.y, q.z, q.w);
        public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
            serializer.SerializeValue(ref z);
            serializer.SerializeValue(ref w);
        }
    }

    [Serializable]
    public record PawnBaseState : INetworkSerializable
    {
        public SerializableVector3 Position;
        public SerializableQuaternion Rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            Position.NetworkSerialize(serializer);
            Rotation.NetworkSerialize(serializer);
        }
    }
}