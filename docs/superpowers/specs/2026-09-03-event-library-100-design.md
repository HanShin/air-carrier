# Event library 50 → 100 — design (1.0 content step 6g)

Date: 2026-09-03.

## Goal

Twenty authored events per non-combat type (100 total) so a six-region campaign rarely repeats one, with region-tagged events that only appear in the regions they were written for.

## Data

`EncounterDefinition.regions` (int[]; empty = any region). `tools/gen_events.py` gains a `regions=` argument per event. `ContentCatalog.EncounterIds(type, regionIndex)` filters by tag; `AssignEncounterVariants(nodes, seed, regionIndex)` draws from the filtered pool (the old two-argument form means region 1). `AdvanceRegion` passes the new region.

## Authoring

Ten new events per type, themed: Icefield Heights and Abyssal Strait rescues, Imperial Cordon and Sky Throne checkpoints, relic salvage near the throne, abyssal storms, throne-court trades. Same rules as before: 2–4 visible choices, a free untagged choice, gambles state their odds, tag-gated blue options, at least eight events carry region tags.

## Tests

≥20 events per type; ≥8 region-tagged events; a region-tagged event never appears outside its regions over many seeds and does appear inside; existing structural/localization checks cover every event.

## Addendum

Fifty events added (ten per type; 14 region-tagged across regions 2–6). The richer event economy lifted the autoplayer to Standard 46% / Harsh 21%, so region 5–6 firepower moved to 1.5 / 1.7 and Harsh firepower to 1.3. Final: Dawn Refuge Standard 44%, Story 60%, Harsh 14%; Iron Bastion 32%; Zephyr Kite 6%; locked seed wins; no stalemates; 109 EditMode tests.
