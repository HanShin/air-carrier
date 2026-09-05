using AetherArk.Content;
using AetherArk.Core;

namespace AetherArk.Runtime
{
    /// <summary>Shared by number-key dispatch and mission badges; never changes simulation state.</summary>
    public static class SquadronShortcuts
    {
        public const int MaxSlots = 9;

        public static SquadronMission MissionFor(int slot, SquadronState squadron)
        {
            if (slot < 0 || slot >= MaxSlots || squadron == null) return SquadronMission.None;
            // Preserve the original 1/2 bombard shortcuts. Additional bays follow the equipped wing's specialty.
            if (slot < 2) return SquadronMission.Bombard;
            var type = ContentCatalog.GetWing(squadron.wingId)?.type ?? squadron.type;
            switch (type)
            {
                case SquadronType.Interceptor: return SquadronMission.Intercept;
                case SquadronType.Bomber: return SquadronMission.Bombard;
                case SquadronType.Escort: return SquadronMission.Escort;
                case SquadronType.Recon: return SquadronMission.Recon;
                case SquadronType.Assault: return SquadronMission.Assault;
                default: return SquadronMission.None;
            }
        }
    }
}
