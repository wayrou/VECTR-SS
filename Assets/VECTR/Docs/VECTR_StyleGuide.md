# VECTR Visual Style Guide

VECTR is the current visual direction for Vector SS: rally and drift machines rebuilt as modular combat hardware, inked with heavy comic blackline, then raced through neon-accented industrial courses at impossible speed.

The project is still Unity runtime-generated art. Some files, shaders, namespaces, and generated object names still say `GTX`; rename them gradually only when it does not risk gameplay.

## Core Formula

- 70% rally/drift motorsport.
- 20% comic/anime blackline stylization.
- 10% cyber-tech cockpit hardware.

Avoid generic cyberpunk, photoreal simulation, and plain Unity prototype geometry with a toon shader dropped on top.

## Visual Pillars

- Blackline cel shading: thick outer silhouettes, hard shadow bands, bold readable shapes, and graphic highlights.
- Rally/drift machine culture: wide fenders, mudflaps, hood vents, roof scoops, rally lights, tire lettering, scuffed bumpers, bolt-on aero, roll cages, and fake sponsor decals.
- Modular cockpit hardware: gauges, switches, dials, levers, warning lights, screws, brackets, small labels, and dashboard module widgets that appear only when installed.
- Graphic speed violence: ink skid marks, comic impact bursts, speed lines, boost trails, outline pulses, sparks, and smoke.
- Controlled neon: cyan, magenta, acid yellow-green, and orange are accents for boost, module LEDs, signs, warnings, and Flow effects. Asphalt, rubber, metal, concrete, dust, oil, and paint remain the base.

## Palette

Use `Assets/Scripts/Visuals/VectrStyleTokens.cs` as the runtime source of truth.

Core neutrals:

- Ink Black: outlines, tire marks, deep UI borders.
- Asphalt Navy: roads, dark panels, night shadows.
- Warm Concrete Gray: barriers, garage floors, industrial walls.
- Bone White: text, decals, lane marks, Needle base.
- Oil Gray: mechanical interiors, undercarriage, Razor base.
- Rubber Black: tires, skid marks, burned drift lines.

Accent colors:

- Signal Red: danger, Hammer, combat, impact language.
- Safety Orange: Scrapline Yard, construction, warnings, heat, Hauler base.
- Electric Cyan: boost, Surge, gauges, tech feedback.
- Hot Magenta: high Flow, neon ad boards, drift effects.
- Acid Yellow-Green: Razor, module alerts, shortcut markers.
- Deep Violet: Volt systems, night shadows, speedline overlays.

Scene rule: use one dark base, one bright accent, one readable neutral, and one warning color. Do not use every neon color at once.

## Linework

- Outer silhouette lines are thickest: vehicles, rivals, major props, garage machinery, barriers.
- Panel lines are medium: fenders, doors, hood seams, vents, aero, armor, module casings.
- Detail lines are sparse: bolts, tire tread, wires, labels, switch markings.
- Dynamic lines animate: boost, drift, Flow, impacts, jumps, near-misses, perfect shifts.

Do not overload tiny details. If it cannot be read at racing speed, enlarge it or remove it.

## Vehicles

All vehicles are low-art modular geometry. Use readable silhouettes first, color second, decals third. The car baseline is a sleek retro 80s/90s wedge: long hood, clear glasshouse, flat roof, rear deck, boxed fenders, visible wheels, grille, lights, and bold livery panels.

Hammer:

- Strike class bruiser.
- Boxy rally sedan energy, planted stance, wide fenders, mudflaps, hood scoop, ram plate, rally lamps.
- Signal Red body, Oil Gray hardware, Safety Orange warning accents.

Needle:

- Drift class technical coupe.
- Low roof, long-hood impression, splitter, drift wing, side slash graphics, visible tire lettering.
- Bone White body, Electric Cyan technical marks, Hot Magenta drift marks.

Surge:

- Volt class boost coupe.
- Rounded fast body, nose ducts, active aero fins, battery spine, cooling vents.
- Electric Cyan body, Deep Violet systems, Bone White charge ticks.

Razor:

- Bike class high-risk precision vehicle.
- Narrow sport-bike silhouette, blade fairings, exposed frame, slim boost trail, strong lean readability.
- Oil Gray / Ink base, Acid Yellow-Green blades, Electric Cyan module lights.

Hauler:

