# Imperial Strike Carrier — design

Date: 2026-09-03

## Goal

Add a third enemy ship, the Imperial Strike Carrier, so that the air-wing rules (intercept, escort, assault) have an opponent that makes them decisive. No new combat rules; the ship's identity comes from existing deck/weapon interactions.

## Ship definition (`ContentCatalog.CreateEnemy`)

| Field | Cutter | **Carrier** | Cruiser |
|---|---:|---:|---:|
| id | enemy_cutter | enemy_carrier | enemy_cruiser |
| hull | 24 | 28 | 34 |
| armor | 10 | 12 | 18 |
| ward | 8 | 10 | 12 |
| coreOutput | 8 | 10 | 11 |
| Weapons power/max | 2/4 | 1/3 | 3/4 |
| FlightDeck power/max | 0/2 | 3/4 | 1/2 |
| Sensors power/max | 0/2 | 1/2 | 0/2 |

All other systems match the cutter. Allocated power sums to coreOutput (10).

Resulting behaviour from existing rules: enemy air strike lands 8 damage on the flight deck every 14 s unless an intercept charge is held; player squadron loss chance rises from 14% to 24.5%; enemy main battery does 2.65 per 5.2 s.

## Placement (`GameSimulation.BeginCombat`)

- Tier 1 battles (route Battle nodes and event-started battles) roll the combat RNG stream: 40% carrier, otherwise cutter.
- Tier 2 (elite) and the Sky Gate keep the cruiser.
- The first expedition (locked seed) never rolls the variant. The roll is skipped entirely so RNG consumption and the audited outcome are unchanged.

## Localized ship names

`ShipState` gains an additive `nameKey` string. `CreateEnemy` sets `ship.enemy_cutter`, `ship.enemy_cruiser`, `ship.enemy_carrier`; the localization tables carry Korean/English for each. The enemy panel shows `l10n.T(nameKey)` when the key is set and falls back to `displayName`. The player flagship keeps its proper-noun `displayName` and an empty key. Save schema version is unchanged (additive field).

## Tests (EditMode)

1. Carrier spawn: allocated power ≤ coreOutput and FlightDeck power = 3.
2. First expedition locked seed never produces a carrier in any battle.
3. Against a carrier, an enemy air strike damages the flight deck when no intercept charge exists, and is absorbed when one does.
4. All three enemy name keys resolve in both languages.

## Verification

- dotnet NUnit harness over Core/Content/SaveService + EditMode tests (no Unity editor on this machine).
- Headless audit (100 Standard, 100 Story, locked seed 32838) compiled with dotnet; victory rate must stay within GDD targets and the locked seed must still win.
- `python3 tools/validate_project.py`.
- README implemented list and GDD table updated (enemy silhouettes 3/3).
