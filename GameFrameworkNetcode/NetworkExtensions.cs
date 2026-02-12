#nullable enable

using AwesomeProjectionCoreUtils.Extensions;
using GameFramework;
using Unity.Netcode;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    public static class NetworkExtensions
    {
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