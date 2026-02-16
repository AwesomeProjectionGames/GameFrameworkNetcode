#nullable enable

using AwesomeProjectionCoreUtils.Extensions;
using GameFramework;
using GameFramework.Saving;
using GameFramework.SpawnPoint;
using Unity.Netcode;
using UnityEngine;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    [RequireComponent(typeof(NetworkedGameModeState))]
    public class NetworkedGameMode : NetBehaviour, IGameMode
    {
        public IPawn DefaultPawnPrefab => defaultPawnPrefab.GetComponent<IPawn>();
        public IController DefaultControllerPrefab => defaultControllerPrefab.GetComponent<IController>();
        public IGameState CurrentGameState => _networkedGameModeState;

        [SerializeField] private GameObject? defaultPawnPrefab;
        [SerializeField] GameObject defaultControllerPrefab = null!;
        [SerializeField] private BaseSpawnPoint spawnPoints;

        private NetworkedGameModeState _networkedGameModeState;

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

        protected virtual void Awake()
        {
            _networkedGameModeState = GetComponent<NetworkedGameModeState>();
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
                    serializable.SendStateToClient(clientId);
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
        public IActor? Spawn(IActor prefab, bool destroyWithScene = true)
        {
            GameObject? spawned = SpawnPawn(prefab.Transform.gameObject, destroyWithScene);
            if (spawned == null) return null;
            return spawned.GetComponent<IActor>();
        }

        public IActor? SpawnAtLocation(IActor prefab, Vector3 location, Quaternion rotation, bool destroyWithScene = true)
        {
            IActor? actor = Spawn(prefab, destroyWithScene);
            if (actor == null) return null;
            if (actor is IPawn pawn)
            {
                pawn.Teleport(location, rotation);
            }
            else
            {
                actor.Transform.position = location;
                actor.Transform.rotation = rotation;
            }
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

            GameObject obj = Instantiate(prefab);
            networkObject = obj.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogWarning($"Spawned {prefab.name} but no NetworkObject was found.");
            }

            return obj;
        }
        #endregion
    }
}