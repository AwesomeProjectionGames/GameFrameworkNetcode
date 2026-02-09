#nullable enable

using Unity.Netcode;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    public class PawnSpawner : NetBehaviour
    {
        public GameObject? SpawnOwnedPawn(ulong clientId, GameObject playerPrefab, bool destroyWithScene = true)
        {
            if (!IsServer)
            {
                Debug.LogError("SpawnOwnerPawn can only be called on the server.");
                return null;
            }

            GameObject playerObject = Instantiate(playerPrefab);
            var networkObject = playerObject.GetComponent<NetworkObject>();
            networkObject?.SpawnAsPlayerObject(clientId, destroyWithScene);
            return playerObject;
        }

        public GameObject? SpawnPawn(GameObject pawnPrefab, bool destroyWithScene = true)
        {
            if (!IsServer)
            {
                Debug.LogError("SpawnPawn can only be called on the server.");
                return null;
            }

            GameObject pawnObject = Instantiate(pawnPrefab);
            var networkObject = pawnObject.GetComponent<NetworkObject>();
            networkObject?.Spawn(destroyWithScene);
            return pawnObject;
        }
    }
}