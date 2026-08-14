namespace AetherArk.Core
{
    public readonly struct CommandResult
    {
        public readonly bool success;
        public readonly string messageKey;

        public CommandResult(bool successValue, string messageKeyValue)
        {
            success = successValue;
            messageKey = messageKeyValue;
        }

        public static CommandResult Ok(string key = "command.ok") => new CommandResult(true, key);
        public static CommandResult Fail(string key) => new CommandResult(false, key);
    }

    public interface IGameCommand
    {
        CommandResult Execute(GameSimulation simulation);
    }

    public sealed class SetPowerCommand : IGameCommand
    {
        private readonly ShipSystemType system;
        private readonly int delta;

        public SetPowerCommand(ShipSystemType systemValue, int deltaValue)
        {
            system = systemValue;
            delta = deltaValue;
        }

        public CommandResult Execute(GameSimulation simulation) => simulation.ChangePower(system, delta);
    }

    public sealed class FireWeaponCommand : IGameCommand
    {
        private readonly ShipSystemType target;
        public FireWeaponCommand(ShipSystemType targetValue) => target = targetValue;
        public CommandResult Execute(GameSimulation simulation) => simulation.FireMainWeapon(target);
    }

    public sealed class LaunchSquadronCommand : IGameCommand
    {
        private readonly string squadronId;
        private readonly SquadronMission mission;
        private readonly ShipSystemType target;

        public LaunchSquadronCommand(string squadronIdValue, SquadronMission missionValue, ShipSystemType targetValue)
        {
            squadronId = squadronIdValue;
            mission = missionValue;
            target = targetValue;
        }

        public CommandResult Execute(GameSimulation simulation) => simulation.LaunchSquadron(squadronId, mission, target);
    }

    public sealed class ChangeAltitudeCommand : IGameCommand
    {
        private readonly AltitudeBand altitude;
        public ChangeAltitudeCommand(AltitudeBand altitudeValue) => altitude = altitudeValue;
        public CommandResult Execute(GameSimulation simulation) => simulation.ChangeAltitude(altitude);
    }

    public sealed class MoveCrewCommand : IGameCommand
    {
        private readonly string crewId;
        private readonly ShipSystemType room;
        public MoveCrewCommand(string crewIdValue, ShipSystemType roomValue)
        {
            crewId = crewIdValue;
            room = roomValue;
        }

        public CommandResult Execute(GameSimulation simulation) => simulation.MoveCrew(crewId, room);
    }

    public sealed class OverchargeCommand : IGameCommand
    {
        private readonly ShipSystemType system;
        public OverchargeCommand(ShipSystemType systemValue) => system = systemValue;
        public CommandResult Execute(GameSimulation simulation) => simulation.Overcharge(system);
    }
}
