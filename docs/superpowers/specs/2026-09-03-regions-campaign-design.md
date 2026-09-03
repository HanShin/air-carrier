# Regions 1 → 4 — design (content step 3c)

Date: 2026-09-03.

## Goal

Turn the single seven-jump route into a four-region campaign, as the GDD's Early Access target (4 regions, 90–120 minute campaign) requires, while keeping the locked first expedition byte-for-byte identical.

## Regions (`ContentCatalog.Regions`, data)

| # | id | Name (ko / en) | Weather bias | Encounter bias | Enemy stat ×  | Extra-cost chance |
|---|---|---|---|---|---:|---:|
| 1 | dawn_archipelago | 여명 군도 / Dawn Archipelago | uniform (legacy) | legacy 38/15/15/11/11/10 | 1.00 | 0.20 |
| 2 | storm_corridor | 폭풍 회랑 / Storm Corridor | thunderhead, turbulence | storms up, trade down | 1.08 | 0.25 |
| 3 | icefield_heights | 빙운 고원 / Icefield Heights | icing, cloud cover | rescues and salvage up | 1.16 | 0.30 |
| 4 | imperial_cordon | 제국 봉쇄권 / Imperial Cordon | aether current, clear | checkpoints and battles up | 1.24 | 0.35 |

Region 1's weights reproduce the legacy rolls exactly (a weighted roll over total 6 for weather and total 100 for encounters consumes the same RNG call and maps to the same outcomes), so seed 32838 generates the same route as before.

## Campaign flow

- `RunState.regionCount` (additive; 1 for the first expedition, 4 afterwards) and the existing `regionIndex`.
- `CreateRoute(seed, regionIndex)`: region 1 uses the legacy stream; later regions derive their stream from the seed and region index.
- Gate victory with `regionIndex < regionCount` calls `AdvanceRegion`: next region's route (with event variants), travel and storm counters reset, current node reset, a port stop (hull +12, armor and ward full, systems and rooms cleared, crew healed, wings refitted, +6 aether, +4 supplies, +3 ordnance, +8 salvage, +5 morale) and `log.region_cleared`. The last region's gate ends the run in Victory as today.
- `BeginCombat` scales enemy hull/armor/ward by the region's multiplier (×1 in region 1).
- `RunState.totalTravelCount` accumulates across regions for the audit.

## UI

Route title shows `지역 n/4 — 이름`; the preview panel's current line is unchanged. Ending screens unchanged.

## Tests

Four localized regions; region-2/3/4 weather and encounter shares over many seeds; region-1 route equals the no-argument route; gate victory advances a region with the port stop; last region ends in Victory; first expedition is single-region; enemy scaling grows with region; save round-trip of region fields.

## Audit

The autoplayer plays the whole campaign. Reported jumps use `totalTravelCount`; the seven-jump completion check becomes `7 × regionCount`.

## Addendum: campaign balance

The first campaign audit won 0/99 Standard runs and stalled against region-2 monitors (a missed shot let the 3.5 s recharge delay lapse). Changes: ward recharge delay 6 s; enemy multipliers 1.08/1.16/1.24; the port stop now fully repairs the hull, grows the flagship (+4 hull, +3 armor, +2 ward, +1 core output routed to weapons) and tops resources up to at least their starting values; victories in regions 3–4 award 2 ordnance.
