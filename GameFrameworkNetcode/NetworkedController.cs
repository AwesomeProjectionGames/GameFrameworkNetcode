#nullable enable
using System;
using AwesomeProjectionCoreUtils.Extensions;
using GameFramework;
using Unity.Netcode;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    public abstract class NetworkedController : NetworkedActor, IController
    {
        public abstract IMachine Machine { get; }
        public abstract string Name { get; }
        public IActor? ControlledActor { get; set; }
        public ISpectateController? SpectateController { get; set; }

        // Initializing with default (null) value
        private readonly NetworkVariable<NetworkObjectReference> _controlledActorReference = new(
            default, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Subscribe to changes
            _controlledActorReference.OnValueChanged += HandleControlledActorChanged;

            // This is usefull only if it's an other client joining the game mid-session (and the ownerReference is already set but didn't trigger the event)
            if (!_controlledActorReference.Value.Equals(default))
            {
                // Initial sync: If we are joining late or just spawned, process the current value
                HandleControlledActorChanged(default, _controlledActorReference.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            _controlledActorReference.OnValueChanged -= HandleControlledActorChanged;
            base.OnNetworkDespawn();
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        public void PossessActor(IActor actor)
        {
            // 1. Validation
            if (actor == null || (actor is UnityEngine.Object obj && obj == null))
            {
                Debug.LogError("Attempted to possess a null actor.",this);
                return;
            }

            // check if actor is compatible with Netcode
            if (actor is not NetworkBehaviour networkBehaviour)
            {
                Debug.LogError($"Attempted to possess an actor that is not a NetworkBehaviour. Actor: {actor}. Operation aborted.", this);
                return;
            }

            // 2. Permission Check
            if (!IsServer && !IsOwner)
            {
                Debug.LogError("Only the Server or the Owner can initiate possession. Operation aborted.", this);
                return;
            }

            // 3. Request Change via RPC
            PossessActorServerRpc(networkBehaviour.NetworkObject);
        }

        public void UnpossessActor()
        {
            // 1. Validation
            if (!ControlledActor.IsAlive())
            {
                Debug.LogError("Attempted to unpossess when there is no currently possessed actor. Operation aborted.", this);
                return;
            }

            // 2. Permission Check
            if (!IsServer && !IsOwner)
            {
                Debug.LogError("Only the Server or the Owner can initiate unpossession. Operation aborted.", this);
                return;
            }

            // 3. Request Change via RPC
            UnpossessActorServerRpc();
        }

        // -------------------------------------------------------------------------
        // Server Logic. Warning : currently, a non owner could hack the packet and possess an actor they shouldn't have access to.
        // -------------------------------------------------------------------------

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]// Everyone because we check permissions manually/allow server calls
        private void PossessActorServerRpc(NetworkObjectReference actorRef)
        {
            if (!IsServer) return; 

            _controlledActorReference.Value = actorRef;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]// Everyone because we check permissions manually/allow server calls
        private void UnpossessActorServerRpc()
        {
            if (!IsServer) return;

            _controlledActorReference.Value = new NetworkObjectReference(); // Null reference
        }

        // -------------------------------------------------------------------------
        // Reactive Logic (Runs on All Clients + Server)
        // -------------------------------------------------------------------------

        /// <summary>
        /// This is the SINGLE point of truth. All logic reacts to the NetworkVariable change.
        /// </summary>
        private void HandleControlledActorChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
        {
            // 1. Handle Unpossess (Cleanup previous)
            // We check if we had a locally tracked actor, or try to resolve the previous ID
            if (ControlledActor != null) 
            {
                // Logic cleanup
                if (ControlledActor.IsOwned())
                {
                    ControlledActor.RemoveOwner();
                }
                
                // Fire virtual callback
                OnUnpossess();
                
                ControlledActor = null;
            }
            // Fallback: If local state was desynced, ensure we don't leave dangling references based on previousValue
            else 
            {
                 IActor? previousActor = previousValue.GetActorFromNetworkObject();
                 if (previousActor != null && previousActor.IsAlive())
                 {
                     // Force cleanup just in case
                     if(previousActor.IsOwned()) previousActor.RemoveOwner();
                 }
            }

            // 2. Handle Possess (Setup new)
            IActor? newActor = newValue.GetActorFromNetworkObject();
            
            // Check if the new value is actually a valid actor
            if (newActor != null && newActor.IsAlive())
            {
                ControlledActor = newActor;
                if (ControlledActor.IsOwned())
                {
                    newActor.SetOwner(this);
                }
                
                // Fire virtual callback
                OnPossess(newActor);
            }
        }

        /// <summary>
        /// Called after a actor has been successfully possessed via Network Sync.
        /// </summary>
        protected virtual void OnPossess(IActor actor)
        {
            // Custom logic override
        }

        /// <summary>
        /// Called after a actor has been unpossessed via Network Sync.
        /// </summary>
        protected virtual void OnUnpossess()
        {
            // Custom logic override
        }
    }
}