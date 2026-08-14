using System;
using System.Collections.Generic;
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
    }

    private static int Main(string[] args)
    {
        var runCount = args.Length > 0 ? int.Parse(args[0]) : 100;
        var difficulty = args.Length > 1 ? (Difficulty)Enum.Parse(typeof(Difficulty), args[1], true) : Difficulty.Standard;
        var baseSeed = args.Length > 2 ? int.Parse(args[2]) : 17000;
        var results = new List<Result>();
        for (var i = 0; i < runCount; i++) results.Add(Play(baseSeed + i * 7919, difficulty));

        var victories = results.FindAll(result => result.Victory);
        if (victories.Count == 0)
        {
            Console.Error.WriteLine("No headless playthrough reached the Sky Gate.");
            foreach (var result in results)
                Console.Error.WriteLine($"LOSS seed={result.Seed} reason={result.Defeat} jumps={result.Jumps} battles={result.Battles} combat={result.ActiveCombatSeconds:0}s morale={result.Morale} survivors={result.Survivors}");
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

        Console.WriteLine("Headless first-run audit");
        Console.WriteLine($"Difficulty: {difficulty}");
        Console.WriteLine($"Runs: {results.Count}");
        Console.WriteLine($"Victories: {victories.Count} ({victories.Count * 100f / results.Count:0.0}%)");
        Console.WriteLine($"Seven-jump completions: {victories.FindAll(result => result.Jumps == 7).Count}/{victories.Count}");
        Console.WriteLine($"Battles per victory: {minBattles}–{maxBattles}");
        Console.WriteLine($"Average active combat simulation: {combatSeconds / victories.Count / 60f:0.0} min");
        Console.WriteLine($"Estimated human first-run duration: {estimatedHumanMinutes / victories.Count:0.0} min");
        Console.WriteLine("Human playtime target adds pause/targeting, event reading, route planning, setup, and repair decisions.");

        var printedLosses = 0;
        foreach (var result in results.FindAll(item => !item.Victory))
        {
            if (printedLosses++ >= 10) break;
            Console.WriteLine($"LOSS seed={result.Seed} reason={result.Defeat} jumps={result.Jumps} battles={result.Battles} combat={result.ActiveCombatSeconds:0}s morale={result.Morale} survivors={result.Survivors}");
        }

        return victories.Count >= Math.Max(1, runCount / 4) ? 0 : 2;
    }

    private static Result Play(int seed, Difficulty difficulty)
    {
        var profile = new ProfileState
        {
            captainName = "Audit Captain",
            captainLineage = CrewLineage.Human,
            difficulty = difficulty,
            supportShip = SupportShipType.Workshop
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
                case GamePhase.Combat:
                    result.Battles++;
                    result.ActiveCombatSeconds += ResolveCombat(simulation);
                    break;
            }
        }

        result.Victory = simulation.State.phase == GamePhase.Victory;
        result.Defeat = simulation.State.defeatReason;
        result.Jumps = simulation.State.travelCount;
        result.Survivors = simulation.State.convoy.survivors;
        result.Morale = simulation.State.convoy.morale;
        return result;
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
            var score = choice.moraleDelta * 3 + choice.survivorDelta / 10 + choice.aetherDelta * 4 + choice.suppliesDelta * 3 + choice.salvageDelta;
            score -= choice.aetherCost * 5 + choice.suppliesCost * 3 + choice.salvageCost;
            if (choice.startsBattle) score -= 12;
            if (score > bestScore)
            {
                best = choice;
                bestScore = score;
            }
        }
        if (best == null) simulation.SkipEncounter();
        else simulation.ChooseEncounter(best.id);
    }

    private static float ResolveCombat(GameSimulation simulation)
    {
        var elapsed = 0f;
        var overcharged = false;
        var launchedBomber = false;
        var launchedInterceptor = false;
        var resonator = simulation.State.crew.Find(crew => crew.role == CrewRole.Resonator);
        if (resonator != null) simulation.MoveCrew(resonator.id, ShipSystemType.Weapons);

        while (simulation.State.phase == GamePhase.Combat && elapsed < 420f)
        {
            if (simulation.State.playerWeaponCooldown <= 0f)
                simulation.FireMainWeapon(ShipSystemType.Weapons);

            var bomber = simulation.State.squadrons.Find(squadron => squadron.type == SquadronType.Bomber);
            if (!launchedBomber && bomber != null && bomber.CanLaunch && simulation.State.resources.ordnance >= bomber.ordnanceCost)
            {
                if (simulation.LaunchSquadron(bomber.id, SquadronMission.Bombard, ShipSystemType.Weapons).success) launchedBomber = true;
            }

            var interceptor = simulation.State.squadrons.Find(squadron => squadron.type == SquadronType.Interceptor);
            if (!launchedInterceptor && interceptor != null && interceptor.CanLaunch && simulation.State.resources.ordnance >= interceptor.ordnanceCost)
            {
                if (simulation.LaunchSquadron(interceptor.id, SquadronMission.Intercept, ShipSystemType.FlightDeck).success) launchedInterceptor = true;
            }

            if (!overcharged && simulation.State.playerShip.instability < 55f)
            {
                if (simulation.Overcharge(ShipSystemType.Weapons).success) overcharged = true;
            }

            AssignDamageControl(simulation);
            if (simulation.State.convoy.supportCooldown <= 0 && simulation.State.playerShip.armor < simulation.State.playerShip.maxArmor * 0.5f)
                simulation.UseSupportAbility();

            simulation.SetPaused(false);
            simulation.Tick(0.1f);
            elapsed += 0.1f;
        }
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