- Pickup class utility combat truck.
- Long-bed low-poly pickup shape, high beltline, open ink-black bed floor, roll bar, push bar, bed rails, cargo light.
- Safety Orange body, Bone White panels, Electric Cyan utility lights.

Never copy exact real cars, sponsor logos, movie designs, or real manufacturer shapes.

## Maps

Keep the racing line readable. Props frame decisions; they do not hide apexes.

Blackline Circuit:

- Elevated city/highway intro map.
- Asphalt Navy road, Warm Concrete/Bone barriers, Electric Cyan signage, Hot Magenta ad boards.
- Clean lines, readable route graphics, overhead light rigs.

Scrapline Yard:

- Industrial combat map.
- Rust Orange ground, Oil Gray metal, Safety Orange hazard teeth, black oil smears.
- Containers, crane silhouettes, heavy barriers, wide lanes.

Rubber Ridge:

- Mountain/canyon drift and jump map.
- Rubber Black road, Dust Tan ground, cool shadows, tire stacks, Acid shortcut/apex markers.
- Hairpins and cliff shapes should advertise drift rhythm without blocking sightlines.

## Garage

The garage should feel like a used tuner shop crossed with a prototype race lab:

- Concrete floor, tire stacks, oil marks, hanging tools, wall panels.
- Resource bins for Metal, Plastic, Rubber.
- Cables, hydraulic lift hints, monitors, neon shop accents, black-and-white floor markings.
- UI tabs: Build, Modules, Tuning, DashGrid.

The garage must clearly communicate selected vehicle, resources, upgrades, module slots, tuning, and HUD layout.

## HUD And Modules

Core HUD always shows speed, gear, RPM, boost/basic boost state, and callouts.

Module widgets are physical dashboard hardware:

- Black casing, thick ink border, recessed plate.
- Screws, brackets, LED state light, warning rail, mechanical segmented bar.
- Compact labels and clear color states.
- Installed module creates widget and control. Uninstalled module means no widget and no active control.

Widget motifs:

- Heat Gauge: orange/red warning zone.
- Clutch Kick Assist: rubber stomp button, readiness LED.
- Boost Valve Lever: LOW/MID/HIGH pressure state, cyan glow.
- Brake Bias Dial: FRONT/REAR readout and tick language.
- Differential Lock: OPEN/LOCKED chunky switch.
- Armor Plate Deploy: red guarded switch and cooldown bar.
- Snap Lean: acid yellow-green directional lean charge.

## Shader And Material Use

Current renderer: Built-in Render Pipeline.

Use:

- `GTX/ToonCel` for cel-shaded vehicle, track, garage, and prop materials.
- `GTX/InvertedHullOutline` plus scaled duplicate geometry for reliable black outlines.
- Unlit/particle materials for boost, smoke, skid ink, speed lines, and impact bursts.

Do not migrate to URP/HDRP just for this pass. Runtime procedural geometry is the practical material library until authored `.mat` assets are added.

## Folder And Naming Guidance

Add new assets under:

- `Assets/VECTR/Docs/`
- `Assets/VECTR/Art/Materials/`
- `Assets/VECTR/Art/VFX/`
- `Assets/VECTR/Art/Decals/`
- `Assets/VECTR/Art/UI/`
- `Assets/VECTR/Prefabs/`
- `Assets/VECTR/ScriptableObjects/`

Generated runtime objects should prefer `VECTR` names for new work. Legacy `GTX` names can remain until a safe rename pass.

## Do / Do Not

Do:

- Use bold silhouettes and thick outlines.
- Use fake decals and non-branded motorsport language.
- Keep effects strong but readable.
- Make every vehicle class obvious at a glance.
- Keep module UI tactile and mechanical.

Do not:

- Import licensed cars, logos, sponsors, tracks, characters, or exact liveries.
- Over-bloom neon until blacklines disappear.
- Add decorative colliders near the racing line unless the prop is intentionally physical.
- Make all cars share the same proportions.
- Chase photoreal materials or high-poly modeling.

## Future Asset Guidance

Hand-drawn PNGs are welcome for icons, decals, module labels, impact words, speed glyphs, and garage posters. Keep them bold, high-contrast, transparent-background, and tintable.

Before adding complex 3D assets, ask whether the shape can be built from chamfered boxes, wedges, prisms, or a tiny custom mesh.
