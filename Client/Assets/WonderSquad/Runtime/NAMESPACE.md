# Namespace plan

| Assembly | Root namespace | P0 responsibility |
|---|---|---|
| `WonderSquad.Core` | `WonderSquad.Core` | Stable infrastructure contracts, logging, project constants |
| `WonderSquad.Content` | `WonderSquad.Content` | Generic read-only content definition and structural validation |
| `WonderSquad.Bootstrap` | `WonderSquad.Bootstrap` | Composition root and Unity adapters |
| `WonderSquad.Player` | `WonderSquad.Player` | Reserved for P1 player domain |
| `WonderSquad.Interaction` | `WonderSquad.Interaction` | Reserved for P1 interaction domain |
| `WonderSquad.Inventory` | `WonderSquad.Inventory` | Reserved for P2 inventory domain |
| `WonderSquad.Item` | `WonderSquad.Item` | Reserved for P2+ item domain |
| `WonderSquad.Crafting` | `WonderSquad.Crafting` | Reserved for P3+ crafting domain |
| `WonderSquad.Puzzle` | `WonderSquad.Puzzle` | Reserved for P3 puzzle domain |
| `WonderSquad.Ability` | `WonderSquad.Ability` | Reserved for P4 ability domain |
| `WonderSquad.Status` | `WonderSquad.Status` | Reserved for P5 status and rescue domain |
| `WonderSquad.Communication` | `WonderSquad.Communication` | Reserved for P6 non-voice communication |
| `WonderSquad.Level` | `WonderSquad.Level` | Reserved for level orchestration and recovery |
| `WonderSquad.Network` | `WonderSquad.Network` | Network-agnostic boundary; Fusion adapter is deferred to P7 |
| `WonderSquad.UI` | `WonderSquad.UI` | Local presentation and ViewModels |
| `WonderSquad.Save` | `WonderSquad.Save` | Local settings boundary |
| `WonderSquad.Editor` | `WonderSquad.Editor` | Editor-only project validation |

Rules:

- Domain assemblies may reference `WonderSquad.Core` and read-only `WonderSquad.Content`.
- Domain assemblies do not reference each other's implementations.
- Unity SDK adapters live at the outside edge.
- No gameplay assembly references `WonderSquad.Editor`.
- Fusion and voice assemblies do not exist in P0.

