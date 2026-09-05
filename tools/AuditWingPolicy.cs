using System;
using System.Collections.Generic;
using AetherArk.Content;
using AetherArk.Core;

// Audit-only policies. These issue ordinary commands; they never patch combat values or RNG.
internal sealed class AuditWingPolicy
{
    public static readonly string[] Modes = { "legacy", "once", "always", "healthy", "adaptive" };
    private readonly string mode;
    private readonly HashSet<string> launched = new HashSet<string>();

    public AuditWingPolicy(string value)
    {
        if (Array.IndexOf(Modes, value) < 0) throw new ArgumentException("Unknown wing policy: " + value);
        mode = value;
    }

    public bool TryChoose(RunState state, SquadronState squadron, out SquadronMission mission)
    {
        mission = MissionFor(squadron.type);
        if (state.phase != GamePhase.Combat || state.enemyShip == null || (mode != "legacy" && state.enemyShip.IsDestroyed) || !squadron.CanLaunch) return false;
        if ((state.playerShip.GetSystem(ShipSystemType.FlightDeck)?.EffectivePower ?? 0) <= 0) return false;
        if (mode == "legacy" && squadron.type != SquadronType.Bomber && squadron.type != SquadronType.Interceptor) return false;
        if ((mode == "legacy" || mode == "once") && launched.Contains(squadron.id)) return false;
        var cost = ContentCatalog.GetWing(squadron.wingId)?.ordnanceCost ?? squadron.ordnanceCost;
        if (state.resources.ordnance < cost) return false;
        // Preserve legacy behavior exactly for ablation; new policies do not knowingly launch without a pilot.
        var pilot = state.crew.Find(crew => crew.id == squadron.pilotCrewId);
        if (mode != "legacy" && (pilot == null || !pilot.IsActive)) return false;
        if (mode == "healthy" && squadron.strength < squadron.maxStrength) return false;
        if (mode != "adaptive") return true;

        // Never risk the last airframe. Recon starts with two; larger wings retain at least 60% strength.
        if (squadron.strength < Math.Max(2, (int)Math.Ceiling(squadron.maxStrength * 0.6))) return false;
        // Far Eyes borrows the starting engineer: keep them aboard while serious damage needs attention.
        if (pilot.role == CrewRole.Engineer && NeedsEngineer(state)) return false;
        var enemyDeck = state.enemyShip.GetSystem(ShipSystemType.FlightDeck);
        var airThreat = enemyDeck != null && enemyDeck.EffectivePower > 0;
        if (mission == SquadronMission.Intercept)
            return airThreat && state.interceptCharges <= 1 && !PendingDefense(state);
        if (mission == SquadronMission.Escort)
            return (state.playerShip.ward < state.playerShip.maxWard * 0.6f || (airThreat && state.interceptCharges <= 1))
                && !PendingDefense(state);
        if (mission == SquadronMission.Recon)
            return state.reconBonusSeconds <= 5f && !Pending(state, SquadronMission.Recon);

        // Reserve two basic defensive sorties when a live enemy flight deck still threatens the ship.
        var reserve = airThreat && state.squadrons.Exists(wing =>
            (wing.type == SquadronType.Interceptor || wing.type == SquadronType.Escort) && wing.strength >= 2) ? 2 : 0;
        return state.resources.ordnance >= cost + reserve
            && state.enemyShip.hull + state.enemyShip.armor + state.enemyShip.ward > 6f;
    }

    public void RecordLaunch(string id) => launched.Add(id);

    public static SquadronMission MissionFor(SquadronType type)
    {
        switch (type)
        {
            case SquadronType.Interceptor: return SquadronMission.Intercept;
            case SquadronType.Escort: return SquadronMission.Escort;
            case SquadronType.Recon: return SquadronMission.Recon;
            case SquadronType.Assault: return SquadronMission.Assault;
            default: return SquadronMission.Bombard;
        }
    }

    private static bool PendingDefense(RunState state) => Pending(state, SquadronMission.Intercept) || Pending(state, SquadronMission.Escort);
    private static bool Pending(RunState state, SquadronMission mission) => state.squadrons.Exists(wing =>
        (wing.status == SquadronStatus.Launching || wing.status == SquadronStatus.OnMission) && wing.mission == mission);

    private static bool NeedsEngineer(RunState state)
    {
        if (state.crew.Exists(crew => crew.role == CrewRole.Engineer && crew.IsActive &&
            !state.squadrons.Exists(wing => wing.type == SquadronType.Recon && wing.pilotCrewId == crew.id))) return false;
        return state.playerShip.rooms.Exists(room => room.fire + room.breach + state.playerShip.GetSystem(room.system).damage >= 35f);
    }
}
