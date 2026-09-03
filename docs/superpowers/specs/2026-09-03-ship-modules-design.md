# Ship modules and the port — design (1.0 content step 6a)

Date: 2026-09-03.

## Goal

Give salvage a purpose and the campaign a progression loop: thirty installable flagship modules bought at the port between regions, applied through one modifier set that the combat and route rules consult.

## Data (`ModuleDefinition`, generated from `tools/gen_modules.py` into `ModuleLibrary.cs` + `LocalizationService.Modules.cs`)

Fields: id, nameKey, descriptionKey, category (Hull, Core, Weapons, Deck, Sensors, Engineering, Bridge, Marines), tier 1–3, cost (salvage 8 / 14 / 22), and effect fields:

| Effect | Applied where |
|---|---|
| maxHull, maxArmor, maxWard, coreOutput (flat) | on install, immediately (and refilled) |
| weaponDamage, weaponCooldown (multipliers) | `PlayerShotDamage`, `FireMainWeapon` cooldown |
| accuracy (flat bonus), weatherResistance (halves weather accuracy penalty) | `Accuracy` for player attacks |
| wardRegen (multiplier) | `TickWard` for the player ship |
| repairRate, healRate (multipliers), autoRepair (per-second passive repair) | `TickCrew` |
| fireResistance, oxygenReserve (halve fire growth and crew fire damage / halve oxygen loss) | `TickRooms`, `TickCrew` |
| squadronStrength (+max strength, also refills), squadronTime (mission duration multiplier) | install / `LaunchSquadron` |
| interceptCharges (at battle start), reconSeconds (at battle start) | `BeginCombat` |
| salvageReward (flat per victory) | `ResolveVictory` |
| aetherDiscount (route cost −1, minimum 1) | `CanTravelTo` / `TravelTo` via `TravelCost(node)` |
| boardingDefense (crew fight ×1.6), assaultBonus (+sabotage), crewHealth (+max health) | `TickIntruders`, squadron assault, install |
| instabilityDecay (multiplier) | `Tick` |

`ModuleRules.Modifiers(RunState)` aggregates installed modules into a `ModuleModifiers` value (multipliers multiply, flats add). `RunState.installedModules` (List<string>, additive) and `ShipState.moduleSlots` (4) persist.

## Port (`GamePhase.Port`, appended to the enum)

`AdvanceRegion` now ends in `Port` instead of `RouteMap`. `ContentCatalog.OfferModules(seed, regionIndex, installed)` returns three distinct uninstalled modules, tiers weighted toward `regionIndex + 1` and below, deterministic per seed and region. `PurchaseModule(id)` checks phase, slot count, duplicates and salvage, then installs. `DepartPort()` moves to `RouteMap`. The first expedition has no port.

Port screen: region cleared header, salvage, installed modules, three offer cards (name, tier, category, description, cost, Buy), Depart button. `-debug-port` opens it. The route preview's empty state lists installed modules.

## Autoplayer

At `Port` it buys by priority (hull, armor, weapon damage, repair, ward, deck, then cheapest) while affordable and slots remain, then departs.

## Tests

Library size and localization, unique ids, at least one effect each; offer determinism and exclusion of installed; purchase rules; flat stats applied on install; save round-trip; modifier hooks (shot damage, cooldown, accuracy, aether discount, intercept charges, salvage reward, squadron strength, boarding defence); port flow; first expedition unaffected.

## Addendum: balance with modules

With the autoplayer shopping (hull, armor, weapons first), Standard rose to 59% and regions 2–3 lost almost nobody. The free port refit was cut to +2 hull, +1 armor, +1 ward (+1 core kept) so growth comes from chosen modules, and region multipliers became 1.0 / 1.12 / 1.38 / 1.6. Result: Standard 45% (losses 32/6/1/16), Story 72%, Harsh 21%, locked seed wins, no stalemates. Standard sits a little above the 25–40% target; human playtests should decide whether to lift region 4 further.
