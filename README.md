# Game Framework Netcode
*Implementation of [Game Framework](https://github.com/AwesomeProjectionGames/GameFramework) core networked features for Unity Netcode (NGO).*

This package provides the necessary base classes to implement a networked game using the Game Framework architecture and Unity Netcode for GameObjects. It handles the synchronization of fundamental properties like identity (UUID), ownership, and game states.

## Core Implementations

This package bridges the gap between `GameFramework` interfaces and Unity's `NetworkBehaviour`.

### Actors & Possession
- **`NetworkedActor`**: Base implementation of `IActor` for networked objects. Automatically synchronizes:
  - `UUID`: Unique identifier across the network.
  - `Owner`: The controller currently possessing this actor.
- **`NetworkedPawn`**: Implementation of `IPawn`. detailed handling for:
  - Spawning and Respawning via the GameMode.
  - Teleportation.
- **`NetworkedController`**: Base class for player or AI controllers in a networked environment.

### Game Flow
- **`NetworkedGameMode`**: Manages the match rules and flow on the server, replicating necessary state to clients.
- **`NetworkedGameModeState`**: Replicated state of the current game mode (e.g., scores, match phase).

### Serialization
- **`SerializedNetworkedActor`**: Helper class to manage networked serialization of actor content using `INetworkedSerializedObject`.

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

## Dependencies
- [Game Framework](https://github.com/AwesomeProjectionGames/GameFramework)
- [Unity Game Framework Base](https://github.com/AwesomeProjectionGames/UnityGameFrameworkImplementations)
- Unity Netcode for GameObjects

## Installation
To install this package, you can use the Unity Package Manager.

### git URL
Open the Unity Package Manager, click the `+` button, select `Add package from git URL...`, and enter:

```
https://github.com/AwesomeProjectionGames/GameFrameworkNetcode.git
```

### manifest.json
Or add this line to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.awesomeprojection.gameframework.netcode": "https://github.com/AwesomeProjectionGames/GameFrameworkNetcode.git"
  }
}
```
