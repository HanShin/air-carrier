# Event library 5 → 50 — design (content step 3b)

Date: 2026-09-03.

## Goal

Ten authored events per non-combat encounter type (Rescue, Salvage, Trade, Checkpoint, Storm), Korean and English, chosen per node so a run rarely repeats an event. The locked first expedition keeps its five baseline events.

## Choice model additions (`EncounterChoiceDefinition`, all data-only)

| Field | Effect |
|---|---|
| `hullDelta`, `armorDelta` | clamp-limited repair or damage to the flagship |
| `instabilityDelta` | core instability change (clamped 0..100) |
| `refitSquadrons` | restore every squadron to full strength |
| `battleTier` | tier passed to `BeginCombat` when `startsBattle` (default 1) |
| `successChance` | 1 = deterministic; below 1 the choice is a gamble rolled on the events RNG stream |
| `failureChoiceId` | the hidden choice whose effects and result text apply when the gamble fails |
| `hidden` | never shown or selectable; exists only as a failure outcome |

`ChooseEncounter` applies the chosen (or failure) choice through one `ApplyChoice` path. Existing id-based specials (`repair`, `high`, `ride`) remain.

## Selection

`ContentCatalog.AssignEncounterVariants(nodes, seed)` runs after route generation on a dedicated RNG stream (`Seed(seed, 0x5E1EC7)`), so route rolls are untouched. Per type it draws without replacement from that type's pool and refills when exhausted. `GameSimulation.NewRun` calls it only when the profile has completed the tutorial.

## Authoring rules

- Every event: 2–4 visible choices, at least one always-affordable choice, and no choice that can spend a resource the player lacks (costs are checked by `CanChoose`).
- Gambles state their odds in the choice text; the UI also appends the percentage.
- Tag-gated "blue" options use existing tags (`lineage.*`, `support.*`).
- New events live in `EncounterLibrary.cs`; strings in `LocalizationService.Encounters.cs` (partial class).

## UI and tools

Encounter screen hides hidden choices and appends `성공 n% / n% chance` to gambles. The autoplayer scores gambles by expected value.

## Tests

Pool sizes, full bilingual coverage of every key, structural validity (failure ids resolve to hidden choices, chances in (0,1]), gamble success/failure paths, hidden choices unselectable, deterministic non-repeating assignment, first expedition unchanged, each new delta effect, elite battle tier.
