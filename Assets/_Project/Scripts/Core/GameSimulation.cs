using System;
using System.Collections.Generic;
using AetherArk.Content;

namespace AetherArk.Core
{
    public sealed class GameSimulation
    {
        public const int FirstExpeditionSeed = 32838;
        public RunState State { get; private set; }

        public GameSimulation(RunState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public static GameSimulation NewRun(ProfileState profile, int seed)
        {
            var state = new RunState
            {
                seed = seed,
                isFirstExpedition = !profile.tutorialSeen,
                regionCount = profile.tutorialSeen ? ContentCatalog.RegionCount : 1,
                difficulty = profile.difficulty,
                autoPauseOnWarning = profile.accessibility.autoPauseOnWarning,
                playerShip = ContentCatalog.CreateVanguard(),
                crew = ContentCatalog.CreateCrew(profile),
                squadrons = ContentCatalog.CreateSquadrons(),
                routeNodes = ContentCatalog.CreateRoute(seed),
                convoy = new ConvoyState { supportShip = profile.supportShip },
                random = new RandomStreamsState
                {
                    route = SeededRandom.Seed(seed, 0xA11CEu),
                    combat = SeededRandom.Seed(seed, 0xC0B47u),
                    events = SeededRandom.Seed(seed, 0xE7E17u)
                }
            };

            if (profile.tutorialSeen) ContentCatalog.AssignEncounterVariants(state.routeNodes, seed);

            if (profile.difficulty == Difficulty.Story)
            {
                state.resources.aether += 3;
                state.resources.supplies += 4;
                state.playerShip.hull += 6f;
                state.playerShip.maxHull += 6f;
            }
            else if (profile.difficulty == Difficulty.Harsh)
            {
                state.resources.aether -= 2;
                state.resources.supplies -= 2;
                state.convoy.morale -= 10;
            }

            return new GameSimulation(state);
        }

        public CommandResult Execute(IGameCommand command)
        {
            return command == null ? CommandResult.Fail("command.invalid") : command.Execute(this);
        }

        public RouteNodeState CurrentNode => State.routeNodes.Find(node => node.id == State.currentNodeId);
        public EncounterDefinition ActiveEncounter => ContentCatalog.GetEncounter(State.activeEncounterId);

        public int TravelCost(RouteNodeState target)
        {
            if (target == null) return 0;
            return ModuleRules.Modifiers(State).aetherDiscount ? Math.Max(1, target.aetherCost - 1) : target.aetherCost;
        }

        public bool CanTravelTo(RouteNodeState target)
        {
            if (target == null || State.phase != GamePhase.RouteMap || target.blocked || target.visited) return false;
            var current = CurrentNode;
            return current != null && current.connectedIds.Contains(target.id) && State.resources.aether >= TravelCost(target);
        }

        public bool HasAffordableRoute()
        {
            var current = CurrentNode;
            if (current == null || State.phase != GamePhase.RouteMap) return false;
            for (var i = 0; i < State.routeNodes.Count; i++)
            {
                if (CanTravelTo(State.routeNodes[i])) return true;
            }
            return false;
        }

        public CommandResult TravelTo(string nodeId)
        {
            var target = State.routeNodes.Find(node => node.id == nodeId);
            if (!CanTravelTo(target)) return CommandResult.Fail("command.route_unavailable");

            State.resources.aether -= TravelCost(target);
            State.currentNodeId = target.id;
            target.visited = true;
            State.travelCount++;
            State.totalTravelCount++;
            State.currentWeather = target.weather;
            if (State.convoy.supportCooldown > 0) State.convoy.supportCooldown--;

            if (State.travelCount % 2 == 0 && State.resources.supplies > 0) State.resources.supplies--;
            if (State.resources.supplies <= 0)
            {
                State.convoy.survivors -= 25;
                State.convoy.morale -= 8;
                AddLog("log.convoy_starving");
            }

            State.stormColumn = Math.Max(-1, State.travelCount - 3);
            for (var i = 0; i < State.routeNodes.Count; i++)
            {
                var node = State.routeNodes[i];
                node.blocked = !node.visited && node.column <= State.stormColumn;
            }

            if (target.encounterType == EncounterType.Battle || target.encounterType == EncounterType.EliteBattle)
            {
                BeginCombat(target.encounterType == EncounterType.EliteBattle ? 2 : 1, false);
            }
            else if (target.encounterType == EncounterType.Gate)
            {
                BeginCombat(2, true);
            }
            else
            {
                State.activeEncounterId = target.encounterId;
                State.phase = GamePhase.Encounter;
            }

            CheckDefeat();
            return CommandResult.Ok("command.travelled");
        }

        public bool CanChoose(EncounterChoiceDefinition choice)
        {
            if (choice == null || choice.hidden || State.phase != GamePhase.Encounter) return false;
            if (State.resources.aether < choice.aetherCost) return false;
            if (State.resources.supplies < choice.suppliesCost) return false;
            if (State.resources.ordnance < choice.ordnanceCost) return false;
            if (State.resources.salvage < choice.salvageCost) return false;
            return string.IsNullOrEmpty(choice.requiredTag) || HasTag(choice.requiredTag);
        }

        public CommandResult ChooseEncounter(string choiceId)
        {
            var encounter = ActiveEncounter;
            var choice = encounter?.choices.Find(item => item.id == choiceId);
            if (!CanChoose(choice)) return CommandResult.Fail("command.choice_unavailable");

            var outcome = choice;
            if (choice.successChance < 1f)
            {
                var random = State.random.events;
                var success = SeededRandom.Chance(ref random, choice.successChance);
                State.random.events = random;
                if (!success)
                {
                    var failure = encounter.choices.Find(item => item.id == choice.failureChoiceId);
                    if (failure != null) outcome = failure;
                }
            }

            // Costs are always paid by the chosen option; rewards come from the resolved outcome.
            State.resources.aether -= choice.aetherCost;
            State.resources.supplies -= choice.suppliesCost;
            State.resources.ordnance -= choice.ordnanceCost;
            State.resources.salvage -= choice.salvageCost;
            ApplyOutcome(outcome);
            AddLog(outcome.resultKey);

            State.activeEncounterId = null;
            if (outcome.startsBattle) BeginCombat(Math.Max(1, outcome.battleTier), false);
            else State.phase = GamePhase.RouteMap;
            CheckDefeat();
            return CommandResult.Ok(outcome.resultKey);
        }

        private void ApplyOutcome(EncounterChoiceDefinition outcome)
        {
            State.resources.aether += outcome.aetherDelta;
            State.resources.supplies += outcome.suppliesDelta;
            State.resources.ordnance += outcome.ordnanceDelta;
            State.resources.salvage += outcome.salvageDelta;
            State.convoy.survivors = Math.Max(0, State.convoy.survivors + outcome.survivorDelta);
            State.convoy.morale = ClampInt(State.convoy.morale + outcome.moraleDelta, 0, 100);

            var ship = State.playerShip;
            if (outcome.hullDelta != 0f) ship.hull = Clamp(ship.hull + outcome.hullDelta, 0f, ship.maxHull);
            if (outcome.armorDelta != 0f) ship.armor = Clamp(ship.armor + outcome.armorDelta, 0f, ship.maxArmor);
            if (outcome.instabilityDelta != 0f) ship.instability = Clamp(ship.instability + outcome.instabilityDelta, 0f, 100f);
            if (outcome.refitSquadrons)
            {
                for (var i = 0; i < State.squadrons.Count; i++)
                {
                    var squadron = State.squadrons[i];
                    squadron.strength = squadron.maxStrength;
                    if (squadron.status == SquadronStatus.Destroyed) squadron.status = SquadronStatus.Ready;
                }
            }

            if (outcome.id == "repair") RepairAtPort();
            if (outcome.id == "high") ship.altitude = AltitudeBand.High;
            if (outcome.id == "ride") ship.instability = Math.Max(0f, ship.instability - 20f);
        }

        public CommandResult PurchaseModule(string moduleId)
        {
            if (State.phase != GamePhase.Port) return CommandResult.Fail("command.port_only");
            var module = ContentCatalog.GetModule(moduleId);
            if (module == null) return CommandResult.Fail("command.module_unknown");
            if (State.installedModules.Contains(moduleId)) return CommandResult.Fail("command.module_installed");
            if (State.installedModules.Count >= State.playerShip.moduleSlots) return CommandResult.Fail("command.module_slots");
            if (State.resources.salvage < module.cost) return CommandResult.Fail("command.module_cost");
            State.resources.salvage -= module.cost;
            State.installedModules.Add(moduleId);
            ModuleRules.ApplyFlatBonuses(State, module);
            AddLog("log.module_installed", module.nameKey);
            return CommandResult.Ok("command.module_bought");
        }

        public List<string> PortOffers()
        {
            return ContentCatalog.OfferModules(State.seed, State.regionIndex, State.installedModules);
        }

        public CommandResult DepartPort()
        {
            if (State.phase != GamePhase.Port) return CommandResult.Fail("command.invalid_phase");
            State.phase = GamePhase.RouteMap;
            return CommandResult.Ok();
        }

        public CommandResult SkipEncounter()
        {
            if (State.phase != GamePhase.Encounter) return CommandResult.Fail("command.invalid_phase");
            State.activeEncounterId = null;
            State.phase = GamePhase.RouteMap;
            return CommandResult.Ok();
        }

        private bool HasTag(string tag)
        {
            if (tag == "support.pathfinder") return State.convoy.supportShip == SupportShipType.Pathfinder;
            if (tag == "support.workshop") return State.convoy.supportShip == SupportShipType.Workshop;
            if (tag == "support.hospital") return State.convoy.supportShip == SupportShipType.Hospital;
            if (tag.StartsWith("lineage.", StringComparison.Ordinal))
            {
                var name = tag.Substring("lineage.".Length);
                for (var i = 0; i < State.crew.Count; i++)
                {
                    if (!State.crew[i].isDead && string.Equals(State.crew[i].lineage.ToString(), name, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        private void RepairAtPort()
        {
            var ship = State.playerShip;
            ship.hull = Math.Min(ship.maxHull, ship.hull + 8f);
            ship.armor = Math.Min(ship.maxArmor, ship.armor + 6f);
            for (var i = 0; i < ship.systems.Count; i++) ship.systems[i].damage = Math.Max(0f, ship.systems[i].damage - 20f);
        }

        public void BeginCombat(int tier, bool finalBattle)
        {
            var combatState = State.random.combat;
            State.enemyShip = ContentCatalog.CreateEnemy(tier, !State.isFirstExpedition, ref combatState);
            var scale = ContentCatalog.GetRegion(State.regionIndex).enemyStatMultiplier;
            if (scale > 1f)
            {
                var enemy = State.enemyShip;
                enemy.maxHull = (float)Math.Round(enemy.maxHull * scale, 1); enemy.hull = enemy.maxHull;
                enemy.maxArmor = (float)Math.Round(enemy.maxArmor * scale, 1); enemy.armor = enemy.maxArmor;
                enemy.maxWard = (float)Math.Round(enemy.maxWard * scale, 1); enemy.ward = enemy.maxWard;
            }
            if (finalBattle)
            {
                // The first gate is only lightly reinforced; later gates stack more on top of region scaling.
                var step = Math.Max(0, State.regionIndex - 1);
                var hullBonus = 6f + 2f * step;
                var armorBonus = 3f + step;
                var wardBonus = 2f + step;
                State.enemyShip.maxHull += hullBonus; State.enemyShip.hull += hullBonus;
                State.enemyShip.maxArmor += armorBonus; State.enemyShip.armor += armorBonus;
                State.enemyShip.maxWard += wardBonus; State.enemyShip.ward += wardBonus;
            }
            State.random.combat = combatState;
            State.phase = GamePhase.Combat;
            State.isFinalBattle = finalBattle;
            State.isPaused = true;
            State.combatElapsed = 0f;
            State.playerWeaponCooldown = 0f;
            State.enemyWeaponCooldown = finalBattle ? 3f : 4.5f;
            State.enemySquadronCooldown = 10f;
            State.altitudeCooldown = 0f;
            State.weatherHazardTimer = ContentCatalog.GetWeather(State.currentWeather).hazardInterval;
            var startModifiers = ModuleRules.Modifiers(State);
            State.interceptCharges = startModifiers.interceptCharges;
            State.reconBonusSeconds = startModifiers.reconSeconds;
            State.combatLog.Clear();
            ClearCombatAlert();
            AddLog(finalBattle ? "log.gate_battle" : "log.combat_started");
        }

        public void SetPaused(bool paused)
        {
            if (State.phase != GamePhase.Combat) return;
            State.isPaused = paused;
            if (!paused && State.combatAlertPausedBattle) State.combatAlertPausedBattle = false;
        }

        public void TogglePause()
        {
            SetPaused(!State.isPaused);
        }

        public void Tick(float deltaTime)
        {
            if (State.phase != GamePhase.Combat || State.isPaused || deltaTime <= 0f) return;
            var dt = Math.Min(deltaTime, 0.1f);
            State.combatElapsed += dt;
            State.playerWeaponCooldown = Math.Max(0f, State.playerWeaponCooldown - dt);
            State.enemyWeaponCooldown = Math.Max(0f, State.enemyWeaponCooldown - dt);
            State.enemySquadronCooldown = Math.Max(0f, State.enemySquadronCooldown - dt);
            State.altitudeCooldown = Math.Max(0f, State.altitudeCooldown - dt);
            State.reconBonusSeconds = Math.Max(0f, State.reconBonusSeconds - dt);
            State.weatherHazardTimer -= dt;
            State.combatAlertSeconds = Math.Max(0f, State.combatAlertSeconds - dt);
            if (State.combatAlertSeconds <= 0f) ClearCombatAlert();

            TickShipSystems(State.playerShip, dt);
            TickShipSystems(State.enemyShip, dt);
            TickCrew(dt);
            TickRooms(dt);
            TickWard(State.playerShip, dt);
            TickWard(State.enemyShip, dt);
            TickSquadrons(dt);

            State.playerShip.instability = Math.Max(0f, State.playerShip.instability - dt * 0.45f * ModuleRules.Modifiers(State).instabilityDecay);
            if (State.weatherHazardTimer <= 0f) ResolveWeatherHazard();
            if (State.enemyWeaponCooldown <= 0f) EnemyFire();
            if (State.enemySquadronCooldown <= 0f) EnemySquadronStrike();

            if (State.enemyShip != null && State.enemyShip.IsDestroyed) ResolveVictory();
            CheckDefeat();
        }

        private static void TickShipSystems(ShipState ship, float dt)
        {
            if (ship == null) return;
            for (var i = 0; i < ship.systems.Count; i++)
            {
                var system = ship.systems[i];
                system.disabledSeconds = Math.Max(0f, system.disabledSeconds - dt);
                system.overchargeSeconds = Math.Max(0f, system.overchargeSeconds - dt);
            }
        }

        /// <summary>A single missed shot must not hand the ward its regeneration back; longer than one weapon cycle.</summary>
        public const float WardRechargeDelay = 6f;

        private void TickWard(ShipState ship, float dt)
        {
            if (ship == null) return;
            if (ship.wardRechargeSeconds > 0f)
            {
                ship.wardRechargeSeconds = Math.Max(0f, ship.wardRechargeSeconds - dt);
                return;
            }
            if (ship.ward >= ship.maxWard) return;
            var ward = ship.GetSystem(ShipSystemType.Ward);
            if (ward == null || ward.EffectivePower <= 0) return;
            var profile = ContentCatalog.GetWeather(State.currentWeather);
            var altitudeModifier = ship.altitude == AltitudeBand.High ? 1.15f : ship.altitude == AltitudeBand.Low ? 0.9f : 1f;
            var moduleRegen = ship == State.playerShip ? ModuleRules.Modifiers(State).wardRegen : 1f;
            ship.ward = Math.Min(ship.maxWard, ship.ward + dt * ward.EffectivePower * 0.22f * profile.wardRegenModifier * altitudeModifier * moduleRegen);
        }

        private void TickRooms(float dt)
        {
            var lifeSupport = State.playerShip.GetSystem(ShipSystemType.LifeSupport);
            var lifePowered = lifeSupport != null && lifeSupport.EffectivePower > 0;
            var modifiers = ModuleRules.Modifiers(State);
            for (var i = 0; i < State.playerShip.rooms.Count; i++)
            {
                var room = State.playerShip.rooms[i];
                var oxygenDelta = lifePowered ? 4f : -1.5f;
                var loss = room.breach * 0.08f + room.fire * 0.025f;
                if (modifiers.oxygenReserve) loss *= 0.5f;
                oxygenDelta -= loss;
                if (!lifePowered && modifiers.oxygenReserve) oxygenDelta = -0.75f - loss;
                room.oxygen = Clamp(room.oxygen + oxygenDelta * dt, 0f, 100f);
                if (room.fire > 0f) room.fire = Math.Min(100f, room.fire + dt * 0.35f * (modifiers.fireResistance ? 0.5f : 1f));
                if (room.intruders > 0) TickIntruders(room, dt);
                if (modifiers.autoRepair > 0f)
                {
                    var system = State.playerShip.GetSystem(room.system);
                    if (system != null) system.damage = Math.Max(0f, system.damage - modifiers.autoRepair * dt);
                }
            }
        }

        private void TickIntruders(RoomState room, float dt)
        {
            var defenders = 0;
            var marinePresent = false;
            for (var i = 0; i < State.crew.Count; i++)
            {
                var crew = State.crew[i];
                if (!crew.IsActive || crew.currentRoom != room.system) continue;
                defenders++;
                if (crew.role == CrewRole.Marine) marinePresent = true;
            }

            if (defenders > 0)
            {
                room.intruderProgress += dt * (0.35f * defenders + (marinePresent ? 0.6f : 0f)) * ModuleRules.Modifiers(State).boardingDefense;
                while (room.intruderProgress >= 1f && room.intruders > 0)
                {
                    room.intruderProgress -= 1f;
                    room.intruders--;
                }
                if (room.intruders <= 0)
                {
                    room.intruderProgress = 0f;
                    AddLog("log.boarders_cleared", room.system.ToString());
                }
                return;
            }

            var system = State.playerShip.GetSystem(room.system);
            if (system != null) system.damage = Math.Min(system.maxDamage, system.damage + dt * room.intruders * 4f);
        }

        private void TickCrew(float dt)
        {
            for (var i = 0; i < State.crew.Count; i++)
            {
                var crew = State.crew[i];
                if (crew.isDead || crew.onSortie) continue;
                if (crew.health <= 0f)
                {
                    crew.downedSeconds += dt;
                    if (crew.downedSeconds >= 12f)
                    {
                        crew.isDead = true;
                        AddLog("log.crew_lost", crew.displayName);
                    }
                    continue;
                }

                var room = State.playerShip.GetRoom(crew.currentRoom);
                var system = State.playerShip.GetSystem(crew.currentRoom);
                if (room == null || system == null) continue;

                var crewModifiers = ModuleRules.Modifiers(State);
                var repairRate = CrewRepairRate(crew) * crewModifiers.repairRate;
                system.damage = Math.Max(0f, system.damage - repairRate * dt);
                room.fire = Math.Max(0f, room.fire - repairRate * 0.8f * dt);
                room.breach = Math.Max(0f, room.breach - repairRate * 0.55f * dt);

                var danger = room.fire * 0.018f * (crewModifiers.fireResistance ? 0.5f : 1f) + (room.oxygen < 22f ? 3.2f : 0f) + room.intruders * 1.5f;
                if (crew.lineage == CrewLineage.Dwarf) danger *= 0.72f;
                if (crew.lineage == CrewLineage.Avian && room.oxygen < 22f) danger *= 0.5f;
                crew.health = Math.Max(0f, crew.health - danger * dt);
            }

            var infirmary = State.playerShip.GetSystem(ShipSystemType.Infirmary);
            if (infirmary != null && infirmary.EffectivePower > 0)
            {
                for (var i = 0; i < State.crew.Count; i++)
                {
                    var crew = State.crew[i];
                    if (!crew.isDead && !crew.onSortie && crew.currentRoom == ShipSystemType.Infirmary && crew.health > 0f)
                        crew.health = Math.Min(crew.maxHealth, crew.health + dt * (2f + infirmary.EffectivePower) * ModuleRules.Modifiers(State).healRate);
                }
            }
        }

        private static float CrewRepairRate(CrewState crew)
        {
            var rate = 0.65f + crew.skillLevel * 0.25f;
            if (crew.lineage == CrewLineage.Dwarf) rate *= 1.4f;
            if (crew.lineage == CrewLineage.Goblin) rate *= 1.22f;
            if (crew.role == CrewRole.Engineer) rate *= 1.35f;
            return rate;
        }

        public CommandResult ChangePower(ShipSystemType type, int delta)
        {
            if (State.phase != GamePhase.Combat) return CommandResult.Fail("command.invalid_phase");
            var system = State.playerShip.GetSystem(type);
            if (system == null || type == ShipSystemType.AetherCore) return CommandResult.Fail("command.invalid_system");
            var next = ClampInt(system.power + delta, 0, system.maxPower);
            var added = next - system.power;
            if (added > 0 && State.playerShip.AllocatedPower() + added > State.playerShip.coreOutput)
                return CommandResult.Fail("command.no_power");
            system.power = next;
            State.hasChangedPower = true;
            return CommandResult.Ok("command.power_changed");
        }

        public CommandResult Overcharge(ShipSystemType type)
        {
            if (State.phase != GamePhase.Combat) return CommandResult.Fail("command.invalid_phase");
            var system = State.playerShip.GetSystem(type);
            var resonator = State.crew.Find(crew => crew.role == CrewRole.Resonator && crew.IsActive && crew.currentRoom == type);
            if (system == null || system.maxPower == 0) return CommandResult.Fail("command.invalid_system");
            if (resonator == null) return CommandResult.Fail("command.need_resonator");
            if (system.overchargeSeconds > 0f) return CommandResult.Fail("command.already_overcharged");
            system.overchargeSeconds = 8f;
            State.playerShip.instability = Clamp(State.playerShip.instability + 22f, 0f, 100f);
            AddLog("log.overcharge", type.ToString());

            if (State.playerShip.instability >= 75f)
            {
                var random = State.random.combat;
                if (SeededRandom.Chance(ref random, 0.32f))
                {
                    system.damage = Math.Min(system.maxDamage, system.damage + 18f);
                    State.playerShip.GetRoom(type).fire = Math.Min(100f, State.playerShip.GetRoom(type).fire + 22f);
                    AddLog("log.resonance_accident", type.ToString());
                    RaiseCombatAlert("alert.resonance_fire", type.ToString(), AlertSeverity.Critical, true);
                }
                State.random.combat = random;
            }
            return CommandResult.Ok("command.overcharged");
        }

        public CommandResult MoveCrew(string crewId, ShipSystemType room)
        {
            if (State.phase != GamePhase.Combat) return CommandResult.Fail("command.invalid_phase");
            var crew = State.crew.Find(item => item.id == crewId);
            if (crew == null || crew.isDead || crew.onSortie || State.playerShip.GetRoom(room) == null)
                return CommandResult.Fail("command.crew_unavailable");
            crew.currentRoom = room;
            State.hasMovedCrew = true;
            return CommandResult.Ok("command.crew_moved");
        }

        public CommandResult FireMainWeapon(ShipSystemType target)
        {
            if (State.phase != GamePhase.Combat || State.enemyShip == null) return CommandResult.Fail("command.invalid_phase");
            var weapons = State.playerShip.GetSystem(ShipSystemType.Weapons);
            if (weapons == null || weapons.EffectivePower <= 0) return CommandResult.Fail("command.weapons_unpowered");
            if (State.playerWeaponCooldown > 0f) return CommandResult.Fail("command.weapon_cooldown");

            State.selectedEnemySystem = target;
            State.hasFiredWeapon = true;
            State.playerWeaponCooldown = Math.Max(2.2f, 5.2f - weapons.EffectivePower * 0.55f) * ModuleRules.Modifiers(State).weaponCooldown;
            var random = State.random.combat;
            var hit = SeededRandom.Chance(ref random, Accuracy(State.playerShip, State.enemyShip, true));
            State.random.combat = random;
            if (!hit)
            {
                AddLog("log.player_miss");
                return CommandResult.Ok("command.weapon_fired");
            }

            ApplyDamage(State.enemyShip, target, PlayerShotDamage(), false);
            AddLog("log.player_hit", target.ToString());
            if (State.enemyShip.IsDestroyed) ResolveVictory();
            return CommandResult.Ok("command.weapon_fired");
        }

        /// <summary>Main-battery damage per hit: base by weapon power, multiplied by installed weapon modules.</summary>
        public float PlayerShotDamage()
        {
            var weapons = State.playerShip?.GetSystem(ShipSystemType.Weapons);
            if (weapons == null) return 0f;
            return (3.5f + weapons.EffectivePower * 0.7f) * ModuleRules.Modifiers(State).weaponDamage;
        }

        private void EnemyFire()
        {
            if (State.enemyShip == null) return;
            var weapons = State.enemyShip.GetSystem(ShipSystemType.Weapons);
            if (weapons == null || weapons.EffectivePower <= 0)
            {
                State.enemyWeaponCooldown = 1f;
                return;
            }

            State.enemyWeaponCooldown = State.isFinalBattle ? 3.2f : 4.3f;
            var random = State.random.combat;
            var targets = State.playerShip.systems;
            var target = targets[SeededRandom.Range(ref random, 0, targets.Count)].type;
            var hit = SeededRandom.Chance(ref random, Accuracy(State.enemyShip, State.playerShip, false));
            State.random.combat = random;
            if (!hit)
            {
                AddLog("log.enemy_miss");
                return;
            }

            ApplyDamage(State.playerShip, target, EnemyShotDamage(), false);
            AddLog("log.enemy_hit", target.ToString());
        }

        /// <summary>Enemy main-battery damage: base by weapon power, scaled by difficulty and by the region multiplier.</summary>
        public float EnemyShotDamage()
        {
            var weapons = State.enemyShip?.GetSystem(ShipSystemType.Weapons);
            if (weapons == null) return 0f;
            var difficultyMultiplier = State.difficulty == Difficulty.Story ? 0.78f : State.difficulty == Difficulty.Harsh ? 1.22f : 1f;
            var regionMultiplier = ContentCatalog.GetRegion(State.regionIndex).enemyStatMultiplier;
            return (2.4f + weapons.EffectivePower * 0.5f) * difficultyMultiplier * regionMultiplier;
        }

        private float Accuracy(ShipState attacker, ShipState defender, bool playerAttack)
        {
            var sensors = attacker.GetSystem(ShipSystemType.Sensors);
            var engines = defender.GetSystem(ShipSystemType.Engines);
            var weather = ContentCatalog.GetWeather(State.currentWeather);
            var value = 0.72f + (sensors?.EffectivePower ?? 0) * 0.035f - (engines?.EffectivePower ?? 0) * 0.025f;
            var accuracyModifiers = playerAttack ? ModuleRules.Modifiers(State) : ModuleModifiers.None;
            value += weather.accuracyModifier < 0f && accuracyModifiers.weatherResistance ? weather.accuracyModifier * 0.5f : weather.accuracyModifier;
            value -= Math.Abs((int)attacker.altitude - (int)defender.altitude) * 0.07f;
            if (playerAttack && State.reconBonusSeconds > 0f) value += 0.16f;
            value += accuracyModifiers.accuracy;
            return Clamp(value, 0.2f, 0.96f);
        }

        public CommandResult ChangeAltitude(AltitudeBand altitude)
        {
            if (State.phase != GamePhase.Combat) return CommandResult.Fail("command.invalid_phase");
            if (State.altitudeCooldown > 0f) return CommandResult.Fail("command.altitude_cooldown");
            var lift = State.playerShip.GetSystem(ShipSystemType.LiftArray);
            if (lift == null || lift.EffectivePower <= 0) return CommandResult.Fail("command.lift_unpowered");
            if (State.playerShip.altitude == altitude) return CommandResult.Fail("command.altitude_same");

            var distance = Math.Abs((int)State.playerShip.altitude - (int)altitude);
            State.playerShip.altitude = altitude;
            State.altitudeCooldown = Math.Max(2f, 6f - lift.EffectivePower * 0.8f) * distance;
            State.playerShip.instability = Clamp(State.playerShip.instability + 4f * distance, 0f, 100f);
            AddLog("log.altitude_changed", altitude.ToString());
            return CommandResult.Ok("command.altitude_changed");
        }

        public CommandResult LaunchSquadron(string squadronId, SquadronMission mission, ShipSystemType target)
        {
            if (State.phase != GamePhase.Combat) return CommandResult.Fail("command.invalid_phase");
            var deck = State.playerShip.GetSystem(ShipSystemType.FlightDeck);
            var squadron = State.squadrons.Find(item => item.id == squadronId);
            if (deck == null || deck.EffectivePower <= 0) return CommandResult.Fail("command.deck_unpowered");
            if (squadron == null || !squadron.CanLaunch) return CommandResult.Fail("command.squadron_unavailable");
            if (State.resources.ordnance < squadron.ordnanceCost) return CommandResult.Fail("command.no_ordnance");
            if (mission == SquadronMission.None || mission == SquadronMission.Recall) return CommandResult.Fail("command.invalid_mission");

            State.resources.ordnance -= squadron.ordnanceCost;
            State.hasLaunchedSquadron = true;
            squadron.mission = mission;
            squadron.targetSystem = target;
            squadron.status = SquadronStatus.Launching;
            squadron.missionTimer = Math.Max(0.8f, 2.2f - deck.EffectivePower * 0.3f) * ModuleRules.Modifiers(State).squadronTime;
            squadron.phaseDuration = squadron.missionTimer;
            var pilot = State.crew.Find(crew => crew.id == squadron.pilotCrewId);
            if (pilot != null) pilot.onSortie = true;
            AddLog("log.squadron_launch", squadron.displayKey);
            return CommandResult.Ok("command.squadron_launched");
        }

        private void TickSquadrons(float dt)
        {
            var weather = ContentCatalog.GetWeather(State.currentWeather);
            for (var i = 0; i < State.squadrons.Count; i++)
            {
                var squadron = State.squadrons[i];
                if (squadron.status == SquadronStatus.Ready || squadron.status == SquadronStatus.Destroyed) continue;
                squadron.missionTimer -= dt;
                if (squadron.missionTimer > 0f) continue;

                if (squadron.status == SquadronStatus.Launching)
                {
                    squadron.status = SquadronStatus.OnMission;
                    squadron.missionTimer = 4.5f * weather.squadronTimeModifier;
                    squadron.phaseDuration = squadron.missionTimer;
                    AddLog("log.squadron_on_mission", squadron.displayKey);
                    RaiseCombatAlert("alert.squadron_on_mission", squadron.displayKey, AlertSeverity.Info, false);
                }
                else if (squadron.status == SquadronStatus.OnMission)
                {
                    ResolveSquadronMission(squadron);
                    if (squadron.status != SquadronStatus.Destroyed)
                    {
                        squadron.status = SquadronStatus.Recovering;
                        squadron.missionTimer = 2.3f * weather.squadronTimeModifier;
                        squadron.phaseDuration = squadron.missionTimer;
                        AddLog("log.squadron_returning", squadron.displayKey);
                        RaiseCombatAlert("alert.squadron_returning", squadron.displayKey, AlertSeverity.Info, false);
                    }
                }
                else if (squadron.status == SquadronStatus.Recovering)
                {
                    squadron.status = SquadronStatus.Ready;
                    squadron.mission = SquadronMission.None;
                    squadron.phaseDuration = 0f;
                    var pilot = State.crew.Find(crew => crew.id == squadron.pilotCrewId);
                    if (pilot != null) pilot.onSortie = false;
                    AddLog("log.squadron_recovered", squadron.displayKey);
                    RaiseCombatAlert("alert.squadron_recovered", squadron.displayKey, AlertSeverity.Info, false);
                }
            }
        }

        private void ResolveSquadronMission(SquadronState squadron)
        {
            switch (squadron.mission)
            {
                case SquadronMission.Intercept:
                    State.interceptCharges += 2;
                    AddLog("log.intercept_ready", squadron.displayKey);
                    break;
                case SquadronMission.Bombard:
                    ApplyDamage(State.enemyShip, squadron.targetSystem, 6f + squadron.strength, false);
                    AddLog("log.bombardment", squadron.targetSystem.ToString());
                    break;
                case SquadronMission.Escort:
                    State.playerShip.ward = Math.Min(State.playerShip.maxWard, State.playerShip.ward + 5f);
                    State.interceptCharges++;
                    AddLog("log.escort_ready", squadron.displayKey);
                    break;
                case SquadronMission.Recon:
                    State.reconBonusSeconds = 15f;
                    AddLog("log.recon_ready", squadron.displayKey);
                    break;
                case SquadronMission.Assault:
                    var system = State.enemyShip.GetSystem(squadron.targetSystem);
                    if (system != null) system.damage = Math.Min(system.maxDamage, system.damage + 32f + ModuleRules.Modifiers(State).assaultBonus);
                    State.enemyShip.hull = Math.Max(0f, State.enemyShip.hull - 1f);
                    AddLog("log.assault", squadron.targetSystem.ToString());
                    break;
            }

            var random = State.random.combat;
            var enemyDeck = State.enemyShip?.GetSystem(ShipSystemType.FlightDeck);
            var lossChance = 0.14f + (enemyDeck?.EffectivePower ?? 0) * 0.035f;
            if (State.currentWeather == WeatherType.Turbulence || State.currentWeather == WeatherType.Icing) lossChance += 0.1f;
            if (SeededRandom.Chance(ref random, lossChance))
            {
                squadron.strength--;
                AddLog("log.squadron_damaged", squadron.displayKey);
            }
            State.random.combat = random;

            if (squadron.strength <= 0)
            {
                squadron.strength = 0;
                squadron.status = SquadronStatus.Destroyed;
                var pilot = State.crew.Find(crew => crew.id == squadron.pilotCrewId);
                if (pilot != null)
                {
                    pilot.onSortie = false;
                    pilot.health = 0f;
                    pilot.downedSeconds = 6f;
                }
                AddLog("log.squadron_destroyed", squadron.displayKey);
                RaiseCombatAlert("alert.squadron_destroyed", squadron.displayKey, AlertSeverity.Critical, true);
            }
        }

        private void EnemySquadronStrike()
        {
            var deck = State.enemyShip?.GetSystem(ShipSystemType.FlightDeck);
            State.enemySquadronCooldown = State.isFinalBattle ? 10f : 14f;
            if (deck == null || deck.EffectivePower <= 0) return;
            if (State.interceptCharges > 0)
            {
                State.interceptCharges--;
                AddLog(State.enemyShip.boardingCapable ? "log.boarders_repelled" : "log.enemy_squadron_intercepted");
                return;
            }
            var regionMultiplier = ContentCatalog.GetRegion(State.regionIndex).enemyStatMultiplier;
            if (State.enemyShip.boardingCapable)
            {
                LandBoarders(State.regionIndex >= 3 ? 4 : 3);
                return;
            }
            ApplyDamage(State.playerShip, ShipSystemType.FlightDeck, (5f + deck.EffectivePower) * regionMultiplier, false);
            AddLog("log.enemy_squadron_hit");
            RaiseCombatAlert("alert.enemy_airstrike", "", AlertSeverity.Warning, true);
        }

        private void LandBoarders(int count)
        {
            var random = State.random.combat;
            var rooms = State.playerShip.rooms;
            var room = rooms[SeededRandom.Range(ref random, 0, rooms.Count)];
            State.random.combat = random;
            room.intruders += count;
            AddLog("log.boarders", room.system.ToString());
            RaiseCombatAlert("alert.boarders", room.system.ToString(), AlertSeverity.Warning, true);
        }

        private void ResolveWeatherHazard()
        {
            var profile = ContentCatalog.GetWeather(State.currentWeather);
            State.weatherHazardTimer = profile.hazardInterval;
            var random = State.random.combat;
            switch (State.currentWeather)
            {
                case WeatherType.Thunderhead:
                    State.playerShip.ward = Math.Max(0f, State.playerShip.ward - 3f);
                    DamageRandomSystem(8f, ref random);
                    AddLog("log.weather_thunder");
                    RaiseCombatAlert("alert.thunder_strike", "", AlertSeverity.Warning, true);
                    break;
                case WeatherType.Turbulence:
                    DamageRandomCrew(8f, ref random);
                    AddLog("log.weather_turbulence");
                    break;
                case WeatherType.AetherCurrent:
                    State.playerShip.instability = Clamp(State.playerShip.instability + 12f, 0f, 100f);
                    State.playerShip.ward = Math.Min(State.playerShip.maxWard, State.playerShip.ward + 2f);
                    AddLog("log.weather_aether");
                    break;
                case WeatherType.Icing:
                    var lift = State.playerShip.GetSystem(ShipSystemType.LiftArray);
                    if (lift != null) lift.damage = Math.Min(lift.maxDamage, lift.damage + 7f);
                    AddLog("log.weather_icing");
                    RaiseCombatAlert("alert.icing", ShipSystemType.LiftArray.ToString(), AlertSeverity.Warning, true);
                    break;
                case WeatherType.CloudCover:
                    State.reconBonusSeconds = Math.Max(0f, State.reconBonusSeconds - 3f);
                    AddLog("log.weather_cloud");
                    break;
            }
            State.random.combat = random;
        }

        private void DamageRandomSystem(float amount, ref uint random)
        {
            var systems = State.playerShip.systems;
            var target = systems[SeededRandom.Range(ref random, 0, systems.Count)];
            target.damage = Math.Min(target.maxDamage, target.damage + amount);
            var room = State.playerShip.GetRoom(target.type);
            if (room != null) room.fire = Math.Min(100f, room.fire + amount);
        }

        private void DamageRandomCrew(float amount, ref uint random)
        {
            var active = State.crew.FindAll(crew => crew.IsActive);
            if (active.Count == 0) return;
            var crew = active[SeededRandom.Range(ref random, 0, active.Count)];
            crew.health = Math.Max(0f, crew.health - amount);
        }

        public void ApplyDamage(ShipState defender, ShipSystemType target, float amount, bool ignoresWard)
        {
            if (defender == null || amount <= 0f) return;
            // Any hit interrupts ward regeneration so sustained fire can wear a ward down.
            defender.wardRechargeSeconds = WardRechargeDelay;
            var remaining = amount;
            if (!ignoresWard && defender.ward > 0f)
            {
                var absorbed = Math.Min(defender.ward, remaining);
                defender.ward -= absorbed;
                remaining -= absorbed;
            }
            if (remaining > 0f && defender.armor > 0f)
            {
                var absorbed = Math.Min(defender.armor, remaining);
                defender.armor -= absorbed;
                remaining -= absorbed;
            }
            if (remaining <= 0f) return;

            defender.hull = Math.Max(0f, defender.hull - remaining);
            var system = defender.GetSystem(target);
            if (system != null) system.damage = Math.Min(system.maxDamage, system.damage + remaining * 12f);
            var room = defender.GetRoom(target);
            if (room != null)
            {
                var random = State.random.combat;
                if (SeededRandom.Chance(ref random, 0.34f)) room.fire = Math.Min(100f, room.fire + remaining * 8f);
                if (SeededRandom.Chance(ref random, 0.22f)) room.breach = Math.Min(100f, room.breach + remaining * 7f);
                State.random.combat = random;
            }
            if (defender == State.playerShip)
                RaiseCombatAlert("alert.hull_breached", target.ToString(), defender.hull <= defender.maxHull * 0.3f ? AlertSeverity.Critical : AlertSeverity.Warning, true);
        }

        public CommandResult UseSupportAbility()
        {
            if (State.convoy.supportCooldown > 0) return CommandResult.Fail("command.support_cooldown");
            switch (State.convoy.supportShip)
            {
                case SupportShipType.Hospital:
                    for (var i = 0; i < State.crew.Count; i++)
                    {
                        var crew = State.crew[i];
                        if (crew.isDead || crew.onSortie) continue;
                        if (crew.health <= 0f)
                        {
                            crew.health = crew.maxHealth * 0.3f;
                            crew.downedSeconds = 0f;
                        }
                        else crew.health = Math.Min(crew.maxHealth, crew.health + 35f);
                    }
                    AddLog("log.support_hospital");
                    break;
                case SupportShipType.Workshop:
                    State.playerShip.armor = Math.Min(State.playerShip.maxArmor, State.playerShip.armor + 6f);
                    for (var i = 0; i < State.playerShip.systems.Count; i++)
                        State.playerShip.systems[i].damage = Math.Max(0f, State.playerShip.systems[i].damage - 12f);
                    AddLog("log.support_workshop");
                    break;
                case SupportShipType.Pathfinder:
                    State.reconBonusSeconds = Math.Max(State.reconBonusSeconds, 25f);
                    State.playerShip.ward = Math.Min(State.playerShip.maxWard, State.playerShip.ward + 4f);
                    AddLog("log.support_pathfinder");
                    break;
            }
            State.convoy.supportCooldown = 3;
            return CommandResult.Ok("command.support_used");
        }

        public CommandResult FieldRepair()
        {
            if (State.phase != GamePhase.RouteMap) return CommandResult.Fail("command.invalid_phase");
            if (State.resources.salvage < 5) return CommandResult.Fail("command.no_salvage");
            State.resources.salvage -= 5;
            State.playerShip.hull = Math.Min(State.playerShip.maxHull, State.playerShip.hull + 6f);
            State.playerShip.armor = Math.Min(State.playerShip.maxArmor, State.playerShip.armor + 5f);
            return CommandResult.Ok("command.field_repair");
        }

        public CommandResult RefitSquadrons()
        {
            if (State.phase != GamePhase.RouteMap) return CommandResult.Fail("command.invalid_phase");
            if (State.resources.salvage < 4) return CommandResult.Fail("command.no_salvage");
            var damaged = State.squadrons.Find(squadron => squadron.strength < squadron.maxStrength);
            if (damaged == null) return CommandResult.Fail("command.no_squadron_damage");
            State.resources.salvage -= 4;
            damaged.strength++;
            if (damaged.status == SquadronStatus.Destroyed)
            {
                damaged.status = SquadronStatus.Ready;
                damaged.mission = SquadronMission.None;
            }
            return CommandResult.Ok("command.squadron_refit");
        }

        public CommandResult EmergencyOrdnanceAssembly()
        {
            if (State.phase != GamePhase.Combat) return CommandResult.Fail("command.invalid_phase");
            if (State.resources.ordnance > 0) return CommandResult.Fail("command.ordnance_remaining");

            var remainingCost = 3;
            var salvageSpent = Math.Min(State.resources.salvage, remainingCost);
            State.resources.salvage -= salvageSpent;
            remainingCost -= salvageSpent;
            var suppliesSpent = Math.Min(State.resources.supplies, remainingCost);
            State.resources.supplies -= suppliesSpent;
            remainingCost -= suppliesSpent;
            if (remainingCost > 0) State.convoy.survivors = Math.Max(0, State.convoy.survivors - remainingCost * 10);
            State.resources.ordnance += 3;
            State.convoy.morale = Math.Max(0, State.convoy.morale - 1 - remainingCost * 2);
            State.playerShip.instability = Math.Min(100f, State.playerShip.instability + 8f + remainingCost * 4f);
            AddLog("log.emergency_ordnance");
            CheckDefeat();
            return CommandResult.Ok("command.emergency_ordnance");
        }

        public CommandResult EmergencyAetherBurn()
        {
            if (State.phase != GamePhase.RouteMap) return CommandResult.Fail("command.invalid_phase");
            if (HasAffordableRoute()) return CommandResult.Fail("command.not_stranded");
            State.resources.aether += 2;
            State.convoy.morale = Math.Max(0, State.convoy.morale - 6);
            State.convoy.survivors = Math.Max(0, State.convoy.survivors - 12);
            AddLog("log.emergency_aether");
            CheckDefeat();
            return CommandResult.Ok("command.emergency_aether");
        }

        private void ResolveVictory()
        {
            if (State.phase != GamePhase.Combat) return;
            State.isPaused = true;
            var reward = State.isFinalBattle ? 0 : (State.difficulty == Difficulty.Harsh ? 7 : 9) + ModuleRules.Modifiers(State).salvageReward;
            State.resources.salvage += reward;
            State.resources.ordnance += State.isFinalBattle ? 0 : (State.regionIndex >= 3 ? 2 : 1);
            for (var i = 0; i < State.crew.Count; i++) State.crew[i].onSortie = false;
            if (State.isFinalBattle && State.regionIndex < State.regionCount)
            {
                AdvanceRegion();
            }
            else if (State.isFinalBattle)
            {
                State.phase = GamePhase.Victory;
                State.convoy.morale = Math.Min(100, State.convoy.morale + 10);
                AddLog("log.gate_opened");
            }
            else
            {
                State.phase = GamePhase.RouteMap;
                AddLog("log.combat_victory");
            }
        }

        /// <summary>Port stop between regions: the next region's route, counters reset and a resupply.</summary>
        private void AdvanceRegion()
        {
            State.regionIndex++;
            State.routeNodes = ContentCatalog.CreateRoute(State.seed, State.regionIndex);
            if (!State.isFirstExpedition) ContentCatalog.AssignEncounterVariants(State.routeNodes, unchecked(State.seed + State.regionIndex * 104729));
            State.travelCount = 0;
            State.stormColumn = -1;
            State.currentNodeId = "n0_1";
            State.activeEncounterId = null;
            State.enemyShip = null;
            State.isFinalBattle = false;
            State.phase = GamePhase.Port;

            // Port refit: the flagship grows with every gate it passes, so later regions are survivable.
            var ship = State.playerShip;
            ship.maxHull += 2f;
            ship.maxArmor += 1f;
            ship.maxWard += 1f;
            ship.coreOutput += 1;
            var weapons = ship.GetSystem(ShipSystemType.Weapons);
            if (weapons != null && weapons.power < weapons.maxPower && ship.AllocatedPower() < ship.coreOutput) weapons.power++;
            ship.hull = ship.maxHull;
            ship.armor = ship.maxArmor;
            ship.ward = ship.maxWard;
            ship.instability = 0f;
            ship.wardRechargeSeconds = 0f;
            for (var i = 0; i < ship.systems.Count; i++) { ship.systems[i].damage = 0f; ship.systems[i].disabledSeconds = 0f; }
            for (var i = 0; i < ship.rooms.Count; i++) { ship.rooms[i].fire = 0f; ship.rooms[i].breach = 0f; ship.rooms[i].oxygen = 100f; ship.rooms[i].intruders = 0; }
            for (var i = 0; i < State.crew.Count; i++)
            {
                var crew = State.crew[i];
                if (crew.isDead) continue;
                crew.health = crew.maxHealth;
                crew.downedSeconds = 0f;
                crew.onSortie = false;
            }
            for (var i = 0; i < State.squadrons.Count; i++)
            {
                var squadron = State.squadrons[i];
                squadron.strength = squadron.maxStrength;
                squadron.status = SquadronStatus.Ready;
                squadron.mission = SquadronMission.None;
            }
            State.resources.aether = Math.Max(State.resources.aether + 6, 16);
            State.resources.supplies = Math.Max(State.resources.supplies + 4, 12);
            State.resources.ordnance = Math.Max(State.resources.ordnance + 3, 8);
            State.resources.salvage += 8;
            State.convoy.morale = Math.Min(100, State.convoy.morale + 5);
            State.convoy.supportCooldown = 0;
            AddLog("log.region_cleared", State.regionIndex.ToString());
        }

        private void CheckDefeat()
        {
            if (State.phase == GamePhase.Defeat || State.phase == GamePhase.Victory) return;
            if (State.playerShip == null || State.playerShip.IsDestroyed)
            {
                Lose(DefeatReason.FlagshipDestroyed);
                return;
            }
            var captain = State.crew.Find(crew => crew.isCaptain);
            if (captain == null || captain.isDead)
            {
                Lose(DefeatReason.CaptainLost);
                return;
            }
            if (State.convoy.survivors <= 0)
            {
                Lose(DefeatReason.ConvoyLost);
                return;
            }
            if (State.convoy.morale <= 0) Lose(DefeatReason.MoraleCollapsed);
        }

        private void Lose(DefeatReason reason)
        {
            State.defeatReason = reason;
            State.phase = GamePhase.Defeat;
            State.isPaused = true;
            AddLog("log.defeat", reason.ToString());
        }

        private void RaiseCombatAlert(string key, string argument, AlertSeverity severity, bool canAutoPause)
        {
            // Never replace a critical warning with lower-priority sortie chatter.
            if (State.combatAlertSeconds > 0f && State.combatAlertSeverity > severity) return;
            State.combatAlertKey = key;
            State.combatAlertArgument = argument ?? string.Empty;
            State.combatAlertSeverity = severity;
            State.combatAlertSeconds = severity == AlertSeverity.Info ? 2.8f : 5f;
            State.combatAlertPausedBattle = canAutoPause && State.autoPauseOnWarning;
            if (State.combatAlertPausedBattle) State.isPaused = true;
        }

        private void ClearCombatAlert()
        {
            State.combatAlertKey = string.Empty;
            State.combatAlertArgument = string.Empty;
            State.combatAlertSeconds = 0f;
            State.combatAlertPausedBattle = false;
        }

        private void AddLog(string key, string argument = "")
        {
            if (string.IsNullOrEmpty(key)) return;
            State.combatLog.Add(new CombatLogEntry(key, argument));
            if (State.combatLog.Count > 8) State.combatLog.RemoveAt(0);
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
