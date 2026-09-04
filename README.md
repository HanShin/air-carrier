# Aether Ark — playable vertical slice

`Aether Ark` is a Unity 6 prototype for a real-time-with-pause sky-carrier roguelite. It implements a campaign from captain setup through branching regional routes to a final Sky Gate battle.

The first expedition defaults to Story difficulty and a locked, audited seed. It is a single region of seven jumps with a mandatory pursuit battle, an elite blockade, and a reinforced Sky Gate finale. After the first victory, expeditions use random seeds and run the full six-region campaign: Dawn Archipelago, Storm Corridor, Icefield Heights, Imperial Cordon, Abyssal Strait and Sky Throne, each with its own weather and encounter mix and progressively tougher enemies (enemy toughness scales 1.0 → 1.9 and firepower 1.0 → 1.6 across the regions; gate reinforcement also grows per region). The Sky Throne's gate is guarded by a fixed-stat boss, the Gate Warden "Undying Oath", instead of a roster draw. Every gate passed is a port stop: full repair, resupply to at least starting stocks, a flagship refit (+hull, +armor, +ward, +1 core output), and a shipyard offering three of thirty flagship modules for salvage.

## What is implemented

- Combat UX pass: mouse-ready command bindings, disabled-state feedback, room integrity/power/hazard bars, layered defense meters, and threat countdowns.
- FTL-style combat screen: both ships are drawn as top-down deck plans (`ShipBlueprintView`) from per-ship `DeckPlan` data. Rooms are coloured by condition, show power pips and integrity, and carry fire/breach/oxygen overlays; crew appear as lineage-coloured tokens inside rooms and as a portrait column on the left. A detail strip under each blueprint reports the selected room's condition, hazards and posted crew, and the air-wing bar uses FTL-style slot cards with strength pips and mission gauges. Clicking a room or token issues the same commands as before.
- Air-wing feedback: launch/mission/return progress, target labels, recovery notices, and pilot sortie state.
- Warning UX: localized severity banner, explicit auto-pause reason, in-combat auto-pause toggle, and warning context that remains visible until combat resumes.

- Route map drawn as a star map: circular nodes with encounter glyphs and a numbered badge on reachable nodes, a storm-front band over closed columns plus an amber warning band for the column that closes after the next jump, a legend, and a select-then-depart preview panel (aether cost vs stock, weather modifiers, recommended altitude, threat note). Number keys still jump immediately.
- Runtime-built Korean/English UI with no scene assembly required.
- Six sequential regions (`ContentCatalog.Regions`): per-region weather and encounter weights, extra-cost chance, and separate enemy toughness and firepower multipliers. Region 1 reproduces the original generator exactly so the locked first expedition is unchanged; saves keep the campaign length they were started with. Difficulty scales enemy firepower (0.66 / 1.0 / 1.25) and toughness (0.9 / 1.0 / 1.15).
- Branching route map with storm closure, aether costs, weather, altitude recommendations, events, trading, rescue, and combat nodes.
- Layered ward → armor → hull damage, ten ship compartments, fires, breaches, oxygen, repairs, crew injury, rescue windows, and captain-loss defeat. Any hit pauses a ship's ward regeneration for 6 s (longer than a weapon cycle), so sustained fire wears a ward down instead of stalling against it even when shots miss.
- Power allocation, resonator overcharge and instability accidents.
- Free pause, adjustable combat speed, warning auto-pause, UI scaling, high contrast, reduced motion, and a rebindable pause key.
- Two persistent air wings with intercept, bombard, escort, recon, and assault missions.
- Twelve enemy silhouettes and thirty configs (`tools/gen_enemies.py` → `EnemyLibrary.cs`): cutter, strike carrier, scout frigate, boarding barge, lancer destroyer, minelayer, firebrand, storm cruiser, bulwark monitor, dreadnought, hive carrier and wraith, each with a deck plan, plus loadout variants and veterans gated by region (`minRegion`) so later regions meet new threats rather than scaled copies. The locked first expedition still meets only the baseline cutter and cruiser. Enemy names are localized.
- Boarding: intruders in a room injure crew and, if nobody is present, wreck the system; crew fight them off, marines fastest. Intercept charges repel boarding craft the way they break air strikes.
- Low/mid/high altitude, six weather profiles, support ships, convoy population/morale, field repairs, and squadron refits.
- Deterministic route/combat/event random streams and versioned atomic profile/run saves. Genuine JsonUtility save fixtures live under `Assets/_Project/Tests/EditMode/Fixtures/<version>/` (written by **Aether Ark → Write Save Fixtures** or `-executeMethod AetherArk.Editor.FixtureWriter.WriteSaveFixtures`); an EditMode test loads them through the current migrations and resumes play, so a schema change that breaks old saves fails CI.
- Last-resort aether and ordnance recovery so a depleted resource cannot silently soft-lock an expedition.
- Nine air wings (`tools/gen_wings.py` → `WingLibrary.cs`): Kestrel and Ember to start, then Gale Lancers, Ghost Kites, Thunder Bombers, Sky Wardens, Far Eyes, Storm Marines and Ruin Dropships across the five specialties. Each wing brings its own strength, ordnance cost, mission time, loss resistance and mission bonuses (intercept charges, bombard multiplier and fire, escort ward and charges, recon length, assault sabotage and hull). Flagships have wing bays (the Zephyr carries a third, Far Eyes); ports offer one wing, which replaces the bay of the same specialty with a half refund while its pilot stays. Saves from before wings are backfilled by specialty.
- Three flagships (`ContentCatalog.Flagships`): the balanced EAS Dawn Refuge, the armoured gun platform EAS Iron Bastion (hull 40, 3 hardpoints, 5 module slots, weak deck; unlocked by the first expedition victory) and the fast light carrier EAS Zephyr Kite (ward 16, engines 3, deck 3, five interceptors, cannon + lance; unlocked by a full campaign victory). Chosen at expedition setup; the tutorial always sails the Dawn Refuge. Each hull has its own deck plan, power budget and starting loadout. Audit with `--flagship=<id>`.
- Eighteen mounted weapons in seven families (cannon, lance, piercer, missile, flak, incendiary, breacher; tiers 1–3 for 10/18/28 salvage), authored in `tools/gen_weapons.py`. The flagship has two hardpoints; mounted weapons are powered in slot order while their power costs fit the Weapons room, spare Weapons power shortens every reload (so overcharging still matters), and each slot fires on its own cooldown ([F] fires everything ready). Lances hit wards at double strength, piercers send part of the hit past armor, missiles ignore wards but burn ordnance, flak grants intercept charges, incendiaries and breachers start fires and breaches. Enemy silhouettes carry loadouts on the same rules. Ports offer two weapons next to the modules; the tutorial expedition starts with the cannon and the ward lance, later runs with the cannon alone.
- Thirty flagship modules in eight categories (hull, core, weapons, deck, sensors, engineering, bridge, marines), tiers 1–3 for 8/14/22 salvage, four slots per flagship. Flat bonuses apply on install; multipliers and bonuses flow through `ModuleRules.Modifiers` into weapon damage and cooldown, accuracy, ward regen, repair and healing, fire and oxygen resistance, wing strength and mission time, intercept charges, recon, salvage rewards, jump discounts, boarding defence and assault sabotage. Authored in `tools/gen_modules.py`.
- Fifty authored events (ten per non-combat encounter type) in Korean and English, drawn per node without repeats from the second expedition onward; the locked first expedition keeps its five baseline events. Choices can be gambles with a stated success chance and a hidden failure outcome, gate on lineage or support-ship tags, and repair, reinforce, refit wings, shift core instability or start elite battles. Events are authored in `tools/gen_events.py`, which generates `EncounterLibrary.cs` and `LocalizationService.Encounters.cs` together so ids and strings cannot drift.
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

