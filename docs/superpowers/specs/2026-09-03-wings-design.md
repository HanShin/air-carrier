# Air wings — design (1.0 content step 6d)

Date: 2026-09-03.

## Goal

Nine authored wings with distinct specialties, bought at ports into the flagship's wing bays, applied through data-driven mission bonuses.

## Data (`WingDefinition`, `tools/gen_wings.py` → `WingLibrary.cs` + strings)

Fields: id, nameKey, descriptionKey, type (specialty), tier 1–3, cost (salvage 12/20/30), strength, ordnanceCost, missionTime (multiplier), lossResistance (loss-chance multiplier), interceptCharges (per Intercept sortie), bombardDamage (multiplier), bombardFire (extra fire on the target room), escortWard (flat), escortCharges, reconSeconds, assaultSabotage (flat replaces 32), assaultHull.

| id | Type | Tier | Signature |
|---|---|---:|---|
| kestrel_interceptors (start) | Interceptor | 1 | 4 craft, 2 charges |
| ember_bombers (start) | Bomber | 1 | 3 craft, 6 + strength |
| gale_lancers | Interceptor | 2 | 3 charges, loss ×0.8 |
| ghost_kites | Interceptor | 3 | 5 craft, time ×0.7, loss ×0.7 |
| thunder_bombers | Bomber | 2 | damage ×1.5, fire on target, 3 ordnance |
| sky_wardens | Escort | 2 | ward +8 and 2 charges |
| far_eyes | Recon | 1 | 2 craft, 0 ordnance, recon 25 s, loss ×0.5 |
| storm_marines | Assault | 2 | sabotage 48, hull −3 |
| ruin_dropships | Assault | 3 | sabotage 64, hull −5, 4 craft |

`SquadronState.wingId` (additive); saves without it are backfilled by type. Flagships define `wingBays` (Dawn Refuge 2, Bastion 2, Zephyr 3 with far_eyes in the third bay). `CreateSquadrons(flagship)` builds from the flagship's `startingWings`.

## Rules

`LaunchSquadron` uses the wing's ordnance cost and mission time; `TickSquadrons` applies the wing's bonuses per mission and its loss resistance. Missions stay universal (any wing can fly any mission) but bonuses only apply on the wing's specialty.

## Port

`PortWingOffers()` returns one wing (seeded, tier ≤ regionIndex + 1, not already carried). `PurchaseWing(id)` replaces the bay of the same specialty if present, else the last bay, refunding half of the old wing's cost; the pilot stays with the bay; the new wing starts at full strength. The port screen shows the wing card; slot cards in combat show wing names.

## Autoplayer

Buys a same-specialty higher-tier wing when affordable after modules and weapons.

## Tests

Library size/localization/validity; default wings per flagship and save backfill; per-wing ordnance and mission time; bombard/intercept/escort/recon/assault bonuses; loss resistance; port purchase/replacement/refund.

## Addendum

Implemented as specified. The replacement of `CreateSquadrons` accidentally removed the region definitions during the edit and was restored from the previous commit. Campaign audit after wings: Dawn Refuge Standard 30%, Story 61%, Harsh 16%; Iron Bastion 39%; Zephyr Kite 19%; locked seed wins; no stalemates. 103 EditMode tests.
