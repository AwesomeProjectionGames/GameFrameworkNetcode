using System;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Events;
using GameFramework.Identification;
using Unity.Netcode;

namespace UnityGameFrameworkImplementations.Core.Netcode
{
    public class NetworkedGameModeState : NetBehaviour, IGameState
    {
        public IEnumerable<IController> Controllers => _controllers;
        public IEnumerable<IActor> Actors => _actors;
        public Dictionary<string, IActor> ActorsById { get; } = new();

        public event Action OnControllersChanged;
        public event Action OnActorsChanged;

        private int _oldSpawnedObjectsCount = 0;
        private HashSet<IActor> _actors = new HashSet<IActor>();
        private HashSet<IController> _controllers = new HashSet<IController>();

        private void Update()
        {
            if (NetworkManager.Singleton?.SpawnManager == null)
            {
                if(_actors.Count > 0)
                {
                    _actors.Clear();
                    ActorsById.Clear();
                    OnActorsChanged?.Invoke();
                    GameInstance.Instance!.EventBus.Publish(new OnActorsListChanges());
                }
                if (_controllers.Count > 0)
                {
                    _controllers.Clear();
                    OnControllersChanged?.Invoke();
                    GameInstance.Instance!.EventBus.Publish(new OnControllersListChanges());
                }
                return;
            }
            HashSet<NetworkObject> set = NetworkManager.Singleton.SpawnManager.SpawnedObjectsList;
            if (set.Count != _oldSpawnedObjectsCount)
            {
                _oldSpawnedObjectsCount = set.Count;
                HashSet<IActor> newActors = new HashSet<IActor>();
                ActorsById.Clear();
                foreach (var networkObject in set)
                {
                    if (networkObject.TryGetComponent<IActor>(out var actor))
                    {
                        newActors.Add(actor);
                        ActorsById[actor.UUID] = actor;
                    }
                }
                if (!newActors.SetEquals(_actors))
                {
                    _actors = newActors;
                    OnActorsChanged?.Invoke();
                    GameInstance.Instance!.EventBus.Publish(new OnActorsListChanges());
                    
                    // Update Controllers and Pawns based on the new actors
                    HashSet<IController> newControllers = new HashSet<IController>();
                    foreach (var actor in _actors)
                    {
                        if (actor is IController controller)
                        {
                            newControllers.Add(controller);
                        }
                    }
                    if (!newControllers.SetEquals(_controllers))
                    {
                        _controllers = newControllers;
                        OnControllersChanged?.Invoke();
                        GameInstance.Instance!.EventBus.Publish(new OnControllersListChanges());
                    }
                }
            }
        }

        public T GetByID<T>(string id) where T : IHaveUUID
        {
            if (typeof(T) == typeof(IActor))
            {
                if (ActorsById.TryGetValue(id, out var actor))
                {
                    return (T)actor;
                }
            }
            return default!;
        }
    }
}