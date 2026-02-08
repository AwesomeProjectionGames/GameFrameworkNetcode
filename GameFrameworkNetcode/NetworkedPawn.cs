#nullable enable

using System;
using GameFramework;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    /// <summary>
    /// The netcode implementation of a pawn.
    /// </summary>
    public abstract class NetworkedPawn : SerializedNetworkedActor<PawnBaseState>, IPawn
    {
        #region Pawn
        public void Respawn()
        {
            if(GameInstance.Instance?.CurrentGameMode == null)
            {
                Debug.LogError("GameMode is not set. Cannot respawn player.");
                return;
            }

            Tuple<Vector3, Quaternion> tuple = GameInstance.Instance.CurrentGameMode.GetSpawnPoint(this).Select();
            if (tuple == null)
            {
                Debug.LogError("No spawn point found for player " + gameObject.name);
                return;
            }
            Teleport(tuple.Item1, tuple.Item2);
        }

        public abstract void Teleport(Vector3 location, Quaternion rotation);

        protected override PawnBaseState GetState()
        {
            return new PawnBaseState
            {
                Position = new SerializableVector3(transform.position),
                Rotation = new SerializableQuaternion(transform.rotation),
            };
        }

        protected override void SetState(PawnBaseState state)
        {
            transform.position = state.Position.ToVector3();
            transform.rotation = state.Rotation.ToQuaternion();
        }
        #endregion
    }
}