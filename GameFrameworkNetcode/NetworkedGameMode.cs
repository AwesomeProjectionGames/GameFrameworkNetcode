#nullable enable

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

        public override void OnNetworkSpawn()
        {
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
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
            Server_HandleClientConnected(clientId);
        }

        protected virtual void Server_HandleClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            Server_SpawnPlayer(clientId);
            Server_SendActorStatesToNewClient(clientId);
        }

        /// <summary>
        /// Spawn a full player, inclusing Controller and optionnally spectate controller, and possess a pawn if a default pawn prefab is set.
        /// </summary>
        /// <param name="clientId"></param>
        protected IController? Server_SpawnPlayer(ulong clientId)
        {
            var controllerGO = Server_SpawnOwnedPawn(clientId, defaultControllerPrefab, false);
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

        protected void Server_SendActorStatesToNewClient(ulong clientId)
        {
            //Send other actor states
            foreach (var actor in CurrentGameState.Actors)
            {
                if (actor is INetworkedSerializedObject serializable)
                {
                    serializable.Server_SendStateToClient(clientId);
                }
            }
        }

        protected void HandleClientDisconnected(ulong clientId)
        {
            Server_HandleClientDisconnected(clientId);
        }

        protected virtual void Server_HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;
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
        [ReplicatedMethod]
        public IEntity? Spawn(IEntity prefab, bool destroyWithScene = true)
        {
            GameObject? spawned = Server_SpawnPawn(prefab.Transform.gameObject, destroyWithScene);
            if (spawned == null) return null;
            return spawned.GetComponent<IActor>();
        }

        [ReplicatedMethod]
        public IEntity? SpawnAtLocation(IEntity prefab, Vector3 location, Quaternion rotation, bool destroyWithScene = true)
        {
            IEntity? actor = Spawn(prefab, destroyWithScene);
            if (actor == null) return null;
            actor.Move(location, rotation);
            return actor;
        }

        private GameObject? Server_SpawnOwnedPawn(ulong clientId, GameObject playerPrefab, bool destroyWithScene = true)
        {
            var obj = Server_InternalSpawn(playerPrefab, destroyWithScene, out var netObj);
            netObj?.SpawnAsPlayerObject(clientId, destroyWithScene);
            return obj;
        }

        private GameObject? Server_SpawnPawn(GameObject pawnPrefab, bool destroyWithScene = true)
        {
            var obj = Server_InternalSpawn(pawnPrefab, destroyWithScene, out var netObj);
            netObj?.Spawn(destroyWithScene);
            return obj;
        }

        private GameObject? Server_InternalSpawn(GameObject prefab, bool destroyWithScene, out NetworkObject? networkObject)
        {
            networkObject = null;

            if (!IsServer)
            {
                Debug.LogError($"{nameof(Server_InternalSpawn)} can only be called on the server.");
                return null;
            }

            // If a scene instance is passed, load its project prefab asset via ResourcePath
            if (prefab.scene.IsValid())
            {
                var actor = prefab.GetComponent<IActor>();
                if (actor != null && !string.IsNullOrEmpty(actor.ResourcePath))
                {
                    var loadedAsset = Resources.Load<GameObject>(actor.ResourcePath);
                    if (loadedAsset != null)
                    {
                        prefab = loadedAsset;
                    }
                    else
                    {
                        Debug.LogError($"Cannot spawn scene instance '{prefab.name}': failed to load prefab from Resources at '{actor.ResourcePath}'.", prefab);
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"Cannot spawn scene instance '{prefab.name}': it is not an IActor or has no ResourcePath. Configure ResourcePath on the actor prefab in a Resources folder.", prefab);
                    return null;
                }
            }

            GameObject obj = Instantiate(prefab);
            networkObject = obj.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogWarning($"Spawned {prefab.name} but no NetworkObject was found.", prefab);
            }

            return obj;
        }
        #endregion
    }
}
