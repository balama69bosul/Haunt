# Haunt online multiplayer setup

This branch adds the first Relay/Netcode connection layer for the Unity 2021.3.10f1 project.

## What's included

- Unity Netcode for GameObjects and Unity Transport dependencies.
- Unity Authentication and Relay dependencies.
- A persistent `OnlineSessionManager` that initializes Unity Services anonymously.
- Host room creation with a Relay join code.
- Client joining by Relay join code.
- A small temporary on-screen panel for creating/joining/leaving a room.

## Required Unity Dashboard setup

Relay requires a Unity Cloud project. The repository currently has no linked Unity Cloud project ID, so each developer must link the local Unity project to a project they can access.

1. Open the project with Unity **2021.3.10f1**.
2. Open **Edit > Project Settings > Services** (or the Services panel, depending on the editor layout).
3. Sign in to a Unity account and link/create a Unity Cloud project.
4. In the Unity Dashboard for that project, enable **Authentication** and **Relay**.
5. In Authentication settings, enable **Anonymous** sign-in for development.
6. Return to Unity and wait for Package Manager to resolve the new packages.
7. Open the **Lobby** scene and press Play. The temporary panel appears in the top-left.

## Two-computer smoke test

1. Start one copy of the game and click **Create room (Investigator)**.
2. Copy the displayed room code and send it to the other player.
3. On the second computer, start the same build and enter the code under **Join a friend's room**.
4. The first build is the host and the second build is the client.

## Current milestone limitations

This is the connection foundation only. It does **not yet** spawn or synchronize the game's Hero/Ghost prefabs, movement, ghost abilities, object interactions, evidence, sanity, or hunts. The labels in the temporary panel describe the intended player roles; gameplay role assignment is the next implementation milestone.

The connection panel uses Unity's immediate-mode GUI as a temporary test UI. It will be replaced/integrated into the game's actual lobby UI after the network player and role flow is in place.

## Important

Do not publish Unity service secrets or access tokens in the repository. The Relay join code is temporary and should only be shared with the intended player.
