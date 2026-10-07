#nullable enable

using System;
using AwesomeProjectionCoreUtils.Extensions;
using GameFramework;
using GameFramework.Dependencies;
using MemoryPack;
using UnityEngine;
using UnityGameFrameworkImplementations.BaseImplementation;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    /// <summary>
    /// The netcode implementation of a pawn.
    /// Transform persistence is handled automatically via <see cref="TransformPersistence"/>.
    /// </summary>
    public abstract class NetworkedPawn : NetworkedActor, IPawn
    {
        protected override void Awake()
        {
            base.Awake();
            ComponentsContainerField.RegisterComponents(new object[] { new TransformPersistence(this) });
        }

        #region Pawn
        [ReplicatedMethod]
        public void Respawn()
        {
            if (!this.GameMode().IsAlive())
            {
                Debug.LogError("GameMode is not set. Cannot respawn player.");
                return;
            }

            Tuple<Vector3, Quaternion> tuple = this.GameMode()!.GetSpawnPoint(this).Select();
            if (tuple == null)
            {
                Debug.LogError("No spawn point found for player " + gameObject.name);
                return;
            }
            Teleport(tuple.Item1, tuple.Item2);
        }

        [ReplicatedMethod]
        public virtual void Teleport(Vector3 location, Quaternion rotation)
        {
            transform.position = location;
            transform.rotation = rotation;
        }
        #endregion

        #region Persistence
        public class TransformPersistence : ISerializedObject
        {
            private readonly NetworkedPawn _pawn;

            public TransformPersistence(NetworkedPawn pawn)
            {
                _pawn = pawn;
            }

            public byte[] Serialize()
            {
                var state = new PawnBaseState(_pawn.transform.position, _pawn.transform.rotation);
                return MemoryPackSerializer.Serialize(state);
            }

            public void Deserialize(byte[] data)
            {
                if (data == null || data.Length == 0) return;
                var state = MemoryPackSerializer.Deserialize<PawnBaseState>(data);
                _pawn.Teleport(state.Position, state.Rotation);
            }
        }
        #endregion
    }
}