#nullable enable

using System;
using MemoryPack;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    [Serializable]
    [MemoryPackable(SerializeLayout.Explicit)]
    public partial struct PawnBaseState : IEquatable<PawnBaseState>
    {
        [MemoryPackOrder(0)]
        public Vector3 Position;

        [MemoryPackOrder(1)]
        public Quaternion Rotation;

        public PawnBaseState(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public bool Equals(PawnBaseState other) => Position.Equals(other.Position) && Rotation.Equals(other.Rotation);
        public override bool Equals(object? obj) => obj is PawnBaseState other && Equals(other);
        public override int GetHashCode() => unchecked((Position.GetHashCode() * 397) ^ Rotation.GetHashCode());
        public static bool operator ==(PawnBaseState left, PawnBaseState right) => left.Equals(right);
        public static bool operator !=(PawnBaseState left, PawnBaseState right) => !left.Equals(right);
    }
}