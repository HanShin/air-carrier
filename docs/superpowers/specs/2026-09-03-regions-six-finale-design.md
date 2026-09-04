# Regions 4 → 6 and the finale — design (1.0 content step 6f)

Date: 2026-09-03.

## Regions

| # | id | Name | Weather bias | Encounter bias | Enemy × | Extra-cost chance |
|---|---|---|---|---|---:|---:|
| 5 | abyssal_strait | 심연 해협 / Abyssal Strait | turbulence, aether current | storms and salvage up | 1.75 | 0.4 |
| 6 | sky_throne | 천공 왕좌 / Sky Throne | clear, aether current | checkpoints and battles up | 1.9 | 0.45 |

`ContentCatalog.RegionCount` becomes 6; post-tutorial runs get `regionCount = 6`. Saves keep the region count they were started with (a v1 fixture at 4 stays 4).

## Finale

The gate of the last region of a campaign (`regionIndex == regionCount && regionCount > 1`) always spawns `enemy_gate_warden` (silhouette `enemy_warden`, 7x3 plan): hull 46 / armor 28 / ward 20, core 14, siege cannon + resonance lance, deck 2, boarding, sensors 2. The gate reinforcement and region scaling still apply. The final gate node is named `node.final_gate` ("천공 왕좌의 문 / The Throne Gate"). The tutorial (regionCount 1) keeps its cruiser gate.

## Tests

Six localized regions; post-tutorial regionCount 6; region 5/6 weather shares; the warden spawns only at the campaign's last gate; tutorial gate unchanged; warden config valid with a deck plan; fixture keeps regionCount 4; final gate node name.

## Addendum: balance for six regions

Six regions compounded the old per-region losses to Standard 3%. Changes: `RegionDefinition.enemyDamageMultiplier` separated from toughness (1.0/1.08/1.18/1.28/1.45/1.6 vs 1.0/1.12/1.3/1.5/1.7/1.9); port refit back to +3 hull, +2 armor, +2 ward, +1 core and +12 salvage; the boss ignores region scaling; every Dawn Refuge and Zephyr run now starts with cannon + ward lance (the Bastion keeps its heavy cannon alone); difficulty scales firepower 0.66/1.0/1.25 and toughness 0.9/1.0/1.15; the Zephyr powers both guns (weapons 3) and has hull 34 / armor 18.

Result: Dawn Refuge Standard 39% (losses 1/0/7/19/17/17), Story 59%, Harsh 13%; Iron Bastion 46%; Zephyr Kite 5% (known gap: wings unused by the autoplayer); locked seed wins; no stalemates. The curve is now FTL-shaped: early regions are safe, the last three decide the run.
