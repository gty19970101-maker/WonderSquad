# Sprint003B Unity Failure Audit

## Status

`RESOLVED — UNITY VERIFICATION PASSED`

This audit records the Unity `6000.3.21f1` failure report and the limited corrective change. It does not mark Sprint003B as passed.

## Observed failures

| Scope | Result | Root cause |
| --- | --- | --- |
| `ForestSignalTrialEditModeTests` | 2 failures | The fixture configured visual components while their inactive root prevented the normal runtime enable/subscription path. The state was correct; `ForestBeaconVisual.CurrentMode` remained `Unknown`. |
| `ForestSignalTrialPlayModeTests` | 4 failures | `ForestSignalTrial` treated a valid same-scene beacon as invalid during early scene activation because it also required `Scene.isLoaded`. Unity invokes `Awake`/`OnEnable` before the asynchronous scene load is reported complete. |
| `SleepingForestGreyboxPlayModeTests` | 3 failures | These tests shared the same scene-load error. They failed before their Player, Camera, or FallRecovery assertions were reached. |

## Targeted fixes

1. `ForestSignalTrial` now validates a valid same-Scene relationship without requiring `Scene.isLoaded`. This preserves all trial state, revision, snapshot, source-validation, and result semantics; it only makes initialization valid in Unity's documented early lifecycle window.
2. The authoring command now keeps the new `ForestSignalTrialGameplay` hierarchy inactive while every serialized scene reference is configured. It explicitly persists Definition, Trial, Beacon Interaction, and Beacon Visual references through `SerializedObject`, marks prefab overrides dirty, validates the complete composition, and only then enables the hierarchy.
3. The EditMode fixture now completes trial configuration before explicitly configuring each visual while active. The expected `Dormant` result is retained; the test no longer relies on EditMode lifecycle callbacks that are not part of its subject under test.
4. The formal-scene EditMode assertion now verifies the serialized Trial/Beacon/Visual references and slots, not only the runtime properties.

## Serialization and idempotence

- The Definition is saved before prefab and scene authoring.
- Each formal Beacon has a non-null, distinct `ForestBeaconInteraction`, an explicit stable `TargetId`, an explicit slot, and a reference to the same scene-local Trial.
- The Trial stores the Definition, Beacon A, and Beacon B as serialized references.
- `Apply Forest Signal Trial` remains explicit and idempotent: it replaces only the Sprint003B composition, verifies the result, marks the SleepingForest scene dirty, and saves it. It never runs automatically.

## Scope audit

No Character Foundation, Interaction Foundation, Core interaction contracts, Input Actions, Player, Camera, FallRecovery, or Scene gameplay geometry was changed. No Root Bridge, route, completion, inventory, ability, save, or network feature was introduced.

## Static verification

Using the Unity `6000.3.21f1` compiler response files, the following assemblies compiled with zero errors:

- `WonderSquad.Puzzle`
- `WonderSquad.Editor`
- `WonderSquad.Tests.EditMode`
- `WonderSquad.Tests.PlayMode`

This is not a Unity Test Runner result.

## Required Unity rerun

1. Allow the open Unity Editor to import and compile the changed scripts.
2. Run `Wonder Squad → Sprint003B → Apply Forest Signal Trial` once. This is required so the strengthened authoring flow re-saves the formal scene composition.
3. Run `ForestSignalTrialEditModeTests` and `ForestBeaconConfigurationEditModeTests`.
4. Run the full EditMode suite.
5. Run `ForestSignalTrialPlayModeTests` and `SleepingForestGreyboxPlayModeTests`.
6. Run the full PlayMode suite and confirm Console Error is zero.

## Unity rerun result — 2026-08-09

The requested Unity rerun completed successfully in Unity `6000.3.21f1`:

- Sprint003B EditMode: `18 Passed / 0 Failed / 0 Skipped`
- Sprint003B PlayMode: `4 Passed / 0 Failed / 0 Skipped`
- Full EditMode: `100 Passed / 0 Failed / 0 Skipped`
- Full PlayMode: `54 Passed / 0 Failed / 0 Skipped`
- Console Error: `0`

Manual verification also confirmed A → B completion, recoverable B → A → B IncorrectOrder, repeat/held-input protection, independent PropertyBlock and Marker feedback, plus Movement, GroundDetector, Camera Follow, FallRecovery, and unchanged route accessibility. The former failure is resolved and Sprint003B may proceed to Gate Review.
