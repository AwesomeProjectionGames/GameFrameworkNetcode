#nullable enable

using System.Collections.Generic;
using AwesomeProjectionCoreUtils.Extensions;
using GameFramework;
using GameFramework.Saving;
using GameFramework.SpawnPoint;
using Unity.Netcode;
using UnityEngine;
using UnityGameFrameworkImplementations.BaseImplementation;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    [RequireComponent(typeof(NetworkedGameModeState))]
    public class NetworkedGameMode : NetEntity, IGameMode
    {
        public IPawn DefaultPawnPrefab => defaultPawnPrefab.GetComponent<IPawn>();
        public IController DefaultControllerPrefab => defaultControllerPrefab.GetComponent<IController>();
        public IGameState CurrentGameState => _networkedGameModeState;

        [SerializeField] private GameObject? defaultPawnPrefab;
        [SerializeField] GameObject defaultControllerPrefab = null!;
        [SerializeField] private BaseSpawnPoint spawnPoints;

        [BindEntityComponent] NetworkedGameModeState _networkedGameModeState;

        /// <summary>
        /// Maps a network prefab hash (<see cref="NetworkObject.PrefabIdHash"/>) to its registered
        /// NetworkPrefab asset. Built once on spawn so resolving a prefab during <see cref="Spawn"/>
        /// is an O(1) lookup over already-resident assets.
        /// A scene instance of a prefab reports the same hash as the prefab asset it derives from,
        /// so this resolves any networked actor, not just those exposing a content identifier.
        /// </summary>
        private readonly Dictionary<uint, GameObject> _prefabsByNetworkHash = new();

        public override void OnNetworkSpawn()
        {
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

            CacheRegisteredNetworkPrefabs();
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;

            _prefabsByNetworkHash.Clear();
        }

        private void CacheRegisteredNetworkPrefabs()
        {
            _prefabsByNetworkHash.Clear();

            foreach (NetworkPrefab networkPrefab in NetworkManager.NetworkConfig.Prefabs.Prefabs)
            {
                if (networkPrefab.Prefab == null) continue;

                uint hash = networkPrefab.Prefab.GetComponent<NetworkObject>()?.PrefabIdHash ?? 0;
                if (hash != 0)
                {
                    _prefabsByNetworkHash[hash] = networkPrefab.Prefab;
                }
            }
        }


        protected virtual void OnEnable()
        {
            if (GameInstance.Instance == null)
            {
                Debug.LogError(
                    "No GameInstance found in the scene. Make sure to have one GameInstance prefab in your scene.");
                return;
            }

            if (GameInstance.Instance.CurrentGameMode != null)
            {
                Debug.LogWarning("A game mode is already set. Replacing it with the new one.");
            }

            GameInstance.Instance.CurrentGameMode = this;

            if (!spawnPoints.IsAlive())
            {
                Debug.LogError("Spawn points are not correctly set in the MainGameMode.");
                return;
            }
        }

        protected virtual void OnDisable()
        {
            if (ReferenceEquals(GameInstance.Instance?.CurrentGameMode, this))
            {
                GameInstance.Instance.CurrentGameMode = null;
            }
        }

        protected virtual void HandleClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            SpawnPlayer(clientId);
            SendActorStatesToNewClient(clientId);
        }

        
        /// <summary>
        /// Spawn a full player, inclusing Controller and optionnally spectate controller, and possess a pawn if a default pawn prefab is set.
        /// </summary>
        /// <param name="clientId"></param>
        protected IController? SpawnPlayer(ulong clientId)
        {
            var controllerGO = SpawnOwnedPawn(clientId, defaultControllerPrefab, false);
            var controller = controllerGO?.GetComponent<IController>();
            if (controller == null) return null;

            if (defaultPawnPrefab.IsAlive())
            {
                var pawn = Spawn(defaultPawnPrefab.GetComponent<IPawn>()) as IPawn;
                if (pawn.IsAlive())
                {
                    pawn!.Respawn();
                    controller.PossessActor(pawn);
                }
            }
            return controller;
        }

        protected void SendActorStatesToNewClient(ulong clientId)
        {
            //Send other actor states
            foreach (var actor in CurrentGameState.Actors)
            {
                if (actor is INetworkedSerializedObject serializable)
                {
                    serializable.SendStateToClientFromServer(clientId);
                }
            }
        }

        protected void HandleClientDisconnected(ulong clientId)
        {
            foreach (var ctrl in CurrentGameState.Controllers)
            {
                if (((NetworkBehaviour)ctrl).OwnerClientId == clientId)
                {
                    if(ctrl.ControlledActor.IsAlive()) ctrl.UnpossessActor();
                    break;
                }
            }
        }

        public ISpawnPoint GetSpawnPoint(IPawn pawn)
        {
            return spawnPoints;
        }

        #region Spawning
        public IEntity? Spawn(IEntity prefab, bool destroyWithScene = true)
        {
            GameObject? spawned = SpawnPawn(prefab.Transform.gameObject, destroyWithScene);
            if (spawned == null) return null;
            return spawned.GetComponent<IActor>();
        }

        public IEntity? SpawnAtLocation(IEntity prefab, Vector3 location, Quaternion rotation, bool destroyWithScene = true)
        {
            IEntity? actor = Spawn(prefab, destroyWithScene);
            if (actor == null) return null;
            actor.Move(location, rotation);
            return actor;
        }

        private GameObject? SpawnOwnedPawn(ulong clientId, GameObject playerPrefab, bool destroyWithScene = true)
        {
            var obj = InternalSpawn(playerPrefab, destroyWithScene, out var netObj);
            netObj?.SpawnAsPlayerObject(clientId, destroyWithScene);
            return obj;
        }

        private GameObject? SpawnPawn(GameObject pawnPrefab, bool destroyWithScene = true)
        {
            var obj = InternalSpawn(pawnPrefab, destroyWithScene, out var netObj);
            netObj?.Spawn(destroyWithScene);
            return obj;
        }

        private GameObject? InternalSpawn(GameObject prefab, bool destroyWithScene, out NetworkObject? networkObject)
        {
            networkObject = null;

            if (!IsServer)
            {
                Debug.LogError($"{nameof(InternalSpawn)} can only be called on the server.");
                return null;
            }
            
            GameObject? prefabAsset = ResolveProjectPrefab(prefab);
            if (prefabAsset == null)
            {
                networkObject = null;
                return null;
            }

            GameObject obj = Instantiate(prefabAsset);

            networkObject = obj.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogWarning($"Spawned {prefab.name} but no NetworkObject was found.", prefab);
            }

            return obj;
        }


        /// <summary>
        /// Resolves a scene-placed GameObject back to the registered NetworkPrefab asset it derives
        /// from, so that network spawning operates on a project asset instead of a scene instance.
        /// Netcode cannot spawn an unregistered prefab: clients resolve the prefab by hash, so
        /// instantiating a scene object would produce an object no client can spawn.
        /// If <paramref name="source"/> is already a project asset it is returned directly.
        /// Returns null and logs the specific reason when no registered prefab can be resolved.
        /// </summary>
        protected virtual GameObject? ResolveProjectPrefab(GameObject source)
        {
            // Already an asset (prefab reference or Resources.Load result), nothing to resolve.
            if (!source.scene.IsValid()) return source;

            NetworkObject? sourceNetObj = source.GetComponent<NetworkObject>();
            if (sourceNetObj == null)
            {
                Debug.LogError(
                    $"Cannot spawn '{source.name}' as a scene instance: it has no NetworkObject. " +
                    "Add a NetworkObject, or reference the prefab asset instead of a scene instance.", source);
                return null;
            }

            uint hash = sourceNetObj.PrefabIdHash;
            if (hash == 0)
            {
                Debug.LogError(
                    $"Cannot spawn '{source.name}' as a scene instance: its NetworkObject has no prefab hash. " +
                    "This object was likely added procedurally without being saved; re-save the scene.", source);
                return null;
            }

            if (_prefabsByNetworkHash.TryGetValue(hash, out GameObject? registered))
            {
                return registered;
            }

            Debug.LogError(
                $"Cannot spawn '{source.name}' as a scene instance: its prefab hash {hash} is not among the " +
                "registered NetworkPrefabs. Add it to the NetworkManager's Network Prefabs list.", source);
            return null;
        }
        #endregion
    }
}
