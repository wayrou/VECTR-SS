# Vector SS 0.1.0 Unity Vertical Slice

Vector SS is the current title for the project previously referred to as GTX. Some internal namespaces, filenames, shaders, and scene names still say `GTX`; they are legacy identifiers and can be renamed gradually.

Vector SS 0.1.0 is a playable low-art, cel-shaded 3D action racing vertical slice with this lightweight loop:

`Main Menu -> Map Select -> Vehicle Select -> Race -> Results -> Garage -> Race Again`

## What Was Built

- Unity/C# runtime bootstrap in `Assets/Scenes/GTXVerticalSlice.unity`.
- Runtime menu flow for main menu, map select, vehicle select, garage, race HUD, and results.
- Three playable map variants:
  - Blackline Circuit: intro elevated highway/city circuit.
  - Scrapline Yard: wide industrial combat course with movable crates.
  - Rubber Ridge: canyon/hairpin drift course with a narrow Razor shortcut.
- Four selectable vehicles:
  - Hammer: Strike class heavy combat car.
  - Needle: Drift class technical handling car.
  - Surge: Volt class boost-focused car.
  - Vector SS-B "Razor": Bike class precision/near-miss vehicle.
- Existing manual driving remains: throttle, brake, steering, clutch, manual gears, handbrake/rear brake slide, boost, and reset.
- Existing combat remains: side slam, boost ram, and spin guard.
- Hidden Flow remains visual-first: speed lines, boost trail changes, skid ink, impact bursts, camera FOV, and outline pulse.
- Metal, Plastic, and Rubber resource inventory.
- Race rewards with completion, style/combat, and map bonus payouts.
- Garage tuning sliders and upgrade purchases saved through PlayerPrefs.
- Procedural low-poly mesh kit for track ribbons, chamfered boxes, wedges, prisms, rounded rails, and bike/car visuals.
- Additive UGUI menu prototype in `Assets/Scripts/UI/VectorSSMenuUI.cs` for later replacement of the current immediate-mode menu.

## Controls

- `W` / Up: throttle
- `S` / Down: brake
- `A` / `D`: steer
- `Space`: handbrake / Razor rear brake slide
- `Left Shift`: clutch
- `E`: upshift
- `Q`: downshift
- `F`: boost
- `Z`: side slam left / Razor side check left
- `C`: side slam right / Razor side check right
- `X`: spin guard / parry
- `R`: reset car
- `Tab`: legacy HUD garage/debug panel during race
- Backquote `` ` `` or `\`: QUAC terminal
- `F1`: controls overlay
- `F3`: debug Flow panel

## Maps

- Blackline Circuit pays balanced resources and has the clearest straight, ramp, wide combat zone, and flowing curves.
- Scrapline Yard pays extra Metal and adds wide lanes, industrial dressing, movable crate props, and heavy combat spacing.
- Rubber Ridge pays extra Rubber and emphasizes hairpins, drifting, jumps, tire barriers, and a narrow bike-friendly shortcut.

## Vehicles

- Hammer: higher mass, stronger side slam/ram, better impact resistance, slower agility.
- Needle: lighter, faster steering, better drift/clutch-kick behavior, weaker contact.
- Surge: stronger boost and acceleration under boost, higher heat/risk profile.
- Razor: narrow bike-class vehicle with lower mass, bike-like lean visuals, fast steering, weak impact resistance, lower slam strength, stronger near-miss Flow gain, and access to tight routes.

## Resources And Garage

Vector SS 0.1.0 has three resources:

- Metal: engine, chassis, armor, gearbox, ram strength.
- Plastic: aero, boost, cooling, visual/lightweight panels.
- Rubber: tires, grip, clutch, suspension, drift control.

Free tuning sliders include steering, brake bias, drift grip, final drive, boost valve, suspension, tire grip, clutch bite, outline thickness, camera shake, and Razor bike tuning such as lean response and rear brake slide.

Purchasable upgrades include:

- Engine Torque I
- Grip Tires I
- Combat Plating I
- Clutch Response I
- Boost Valve I
- Lightweight Aero I
- Lightweight Frame I
- Razor Tires I
- Lean Stabilizer I
- Boost Tuck I

Resources, purchased upgrades, selected map, selected vehicle, and tuning values persist with PlayerPrefs under the Vector SS 0.1.0 save prefix.

## How To Run

1. Install Unity 2022.3 LTS or newer.
2. Open this folder as a Unity project.
3. Open `Assets/Scenes/GTXVerticalSlice.unity`.
4. Press Play.
5. Use the Vector SS menu to select a map and vehicle, then start a race.

The scene is intentionally mostly empty. Runtime scripts build the menus, track, selected vehicle, camera, HUD, dummy rival, materials, combat systems, rewards, and effects.

## Project Structure

- `Assets/Scripts/Core/` - runtime bootstrap, race/session flow, driving-to-Flow bridge, tuning applier, architecture notes.
- `Assets/Scripts/Progression/` - Vector SS vehicles, maps, resources, upgrades, rewards, and save/load helpers.
- `Assets/Scripts/Vehicle/` - vehicle controller, engine, gearbox, clutch/drift, boost, input state.
- `Assets/Scripts/Combat/` - side slam, boost ram, combat target interface, dummy rival target.
- `Assets/Scripts/Flow/` - hidden Flow value, tiers, award helper.
- `Assets/Scripts/Visuals/` - runtime effects, bike lean visual, Flow visual controller, camera rig, low-poly mesh helpers.
- `Assets/Scripts/UI/` - race HUD, legacy tuning HUD, and additive Vector SS UGUI menu prototype.
- `Assets/Scripts/Data/` - legacy tuning, telemetry, and class data.
- `Assets/Scripts/Terminal/` - local QUAC command overlay.
- `Assets/Shaders/` - toon and inverted hull outline shaders.
- `Assets/Art/` - low-art pipeline notes.
- `Docs/` - customization, lobby, Loom, and QUAC direction notes.

## Known Issues

- Many internal identifiers still say GTX. The player-facing direction is Vector SS.
- The current primary menu is immediate-mode GUI for speed of integration; the additive `VectorSSMenuUI` script is ready for a later UGUI swap.
- Race completion uses a lightweight route-progress check plus a `Finish Test Race` button, not a full checkpoint/lap system.
- The rival is still a dummy target, not a racing AI.
- Razor uses four hidden WheelColliders for stability while rendering as a two-wheel bike; it is bike-like, not a true motorcycle sim.
- Unity still shows a disabled built-in AudioListener package warning locally; it is pre-existing and not a gameplay blocker.
- Audio hooks are still placeholders; no final engine/boost/impact audio mix is wired.
- Multiplayer, Loom execution, package loading, and full QUAC CLI support remain future-facing contracts.

## 0.2.0 Next Steps

- Replace the immediate-mode menus with the UGUI `VectorSSMenuUI` flow.
- Add checkpoint/lap timing and clearer completion goals.
- Add a simple AI rival that can drive all three maps.
- Add wheel visual spin/suspension sync.
- Expand Razor-specific moves: Boost Pierce, Snap Lean, and stronger near-miss effects.
- Add original hand-drawn PNG icons, decals, map cards, upgrade icons, and comic burst sprites.
- Migrate remaining GTX identifiers to Vector SS where practical.
- Build deterministic mod/lobby schemas before starting real multiplayer.
