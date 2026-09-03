# Route map restyle — design

Date: 2026-09-03. Follows the FTL-style combat screen; same visual language.

## Goal

Make the route map read like a star map: iconic circular nodes, a visible storm front, and a select-then-depart flow with a preview panel, instead of text buttons that travel on click.

## Layout (1920x1080)

- Status bar (unchanged). Title row with the route title and a short hint.
- Map panel: x 70, y 250, w 1300, h 590. 8 columns x 3 lanes.
- Preview panel: x 1390, y 250, w 460, h 590.
- Legend row under the map (y 200..240): one glyph + name per encounter type.
- Bottom row unchanged: field repair, refit, support call, latest report, emergency aether.

## Nodes

Circle (64 px) with an encounter glyph: Start ●, Battle ▲, EliteBattle ▲ with a second red ring, Rescue +, Salvage ◆, Trade $, Checkpoint ▼, Storm ◇, Gate ★ (larger, brass double ring).

Fill/ring by state: current = brass fill, white ring; available = teal fill, brass ring, number badge `[n]`; visited = grey; blocked = dark red under the storm band; unreachable = dark panel; selected = thick white ring. Name below the circle (13 px); under it cost and weather (11 px) for unvisited nodes.

Edges: 2 px lines; edges leaving the current node to available nodes are brass, blocked edges dim red, others muted teal.

## Storm front

Columns `<= stormColumn` are covered by a translucent red band with a jagged right edge made of rotated squares and a label. The column that will be swallowed after the next jump (`RouteRules.NextStormColumn`) gets a faint amber band and a "closes after next jump" label.

## Selection flow

`GameView.selectedRouteNodeId` (view state). Clicking a node selects it and fills the preview; clicking the selected available node again departs. The preview panel's Depart button and the Return key depart to the selected node. Number keys keep their immediate-travel behaviour. Selection is cleared when it no longer refers to a travelable node.

Preview contents: glyph + name, encounter type, aether cost vs available (red if unaffordable), weather with accuracy/ward modifiers, recommended altitude vs current, a threat note for battles, and the depart button.

## Core rules (`AetherArk.Core.RouteRules`, tested)

- `NextStormColumn(RunState)`: the storm column after one more jump = `max(-1, travelCount + 1 - 3)`.
- `Glyph(EncounterType)`: the glyph table above.
- `NameKey(EncounterType)`: `node.departure` for Start, otherwise `node.<type lowercase>`.

## Out of scope

Animated ship marker, fog of war, per-region backgrounds.
