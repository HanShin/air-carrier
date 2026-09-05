using System;

namespace AetherArk.Core
{
    /// <summary>
    /// Deterministic state setups used by development-build flags to inspect UI states
    /// that are otherwise only reachable mid-battle.
    /// </summary>
    public static class DebugScenarios
    {
        public static void ApplyDamageShowcase(RunState state)
        {
            if (state == null || state.playerShip == null) return;
            var ship = state.playerShip;
            ship.ward = 0f;
            ship.armor = Math.Min(ship.armor, ship.maxArmor * 0.4f);
            ship.hull = Math.Min(ship.hull, ship.maxHull * 0.7f);

            var weapons = ship.GetSystem(ShipSystemType.Weapons);
            if (weapons != null) weapons.damage = 45f;
            var weaponsRoom = ship.GetRoom(ShipSystemType.Weapons);
            if (weaponsRoom != null) weaponsRoom.fire = 40f;

            var enginesRoom = ship.GetRoom(ShipSystemType.Engines);
            if (enginesRoom != null) enginesRoom.breach = 35f;

            var bridgeRoom = ship.GetRoom(ShipSystemType.Bridge);
            if (bridgeRoom != null) bridgeRoom.intruders = 2;

            var lifeRoom = ship.GetRoom(ShipSystemType.LifeSupport);
            if (lifeRoom != null) lifeRoom.oxygen = 18f;

            var sensors = ship.GetSystem(ShipSystemType.Sensors);
            if (sensors != null) sensors.damage = sensors.maxDamage;

            var infirmary = ship.GetSystem(ShipSystemType.Infirmary);
            if (infirmary != null) infirmary.power = 0;

            var downed = state.crew.Find(crew => crew.IsActive && !crew.isCaptain && crew.role == CrewRole.Medic)
                         ?? state.crew.Find(crew => crew.IsActive && !crew.isCaptain);
            if (downed != null)
            {
                downed.health = 0f;
                downed.downedSeconds = 20f;
                CrewMovementRules.Stop(downed);
            }

            var enemy = state.enemyShip;
            if (enemy == null) return;
            var enemyWard = enemy.GetSystem(ShipSystemType.Ward);
            if (enemyWard != null) enemyWard.damage = 60f;
            var enemyDeck = enemy.GetRoom(ShipSystemType.FlightDeck);
            if (enemyDeck != null) enemyDeck.fire = 30f;
        }
    }
}
