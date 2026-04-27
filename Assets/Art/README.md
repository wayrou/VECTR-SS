# Vector SS Low-Art Style Guide

Vector SS should stay asset-light: mostly procedural low-poly geometry, flat colors, thick black outlines, and hand-drawn PNG overlays. This project was previously called GTX, so some legacy filenames and shaders still use that name.

## Current Reference Direction

The working reference direction is original anime rally model-kit energy:

- 80s/90s hand-drawn car and motorcycle illustration attitude.
- Boxy compact rally silhouettes, garage clutter, spare parts, tire stacks, and tool-mat staging.
- Sun-baked desert/rally colors with off-white shells, orange safety blocks, deep print blue, charcoal ink, and cyan boost.
- Sticker-sheet / plastic model-kit graphics: stripes, number plates, circles, arrows, vents, and abstract class marks.
- Retro arcade motion: lateral speed streaks, comic impact panels, chunky boost prisms, and ink-like skid lines.

Avoid copying any exact Pinterest pin, car model, anime character, brand logo, sponsor decal, or motorsport livery. Treat the board as taste direction only.

## Preferred Asset Types

- Unity primitives and simple procedural meshes.
- Low-poly wedges, prisms, chamfered boxes, curved ribbon roads, rounded rails, panels, fins, and chunky tires.
- Hand-drawn PNG icons for UI, callouts, cockpit switches, class emblems, decals, speed glyphs, and impact words.
- PNG texture strips for tire ink, boost flames, slash marks, comic bursts, and garage stickers.
- Small original decal packs with simple symbols, fake class marks, and non-branded racing numbers.

## Avoid

- Realistic car models.
- Licensed cars, logos, tracks, or recognizable movie designs.
- High-poly mesh detail that requires a dedicated modeling pipeline.
- Photoreal materials, normal-map-heavy surfaces, or complex PBR asset packs.

## Vehicle Art Rule

Build cars from readable polygon chunks:

- Core hull: 3-6 chamfered or blocky pieces.
- Cockpit: wedge or prism silhouette.
- Wheels: 6-10 sided prisms.
- Armor identity: fins, blades, rams, intake blocks.
- Class identity: silhouette first, decal/icon second.

If a complex mesh is ever needed, generate it procedurally or author it as a very small custom mesh with obvious polygon faces.

## PNG Workflow

Hand-drawn PNGs should go in future folders like:

- `Assets/Art/Icons/`
- `Assets/Art/Decals/`
- `Assets/Art/Effects/`
- `Assets/Art/UI/`

Keep PNGs bold and readable at speed. White/black transparent line art is ideal because scripts can tint it per class or Flow tier later.
