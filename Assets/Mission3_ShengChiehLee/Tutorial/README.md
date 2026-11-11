# Mission3 — Mandatory Tutorial

All new assets live under `Assets/Mission3_ShengChiehLee/Tutorial`.

## Entry and editing

- The room starts only in `Assets/Scenes/Mission3.unity`.
- Edit `Resources/Mission3/TutorialRoom.prefab` to change the room, UI, markers, desk or exit.
- `Mission3 > Build Mandatory Tutorial Room` recreates this owned Prefab and owned materials. It does not regenerate the hotel or the teammate tutorial.
- `Mission3 > Test Mandatory Tutorial` exercises the gate, actual input and physics, practice tools, exit and hotel state.
- `Mission3 > Play Mandatory Tutorial` starts a fresh playable preview. `Mission3 > Export My Tutorial Assets` exports only this owned folder; teammate dependencies are not included.

## Required actions

1. Mouse look.
2. Walk to the cyan marker with WASD.
3. Run to the second marker with Left Shift and movement.
4. Use Space to jump across the low barrier.
5. Aim at the training card and press E.
6. Open and close the floor map with M and the journal with TAB.

The exit remains locked until all actions are complete. ESC pauses training; Resume keeps progress. The room has no enemies or time limit. The practice card and journal do not change real hotel inventory, clues or score. Leaving the room restores reception and starts exploration. Training time is excluded from the hotel timer. Completion is kept only for the current play session, so scene retries do not repeat completed training; a new game launch requires training again.

## Ownership and shared resources

The hotel scene is not regenerated or edited. The existing ice/fire tutorial and shared Moving, MouseLook and HeadBob source assets are unchanged. A renamed movement copy is installed at runtime only for Mission3 and keeps the existing speed, jump and sound settings. A renamed look copy is used only inside tutorial. Materials are owned by this folder. The existing font and floor-map texture are referenced read-only.

The private movement copy uses the actual collider bounds for ground checks and retains the Moving base type so the existing Mission3 manager can pause and restore it normally.

No GitHub push, deployment or WebGL rebuild is performed by the editor tools.
