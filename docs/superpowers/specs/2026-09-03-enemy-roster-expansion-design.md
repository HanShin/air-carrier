# Enemy roster 3 → 6 — design (content step 3a)

Date: 2026-09-03.

## Goal

Reach the Early Access target of six enemy silhouettes with distinct counters, using existing rules wherever possible. One new rule family (boarding) is added because the crew-movement mechanic has no threat that demands it.

## Roster (`ContentCatalog.EnemyRoster`, data-driven; `CreateEnemy` picks by tier and weight, `CreateEnemyById` for tests and debug)

| id | Name (ko / en) | Tier | Weight | Hull/Armor/Ward | Core | Signature | Counter |
|---|---|---:|---:|---|---:|---|---|
| enemy_cutter | 제국 추격 커터 / Imperial Pursuit Cutter | 1 | 40 | 24/10/8 | 8 | balanced | main battery |
| enemy_carrier | 제국 강습 항모 / Imperial Strike Carrier | 1 | 20 | 28/12/10 | 10 | deck 3 | interceptors, assault |
| enemy_scout | 제국 정찰 프리깃 / Imperial Scout Frigate | 1 | 20 | 20/8/8 | 10 | sensors 2, engines 3: accurate and evasive | recon, altitude matching |
| enemy_boarder | 제국 강습 바지선 / Imperial Boarding Barge | 1 | 20 | 26/12/6 | 9 | boarding parties instead of air strikes | intercept charges, marines |
| enemy_cruiser | 제국 폭풍 순양함 / Imperial Storm Cruiser | 2 | 60 | 34/18/12 | 11 | weapons 3 | bombard weapons |
| enemy_monitor | 제국 방벽 감시함 / Imperial Bulwark Monitor | 2 | 40 | 30/22/16 | 11 | ward 3: fast ward regen | assault the ward room, bombard |

The first expedition never rolls variants (unchanged). Tier 1 rolls once on the combat stream (`Range(0,100)` against cumulative weights); tier 2 likewise. Deck plans: scout 5x2 with a 2-wide engine block, boarder 6x2 with a 2x2 assault bay (FlightDeck), monitor 6x3 with a 2x2 ward block.

## Boarding rule (`ShipState.boardingCapable`, additive field)

- In `EnemySquadronStrike`, a boarding-capable enemy with a powered FlightDeck sends a boarding party instead of an air strike: intercept charges repel it (same log as strikes, new `log.boarders_repelled`); otherwise `room.intruders += 2` on a random player room, `log.boarders` and a Warning alert `alert.boarders` naming the room.
- `TickRooms`: for each player room with intruders, active crew in the room fight them off at `0.35 × crew + 0.6 if a Marine is present` per second. With nobody present, intruders damage that room's system at `intruders × 4` per second. Crew already take `intruders × 1.5` damage per second (existing rule).
- Blueprint: rooms with intruders get a red "침입 n / Boarders n" label stacked above the hazard labels and a red outline; the detail strip lists intruders.

## Tests

- Roster: all six ids spawn through `CreateEnemyById`; tier 1 with variants produces scout and boarder within 500 seeds; tier 2 produces monitor; first expedition still cutter-only.
- Deck plans and localization: the existing parametrised tests gain the three new ids.
- Boarding: no intercept → intruders appear; intercept → repelled; a Marine clears intruders faster than an unattended room loses system integrity.
- Headless audit after the change stays within GDD targets.

## Docs

README roster line, GDD table (6 / 7 configs counting the reinforced gate).

## Addendum: ward recharge delay and balance pass (same day)

Per-ship audits with `--enemy=` exposed a stalemate class: ward regeneration (`power × 0.22/s`) was comparable to sustained weapon damage, so the bulwark monitor stalled 27/30 autoplayer runs and even forced cruisers stalled 3/30. Instead of cutting the monitor's ward, the rule changed: `ShipState.wardRechargeSeconds` is set to 3.5 s on any hit and `TickWard` skips regeneration while it runs (both ships). The audit now reports stalemates explicitly (exit code 3).

The delay shortened fights and raised autoplayer win rates (Standard 79%, Story 91%), so enemy fire was tightened: cooldown 5.2 → 4.3 s (final battle 3.8 → 3.2 s) and damage `2.2 + 0.45×power` → `2.6 + 0.5×power`. Result: Standard 51%, Story 79%, locked seed still wins, no stalemates. Forced per-ship Standard win rates: cutter/scout/boarder 100%, carrier 67%, monitor 63%, cruiser 23%; the autoplayer always launches interceptors, which repels boarders, so the barge is easier for it than for a player who does not.
