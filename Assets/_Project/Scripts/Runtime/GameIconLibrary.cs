using System.Collections.Generic;
using AetherArk.Core;
using UnityEngine;

namespace AetherArk.Runtime
{
    /// <summary>Shared category icons for equipment cards and combat commands.</summary>
    public static class GameIconLibrary
    {
        private const string Root = "Art/Icons/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Module(ModuleCategory category) => Load("Modules/" + category.ToString().ToLowerInvariant());
        public static Sprite Weapon(WeaponFamily family) => Load("Weapons/" + family.ToString().ToLowerInvariant());
        public static Sprite Wing(SquadronType type) => Load("Wings/" + type.ToString().ToLowerInvariant());

        public static Sprite Mission(SquadronMission mission)
        {
            switch (mission)
            {
                case SquadronMission.Intercept: return Wing(SquadronType.Interceptor);
                case SquadronMission.Bombard: return Wing(SquadronType.Bomber);
                case SquadronMission.Escort: return Wing(SquadronType.Escort);
                case SquadronMission.Recon: return Wing(SquadronType.Recon);
                case SquadronMission.Assault: return Wing(SquadronType.Assault);
                default: return null;
            }
        }

        private static Sprite Load(string path)
        {
            if (Cache.TryGetValue(path, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>(Root + path);
            Cache[path] = sprite;
            return sprite;
        }
    }
}
