#nullable enable

using System;
using System.Collections.Generic;
using AwesomeProjectionCoreUtils.Extensions;
using GameFramework;
using GameFramework.Dependencies;
using MemoryPack;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    /// <summary>
    /// The netcode implementation of an actor.
    /// Keeps the UUID and owner in sync across the network.
    /// </summary>
    public class NetworkedActor : AbstractActor
    {
        public override string UUID => NetUUID.Value.ToString();

        [UnityEngine.Serialization.FormerlySerializedAs("networkedUUID")]
        public NetworkVariable<FixedString64Bytes> NetUUID = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
        
        [UnityEngine.Serialization.FormerlySerializedAs("ownerReference")]
        public NetworkVariable<NetworkObjectReference> NetOwnerReference = new NetworkVariable<NetworkObjectReference>(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);

        public override void OnNetworkSpawn()
        {
            NetOwnerReference.OnValueChanged += HandleOwnerChanged;

            // This is usefull only if it's an other client joining the game mid-session (and the ownerReference is already set but didn't trigger the event)
            if (!NetOwnerReference.Value.Equals(default))
            {
                HandleOwnerChanged(default, NetOwnerReference.Value);
            }
            if (IsServer && NetUUID.Value.IsEmpty)
            {
                TryToChangeUUID(Server_GetUniqueUuid());
            }
        }

        /// <summary>
        /// Formats the deterministic scene UUID from a <see cref="NetworkObject"/>'s <see cref="NetworkObject.PrefabIdHash"/>.
        /// Single source of truth shared between runtime spawning and editor tooling.
        /// </summary>
        public static bool TryGetSceneUuid(NetworkObject? networkObject, out string uuid)
        {
            if (networkObject != null && networkObject.PrefabIdHash != 0)
            {
                uuid = networkObject.PrefabIdHash.ToString("x8");
                return true;
            }

            uuid = string.Empty;
            return false;
        }

        /// <summary>
        /// Generates a unique UUID for this actor.
        /// Scene-placed actors reuse their deterministic <see cref="NetworkObject.PrefabIdHash"/> (GlobalObjectIdHash)
        /// so their UUID stays stable across sessions; dynamically spawned actors get a fresh <see cref="Guid"/>.
        /// </summary>
        /// <returns>A unique UUID string.</returns>
        protected virtual string Server_GetUniqueUuid()
        {
            if (NetworkObject != null && NetworkObject.IsSceneObject == true && TryGetSceneUuid(NetworkObject, out string sceneUuid))
            {
                return sceneUuid;
            }

            return Guid.NewGuid().ToString();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            NetOwnerReference.OnValueChanged -= HandleOwnerChanged;
            if (_owner != null)
            {
                _owner.OnAnyOwnerChanged -= HandleAnyOwnerChange;
            }
        }

        [ReplicatedMethod]
        public override bool TryToChangeUUID(string newUUID)
        {
            if (!IsServer) return false;
            if (string.IsNullOrEmpty(newUUID))
            {
                Debug.LogError("Cannot set an empty or null UUID.");
                return false;
            }
            NetUUID.Value = newUUID;
            return true;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        [ReplicatedMethod]
        public override void SetOwner(IActor newOwner)
        {
            if (!newOwner.IsAlive())
            {
                Debug.LogError("Cannot set owner to null.");
                return;
            }
            if (newOwner is not NetBehaviour newOwnerNetBehaviour)
            {
                Debug.LogError("New owner must be a NetworkBehaviour.");
                return;
            }

            if (!IsServer && !newOwner.IsOwned())
            {
                Debug.LogError("Can only set owner to an already owned actor when not the server.");
                return;
            }

            RequestSetOwnerRpc(newOwnerNetBehaviour.NetworkObject);
        }

        [ReplicatedMethod]
        public override void RemoveOwner()
        {
            if (!Owner.IsAlive())
            {
                Debug.LogError("No owner to remove.");
                return;
            }
            if (!IsServer && !Owner!.IsOwned())
            {
                Debug.LogError("Can only remove owner if it's already owned by the same player when not the server.");
                return;
            }
            RequestRemoveOwnerRpc();
        }

        // -------------------------------------------------------------------------
        // Server Logic.
        // -------------------------------------------------------------------------

#if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestSetOwnerRpc(NetworkObjectReference newOwnerRef, RpcParams serverRpcParams = default)
#else
        [ServerRpc(RequireOwnership = false)]
        public void RequestSetOwnerRpc(NetworkObjectReference newOwnerRef, ServerRpcParams serverRpcParams = default)
#endif
        {
            if (!IsServer) return;

            ulong senderId = serverRpcParams.Receive.SenderClientId;

            // Verify authority over this actor
            if (senderId != OwnerClientId && senderId != NetworkManager.ServerClientId)
            {
                Debug.LogError($"[Security] Client {senderId} tried to modify actor {name} but does not own it.");
                return;
            }

            // Optional: Verify authority over the new owner (parent) to prevent spamming/griefing
            // For now, we only enforcing ownership of the child, assuming that parenting to a public object might be valid in some game logic,
            // or that the parent's own security checks (if any) would handle it. 
            // Strengthening this to require ownership of BOTH would be safer but more restrictive.
            // Given the audit scope, securing 'this' is the critical first step.

            NetOwnerReference.Value = newOwnerRef;
        }

#if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void RequestRemoveOwnerRpc(RpcParams serverRpcParams = default)
#else
        [ServerRpc(RequireOwnership = false)]
        public void RequestRemoveOwnerRpc(ServerRpcParams serverRpcParams = default)
#endif
        {
            if (!IsServer) return;

            ulong senderId = serverRpcParams.Receive.SenderClientId;
            if (senderId != OwnerClientId && senderId != NetworkManager.ServerClientId)
            {
                Debug.LogError($"[Security] Client {senderId} tried to remove owner of {name} but does not own it.");
                return;
            }

            NetOwnerReference.Value = new NetworkObjectReference(); // Clear the owner reference (equivalent to null)
        }

        private void Server_UpdateNetworkBehaviourOwnership()
        {
            if (!IsServer) return;
            IMachine? machine = (this as IActor).Controller?.Machine;
            if (machine.IsAlive())
            {
                NetworkObject.ChangeOwnership(machine!.ClientId);
            }
            else
            {
                NetworkObject.RemoveOwnership();
            }
        }


        // -------------------------------------------------------------------------
        // Reactive Logic (Runs on All Clients + Server)
        // -------------------------------------------------------------------------

        /// <summary>
        /// This is the SINGLE point of truth. All logic reacts to the NetworkVariable change.
        /// </summary>
        private void HandleOwnerChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
        {
            IActor? lastOwner = _owner;
            _owner = newValue.GetActorFromNetworkObject();

            OnOwnerDidChange(lastOwner, _owner);
            if (IsServer)
            {
                Server_UpdateNetworkBehaviourOwnership();
            }
        }
    }
}