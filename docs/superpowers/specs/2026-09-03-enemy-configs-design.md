# Enemy silhouettes 6 → 12 and configs ~30 — design (1.0 content step 6e)

Date: 2026-09-03.

## Goal

Twelve enemy silhouettes (hull identity + deck plan) and about thirty configs (loadout/stat variants of a silhouette gated by region) so later regions meet new threats instead of only scaled copies.

## Data (`tools/gen_enemies.py` → `EnemyLibrary.cs` + strings)

Each config: id, silhouette (deck plan and family name), nameKey (config-specific: e.g. "제국 노병 커터"), tier (1 regular, 2 elite/gate), weight, minRegion, hull/armor/ward, core, boarding flag, weapons, power/maxPower per system. `ShipState.deckPlanId` (additive) carries the silhouette so `ContentCatalog.DeckPlanFor(ship)` resolves plans for every config.

New silhouettes: Lancer Destroyer (ward-stripping lance, tier 1), Minelayer (breacher, tier 1), Firebrand (incendiary mortars, tier 1), Dreadnought (siege cannon, slow, tier 2), Hive Carrier (deck 3 plus boarding, tier 2), Wraith (evasive, accurate rail harpoon, tier 2). Existing six keep their ids as the base configs.

Configs: each silhouette has 2–3 (base, a loadout variant from region 2, a veteran from region 3 with +10% stats and a stronger weapon) for ~30 total.

## Selection

`CreateEnemy(tier, allowVariants, regionIndex, ref random)`: with variants, the weighted pool is every config of the tier whose `minRegion <= regionIndex`; without variants only the baseline cutter/cruiser (locked first expedition unchanged). The old three-argument overload maps to region 1. Region scaling still applies on top.

## Tests

Twelve silhouettes each with a deck plan; ≥30 configs, all localized, ten systems, power within budget, weights positive, weapons known; a region-3-only config never appears in region 1 over many seeds but does in region 4; first expedition still cutter/cruiser; `DeckPlanFor` resolves for every config; per-enemy audits have no stalemates.

## Addendum: balance

The first roster pushed Standard to 18%: the lancer's resonance lance and the dreadnought's siege cannon were too much for their slots. Base lancer now carries a ward lance and starts in region 2, the base dreadnought a heavy cannon at 34/22/12 (the siege cannon belongs to the region-4 bastion variant), the hive carrier deck 3. Forced-enemy audits stopped being an absolute measure once weapons landed (a forced cruiser also wins ~3%), so tuning used the campaign funnel instead. Final: Dawn Refuge Standard 30% with losses 27/20/12/11, Story 60%, Harsh 14%; Iron Bastion 42%; Zephyr Kite 21%; locked seed wins; no stalemates; 105 EditMode tests.
