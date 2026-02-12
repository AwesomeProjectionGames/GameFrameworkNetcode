#nullable enable
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
        public IActor ControlledActor { get; set; }
        public ISpectateController? SpectateController { get; set; }
        
        private NetworkVariable<NetworkObjectReference> _controlledActorReference = new NetworkVariable<NetworkObjectReference>(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);
        
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                _controlledActorReference.OnValueChanged += ProcessControlledActorChange;
                ProcessControlledActorChange(default, _controlledActorReference.Value);
            }
            base.OnNetworkSpawn();
        }


        public void PossessActor(IActor actor)
        {
            if ((Object)actor == null)
            {
                Debug.LogError("Cannot possess a null pawn.");
                return;
            }

            if ((ControlledActor as Object) != null)
            {
                UnpossessActor();
            }

            ControlledActor = actor;
            actor.SetOwner(this);
            OnPossess(actor);
        }

        public void OnPossess(IActor actor)
        {
            if (!IsServer)
            {
                //Only the owner of this controller can possess a actor / the server of this controller.
                //Don't emit a warning here, as this is expected behavior.
                //As this can be executed on any other client after PossessActor has been called.
                return;
            }
            if (!actor.IsAlive())
            {
                Debug.LogError("Cannot possess a null or dead actor.");
                return;
            }
            if (actor is not NetworkBehaviour networkBehaviour)
            {
                Debug.LogError("The actor must be a NetworkBehaviour to be possessed by a NetworkedController.");
                return;
            }
            _controlledActorReference.Value = networkBehaviour.NetworkObject;
        }

        public void UnpossessActor()
        {
            if (!ControlledActor.IsAlive())
            {
                Debug.LogError("Controller is not possessing any actor.");
                return;
            }

            if(ControlledActor.IsOwned()) ControlledActor.RemoveOwner();
            ControlledActor = null;
            OnUnpossess();
        }

        public void OnUnpossess()
        {
            if (!IsServer)
            {
                return;
            }

            _controlledActorReference.Value = new NetworkObjectReference();
        }

        private void ProcessControlledActorChange(NetworkObjectReference previousValue, NetworkObjectReference newValue)
        {
            IActor? actor = newValue.GetActorFromNetworkObject();
            if (actor.IsAlive())
            {
                PossessActor(actor);
            }
            else
            {
                ControlledActor = null;
            }
        }
    }
}