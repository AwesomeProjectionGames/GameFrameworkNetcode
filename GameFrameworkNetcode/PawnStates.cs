using System;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;
        public SerializableVector3(Vector3 v) => (x, y, z) = (v.x, v.y, v.z);
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public struct SerializableQuaternion
    {
        public float x, y, z, w;
        public SerializableQuaternion(Quaternion q) => (x, y, z, w) = (q.x, q.y, q.z, q.w);
        public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
    }

    public record PawnBaseState
    {
        public SerializableVector3 Position;
        public SerializableQuaternion Rotation;
    }
}