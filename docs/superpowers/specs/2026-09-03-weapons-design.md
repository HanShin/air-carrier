# Weapons and weapon slots — design (1.0 content step 6b)

Date: 2026-09-03.

## Goal

Replace the single main battery with mounted weapons: eighteen authored weapons in seven families, two flagship hardpoints, shared Weapons-system power, per-slot firing, and enemy loadouts on the same rules.

## Data (`WeaponDefinition`, generated from `tools/gen_weapons.py` into `WeaponLibrary.cs` + `LocalizationService.Weapons.cs`)

Fields: id, nameKey, descriptionKey, family (Cannon, Lance, Piercer, Missile, Flak, Incendiary, Breacher), tier 1–3, cost (salvage 10/18/28), powerCost 1–3, damage, cooldown, accuracyBonus, wardMultiplier (damage applied while a ward absorbs; lances 2.0, piercers 0.5), armorPiercing (fraction of the hit that bypasses armor; piercers 0.6), ignoresWard (missiles), ordnancePerShot (missiles 1), systemDamageMultiplier, fireChance, breachChance, interceptCharge (flak grants one per shot).

| Family | Tier 1 | Tier 2 | Tier 3 | Role |
|---|---|---|---|---|
| Cannon | aether_cannon (start) | heavy_cannon | siege_cannon | balanced |
| Lance | ward_lance | resonance_lance | sky_lance | strips wards, weak once armor shows |
| Piercer | bolt_thrower | rail_harpoon | gate_piercer | bites through armor, poor against wards |
| Missile | rocket_pod | storm_missiles | ruin_missiles | ignores wards, costs ordnance, starts fires |
| Flak | flak_battery | flak_curtain | — | low damage, each shot grants an intercept charge |
| Incendiary | ember_mortar | — | hellfire_mortar | fires and system damage |
| Breacher | breacher_charges | hull_ripper | — | breaches and hull damage |

## Slots and power

`RunState.weaponSlots`: list of `WeaponSlotState { weaponId, cooldown }`; `ShipState.weaponHardpoints` (2 on the vanguard). Mounted weapons are powered in slot order while their summed powerCost fits the Weapons system's effective power; the rest are unpowered and cannot fire. `NewRun` mounts the aether cannon in slot 0; `SaveService.LoadRun` fills an empty list the same way for older saves.

## Firing and damage

`FireWeapon(slot, target)`: checks phase, power, cooldown and ordnance; rolls accuracy (player accuracy + weapon bonus); applies `ApplyWeaponHit`: ward absorption uses `wardMultiplier`, `armorPiercing` sends that fraction past armor straight to hull/system, `ignoresWard` skips the ward, system damage uses `systemDamageMultiplier`, fire/breach rolls use the weapon's chances; flak adds an intercept charge on every shot. Cooldown = weapon cooldown × module weaponCooldown; damage × module weaponDamage. `FireAllReady(target)` fires every ready powered slot ([F]). `FireMainWeapon` remains as a compatibility alias for `FireAllReady`.

Enemy roster entries carry `weaponIds`; `EnemyFire` runs one cooldown per mounted weapon (`enemyShip` gets its own `WeaponSlotState` list on `ShipState.weaponSlots`), damage × difficulty × region multiplier as before. Loadouts approximate current firepower: cutter aether_cannon+bolt_thrower, scout ward_lance, carrier aether_cannon, boarder rocket_pod, cruiser heavy_cannon+ward_lance, monitor heavy_cannon+flak_battery.

## Port and UI

Port offers two weapons (seeded, tier ≤ regionIndex + 1) next to the modules; buying mounts into the first empty hardpoint or replaces the last one (half its cost refunded). Combat command panel lists the slots (name, power pips, cooldown bar, powered state, Fire) plus [F] fire all. Number keys unchanged.

## Autoplayer

Fires all ready weapons each tick; at the port buys the best affordable weapon after modules; raises Weapons power with spare core output at battle start.

## Tests

Library size/localization/validity; default loadout and old-save migration; power gating in slot order; per-slot cooldown; lance vs piercer vs missile vs flak behaviour; ordnance consumption; port purchase, replacement refund and slot limit; enemy loadouts fire; save round-trip.

## Addendum: balance after implementation

Two enemy weapons per ship doubled enemy firepower and produced stalemates; enemies now mount one weapon each (cutter aether cannon, scout ward lance, carrier flak, boarder ember mortar, cruiser heavy cannon at 6.4, monitor bolt thrower). Because Weapons power no longer scales damage, the player lost the overcharge burst; spare Weapons power now divides every reload by `1 + 0.2 × spare`. Starting with two weapons made every difficulty ~69%, so later runs start with the cannon alone while the locked tutorial expedition keeps cannon + lance (it lost otherwise). Difficulty damage multipliers moved to 0.72 / 1.0 / 1.15. Final: Standard 36%, Story 61%, Harsh 18%, locked seed wins, no stalemates; 90 EditMode tests, with piercing and ordnance rules mutation-checked.
