#nullable enable

using AwesomeProjectionCoreUtils.Extensions;
using GameFramework;
using Unity.Netcode;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    public static class NetworkExtensions
    {
        /// <summary>
        /// Attempts to retrieve the <see cref="NetworkObject"/> associated with this actor.
        /// Returns null if the actor is not alive SILENTLY.
        /// </summary>
        /// <returns>The NetworkObject if found and the actor is valid; otherwise, null.</returns>
        public static NetworkObject? GetNetworkObject(this IActor actor)
        {
            if(!actor.IsAlive()) return null;
            if(!actor.Transform.TryGetComponent(out NetworkObject networkObject))
            {
                Debug.LogError("Actor does not have a NetworkObject or it is not spawned.");
                return null;
            }
            return networkObject;
        }
        
        /// <summary>
        /// Returns true if the local client is the owner of this actor's NetworkObject.
        /// Returns false if the actor is not alive, does not have a NetworkObject, or if the local client is not the owner of the NetworkObject.
        /// </summary>
        public static bool IsOwned(this IActor actor)
        {
            var networkObject = actor.GetNetworkObject();
            return networkObject != null && networkObject.IsOwner;
        }
        
        /// <summary>
        /// Resolves a <see cref="NetworkObjectReference"/> back into a local <see cref="IPawn"/> instance.
        /// Log error only if the NetworkObject exists but does not have a valid IPawn component or the pawn is not alive. Otherwise, return null silently.
        /// </summary>
        public static IPawn? GetPawnFromNetworkObject(this NetworkObjectReference networkObjectRef)
        {
            if (networkObjectRef.TryGet(out NetworkObject networkObject))
            {
                if (networkObject.TryGetComponent(out IPawn pawn))
                {
                    return pawn;
                }
                Debug.LogError("NetworkObject does not have a valid IPawn component or the pawn is not alive.");
                return null;
            }
            return null;
        }
        
        /// <summary>
        /// Resolves a <see cref="NetworkObjectReference"/> back into a local <see cref="IActor"/> instance.
        /// Log error only if the NetworkObject exists but does not have a valid IActor component or the actor is not alive. Otherwise, return null silently.
        /// </summary>
        public static IActor? GetActorFromNetworkObject(this NetworkObjectReference networkObjectRef)
        {
            if (networkObjectRef.TryGet(out NetworkObject networkObject))
            {
                if (networkObject.TryGetComponent(out IActor actor))
                {
                    return actor;
                }
                Debug.LogError("NetworkObject does not have a valid IActor component or the pawn is not alive.");
                return null;
            }
            return null;
        }
    }
}