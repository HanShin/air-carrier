using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AetherArk.Content;
using AetherArk.Core;

internal static class HeadlessPlaythrough
{
    private sealed class Result
    {
        public int Seed;
        public bool Victory;
        public DefeatReason Defeat;
        public int Jumps;
        public int Battles;
        public int Events;
        public float ActiveCombatSeconds;
        public int Survivors;
        public int Morale;
        public bool Stalemate;
        public int Regions = 1;
        public string StalemateEnemy;
        public string StalemateState;
        public int RegionReached = 1;
        public int Sorties, WingOrdnance, AirframesLost, WingsDestroyed, UnopposedRaids, PilotDeaths;
        public int ReconSorties, InterceptSorties, BombardSorties;
        public float DryMagazineSeconds;
        public List<BattleRecord> BattleRecords = new List<BattleRecord>();
        public List<string> GateSnapshots = new List<string>();
    }

    private sealed class BattleRecord
    {
        public string Enemy;
        public int Region;
        public float Seconds;
        public bool Won;
        public float HullLost;
        public bool Final;
        public int Sorties, WingOrdnance, AirframesLost, WingsDestroyed, UnopposedRaids;
        public int ReconSorties, InterceptSorties, BombardSorties;
        public float DryMagazineSeconds;
    }

    private static string forcedEnemy;
    private static string strategy = "standard";
    private static string flagship;
    private static bool report;
    private static bool records;
    private static bool tutorial;
    private static string wingPolicy = "legacy";
    private static float combatTimeCap = 420f;

    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        if (Array.IndexOf(args, "--self-test") >= 0) return AuditWingPolicyTests.Run();
        tutorial = Array.IndexOf(args, "--tutorial") >= 0;
        var positional = new List<string>();
        foreach (var arg in args) if (!arg.StartsWith("--", StringComparison.Ordinal)) positional.Add(arg);
        var runCount = positional.Count > 0 ? int.Parse(positional[0]) : tutorial ? 1 : 100;
        var difficulty = positional.Count > 1 ? (Difficulty)Enum.Parse(typeof(Difficulty), positional[1], true) : tutorial ? Difficulty.Story : Difficulty.Standard;
        var baseSeed = positional.Count > 2 ? int.Parse(positional[2]) : tutorial ? GameSimulation.FirstExpeditionSeed : 17000;
        if (runCount < 1 || runCount > 10000 || positional.Count > 3 || !Enum.IsDefined(typeof(Difficulty), difficulty))
            throw new ArgumentException("Usage: [1..10000 runs] [Story|Standard|Harsh] [base seed] [options]");
        foreach (var arg in args)
        {
            if (arg.StartsWith("--enemy=", StringComparison.Ordinal)) forcedEnemy = arg.Substring("--enemy=".Length);
            if (arg.StartsWith("--strategy=", StringComparison.Ordinal)) strategy = arg.Substring("--strategy=".Length).ToLowerInvariant();
            if (arg == "--report") report = true;
            if (arg == "--records") records = true;
            if (arg.StartsWith("--wings=", StringComparison.Ordinal)) wingPolicy = arg.Substring("--wings=".Length);
            if (arg.StartsWith("--combat-cap=", StringComparison.Ordinal)) combatTimeCap = float.Parse(arg.Substring("--combat-cap=".Length));
            if (arg.StartsWith("--flagship=", StringComparison.Ordinal)) flagship = arg.Substring("--flagship=".Length);
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg != "--report" && arg != "--records" && arg != "--tutorial"
                && !arg.StartsWith("--enemy=") && !arg.StartsWith("--strategy=") && !arg.StartsWith("--flagship=") && !arg.StartsWith("--wings=") && !arg.StartsWith("--combat-cap="))
                throw new ArgumentException("Unknown option: " + arg);
        }
        if (strategy != "standard" && strategy != "cautious") throw new ArgumentException("Unknown strategy: " + strategy);
        if (Array.IndexOf(AuditWingPolicy.Modes, wingPolicy) < 0) throw new ArgumentException("Unknown wing policy: " + wingPolicy);
        if (float.IsNaN(combatTimeCap) || combatTimeCap < 0.1f || combatTimeCap > 3600f) throw new ArgumentException("Combat cap must be 0.1..3600 seconds.");
        if (flagship != null && ContentCatalog.GetFlagship(flagship) == null) throw new ArgumentException("Unknown flagship: " + flagship);
        if (tutorial && (runCount != 1 || baseSeed != GameSimulation.FirstExpeditionSeed || (flagship != null && flagship != "ship_vanguard")))
            throw new ArgumentException("Tutorial audit requires one run, seed 32838 and the Dawn Refuge.");
        var results = new List<Result>();
        for (var i = 0; i < runCount; i++) results.Add(Play(baseSeed + i * 7919, difficulty));
        if (records) foreach (var result in results) PrintRecord(result);

        var stalemates = results.FindAll(result => result.Stalemate);
        if (stalemates.Count > 0)
        {
            Console.Error.WriteLine($"COMBAT TIMEOUT in {stalemates.Count} run(s) at {combatTimeCap:0.0}s: inconclusive, not a defeat or proof of a permanent stalemate.");
            foreach (var result in stalemates) Console.Error.WriteLine($"  seed={result.Seed} enemy={result.StalemateEnemy} battles={result.Battles} {result.StalemateState}");
            return 3;
        }

        var victories = results.FindAll(result => result.Victory);
        if (victories.Count == 0)
        {
            Console.Error.WriteLine("No headless playthrough reached the Sky Gate.");
            var shown = 0;
            foreach (var result in results)
            {
                if (shown++ >= 10) break;
                Console.Error.WriteLine($"LOSS seed={result.Seed} reason={result.Defeat} jumps={result.Jumps} battles={result.Battles} combat={result.ActiveCombatSeconds:0}s morale={result.Morale} survivors={result.Survivors}");
            }
            if (report) PrintReport(results);
            return 1;
        }

        var combatSeconds = 0f;
        var estimatedHumanMinutes = 0f;
        var minBattles = int.MaxValue;
        var maxBattles = 0;
        foreach (var result in victories)
        {
            combatSeconds += result.ActiveCombatSeconds;
            estimatedHumanMinutes += result.ActiveCombatSeconds / 60f * 1.35f + 2f + result.Jumps * 0.5f + result.Events * 0.75f + result.Battles * 0.4f;
            minBattles = Math.Min(minBattles, result.Battles);
            maxBattles = Math.Max(maxBattles, result.Battles);
        }

        Console.WriteLine(tutorial ? "Headless tutorial audit" : "Headless campaign audit");
        Console.WriteLine($"Difficulty: {difficulty}");
        Console.WriteLine($"Wing policy: {wingPolicy}; strategy: {strategy}; tutorial: {tutorial}");
        if (forcedEnemy != null) Console.WriteLine($"Forced enemy: {forcedEnemy}");
        if (flagship != null) Console.WriteLine($"Flagship: {flagship}");
        Console.WriteLine($"Runs: {results.Count}");
        Console.WriteLine($"Victories: {victories.Count} ({victories.Count * 100f / results.Count:0.0}%)");
        Console.WriteLine($"Full-length completions (7 jumps x regions): {victories.FindAll(result => result.Jumps == 7 * result.Regions).Count}/{victories.Count}");
        Console.WriteLine($"Battles per victory: {minBattles}–{maxBattles}");
        Console.WriteLine($"Average active combat simulation: {combatSeconds / victories.Count / 60f:0.0} min");
        Console.WriteLine($"Estimated human duration (not playtested): {estimatedHumanMinutes / victories.Count:0.0} min");
        Console.WriteLine("Human playtime target adds pause/targeting, event reading, route planning, setup, and repair decisions.");

        var printedLosses = 0;
        foreach (var result in results.FindAll(item => !item.Victory))
        {
            if (printedLosses++ >= 10) break;
            Console.WriteLine($"LOSS seed={result.Seed} reason={result.Defeat} jumps={result.Jumps} battles={result.Battles} combat={result.ActiveCombatSeconds:0}s morale={result.Morale} survivors={result.Survivors}");
        }

        if (report) PrintReport(results);
        return victories.Count >= Math.Max(1, runCount / 4) ? 0 : 2;
    }

    private static void PrintRecord(Result r)
    {
        Console.WriteLine($"RUN seed={r.Seed} victory={(r.Victory ? 1 : 0)} stalemate={(r.Stalemate ? 1 : 0)} regions={r.Regions} reached={r.RegionReached} " +
            $"jumps={r.Jumps} battles={r.Battles} seconds={r.ActiveCombatSeconds:0.0} sorties={r.Sorties} wing_ordnance={r.WingOrdnance} " +
            $"airframes_lost={r.AirframesLost} wings_destroyed={r.WingsDestroyed} raids={r.UnopposedRaids} pilot_deaths={r.PilotDeaths} " +
            $"recon={r.ReconSorties} intercept={r.InterceptSorties} bombard={r.BombardSorties} dry_seconds={r.DryMagazineSeconds:0.0}");
    }

    private static void PrintReport(List<Result> results)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Balance report (strategy: {strategy}) ===");
        var battles = Math.Max(1, results.Sum(r => r.Battles));
        Console.WriteLine($"Wing telemetry ({wingPolicy}), per battle: sorties {results.Sum(r => r.Sorties) / (float)battles:0.00}, " +
            $"ordnance {results.Sum(r => r.WingOrdnance) / (float)battles:0.00}, airframes lost {results.Sum(r => r.AirframesLost) / (float)battles:0.00}, " +
            $"unopposed raids {results.Sum(r => r.UnopposedRaids) / (float)battles:0.00}");
        Console.WriteLine($"Pilot deaths: {results.Sum(r => r.PilotDeaths)}; destroyed wings: {results.Sum(r => r.WingsDestroyed)}; recon sorties: {results.Sum(r => r.ReconSorties)}");
        var regions = 0;
        foreach (var result in results) regions = Math.Max(regions, result.Regions);

        Console.WriteLine("Survival funnel (runs alive at the start of each region / victories):");
        for (var region = 1; region <= regions; region++)
        {
            var reached = results.FindAll(result => result.RegionReached >= region).Count;
            var lostHere = results.FindAll(result => !result.Victory && !result.Stalemate && result.RegionReached == region).Count;
            Console.WriteLine($"  region {region}: reached {reached,3}  lost here {lostHere,3}");
        }
        Console.WriteLine($"  victory: {results.FindAll(result => result.Victory).Count}");

        Console.WriteLine("Loss reasons by region:");
        var reasons = new Dictionary<string, int>();
        foreach (var result in results)
        {
            if (result.Victory || result.Stalemate) continue;
            var key = $"region {result.RegionReached} {result.Defeat}";
            reasons[key] = reasons.TryGetValue(key, out var count) ? count + 1 : 1;
        }
        foreach (var pair in reasons) Console.WriteLine($"  {pair.Key}: {pair.Value}");

        Console.WriteLine("Battles by enemy (count / win rate / avg seconds / avg hull lost):");
        var byEnemy = new Dictionary<string, List<BattleRecord>>();
        foreach (var result in results)
        foreach (var record in result.BattleRecords)
        {
            var key = record.Enemy + (record.Final ? " (gate)" : "");
            if (!byEnemy.TryGetValue(key, out var list)) byEnemy[key] = list = new List<BattleRecord>();
            list.Add(record);
        }
        foreach (var pair in byEnemy)
        {
            var wins = pair.Value.FindAll(record => record.Won).Count;
            var seconds = 0f; var hull = 0f;
            foreach (var record in pair.Value) { seconds += record.Seconds; hull += record.HullLost; }
            Console.WriteLine($"  {pair.Key,-24} {pair.Value.Count,4}  {wins * 100f / pair.Value.Count,5:0.0}%  {seconds / pair.Value.Count,6:0}s  {hull / pair.Value.Count,5:0.0}");
        }

        Console.WriteLine("Battles by region (count / win rate / avg seconds):");
        for (var region = 1; region <= regions; region++)
        {
            var list = new List<BattleRecord>();
            foreach (var result in results) list.AddRange(result.BattleRecords.FindAll(record => record.Region == region));
            if (list.Count == 0) continue;
            var wins = list.FindAll(record => record.Won).Count;
            var seconds = 0f; foreach (var record in list) seconds += record.Seconds;
            Console.WriteLine($"  region {region}: {list.Count,4}  {wins * 100f / list.Count,5:0.0}%  {seconds / list.Count,6:0}s");
        }

        Console.WriteLine("Resources entering each region (avg aether/supplies/ordnance/salvage, hull%):");
        for (var region = 2; region <= regions; region++)
        {
            var samples = new List<string>();
            foreach (var result in results) if (result.GateSnapshots.Count >= region - 1) samples.Add(result.GateSnapshots[region - 2]);
            if (samples.Count == 0) continue;
            float a = 0, su = 0, o = 0, sa = 0, h = 0;
            foreach (var sample in samples)
            {
                var parts = sample.Split(',');
                a += float.Parse(parts[0]); su += float.Parse(parts[1]); o += float.Parse(parts[2]); sa += float.Parse(parts[3]); h += float.Parse(parts[4]);
            }
            var n = samples.Count;
            Console.WriteLine($"  region {region}: {a / n:0.0} / {su / n:0.0} / {o / n:0.0} / {sa / n:0.0}, hull {h / n * 100f:0}%  (n={n})");
        }
    }

    private static Result Play(int seed, Difficulty difficulty)
    {
        var profile = new ProfileState
        {
            captainName = "Audit Captain",
            captainLineage = CrewLineage.Human,
            difficulty = difficulty,
            supportShip = SupportShipType.Workshop,
            tutorialSeen = !tutorial,
            campaignVictories = flagship != null ? 1 : 0,
            flagshipId = flagship ?? "ship_vanguard"
        };
        var simulation = GameSimulation.NewRun(profile, seed);
        var result = new Result { Seed = seed };
        var guard = 0;

        while (simulation.State.phase != GamePhase.Victory && simulation.State.phase != GamePhase.Defeat && guard++ < 250000)
        {
            switch (simulation.State.phase)
            {
                case GamePhase.RouteMap:
                    ResolveMap(simulation);
                    break;
                case GamePhase.Encounter:
                    result.Events++;
                    ResolveEncounter(simulation);
                    break;
                case GamePhase.Port:
                    ResolvePort(simulation);
                    break;
                case GamePhase.Combat:
                    result.Battles++;
                    if (forcedEnemy != null && simulation.State.combatElapsed <= 0f) ForceEnemy(simulation);
                    var record = new BattleRecord
                    {
                        Enemy = simulation.State.enemyShip?.id, Region = simulation.State.regionIndex, Final = simulation.State.isFinalBattle
                    };
                    var hullBefore = simulation.State.playerShip.hull;
                    var regionBefore = simulation.State.regionIndex;
                    record.Seconds = ResolveCombat(simulation, record);
                    record.Won = simulation.State.phase != GamePhase.Defeat && simulation.State.phase != GamePhase.Combat;
                    record.HullLost = Math.Max(0f, hullBefore - simulation.State.playerShip.hull);
                    result.ActiveCombatSeconds += record.Seconds;
                    result.BattleRecords.Add(record);
                    result.Sorties += record.Sorties;
                    result.WingOrdnance += record.WingOrdnance;
                    result.AirframesLost += record.AirframesLost;
                    result.WingsDestroyed += record.WingsDestroyed;
                    result.UnopposedRaids += record.UnopposedRaids;
                    result.ReconSorties += record.ReconSorties;
                    result.InterceptSorties += record.InterceptSorties;
                    result.BombardSorties += record.BombardSorties;
                    result.DryMagazineSeconds += record.DryMagazineSeconds;
                    if (simulation.State.regionIndex > regionBefore)
                    {
                        var r = simulation.State.resources; var ship = simulation.State.playerShip;
                        result.GateSnapshots.Add($"{r.aether},{r.supplies},{r.ordnance},{r.salvage},{ship.hull / ship.maxHull}");
                    }
                    if (simulation.State.phase == GamePhase.Combat)
                    {
                        // Both ships are still alive at the audit cap: retain an inconclusive result, never a loss.
                        result.Stalemate = true;
                        result.StalemateEnemy = simulation.State.enemyShip?.id;
                        var player = simulation.State.playerShip;
                        var enemy = simulation.State.enemyShip;
                        result.StalemateState = $"player={player.hull:0.0}/{player.maxHull:0.0} ward={player.ward:0.0} weapons={player.GetSystem(ShipSystemType.Weapons)?.damage:0.0} power={player.GetSystem(ShipSystemType.Weapons)?.EffectivePower} " +
                            $"enemy={enemy?.hull:0.0}/{enemy?.maxHull:0.0} armor={enemy?.armor:0.0} ward={enemy?.ward:0.0} weapons={enemy?.GetSystem(ShipSystemType.Weapons)?.damage:0.0} " +
                            $"activeCrew={simulation.State.crew.FindAll(crew => crew.IsActive).Count} ordnance={simulation.State.resources.ordnance} slots={simulation.State.weaponSlots.Count} " +
                            $"slot0={(simulation.State.weaponSlots.Count > 0 ? simulation.State.weaponSlots[0].weaponId : "none")} cd0={(simulation.State.weaponSlots.Count > 0 ? simulation.State.weaponSlots[0].cooldown : 0f):0.0}";
                        goto done;
                    }
                    break;
            }
        }
        done:

        result.Victory = simulation.State.phase == GamePhase.Victory;
        result.Defeat = simulation.State.defeatReason;
        result.Jumps = simulation.State.totalTravelCount;
        result.Regions = simulation.State.regionCount;
        result.RegionReached = simulation.State.regionIndex;
        result.Survivors = simulation.State.convoy.survivors;
        result.Morale = simulation.State.convoy.morale;
        result.PilotDeaths = simulation.State.crew.FindAll(crew => crew.isDead && simulation.State.squadrons.Exists(wing => wing.pilotCrewId == crew.id)).Count;
        return result;
    }

    private static void ForceEnemy(GameSimulation simulation)
    {
        var random = simulation.State.random.combat;
        var replacement = ContentCatalog.CreateEnemyById(forcedEnemy, ref random);
        if (replacement == null) throw new ArgumentException("Unknown enemy id: " + forcedEnemy);
        simulation.State.random.combat = random;
        var scale = ContentCatalog.GetRegion(simulation.State.regionIndex).enemyStatMultiplier;
        if (scale > 1f)
        {
            replacement.maxHull = (float)Math.Round(replacement.maxHull * scale, 1); replacement.hull = replacement.maxHull;
            replacement.maxArmor = (float)Math.Round(replacement.maxArmor * scale, 1); replacement.armor = replacement.maxArmor;
            replacement.maxWard = (float)Math.Round(replacement.maxWard * scale, 1); replacement.ward = replacement.maxWard;
        }
        if (simulation.State.isFinalBattle)
        {
            replacement.maxHull += 10f; replacement.hull += 10f;
            replacement.maxArmor += 6f; replacement.armor += 6f;
            replacement.maxWard += 4f; replacement.ward += 4f;
        }
        simulation.State.enemyShip = replacement;
    }

    private static readonly string[] ModulePriority =
    {
        "storm_keel", "reinforced_ribs", "ablative_plating", "aether_shells", "rifled_barrels", "autoloader",
        "damage_control_teams", "ward_lattice", "ward_harmonizer", "escort_doctrine", "extended_hangar", "gunnery_computer"
    };

    private static void ResolvePort(GameSimulation simulation)
    {
        // A seventh and eighth specialist are a meaningful campaign investment; keep a small refit reserve.
        var recruit = simulation.PortRecruitOffer();
        if (recruit != null && CrewProgressionRules.ActiveCrewCount(simulation.State.crew) < CrewProgressionRules.MaxActiveCrew
            && simulation.State.resources.salvage >= recruit.cost + 10)
            simulation.RecruitCrew(recruit.id);

        var bought = true;
        while (bought)
        {
            bought = false;
            var offers = simulation.PortOffers();
            string pick = null;
            foreach (var preferred in ModulePriority)
                if (offers.Contains(preferred) && simulation.State.resources.salvage >= ContentCatalog.GetModule(preferred).cost) { pick = preferred; break; }
            if (pick == null)
            {
                var cheapest = int.MaxValue;
                foreach (var offer in offers)
                {
                    var cost = ContentCatalog.GetModule(offer).cost;
                    if (cost < cheapest && simulation.State.resources.salvage >= cost) { cheapest = cost; pick = offer; }
                }
            }
            if (pick != null && simulation.PurchaseModule(pick).success) bought = true;
        }
        // Then the best affordable weapon if a hardpoint is free (or clearly better than the last one).
        var weaponOffers = simulation.PortWeaponOffers();
        string bestWeapon = null;
        var bestScore = 0f;
        foreach (var offer in weaponOffers)
        {
            var weapon = ContentCatalog.GetWeapon(offer);
            if (weapon == null || simulation.State.resources.salvage < weapon.cost || weapon.ordnancePerShot > 0) continue;
            var score = weapon.damage / weapon.cooldown;
            if (score > bestScore) { bestScore = score; bestWeapon = offer; }
        }
        if (bestWeapon != null)
        {
            var slots = simulation.State.weaponSlots;
            var last = slots.Count > 0 ? ContentCatalog.GetWeapon(slots[slots.Count - 1].weaponId) : null;
            var lastScore = last == null ? 0f : last.damage / last.cooldown;
            if (slots.Count < simulation.State.playerShip.weaponHardpoints || bestScore > lastScore * 1.2f) simulation.PurchaseWeapon(bestWeapon);
        }
        // Finally a same-specialty wing upgrade when clearly better and affordable.
        var wingOffers = simulation.PortWingOffers();
        if (wingOffers.Count > 0)
        {
            var wing = ContentCatalog.GetWing(wingOffers[0]);
            var current = simulation.State.squadrons.Find(sq => (ContentCatalog.GetWing(sq.wingId)?.type ?? sq.type) == wing.type);
            var currentTier = current != null ? (ContentCatalog.GetWing(current.wingId)?.tier ?? 1) : 0;
            if (wing != null && wing.tier > currentTier && simulation.State.resources.salvage >= wing.cost + 10) simulation.PurchaseWing(wing.id);
        }
        simulation.DepartPort();
    }

    private static void ResolveMap(GameSimulation simulation)
    {
        if (simulation.State.playerShip.hull < simulation.State.playerShip.maxHull * 0.72f && simulation.State.resources.salvage >= 5)
            simulation.FieldRepair();
        if (simulation.State.squadrons.Exists(squadron => squadron.strength < squadron.maxStrength) && simulation.State.resources.salvage >= 4)
            simulation.RefitSquadrons();

        var current = simulation.CurrentNode;
        RouteNodeState destination = null;
        for (var i = 0; i < current.connectedIds.Count; i++)
        {
            var candidate = simulation.State.routeNodes.Find(node => node.id == current.connectedIds[i]);
            if (!simulation.CanTravelTo(candidate)) continue;
            if (destination == null || Score(candidate) > Score(destination)) destination = candidate;
        }

        if (destination == null) simulation.EmergencyAetherBurn();
        else simulation.TravelTo(destination.id);
    }

    private static int Score(RouteNodeState node)
    {
        switch (node.encounterType)
        {
            case EncounterType.Rescue: return 7;
            case EncounterType.Salvage: return 8;
            case EncounterType.Trade: return 6;
            case EncounterType.Storm: return 4;
            case EncounterType.Checkpoint: return 3;
            case EncounterType.Battle: return 2;
            case EncounterType.EliteBattle: return 1;
            case EncounterType.Gate: return 10;
            default: return 0;
        }
    }

    private static void ResolveEncounter(GameSimulation simulation)
    {
        var encounter = simulation.ActiveEncounter;
        EncounterChoiceDefinition best = null;
        var bestScore = int.MinValue;
        for (var i = 0; i < encounter.choices.Count; i++)
        {
            var choice = encounter.choices[i];
            if (!simulation.CanChoose(choice)) continue;
            var score = OutcomeScore(choice) - (choice.aetherCost * 5 + choice.suppliesCost * 3 + choice.salvageCost);
            if (choice.successChance < 1f)
            {
                var failure = encounter.choices.Find(item => item.id == choice.failureChoiceId);
                if (failure != null)
                    score = (int)Math.Round(OutcomeScore(choice) * choice.successChance + OutcomeScore(failure) * (1f - choice.successChance))
                            - (choice.aetherCost * 5 + choice.suppliesCost * 3 + choice.salvageCost);
            }
            if (score > bestScore)
            {
                best = choice;
                bestScore = score;
            }
        }
        if (best == null) simulation.SkipEncounter();
        else simulation.ChooseEncounter(best.id);
    }

    private static int OutcomeScore(EncounterChoiceDefinition outcome)
    {
        var score = outcome.moraleDelta * 3 + outcome.survivorDelta / 10 + outcome.aetherDelta * 4 + outcome.suppliesDelta * 3 + outcome.salvageDelta
                    + (int)outcome.hullDelta * 2 + (int)outcome.armorDelta - (int)outcome.instabilityDelta / 5 + (outcome.refitSquadrons ? 6 : 0);
        if (outcome.startsBattle) score -= outcome.battleTier >= 2 ? 20 : 12;
        return score;
    }

    private static float ResolveCombat(GameSimulation simulation, BattleRecord record)
    {
        var elapsed = 0f;
        var overcharged = false;
        var policy = new AuditWingPolicy(wingPolicy);
        Action<CombatLogEntry> observer = entry =>
        {
            if (entry.key == "log.squadron_damaged") record.AirframesLost++;
            if (entry.key == "log.squadron_destroyed") record.WingsDestroyed++;
            if (entry.key == "log.enemy_squadron_hit" || entry.key == "log.boarders") record.UnopposedRaids++;
        };
        simulation.LogAdded += observer;
        var resonator = simulation.State.crew.Find(crew => crew.role == CrewRole.Resonator);
        if (resonator != null) simulation.MoveCrew(resonator.id, ShipSystemType.Weapons);
        // Route spare core output into the weapons room so every mounted weapon can be powered.
        var ship = simulation.State.playerShip;
        var weaponsSystem = ship.GetSystem(ShipSystemType.Weapons);
        // Spare power beyond the mounted weapons still shortens every reload, so route all of it.
        while (weaponsSystem != null && weaponsSystem.power < weaponsSystem.maxPower && ship.AllocatedPower() < ship.coreOutput)
            simulation.ChangePower(ShipSystemType.Weapons, 1);
        var order = wingPolicy == "legacy"
            ? new[] { SquadronType.Bomber, SquadronType.Interceptor }
            : new[] { SquadronType.Interceptor, SquadronType.Escort, SquadronType.Recon, SquadronType.Bomber, SquadronType.Assault };

        while (simulation.State.phase == GamePhase.Combat && elapsed < combatTimeCap)
        {
            simulation.FireAllReady(ShipSystemType.Weapons);

            var cautious = strategy == "cautious";
            if (!cautious)
            {
                // Legacy order is intentionally bomber first. All new modes share defense/recon/offense order.
                foreach (var type in order)
                foreach (var squadron in simulation.State.squadrons)
                {
                    if (squadron.type != type || !policy.TryChoose(simulation.State, squadron, out var mission)) continue;
                    var ordnanceBefore = simulation.State.resources.ordnance;
                    var target = mission == SquadronMission.Intercept ? ShipSystemType.FlightDeck : ShipSystemType.Weapons;
                    if (!simulation.LaunchSquadron(squadron.id, mission, target).success) continue;
                    policy.RecordLaunch(squadron.id);
                    record.Sorties++;
                    record.WingOrdnance += ordnanceBefore - simulation.State.resources.ordnance;
                    if (mission == SquadronMission.Recon) record.ReconSorties++;
                    if (mission == SquadronMission.Intercept) record.InterceptSorties++;
                    if (mission == SquadronMission.Bombard) record.BombardSorties++;
                }
            }

            if (!cautious && !overcharged && simulation.State.playerShip.instability < 55f)
            {
                if (simulation.Overcharge(ShipSystemType.Weapons).success) overcharged = true;
            }

            AssignDamageControl(simulation);
            if (simulation.State.convoy.supportCooldown <= 0 && simulation.State.playerShip.armor < simulation.State.playerShip.maxArmor * 0.5f)
                simulation.UseSupportAbility();

            simulation.SetPaused(false);
            simulation.Tick(0.1f);
            elapsed += 0.1f;
            if (simulation.State.resources.ordnance == 0) record.DryMagazineSeconds += 0.1f;
        }
        simulation.LogAdded -= observer;
        return elapsed;
    }

    private static void AssignDamageControl(GameSimulation simulation)
    {
        RoomState worst = null;
        var worstScore = 1f;
        foreach (var room in simulation.State.playerShip.rooms)
        {
            var system = simulation.State.playerShip.GetSystem(room.system);
            var score = room.fire + room.breach + system.damage;
            if (score <= worstScore) continue;
            worst = room;
            worstScore = score;
        }
        if (worst == null) return;

        var engineer = simulation.State.crew.Find(crew => !crew.isCaptain && crew.IsActive && !crew.onSortie && crew.role == CrewRole.Engineer);
        if (engineer == null) engineer = simulation.State.crew.Find(crew => !crew.isCaptain && crew.IsActive && !crew.onSortie);
        if (engineer != null && engineer.currentRoom != worst.system) simulation.MoveCrew(engineer.id, worst.system);
    }
}
