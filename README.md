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
- Five selectable vehicles:
  - Hammer: Strike class heavy combat car.
  - Needle: Drift class technical handling car.
  - Surge: Volt class boost-focused car.
  - Vector SS-B "Razor": Bike class precision/near-miss vehicle.
  - Vector SS-P "Hauler": Pickup class utility combat truck.
- Existing manual driving remains: throttle, brake, steering, clutch, manual gears, handbrake/rear brake slide, boost, and reset.
- Existing combat remains: side slam, boost ram, and spin guard.
- Hidden Flow remains visual-first: speed lines, boost trail changes, skid ink, impact bursts, camera FOV, and outline pulse.
- Metal, Plastic, and Rubber resource inventory.
- Race rewards with completion, style/combat, and map bonus payouts.
- Lightweight one-lap checkpoint objective with visible checkpoint gates and automatic finish through the start gate.
- Simple route-following AI rival that drives the selected map while remaining a combat target.
- Garage tuning sliders, upgrade purchases, module installs, and module HUD layouts saved through PlayerPrefs.
- Cockpit module system with sensor, active-control, combat, and utility modules.
- VECTR visual pass: centralized style tokens, stronger rally/drift vehicle silhouettes, map-specific color identities, mechanical module HUD widgets, richer ink/spark/smoke VFX, and a practical style guide in `Assets/VECTR/Docs/VECTR_StyleGuide.md`.
- Procedural low-poly mesh kit for track ribbons, chamfered boxes, wedges, prisms, rounded rails, and bike/car visuals.
- Additive UGUI menu prototype in `Assets/Scripts/UI/VectorSSMenuUI.cs` for later replacement of the current immediate-mode menu.

## Controls

