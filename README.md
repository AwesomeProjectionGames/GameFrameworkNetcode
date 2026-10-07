# Game Framework Netcode
*Implementation of [Game Framework](https://github.com/AwesomeProjectionGames/GameFramework) core networked features for Unity Netcode (NGO).*

This package provides the necessary base classes to implement a networked game using the Game Framework architecture and Unity Netcode for GameObjects. It handles the synchronization of fundamental properties like identity (UUID), ownership, and game states.

## Core Implementations

This package bridges the gap between `GameFramework` interfaces and Unity's `NetworkBehaviour`.

### Actors & Possession
- **`NetworkedActor`**: Base implementation of `IActor` for networked objects. Automatically synchronizes:
  - `UUID`: Unique identifier across the network.
  - `Owner`: The controller currently possessing this actor.
  - Inherits `Serialize()` / `Deserialize()` and `ISerializedObject` from `AbstractActor`.
- **`NetworkedPawn`**: Implementation of `IPawn`. Automatically handles transform persistence via internal `TransformPersistence`.
  - Spawning and Respawning via the GameMode.
  - Teleportation.
- **`NetworkedController`**: Base class for player or AI controllers in a networked environment.

### Components & State (Composition-First)
- **`NetworkedComponent<TState>`**: Composition-first base component for networked state.
  - Synchronizes state through a native NGO `NetworkVariable<TState>`.
  - Serializes state with MemoryPack (zero JSON, zero allocations).
  - Implements `ISerializedObject` for state persistence and snapshots.

### Game Flow
- **`NetworkedGameMode`**: Manages match rules and flow on the server, replicating necessary state to clients.
- **`NetworkedGameModeState`**: Replicated state of the current game mode (e.g., scores, match phase).

## Networking & Replication Conventions

> **Golden Rule**: Nothing is replicated by default. All state synchronization and remote calls must be explicit in naming or documentation.

### 1. Variables & Methods
- **Synchronized Variables**: Prefix with `Net` to indicate replicated state (`NetworkVariable`, also serialized for saves).
- **RPC Methods**: Make the network mechanism explicit in the name (e.g. `Request...Rpc`).
- **Polymorphic / Interface Methods (`[ReplicatedMethod]`)**: When a method name is enforced by an interface or override and cannot use the `...Rpc` suffix, annotate it with `[ReplicatedMethod]`.
- **Non-Owner Access (`AllowNonOwner` / `...NonOwn`)**:
  - By default, only the owning entity (`Owner`) and the server can invoke a replicated member (`AllowNonOwner = false`).

```csharp
// Example 1: Polymorphic method invoked only by the Owner
[ReplicatedMethod]
public override void Jump()
{
    if (!IsOwner) return;
    RequestJumpRpc();
}

// Example 2: Polymorphic method callable by any client
[ReplicatedMethod(AllowNonOwner = true)]
public override void Interact(IInteractor interactor)
{
    sound?.Play();
}
```

### 2. Execution Context & Naming

| Attribute / Prefix / Suffix | Scope & Role | Example |
| :--- | :--- | :--- |
| **`Server_`** | Server-only business logic | `Server_ValidateOrder()` |
| **`Client_`** | Local logic, prediction, inputs | `Client_PlayPickupSound()` |
| **`...Rpc`** | NGO Remote Procedure Call | `RequestPickupRpc()`, `BroadcastPickupRpc()` |
| **`Net` or `[ReplicatedMethod]`** | Replicated network property or method | `NetOwnerPlayer` |
| **`...NonOwn`** | RPC callable or modifiable without being the owner | `SetTargetNonOwnRpc()` |

### 3. Authoritative Flow (Intent $\rightarrow$ Validation $\rightarrow$ Broadcast)

| Step | Naming | Target (`SendTo`) | Role & Example |
| :--- | :--- | :--- | :--- |
| **Intent (Client)** | `Request[Action]Rpc` | `Server` | The client states what it wants to do. |
| **Validation (Success)** | `Confirm[Action]Rpc` | `Owner` | Server approves and notifies the requester. |
| **Broadcast** | `Broadcast[Action]Rpc` | `Everyone` | Ephemeral events (mainly VFX/SFX; can include temporary logic if it does not impact late joiners). |

## State Serialization & Persistence (MemoryPack)

To unify **network replication (deltas/ticks)** and **game persistence (savegames/snapshots)** without runtime reflection overhead, we adopt [MemoryPack](https://github.com/Cysharp/MemoryPack).

### Why MemoryPack?
* **Zero Allocations & Zero Copy**: Operates directly on `Span<byte>`, significantly faster than JSON or MessagePack.
* **Source Generators**: No runtime reflection, zero manual `serializer.SerializeValue()` boilerplate.
* **Strict Blittable / Unmanaged Models**: All state models are `unmanaged` structs.

### Model Definition & Versioning

Tag state models with explicit ordering and define them as `partial record struct`:

```csharp
[MemoryPackable(SerializeLayout.Explicit)]
public partial record struct PawnBaseState
{
    [MemoryPackOrder(0)] public Vector3 Position;
    [MemoryPackOrder(1)] public Quaternion Rotation;
    
    // Added in v2: older saves without this field safely fallback to default
    [MemoryPackOrder(2)] public int Health = 100;
}
```

#### Golden Rules for Safe Schema Evolution
1. **Append-Only IDs**: Always allocate a new incremented index (`[MemoryPackOrder(N)]`) when adding fields. Never reuse an existing ID.
2. **Never change data types**: If `Position` is `Vector3`, don't change it to `Vector2`. Introduce a new index instead.
3. **Deprecation without schema break**:
   - To remove a field, mark it `[MemoryPackIgnore]` or keep it as an obsolete dummy field to avoid index shifts.
4. **Default Initializers**: Always provide default values for newly added fields so that older save files deserialize gracefully.
5. **Strict `unmanaged`**: Compose complex states from simple nested unmanaged structs.

#### Multi-Component Disambiguation & Limitation
When an actor has multiple components of the same type, `Serialize()` uses a 32-bit key combining a 28-bit TypeHash with a 4-bit InstanceIndex (`0..15`).
- **Ordering**: Disambiguation relies on the deterministic discovery order in `ComponentsContainer` (Unity DFS hierarchy traversal).
- **Limitation**: Avoid reordering child GameObjects holding identical component types on an existing prefab across versions, as their index mapping could change between saves.

## Dependencies
- [Game Framework](https://github.com/AwesomeProjectionGames/GameFramework)
- [Unity Game Framework Base](https://github.com/AwesomeProjectionGames/UnityGameFrameworkImplementations)
- Unity Netcode for GameObjects
- MemoryPack
