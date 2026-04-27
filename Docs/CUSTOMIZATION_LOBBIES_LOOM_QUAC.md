# GTX Customization, Lobbies, Loom, and QUAC Direction

GTX should grow into a deeply customizable racing platform, not a closed one-off racer. The design target is "near open source" in spirit: readable data, documented schemas, moddable rules, inspectable systems, and player-run lobbies with clear customization contracts.

This does not mean the project is currently open source or that multiplayer is implemented. Licensing, contribution policy, distribution, moderation, and network security still need decisions.

## Core Principles

- Everything important should eventually be data-driven: cars, classes, parts, tuning, tracks, race rules, lobby presets, UI command surfaces, and Flow behavior.
- Local customization should work first. Multiplayer customization should build on the same data contracts plus validation and sync rules.
- The low-art pipeline stays compatible with customization: procedural polygons, simple mesh recipes, hand-drawn PNGs, and flat material palettes.
- Competitive multiplayer needs server/lobby authority for anything that affects outcomes. Cosmetic and private lobby chaos can be looser.
- The terminal is part of the fantasy. QUAC should be a real command surface for debug, admin, lobby, mod, and scripting workflows.

## Customization Surfaces

Planned customization categories:

- Vehicle hull recipes: low-poly blocks, wedges, prisms, fins, rams, wings, wheel geometry.
- Vehicle handling: engine, final drive, gear ratios, clutch bite, grip, steering, boost, cooling, armor, ram strength.
- Visual identity: palette, outline thickness, PNG decals, class emblems, boost trail style, tire ink style.
- Combat rules: side slam force, boost ram multipliers, parry windows, damage/stun, hit pause.
- Track and arena recipes: primitive segments, ramps, barriers, gates, hazards, checkpoints, spawn zones.
- Flow rules: awards, decay, visual tier thresholds, lobby-visible debug behavior.
- UI and cockpit panels: hand-drawn PNG icons, command button layouts, telemetry skins.
- Race/lobby rules: teams, laps, combat settings, allowed mods, boost rules, class restrictions, catch-up settings.

## Multiplayer Lobby Direction

GTX lobbies should eventually support:

- Host and join flows with callsigns and join codes.
- Lobby staging space before a race.
- Customizable lobby rule presets.
- Mod/package manifest negotiation before launch.
- Host-selected track, ruleset, allowed car classes, and mutator scripts.
- Ready checks, team assignment, spectator permissions, and rematch flow.
- Trust policies: local/private, friends-only, verified package, tournament-safe.

Important rule: lobby customization must be explicit. A player should know when they are joining a standard lobby versus a heavily customized one.

## Loom Direction

GTX should reuse the same Loom language family and authoring model from Loom Workbench:

- `loom(name)` declares a script document.
- `yarn` stores simple variables.
- `weave` reacts to conditions and runs actions.
- `button` exposes manual commands.
- Documents are `Unbound` until validated and bound to a live context.

GTX-specific Loom domains should include:

- `car(player)`, `car(all)`, `car(rival)`.
- `gate(start)`, `checkpoint(id)`, `hazard(id)`, `boost_pad(id)`.
- `lobby(rule)`, `team(id)`, `track(id)`, `flow(player)`.
- Actions like `open gate(start)`, `set boost to 0.8`, `enable combat`, `spawn hazard`, `lock class(strike)`, `start race`.

Example future GTX Loom sketch:

```loom
loom(boost_duel)

yarn launch_boost = 0.75

weave(start_gate)
if(lobby countdown is below 1)
do(
  set boost of car(all) to launch_boost
  open gate(start)
)
otherwise()

button(reset_combat_zone)
do(
  reset hazard(all)
  move car(all) to checkpoint(combat_gallery)
)
```

## QUAC Direction

QUAC is GTX's command surface, modeled after Chaos Core's Q.U.A.C. style.

Planned command groups:

- `/dev` and `/help`: command discovery.
- `/give boost`, `/heat clear`, `/flow set`: debug/test hooks.
- `/garage preset strike`, `/tune grip 1.2`: tuning and garage commands.
- `/lobby create`, `/lobby join`, `/lobby rules`, `/ready`: lobby commands.
- `/mod load`, `/mod list`, `/mod verify`: customization package commands.
- `/loom bind`, `/loom run`, `/loom unbind`: script lifecycle commands.

QUAC should exist both as an in-game terminal/palette and as a future external CLI where practical.

## Near-Open-Source Stance

GTX should be built as if curious players can read and extend it:

- Prefer plain JSON/YAML-like manifests for community-facing data.
- Keep schemas versioned.
- Document every supported extension point.
- Keep unsafe capabilities behind trust levels.
- Separate official content from community packages.
- Preserve deterministic validation paths for multiplayer.

## Implementation Order

1. Define manifest and schema contracts for customization packages.
2. Add QUAC in-game terminal overlay with local commands.
3. Add local package loading for cosmetic and tuning data.
4. Add Loom parser/runtime bridge or shared package strategy.
5. Add lobby data contracts and local mock lobby UI.
6. Add real network transport and package negotiation.
7. Add moderation/trust policies and tournament-safe validation.
