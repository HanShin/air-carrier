# Aether Ark — vertical slice design bible

## Pillars

1. **A carrier is a living machine.** Power, rooms, crew, damage control, and the flight deck must compete for attention.
2. **Air wings are orders, not action units.** The player chooses mission, timing, and target; pilots execute the flight.
3. **The sky is a ruleset.** Altitude and weather change accuracy, ward recovery, hazards, and sortie time.
4. **Every victory carries civilians.** Convoy survivors, morale, and supplies turn tactical success into strategic responsibility.
5. **Danger must be legible.** Randomness varies outcomes but never hides a lethal requirement or defeat cause.

## Vertical-slice loop

The player creates a captain, chooses difficulty and one support ship, then commands the artillery-oriented carrier `Dawn Refuge`. A seeded seven-jump route offers patrols, refugees, salvage, a free port, imperial checkpoints, and storm events. The second jump always contains a pursuit battle and the sixth an elite blockade so a first run exercises damage control before the reinforced Sky Gate finale. Every move spends aether; every two moves consume supplies; the storm permanently blocks old columns.

Combat begins paused. The player redistributes twelve core power, moves six crew among ten compartments, chooses a target, fires the main battery, and launches two air wings. Incoming damage first drains ward, then armor, then hull and targeted systems. Fires and breaches reduce oxygen while assigned crew repair the room. A downed crew member has twelve simulated seconds before death; captain death ends the run.

If the magazine reaches zero during combat, emergency assembly converts three points of value into three ordnance: salvage first, supplies second, and ten survivors per remaining point as the final fallback. The action also reduces morale and raises instability. This is deliberately costly, but it prevents a resource mistake from leaving a live run with no possible damaging action.

## Airspace rules

| Rule | Low | Medium | High |
|---|---|---|---|
| Use | Cover and denser air | Neutral operating band | Stronger ward recovery |
| Cost | Normal | Normal | Altitude-change recovery and instability |
| Risk | Terrain is represented through events | No special penalty | Larger relative-altitude accuracy gaps |

Wards regenerate only while a ship has not been hit for 6 seconds; the delay keeps wards as burst absorbers rather than sustained walls, which is what makes heavily warded silhouettes such as the bulwark monitor beatable by steady fire.

Weather profiles are clear, thunderhead, turbulence, aether current, icing, and cloud cover. Each owns an accuracy modifier, ward modifier, sortie-time modifier, and hazard interval. The route previews weather and recommended altitude before travel.

## Production contracts

All persistent state is plain serializable data: `RunState`, `ShipState`, `CrewState`, `SquadronState`, `ConvoyState`, `RouteNodeState`, `EncounterDefinition`, and `WeatherProfile`. UI invokes `IGameCommand` implementations and never owns combat truth. Three RNG streams isolate route, combat, and event outcomes so a save and seed can reproduce a defect.

Profile and suspended run saves are separate, schema-versioned JSON files written through a temporary file before replacement. EA migrations must remain additive; destructive schema changes require a dedicated migrator and fixture saves from every public build. Fixture saves are committed per schema version under the EditMode test folder and loaded by a regression test; write a new fixture folder whenever the schema version is bumped.

## Content path to Early Access and 1.0

| Milestone | Flagships | Lineages | Regions | Events | Enemy silhouettes/configs |
|---|---:|---:|---:|---:|---:|
| Vertical slice | 1 | 6 represented | 6 + finale (tutorial: 1) | 100 authored events | 12 / 30 |
| Early Access | 1 | 4 fully authored | 4 | ~50 | 6 / ~15 |
| 1.0 | 3 | 6 | 6 + finale | 100+ | 12 / ~30 |

Progress toward 1.0 (2026-09-04): 30 flagship modules, the between-region port shop, 18 mounted weapons with hardpoints, three flagships with progress unlocks, nine air wings, twelve enemy silhouettes with thirty region-gated configs, six regions, the Gate Warden finale, one hundred events with region tags, and six mechanically distinct lineages are in. Remaining work is production depth, balance, presentation, accessibility and platform QA rather than another headline system family.

## Lineage rules

Lineage changes tactical specialties without creating permanent stat progression. Every crew member uses their lineage rule, while the custom captain also grants one opening doctrine bonus to shape the expedition. Humans improve repair and boarding and begin with higher morale; Elves are fragile but reduce resonator overcharge instability and accident risk; Dwarves are sturdy repair specialists with hazard resistance; Orcs have the most health, repel boarders quickly and remain rescuable longer; Goblins repair and fly sorties faster; Avians resist oxygen loss and suffer fewer sortie casualties. Captain doctrine bonuses are applied once when a new run is created and are serialized as ordinary run state.

EA requires a completable 90–120 minute campaign, all core rule families, Korean/English parity, save migration fixtures, and Windows/macOS QA. Post-EA development adds content and balance without replacing the core state contracts.

## Acceptance targets

- Ship operations, wings, and altitude/weather each create distinct meaningful decisions in a 20–30 minute slice.
- All critical warnings identify the threatened crew, compartment, resource, or convoy condition and can auto-pause.
- No event choice can spend unavailable resources; no blocked node can be entered; no save can resume an already-ended run.
- Standard difficulty targets a 25–40% experienced win rate after sufficient EA data. Story targets 60%+, Harsh 10–20%. The current Dawn Refuge headless audit (a competent but not expert proxy) lands at 42% / 60% / 15% on Standard, Story and Harsh over the six-region campaign. Difficulty firepower is 0.66 / 1.0 / 1.2 and toughness 0.9 / 1.0 / 1.15, with region firepower reaching 1.7. The Iron Bastion was last measured at 32% and the Zephyr Kite at 6% on Standard, the latter a known gap because the autoplayer never exploits wings. Human playtests remain authoritative over proxy results.
- Windows and macOS builds must share seeds and save payloads and maintain 60 fps at the locked 1920×1080 reference layout before the slice is promoted to EA production.
