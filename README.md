# Aether Ark — playable vertical slice

`Aether Ark` is a Unity 6 prototype for a real-time-with-pause sky-carrier roguelite. It implements a complete short run from captain setup through a branching route and a final Sky Gate battle.

The first expedition defaults to Story difficulty and a locked, audited seed. It contains seven jumps, a mandatory pursuit battle, an elite blockade, and a reinforced Sky Gate finale. After the first victory, subsequent expeditions use random seeds.

## What is implemented

- Combat UX pass: mouse-ready command bindings, disabled-state feedback, room integrity/power/hazard bars, layered defense meters, and threat countdowns.
- FTL-style combat screen: both ships are drawn as top-down deck plans (`ShipBlueprintView`) from per-ship `DeckPlan` data. Rooms are coloured by condition, show power pips and integrity, and carry fire/breach/oxygen overlays; crew appear as lineage-coloured tokens inside rooms and as a portrait column on the left. A detail strip under each blueprint reports the selected room's condition, hazards and posted crew, and the air-wing bar uses FTL-style slot cards with strength pips and mission gauges. Clicking a room or token issues the same commands as before.
- Air-wing feedback: launch/mission/return progress, target labels, recovery notices, and pilot sortie state.
- Warning UX: localized severity banner, explicit auto-pause reason, in-combat auto-pause toggle, and warning context that remains visible until combat resumes.

- Runtime-built Korean/English UI with no scene assembly required.
- Branching route map with storm closure, aether costs, weather, altitude recommendations, events, trading, rescue, and combat nodes.
- Layered ward → armor → hull damage, ten ship compartments, fires, breaches, oxygen, repairs, crew injury, rescue windows, and captain-loss defeat.
- Power allocation, resonator overcharge and instability accidents.
- Free pause, adjustable combat speed, warning auto-pause, UI scaling, high contrast, reduced motion, and a rebindable pause key.
- Two persistent air wings with intercept, bombard, escort, recon, and assault missions.
- Three enemy silhouettes: the pursuit cutter, the storm cruiser, and the deck-heavy Imperial Strike Carrier, which replaces the cutter in 40% of regular battles from the second expedition onward and punishes launches unless interceptors or boarders are used. Enemy ship names are localized.
- Low/mid/high altitude, six weather profiles, support ships, convoy population/morale, field repairs, and squadron refits.
- Deterministic route/combat/event random streams and versioned atomic profile/run saves.
- Last-resort aether and ordnance recovery so a depleted resource cannot silently soft-lock an expedition.
- Korean and English authored content for the vertical slice.

The 1.0 targets in the original plan—three flagships, six regions, 100+ events, 12 enemy silhouettes, 18 weapons, nine wings, and roughly 30 modules—remain production content. The current repository is the intended first vertical slice and architecture foundation, not a claim that the multi-year 1.0 content set is complete.

## Open and play

1. Install a supported Unity 6 LTS editor with Windows and macOS build support.
2. In Unity Hub, add this repository as a project. Unity may update `ProjectVersion.txt` to the installed Unity 6 patch.
3. Open `Assets/Scenes/Main.unity` and press Play. The game bootstraps itself at runtime.
4. Start a new expedition. In combat, click a crew member and then a compartment to move them. Select an enemy compartment before firing or launching a strike.

Keyboard access for the current slice:

- Menu: `N` new expedition, `C` continue, `L` language.
- Setup: `Enter` launch, `Esc` back.
- Route and encounters: `1`–`9` choose the numbered available option.
- Combat: `Space` pause/resume, `F` fire, `1`/`2` launch that wing on bombardment, `S` call support, `R` assemble emergency ordnance.

Emergency ordnance first consumes salvage, then supplies, then lives. It adds instability and costs morale, but guarantees that an empty magazine cannot permanently trap a run in combat.

Development builds accept `-debug-combat [cutter|carrier|cruiser]` to open a paused battle against that enemy directly (add `-debug-unpaused` to start it running and `-debug-damage` to pre-apply fire, breach, low oxygen, damaged/disabled systems and a downed crew member), which is how screenshots are verified without keyboard automation.

Default pause is `Space`; it can be changed to `P` during expedition setup. Save files are written under `Application.persistentDataPath` as `profile.json` and `suspended_run.json`.

## Tests and validation

- In Unity: **Window → General → Test Runner → EditMode → Run All**.
- Command-line Unity test: `Unity -batchmode -nographics -projectPath <repo> -runTests -testPlatform EditMode -testResults <results.xml>`.
- macOS development build: execute `AetherArk.Editor.ProjectBuilder.BuildMac`; output is `Builds/AetherArk.app`.
- Outside Unity: `python3 tools/validate_project.py` validates JSON, C# delimiter balance, localization coverage, and required assets.
- With Mono installed: `bash tools/run_headless_audit.sh` compiles the real core C# sources and runs 100 seeded Standard/Story autoplayer campaigns plus the locked first expedition. Only the locked seed is treated as a first expedition, so the sweeps exercise the strike carrier.
- `bash tools/compile_all_csharp.sh` compiles the complete runtime/UI source against compile-only Unity API stubs, catching C# and project-layer linkage errors before an editor import.

The included EditMode tests cover deterministic generation, the full seven-jump route, power limits, layered damage, event costs, captain death, squadron launch, both emergency-resource fallbacks, difficulty modifiers, weather definitions, save round-trips, the strike carrier's power budget and air-strike behaviour, its exclusion from the locked first expedition, and enemy-name localization.

## Project map

- `Assets/_Project/Scripts/Core`: serializable state contracts, commands, seeded RNG, and deterministic simulation.
- `Assets/_Project/Scripts/Content`: vertical-slice content and Korean/English string tables.
- `Assets/_Project/Scripts/Runtime`: bootstrap, generated UI, blueprint renderer, input, and persistence.
- `Assets/_Project/Tests/EditMode`: automated rule and save tests.
- `docs/GDD.md`: locked design rules and production targets.

The generated sky background lives at `Assets/_Project/Resources/Art/sky_storm_background.png` and is loaded through Unity Resources for the prototype.
