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
