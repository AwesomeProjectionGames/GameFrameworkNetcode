#nullable enable

using System;
using GameFramework;
using GameFramework.Dependencies;
using MemoryPack;
using Unity.Netcode;
using UnityGameFrameworkImplementations.Core;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    /// <summary>
    /// Composition-first base class for networked components where state is synchronized
    /// via NGO NetworkVariable and serialized via MemoryPack.
    /// Replaces monolithic GetState/SetState patterns.
    /// </summary>
    /// <typeparam name="TState">An unmanaged struct implementing IMemoryPackable.</typeparam>
    public abstract class NetworkedComponent<TState> : NetBehaviour, IEntityComponent, ISerializedObject
        where TState : unmanaged, IMemoryPackable<TState>
    {
        /// <summary>
        /// The authoritative networked state for this component.
        /// Consumers access this directly: myComp.State.Value (and myComp.State.OnValueChanged).
        /// </summary>
        public NetworkVariable<TState> State = new();

        /// <summary>
        /// The parent entity owning this component.
        /// Injected automatically by the Actor's ComponentsContainer.
        /// </summary>
        public IEntity Entity { get; set; } = null!;

        public virtual byte[] Serialize()
        {
            return MemoryPackSerializer.Serialize(State.Value);
        }

        public virtual void Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            State.Value = MemoryPackSerializer.Deserialize<TState>(data);
        }
    }
}