- `W`: throttle
- `S`: brake
- `A` / `D`: steer
- Arrow keys: rotate / tilt chase camera
- `Space`: handbrake / Razor rear brake slide
- `Left Shift`: clutch
- `E`: upshift
- `Q`: downshift
- Hold `Q`: quick-shift to Reverse
- `F`: boost
- `Z`: side slam left / Razor side check left
- `C`: side slam right / Razor side check right
- `N`: spin guard / parry
- `R`: reset car
- `Tab`: legacy HUD garage/debug panel during race
- Backquote `` ` `` or `\`: QUAC terminal
- `F1`: controls overlay
- `F3`: debug Flow panel

Module controls only work when the matching module is installed:

- `X`: Clutch Kick Assist
- `V`: Boost Valve Lever Low / Medium / High
- `[` / `]`: Brake Bias Dial rearward / forward
- `G`: Differential Lock Switch
- `B`: Armor Plate Deploy
- `Left Alt`: Razor Snap Lean Module

## Maps

- Blackline Circuit pays balanced resources and has the clearest straight, ramp, wide combat zone, and flowing curves.
- Scrapline Yard pays extra Metal and adds wide lanes, industrial dressing, movable crate props, and heavy combat spacing.
- Rubber Ridge pays extra Rubber and emphasizes hairpins, drifting, jumps, tire barriers, and a narrow bike-friendly shortcut.

## Vehicles

- Hammer: higher mass, stronger side slam/ram, better impact resistance, slower agility.
- Needle: lighter, faster steering, better drift/clutch-kick behavior, weaker contact.
- Surge: stronger boost and acceleration under boost, higher heat/risk profile.
- Razor: narrow bike-class vehicle with lower mass, bike-like lean visuals, fast steering, weak impact resistance, lower slam strength, stronger near-miss Flow gain, and access to tight routes.
- Hauler: pickup-class truck with higher mass, strong contact stability, bed armor visuals, slower steering, and reliable exits.

The current VECTR art pass gives Hammer a signal-red armored rally bruiser read, Needle a bone-white drift coupe silhouette, Surge an electric-cyan boost coupe identity, Razor an oil-gray/acid blade bike profile, and Hauler a safety-orange utility pickup profile.

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

Purchasable cockpit modules include:

- Heat Gauge: adds a heat widget.
- Clutch Kick Assist: adds a dedicated clutch kick input and cooldown widget.
- Boost Valve Lever: adds Low / Medium / High boost pressure control and widget.
- Brake Bias Dial: adds live front/rear brake balance control and widget.
- Differential Lock Switch: adds a traction/ram stability toggle and widget.
- Armor Plate Deploy: adds temporary impact defense and cooldown widget.
- Snap Lean Module: Razor-only evasive lean module.
- Rear Brake Slide Controller: Razor-only slide readout and rear slide improvement.

Each vehicle has limited module slots. The garage Modules section lets you buy, install, uninstall, and reposition installed module widgets with X/Y/scale sliders. Heat remains internal unless the Heat Gauge is installed.

Resources, purchased upgrades/modules, installed modules, selected map, selected vehicle, tuning values, and module widget layouts persist with PlayerPrefs under the Vector SS 0.1.0 save prefix.

## How To Run

1. Install Unity 2022.3 LTS or newer.
2. Open this folder as a Unity project.
3. Open `Assets/Scenes/GTXVerticalSlice.unity`.
4. Press Play.
5. Use the Vector SS menu to select a map and vehicle, then start a race.

The scene is intentionally mostly empty. Runtime scripts build the menus, track, selected vehicle, camera, HUD, AI rival, materials, combat systems, rewards, and effects.

## Project Structure

- `Assets/Scripts/Core/` - runtime bootstrap, race/session flow, driving-to-Flow bridge, tuning applier, architecture notes.
- `Assets/Scripts/Progression/` - Vector SS vehicles, maps, resources, upgrades, rewards, and save/load helpers.
- `Assets/Scripts/Vehicle/` - vehicle controller, engine, gearbox, clutch/drift, boost, input state, simple route rival AI.
- `Assets/Scripts/Combat/` - side slam, boost ram, combat target interface, dummy rival target.
- `Assets/Scripts/Flow/` - hidden Flow value, tiers, award helper.
- `Assets/Scripts/Visuals/` - runtime effects, bike lean visual, Flow visual controller, camera rig, low-poly mesh helpers.
- `Assets/Scripts/UI/` - race HUD, legacy tuning HUD, and additive Vector SS UGUI menu prototype.
- `Assets/Scripts/Data/` - legacy tuning, telemetry, and class data.
- `Assets/Scripts/Terminal/` - local QUAC command overlay.
- `Assets/Shaders/` - toon and inverted hull outline shaders.
- `Assets/Art/` - low-art pipeline notes.
- `Assets/VECTR/Docs/` - VECTR visual bible and style guide.
- `Docs/` - customization, lobby, Loom, and QUAC direction notes.

## Known Issues

- Many internal identifiers still say GTX. The player-facing direction is Vector SS.
- The current primary menu is immediate-mode GUI for speed of integration; the additive `VectorSSMenuUI` script is ready for a later UGUI swap.
- Module HUD layout is slider-based rather than drag-and-drop; it proves persistence and customization but needs a richer editor.
- Visual assets are still mostly runtime-generated geometry and UI; authored hand-drawn PNG icons/decals/posters are still needed.
- Race completion now uses a lightweight checkpoint/lap state machine, but it still uses nearest-route projection rather than physical trigger volumes.
- The AI rival is intentionally simple route-following physics, not a full opponent brain.
- Razor uses four hidden WheelColliders for stability while rendering as a two-wheel bike; it is bike-like, not a true motorcycle sim.
- Unity still shows a disabled built-in AudioListener package warning locally; it is pre-existing and not a gameplay blocker.
- Audio hooks are still placeholders; no final engine/boost/impact audio mix is wired.
- Multiplayer, Loom execution, package loading, and full QUAC CLI support remain future-facing contracts.

## 0.2.0 Next Steps

- Replace the immediate-mode menus with the UGUI `VectorSSMenuUI` flow.
- Replace nearest-route checkpoint logic with physical trigger volumes and timing splits.
- Add richer AI rival behavior: attacks, recovery, rubber-banding options, and difficulty presets.
- Add wheel visual spin/suspension sync.
- Expand Razor-specific moves: Boost Pierce, deeper Snap Lean scoring, and stronger near-miss effects.
- Add original hand-drawn PNG icons, decals, map cards, upgrade icons, and comic burst sprites.
- Migrate remaining GTX identifiers to Vector SS where practical.
- Build deterministic mod/lobby schemas before starting real multiplayer.
