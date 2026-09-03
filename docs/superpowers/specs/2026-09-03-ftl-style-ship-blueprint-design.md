# FTL-style ship blueprint combat screen — design

Date: 2026-09-03. Approved approach: A (data-defined deck plans).

## Goal

Replace the text-button grids of the combat screen with top-down ship blueprints: rooms laid out spatially on a hull silhouette, crew as tokens inside rooms, hazards drawn on rooms, and the enemy ship drawn the same way on the right. All drawn procedurally with Unity UI images; no external art. Interaction rules and command bindings are unchanged.

## Layout (1920x1080 reference)

- Status bar (top, unchanged) and battle strip (pause state, altitude/weather or alert, timer).
- Left column (x 20, w 200): crew portraits. Each shows name, role, HP bar; click selects (`Crew_<id>` names preserved).
- Player blueprint panel (x 232, w 700): ward/armor/hull bars, power and instability, threat forecast, then the blueprint.
- Command panel (x 944, w 300): unchanged contents.
- Enemy panel (x 1256, w 644): localized ship name, defense summary, blueprint. Room click sets the fire target (`EnemySystem_<type>` preserved).
- Squadron bar (bottom, unchanged contents).

## Deck plans (`AetherArk.Core.DeckPlan`, data in `ContentCatalog.GetDeckPlan(shipId)`)

A plan has `columns`, `rows`, and tiles `(system, column, row, width, height)`. Bow faces right (higher column). Row 0 is the top row.

| Ship | Grid | Distinctive shape |
|---|---|---|
| ship_vanguard | 6x3 | 2x2 flight deck amidships, weapons 1x2 forward, bridge 1x2 at the bow |
| enemy_cutter | 5x2 | small and flat, single-cell rooms |
| enemy_cruiser | 6x3 | 2x2 weapons block forward, ward 2x1 dorsal |
| enemy_carrier | 7x3 | 3x2 flight deck dominating the middle |

Invariants (tested): every system on the ship has exactly one tile; tiles never overlap; tiles stay inside the grid.

## Blueprint renderer (`AetherArk.Runtime.ShipBlueprintView`)

Static `Draw(...)` that takes the UI factory, localization, parent rect, ship, plan, area rect and options (object-name prefix, selected system, room click handler, optional crew list with selected crew id and crew click handler, reduced motion, high contrast, show allocated power vs effective power).

- Hull: dark hull plate around the grid with a diamond-shaped bow and a smaller stern fin, brass outline.
- Rooms: one tile per system. Fill colour encodes state: operational teal, unpowered grey, damaged orange tint, disabled dark red. Label = localized short name and power pips (filled/empty dots), integrity bar along the bottom edge.
- Hazards: fire = orange translucent layer plus "🔥"-free text glyph `▲`; breach = teal border; oxygen below 30 darkens the tile. Reduced motion disables any pulsing; high contrast raises fill alpha and border thickness.
- Crew tokens: circle in lineage colour with initial, captain gets ★. Selected crew has a bright ring. Token buttons are named `CrewToken_<id>` and share the portrait's click handler. Tokens are laid out left-to-right along the tile's lower half.
- Room buttons keep the `Room_<type>` / `EnemySystem_<type>` names.

## Debug entry point

`GameController` accepts `-debug-combat [cutter|carrier|cruiser]` in development builds only: creates a post-tutorial run, searches seeds until the requested enemy appears, begins combat paused. Used for screenshot verification without keyboard automation.

## Tests

- EditMode: `DeckPlan` invariants for all four ships; lineage colour and initial helpers are pure functions and get one test.
- PlayMode: existing button test unchanged; adds a check that a `CrewToken_` object exists for every active crew member in combat.

## Out of scope

Weapon slot charge gauges, crew walking animation, sprite art, route map restyle.
