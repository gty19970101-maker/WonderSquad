# Wonder Squad Unity Client

## Required editor

- Unity `6000.3.21f1` (Unity 6.3 LTS)
- Template direction: Universal 3D / URP
- Initial target: Windows desktop development build

## Open

1. Install Unity `6000.3.21f1` in Unity Hub.
2. In Unity Hub, choose **Add project from disk**.
3. Select this `Client` directory.
4. Allow Unity Package Manager to resolve the exact package versions in `Packages/manifest.json`.
5. If Unity asks to enable the new Input System backend, accept and restart the Editor.
6. Open `Assets/WonderSquad/Scenes/Bootstrap/Bootstrap.unity`.
7. Run **Wonder Squad > P0 > Validate Project Skeleton**.
8. Open Test Runner and run EditMode, PlayMode, and ContentValidation tests.

## P0 scope

This project contains infrastructure only:

- project and package configuration;
- assembly and namespace boundaries;
- logging abstraction and Unity log adapter;
- project constants and a project configuration entry;
- Bootstrap entry;
- input action structure;
- Bootstrap plus independent Player, Gameplay, and Recovery P0 sandbox scenes;
- basic tests.

Player movement, inventory, items, crafting, puzzles, abilities, status gameplay, level gameplay, networking, voice, and all other P1+ behavior are intentionally absent.

## First-import note

This repository was scaffolded on a machine without Unity Editor. Unity will generate `.meta` files for assets that do not yet have one, resolve `Packages/packages-lock.json`, and serialize any missing editor-owned project settings on first import. Commit those generated files after the validation menu and tests pass.
