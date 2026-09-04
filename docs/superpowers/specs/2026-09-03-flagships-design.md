# Flagships — design (1.0 content step 6c)

Date: 2026-09-03.

## Goal

Three flagships with different hulls, deck plans, power budgets, hardpoints, module slots and starting loadouts, selectable at expedition setup and unlocked by progress.

## Data (`FlagshipDefinition`, `ContentCatalog.Flagships`)

| id | Name | Hull/Armor/Ward | Core | Signature | Hardpoints | Module slots | Starting weapons | Deck plan |
|---|---|---|---|---:|---|---:|---:|---|
| ship_vanguard | EAS Dawn Refuge (현행) | 32/18/12 | 12 | balanced carrier: deck 1/3, weapons 2/4 | 2 | 4 | aether_cannon (+ ward_lance on the tutorial) | 6x3 (existing) |
| ship_bastion | EAS Iron Bastion / 철벽 | 40/24/8 | 12 | gun platform: weapons 3/4, ward 1/3, deck 0/2, engines 1/3 | 3 | 5 | heavy_cannon, flak_battery | 6x3, 2x2 weapons block |
| ship_zephyr | EAS Zephyr Kite / 서풍 | 26/10/16 | 13 | fast carrier: deck 3/4, engines 3/4, sensors 2/3, weapons 1/3 | 1 | 3 | ward_lance | 7x3, 3x2 deck |

`ProfileState.flagshipId` (additive, default vanguard). `CreateFlagship(id)` builds the ship; unknown or locked ids fall back to the vanguard; the tutorial expedition always uses the vanguard.

## Unlocks (`UnlockRules`, pure)

- ship_vanguard: always.
- ship_bastion: after the first expedition victory (tutorialSeen).
- ship_zephyr: after any full-campaign victory (`campaignVictories >= 1`, new ProfileState field).
`GameController` records victories through `UnlockRules.RecordVictory(profile, state)` when a run reaches Victory.

## Setup screen

A flagship row (◀ name ▶) cycling through unlocked ships, with a one-line description (hull/armor/ward, hardpoints, wings) under it. Locked ships are skipped; the row shows how to unlock the next one.

## Simulation

`NewRun` uses `CreateFlagship(profile.tutorialSeen ? profile.flagshipId : vanguard)`, mounts the definition's starting weapons (`EnsureLoadout` mounts the flagship's list rather than a constant), sets hardpoints and module slots, and derives the deck plan from the ship id. Squadrons stay the two current wings; the Zephyr's interceptor starts at strength 5.

## Audit

`--flagship=<id>` runs the campaign on that hull.

## Tests

Three localized flagships; deck plans cover their systems (parametrised test gains the ids); power budgets respected; NewRun mounts weapons/hardpoints/slots per flagship; locked or unknown falls back; tutorial forces the vanguard; unlock rules; save round-trip of flagshipId and campaignVictories.

## Addendum: balance after implementation

First numbers: Bastion 96% (flak every 3 s made it immune to strikes and boarders), Zephyr 3%. Changes: flak cooldowns 5.0 / 4.5 and intercept charges capped at 3; Bastion keeps hull 40 / armor 24 but ward 6, deck 1/2 (so it can still launch), heavy cannon only; Zephyr hull 32 / armor 14 / ward 16 with ward power 2, core 14, two hardpoints (cannon + lance), 4 module slots. Enemy loadouts: the monitor carries an aether cannon (its bolt thrower could not hurt high-ward hulls and stalled), the scout the bolt thrower. Harsh multiplier 1.1. The autoplayer now routes all spare core power into weapons; relaunching wings every time they are ready was tried and reverted (it bled strength and ordnance, Standard fell to 15%).

Final Standard: Dawn Refuge 32%, Iron Bastion 39%, Zephyr Kite 18% (loses mostly in region 4; the autoplayer sorties each wing once per battle, so a carrier hull is undervalued). Dawn Refuge Story 65%, Harsh 16%; locked seed wins; no stalemates. 97 EditMode tests.
