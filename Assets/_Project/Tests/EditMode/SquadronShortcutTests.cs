using AetherArk.Core;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace AetherArk.Tests
{
    public sealed class SquadronShortcutTests
    {
        [TestCase(0)]
        [TestCase(1)]
        public void OriginalTwoSlotsKeepBombardRegardlessOfWing(int slot)
        {
            Assert.That(SquadronShortcuts.MissionFor(slot, new SquadronState { wingId = "far_eyes" }), Is.EqualTo(SquadronMission.Bombard));
        }

        [TestCase("kestrel_interceptors", SquadronMission.Intercept)]
        [TestCase("ember_bombers", SquadronMission.Bombard)]
        [TestCase("sky_wardens", SquadronMission.Escort)]
        [TestCase("far_eyes", SquadronMission.Recon)]
        [TestCase("storm_marines", SquadronMission.Assault)]
        public void AdditionalSlotsUseEquippedWingEvenWithStaleCachedType(string wing, SquadronMission mission)
        {
            var squadron = new SquadronState { wingId = wing, type = (SquadronType)999 };
            var before = JsonUtility.ToJson(squadron);
            for (var slot = 2; slot < SquadronShortcuts.MaxSlots; slot++)
                Assert.That(SquadronShortcuts.MissionFor(slot, squadron), Is.EqualTo(mission));
            Assert.That(JsonUtility.ToJson(squadron), Is.EqualTo(before));
        }

        [TestCase(SquadronType.Interceptor, SquadronMission.Intercept)]
        [TestCase(SquadronType.Bomber, SquadronMission.Bombard)]
        [TestCase(SquadronType.Escort, SquadronMission.Escort)]
        [TestCase(SquadronType.Recon, SquadronMission.Recon)]
        [TestCase(SquadronType.Assault, SquadronMission.Assault)]
        [TestCase((SquadronType)999, SquadronMission.None)]
        public void MissingWingDefinitionFallsBackToStoredSpecialty(SquadronType type, SquadronMission mission)
        {
            Assert.That(SquadronShortcuts.MissionFor(2, new SquadronState { wingId = "missing", type = type }), Is.EqualTo(mission));
        }

        [TestCase(-1)]
        [TestCase(9)]
        public void UnsupportedSlotsHaveNoShortcut(int slot)
        {
            Assert.That(SquadronShortcuts.MissionFor(slot, new SquadronState()), Is.EqualTo(SquadronMission.None));
        }

        [Test]
        public void EmptyBayHasNoShortcut()
        {
            Assert.That(SquadronShortcuts.MissionFor(2, null), Is.EqualTo(SquadronMission.None));
        }
    }
}
