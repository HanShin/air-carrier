namespace AetherArk.Core
{
    public enum RoomCondition { Operational, Unpowered, Damaged, Disabled }

    public static class BlueprintRules
    {
        public static RoomCondition Classify(ShipSystemState system)
        {
            if (system == null) return RoomCondition.Disabled;
            if (!system.IsOperational) return RoomCondition.Disabled;
            if (system.damage > 0f) return RoomCondition.Damaged;
            if (system.maxPower > 0 && system.power <= 0) return RoomCondition.Unpowered;
            return RoomCondition.Operational;
        }

        public static string CrewInitial(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            return name.Trim().Substring(0, 1).ToUpperInvariant();
        }
    }
}
