using System;
using AetherArk.Core;

internal static class AuditWingPolicyTests
{
    private static int passed;
    public static int Run()
    {
        Check("legacy ignores recon, once uses it only once", () =>
        {
            var state = State(); var wing = state.squadrons[2];
            Expect(!Choose("legacy", state, wing));
            var once = new AuditWingPolicy("once");
            Expect(once.TryChoose(state, wing, out var mission) && mission == SquadronMission.Recon);
            once.RecordLaunch(wing.id);
            Expect(!once.TryChoose(state, wing, out mission));
        });
        Check("all five roles map to their own mission", () =>
        {
            var expected = new[] { SquadronMission.Intercept, SquadronMission.Bombard, SquadronMission.Escort, SquadronMission.Recon, SquadronMission.Assault };
            foreach (SquadronType type in Enum.GetValues(typeof(SquadronType))) Expect(AuditWingPolicy.MissionFor(type) == expected[(int)type]);
        });
        Check("readiness, power, ammo and living pilot required", () =>
        {
            var state = State(); var wing = state.squadrons[1];
            Expect(Choose("always", state, wing));
            wing.status = SquadronStatus.Recovering; Expect(!Choose("always", state, wing));
            wing.status = SquadronStatus.Ready; state.resources.ordnance = 0; Expect(!Choose("always", state, wing));
            state.resources.ordnance = 8; state.crew.Find(c => c.id == wing.pilotCrewId).isDead = true; Expect(!Choose("always", state, wing));
            state.crew.Find(c => c.id == wing.pilotCrewId).isDead = false;
            state.playerShip.GetSystem(ShipSystemType.FlightDeck).power = 0; Expect(!Choose("always", state, wing));
        });
        Check("healthy stops at the first lost airframe; adaptive preserves the last", () =>
        {
            var state = State(); var wing = state.squadrons[1];
            Expect(Choose("healthy", state, wing));
            wing.strength--; Expect(!Choose("healthy", state, wing)); Expect(Choose("adaptive", state, wing));
            wing.strength = 1; Expect(!Choose("adaptive", state, wing));
        });
        Check("adaptive does not intercept a disabled enemy flight deck", () =>
        {
            var state = State(); var wing = state.squadrons[0];
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 1;
            state.interceptCharges = 0; Expect(Choose("adaptive", state, wing));
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).disabledSeconds = 2;
            Expect(!Choose("adaptive", state, wing));
        });
        Check("adaptive avoids full defense buffers and duplicate missions", () =>
        {
            var state = State(); var wing = state.squadrons[0];
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 1;
            state.interceptCharges = 3; Expect(!Choose("adaptive", state, wing));
            state.interceptCharges = 0;
            state.squadrons[1].status = SquadronStatus.OnMission;
            state.squadrons[1].mission = SquadronMission.Escort;
            Expect(!Choose("adaptive", state, wing));
        });
        Check("adaptive reserves defensive ammo", () =>
        {
            var state = State(); var bomber = state.squadrons[1];
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 1;
            state.resources.ordnance = 3; Expect(!Choose("adaptive", state, bomber));
            state.resources.ordnance = 4; Expect(Choose("adaptive", state, bomber));
            state.enemyShip.GetSystem(ShipSystemType.FlightDeck).power = 0;
            state.resources.ordnance = 2; Expect(Choose("adaptive", state, bomber));
        });
        Check("adaptive recon does not waste an active buff or the only repair engineer", () =>
        {
            var state = State(); var recon = state.squadrons[2];
            state.reconBonusSeconds = 20; Expect(!Choose("adaptive", state, recon));
            state.reconBonusSeconds = 0; Expect(Choose("adaptive", state, recon));
            state.playerShip.GetSystem(ShipSystemType.Weapons).damage = 40; Expect(!Choose("adaptive", state, recon));
        });
        Check("choice is read-only and deterministic", () =>
        {
            var state = State(); var policy = new AuditWingPolicy("adaptive"); var wing = state.squadrons[1];
            var rng = state.random.combat; var ammo = state.resources.ordnance; var strength = wing.strength;
            var first = policy.TryChoose(state, wing, out var mission);
            for (var i = 0; i < 20; i++) Expect(policy.TryChoose(state, wing, out var next) == first && next == mission);
            Expect(state.random.combat == rng && state.resources.ordnance == ammo && wing.strength == strength && wing.status == SquadronStatus.Ready);
        });
        Console.WriteLine($"Wing policy tests passed: {passed}");
        return 0;
    }

    private static RunState State()
    {
        var simulation = GameSimulation.NewRun(new ProfileState { tutorialSeen = true, campaignVictories = 1, flagshipId = "ship_zephyr" }, 17000);
        simulation.BeginCombat(1, false);
        return simulation.State;
    }
    private static bool Choose(string mode, RunState state, SquadronState wing) => new AuditWingPolicy(mode).TryChoose(state, wing, out _);
    private static void Expect(bool condition) { if (!condition) throw new Exception("Wing policy assertion failed"); }
    private static void Check(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception exception) { throw new Exception(name, exception); }
    }
}