Development builds accept `-debug-combat [cutter|carrier|scout|boarder|cruiser|monitor]` to open a paused battle against that enemy directly (add `-debug-unpaused` to start it running and `-debug-damage` to pre-apply fire, breach, low oxygen, damaged/disabled systems and a downed crew member), which is how screenshots are verified without keyboard automation. `-debug-route [jumps]` opens the route map after auto-resolving that many jumps so storm bands and visited nodes are visible. `-debug-event <id>` opens one authored event directly. `-debug-port` opens the port after clearing the first gate. `-debug-setup` opens expedition setup.

Default pause is `Space`; it can be changed to `P` during expedition setup. Save files are written under `Application.persistentDataPath` as `profile.json` and `suspended_run.json`.

## Tests and validation

- In Unity: **Window → General → Test Runner → EditMode → Run All**.
- Command-line Unity test: `Unity -batchmode -nographics -projectPath <repo> -runTests -testPlatform EditMode -testResults <results.xml>`.
- macOS development build: execute `AetherArk.Editor.ProjectBuilder.BuildMac`; output is `Builds/AetherArk.app`.
- Outside Unity: `python3 tools/validate_project.py` validates JSON, C# delimiter balance, localization coverage, and required assets.
- With Mono installed: `bash tools/run_headless_audit.sh` compiles the real core C# sources and runs 100 seeded Standard/Story autoplayer campaigns plus the locked first expedition. Only the locked seed is treated as a first expedition, so the sweeps exercise the full roster. Add `--enemy=<id>` (for example `--enemy=enemy_monitor`) to force one silhouette into every battle; a battle that neither side can finish within the time cap is reported as a STALEMATE and exits with code 3. `--report` prints a balance report (survival funnel per region, loss reasons, per-enemy and per-region battle stats, resources entering each region) and `--strategy=cautious` plays without wings or overcharge as a lower bound. Current autoplayer campaign win rates over six regions: Dawn Refuge Standard 39% (losses 1/0/7/19/17/17 by region) / Story 59% / Harsh 13%, Iron Bastion Standard 46%, Zephyr Kite Standard 5% (a known problem: the autoplayer cannot exploit its wings and its light hull folds in regions 4–6), locked seed wins; the cautious profile wins almost nothing, so interceptors and escorts are not optional against strikes.
- `bash tools/compile_all_csharp.sh` compiles the complete runtime/UI source against compile-only Unity API stubs, catching C# and project-layer linkage errors before an editor import.

The included EditMode tests cover deterministic generation, the full seven-jump route, power limits, layered damage, event costs, captain death, squadron launch, both emergency-resource fallbacks, difficulty modifiers, weather definitions, save round-trips, the strike carrier's power budget and air-strike behaviour, its exclusion from the locked first expedition, and enemy-name localization.

## Project map

- `Assets/_Project/Scripts/Core`: serializable state contracts, commands, seeded RNG, and deterministic simulation.
- `Assets/_Project/Scripts/Content`: vertical-slice content and Korean/English string tables.
- `Assets/_Project/Scripts/Runtime`: bootstrap, generated UI, blueprint renderer, input, and persistence.
- `Assets/_Project/Tests/EditMode`: automated rule and save tests.
- `docs/GDD.md`: locked design rules and production targets.

The generated sky background lives at `Assets/_Project/Resources/Art/sky_storm_background.png` and is loaded through Unity Resources for the prototype.
