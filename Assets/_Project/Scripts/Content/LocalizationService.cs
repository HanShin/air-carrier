using System;
using System.Collections.Generic;
using AetherArk.Core;

namespace AetherArk.Content
{
    public sealed class LocalizationService
    {
        private readonly Dictionary<string, string> korean = new Dictionary<string, string>();
        private readonly Dictionary<string, string> english = new Dictionary<string, string>();

        public Language Language { get; set; }

        public LocalizationService(Language language)
        {
            Language = language;
            AddStrings();
        }

        public string T(string key, string argument = "")
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            var table = Language == Language.Korean ? korean : english;
            var fallback = Language == Language.Korean ? english : korean;
            if (!table.TryGetValue(key, out var value) && !fallback.TryGetValue(key, out value)) value = key;
            if (!string.IsNullOrEmpty(argument))
            {
                try { value = string.Format(value, argument); }
                catch (FormatException) { value += " " + argument; }
            }
            return value;
        }

        public string EnumName(Enum value)
        {
            if (value == null) return string.Empty;
            return T("enum." + value.GetType().Name.ToLowerInvariant() + "." + value.ToString().ToLowerInvariant());
        }

        private void Add(string key, string ko, string en)
        {
            korean[key] = ko;
            english[key] = en;
        }

        private void AddStrings()
        {
            Add("game.title", "에테르 아크", "AETHER ARK");
            Add("game.subtitle", "천공 피난선단 지휘 프로토타입", "Sky Convoy Command Prototype");
            Add("menu.new_run", "새 원정", "New Expedition");
            Add("menu.continue", "원정 계속", "Continue Expedition");
            Add("menu.language", "English", "한국어");
            Add("menu.quit", "종료", "Quit");
            Add("setup.title", "원정 준비", "EXPEDITION SETUP");
            Add("setup.captain", "함장 이름", "Captain name");
            Add("setup.lineage", "함장 종족", "Captain lineage");
            Add("setup.support", "지원선", "Support ship");
            Add("setup.difficulty", "난이도", "Difficulty");
            Add("setup.launch", "피난선단 출항", "Launch the convoy");
            Add("setup.back", "돌아가기", "Back");
            Add("setup.warning", "함장이 사망하면 원정은 즉시 끝납니다.", "The expedition ends immediately if the captain dies.");
            Add("ui.pause", "일시정지", "PAUSED");
            Add("ui.running", "전투 진행", "RUNNING");
            Add("ui.resume", "▶ 재개", "▶ Resume");
            Add("ui.pause_button", "Ⅱ 정지", "Ⅱ Pause");
            Add("ui.auto_pause_on", "자동 정지 켜짐", "AUTO-PAUSE ON");
            Add("ui.auto_pause_off", "자동 정지 꺼짐", "AUTO-PAUSE OFF");
            Add("ui.paused_by_warning", "위험 경고로 자동 정지됨 — 상황 확인 후 재개", "AUTO-PAUSED BY ALERT — review the situation, then resume");
            Add("ui.incoming_fire", "적 주포", "Enemy battery");
            Add("ui.incoming_airstrike", "적 공습", "Enemy strike");
            Add("ui.weather_hazard", "기상 위험", "Weather hazard");
            Add("ui.available_power", "가용 출력", "Available power");
            Add("ui.integrity", "내구", "Integrity");
            Add("ui.fire_short", "화재", "Fire");
            Add("ui.breach_short", "감압", "Breach");
            Add("ui.unpowered", "무동력", "UNPOWERED");
            Add("ui.disabled", "고장", "DISABLED");
            Add("ui.mission_target", "목표: {0}", "Target: {0}");
            Add("ui.power", "동력", "POWER");
            Add("ui.hull", "선체", "Hull");
            Add("ui.armor", "장갑", "Armor");
            Add("ui.ward", "결계", "Ward");
            Add("ui.instability", "불안정", "Instability");
            Add("ui.altitude", "고도", "Altitude");
            Add("ui.weather", "기상", "Weather");
            Add("ui.aether", "에테르", "Aether");
            Add("ui.supplies", "보급", "Supplies");
            Add("ui.ordnance", "군수품", "Ordnance");
            Add("ui.salvage", "인양물", "Salvage");
            Add("ui.survivors", "생존자", "Survivors");
            Add("ui.morale", "사기", "Morale");
            Add("ui.support", "지원선", "Support");
            Add("ui.cooldown", "재사용 {0}노드", "Cooldown {0} nodes");
            Add("ui.route_title", "폭풍전선 항로", "STORMFRONT ROUTE");
            Add("ui.route_hint", "연결된 공역을 선택하십시오. 폭풍에 잠긴 노드는 되돌릴 수 없습니다.", "Choose a connected airspace. Nodes swallowed by the storm cannot be reclaimed.");
            Add("ui.current", "현재 위치", "CURRENT");
            Add("ui.blocked", "폭풍 봉쇄", "STORMED");
            Add("ui.cost", "에테르 {0}", "Aether {0}");
            Add("ui.recommended", "권장", "Recommended");
            Add("ui.field_repair", "현장 수리 (-5 인양물)", "Field repair (-5 salvage)");
            Add("ui.refit", "편대 보충 (-4 인양물)", "Refit squadron (-4 salvage)");
            Add("ui.support_call", "지원선 호출", "Call support ship");
            Add("ui.systems", "함선 구획", "SHIP COMPARTMENTS");
            Add("ui.crew", "승무원 — 선택 후 구획 이동", "CREW — select, then choose a room");
            Add("ui.enemy", "제국 함선 — 공격 목표 선택", "IMPERIAL SHIP — select a target");
            Add("ui.squadrons", "함재기 편대", "AIR WINGS");
            Add("ui.fire", "주포 발사", "Fire main battery");
            Add("ui.overcharge", "공명 과부하", "Resonance overcharge");
            Add("ui.low", "저고도", "LOW");
            Add("ui.medium", "중고도", "MID");
            Add("ui.high", "고고도", "HIGH");
            Add("ui.intercept", "요격", "Intercept");
            Add("ui.bombard", "폭격", "Bombard");
            Add("ui.escort", "호위", "Escort");
            Add("ui.recon", "정찰", "Recon");
            Add("ui.assault", "강습", "Assault");
            Add("ui.encounter_continue", "항로로 복귀", "Return to route");
            Add("ui.victory_title", "천공문이 열렸습니다", "THE SKY GATE OPENS");
            Add("ui.victory_body", "당신의 기함이 마지막 포화를 견디는 동안 피난선단은 안전한 반구로 건너갔습니다. 상처 입은 하늘에 새로운 항로가 시작됩니다.", "While your flagship endured the final barrage, the convoy crossed into the safe hemisphere. A new route begins in a wounded sky.");
            Add("ui.defeat_title", "원정 종료", "EXPEDITION LOST");
            Add("ui.new_expedition", "새 원정 준비", "Prepare another expedition");
            Add("ui.main_menu", "메인 메뉴", "Main menu");
            Add("ui.abandon", "원정 포기", "Abandon run");
            Add("ui.selected", "선택됨", "SELECTED");
            Add("ui.ready", "출격 가능", "Ready");
            Add("ui.destroyed", "전멸", "Destroyed");
            Add("ui.unavailable", "사용 불가", "Unavailable");
            Add("ui.last_report", "최근 보고", "LATEST REPORT");
            Add("ui.emergency_aether", "비상 에테르 추출: 생존자 12명·사기 6을 희생해 연료 2 확보", "Emergency aether burn: lose 12 survivors and 6 morale for 2 fuel");
            Add("ui.emergency_ordnance", "비상 탄약 조립 (인양물→보급→인명)", "Emergency ordnance (salvage→supplies→lives)");
            Add("tutorial.fire", "첫 교전: 오른쪽 적 구획을 선택한 뒤 중앙의 주포 발사를 누르십시오.", "FIRST ENGAGEMENT: select an enemy compartment, then fire the main battery.");
            Add("tutorial.squadron", "편대 임무를 선택하십시오. 요격은 적 공습을 막고 폭격은 선택 구획을 공격합니다.", "Launch an air-wing mission. Intercept blocks strikes; Bombard attacks the selected room.");
            Add("tutorial.crew", "손상 발생: 승무원을 선택한 뒤 불타거나 파손된 구획을 선택해 이동시키십시오.", "DAMAGE CONTROL: select a crew member, then the burning or damaged compartment.");
            Add("tutorial.power", "필요한 설비의 동력 +/−를 조정하십시오. 공명자는 같은 구획에서 과부하를 사용할 수 있습니다.", "Adjust system power with +/−. A resonator can overcharge the room they occupy.");

            Add("system.bridge", "함교", "Bridge");
            Add("system.core", "에테르 코어", "Aether Core");
            Add("system.lift", "부양기관", "Lift Array");
            Add("system.engines", "추진기관", "Engines");
            Add("system.ward", "결계기", "Ward Projector");
            Add("system.weapons", "무장실", "Weapons");
            Add("system.deck", "비행갑판", "Flight Deck");
            Add("system.sensors", "센서·통신", "Sensors & Comms");
            Add("system.infirmary", "의무실", "Infirmary");
            Add("system.life", "생명유지실", "Life Support");
            Add("squadron.kestrel", "황조롱이 요격대", "Kestrel Interceptors");
            Add("squadron.ember", "잿불 폭격대", "Ember Bombers");

            Add("node.departure", "새벽 정박지", "Dawn Anchorage");
            Add("node.battle", "추격 초계선", "Pursuit Patrol");
            Add("node.elitebattle", "제국 봉쇄선", "Imperial Blockade");
            Add("node.rescue", "조난 신호", "Distress Signal");
            Add("node.salvage", "폐허 정박지", "Ruined Anchorage");
            Add("node.trade", "자유항", "Free Port");
            Add("node.checkpoint", "제국 검문소", "Imperial Checkpoint");
            Add("node.storm", "폭풍의 눈", "Storm Eye");
            Add("node.gate", "고대 천공문", "Ancient Sky Gate");

            Add("weather.clear", "청명", "Clear");
            Add("weather.thunder", "낙뢰운", "Thunderhead");
            Add("weather.turbulence", "난기류", "Turbulence");
            Add("weather.aether", "에테르류", "Aether Current");
            Add("weather.icing", "결빙", "Icing");
            Add("weather.cloud", "구름 은폐", "Cloud Cover");

            Add("encounter.refugees.title", "부서진 순례선", "THE BROKEN PILGRIM");
            Add("encounter.refugees.body", "구름바다 위에서 엔진이 멎은 순례선이 구조 신호를 보냅니다. 폭풍은 빠르게 가까워지고 있으며, 선단의 보급도 넉넉하지 않습니다.", "A pilgrim vessel with dead engines signals above the cloud sea. The storm is closing quickly, and the convoy's stores are already thin.");
            Add("encounter.dock.title", "침묵한 조선소", "THE SILENT SHIPYARD");
            Add("encounter.dock.body", "제국이 버린 부유 조선소가 천천히 기울고 있습니다. 내부에는 자재가 남아 있지만 하층 거주구도 함께 붕괴 중입니다.", "An abandoned imperial shipyard lists slowly in the wind. Materials remain inside, but its lower habitation ring is collapsing with them.");
            Add("encounter.port.title", "회색돛 자유항", "GREYSAIL FREE PORT");
            Add("encounter.port.body", "상인들은 어느 깃발도 믿지 않지만 인양물의 무게는 믿습니다. 폭풍 가격이 이미 거래판에 반영되어 있습니다.", "The merchants trust no flag, but they trust the weight of salvage. Storm prices are already written across the exchange boards.");
            Add("encounter.checkpoint.title", "흰 창 제국 검문소", "WHITE LANCE CHECKPOINT");
            Add("encounter.checkpoint.body", "추격군의 제복과 다른 휘장을 단 장교가 통신을 요청합니다. 개혁파일 수도, 더 정교한 함정일 수도 있습니다.", "An officer wearing insignia unlike the pursuing fleet requests a channel. They may be a reformist—or simply a more patient trap.");
            Add("encounter.storm.title", "노래하는 폭풍", "THE SINGING STORM");
            Add("encounter.storm.body", "에테르 코어와 폭풍이 같은 음으로 울립니다. 공명자는 상승 기류 안에 안전한 길이 있다고 주장합니다.", "The aether core and the storm begin to sing in the same key. Your resonator claims a safe current is hidden inside the ascent.");

            Add("choice.rescue", "보급 2를 내어 모두 구조한다", "Spend 2 supplies and rescue everyone");
            Add("choice.tow", "에테르 1을 써서 일부와 화물을 견인한다", "Spend 1 aether to tow survivors and cargo");
            Add("choice.leave", "선단을 위험에 빠뜨릴 수 없다", "The convoy cannot be risked");
            Add("choice.salvage", "자재고를 먼저 해체한다", "Strip the material stores");
            Add("choice.stabilize", "인양물 4로 거주구를 안정화한다", "Spend 4 salvage to stabilize the habitation ring");
            Add("choice.scout", "[정찰선] 안전한 진입로를 찾는다", "[Pathfinder] Find a safe approach");
            Add("choice.buy_aether", "인양물 6으로 에테르 4 구입", "Buy 4 aether for 6 salvage");
            Add("choice.buy_supplies", "인양물 6으로 보급 5 구입", "Buy 5 supplies for 6 salvage");
            Add("choice.repair", "인양물 8로 기함 수리", "Repair the flagship for 8 salvage");
            Add("choice.depart", "거래하지 않고 출항", "Depart without trading");
            Add("choice.bribe", "인양물 9로 통행권을 산다", "Buy passage with 9 salvage");
            Add("choice.reformist", "[인간] 보급 1을 넘기고 개혁파와 협상", "[Human] Offer 1 supply and negotiate with the reformists");
            Add("choice.fight", "무장을 전개한다", "Run out the guns");
            Add("choice.climb", "에테르 2로 폭풍 위까지 상승", "Spend 2 aether to climb above the storm");
            Add("choice.ride", "[엘프] 폭풍의 공명 흐름을 탄다", "[Elf] Ride the storm's resonant current");
            Add("choice.push", "선단 전체에 강행 돌파를 명령", "Order the entire convoy through");

            Add("result.rescue", "보급은 줄었지만 84명의 생존자가 선단에 합류했습니다.", "Stores are thinner, but 84 survivors have joined the convoy.");
            Add("result.tow", "구조 가능한 이들과 화물을 견인했습니다.", "You tow the reachable survivors and their cargo clear.");
            Add("result.leave", "정박지를 떠나는 동안 선단 통신망은 조용합니다.", "The convoy channel remains silent as you leave the wreck behind.");
            Add("result.salvage", "조선소가 가라앉기 전 군수품과 자재를 확보했습니다.", "Ordnance and materials are secured before the shipyard sinks.");
            Add("result.stabilize", "거주구를 떠받친 공작반이 생존자들을 구해냈습니다.", "Repair crews brace the habitation ring and bring its people out.");
            Add("result.scout", "정찰선이 감춰진 연료고와 안전한 접근로를 찾았습니다.", "The pathfinder discovers a hidden fuel store and a safe approach.");
            Add("result.trade", "거래가 완료되었습니다.", "The trade is complete.");
            Add("result.repair", "외부 장갑과 손상 구획을 긴급 수리했습니다.", "Outer armor and damaged compartments have been patched.");
            Add("result.depart", "자유항의 불빛이 구름 뒤로 사라집니다.", "The lights of the free port vanish behind the clouds.");
            Add("result.bribe", "검문소는 눈을 돌렸지만 선단 내부의 불만이 남았습니다.", "The checkpoint looks away, but resentment lingers in the convoy.");
            Add("result.reformist", "장교는 비밀 항로와 행운을 빌어 주었습니다.", "The officer gives you a hidden route—and wishes you luck.");
            Add("result.fight", "제국 함선이 결계를 올립니다.", "The imperial vessel raises its ward.");
            Add("result.climb", "선단은 에테르를 태워 폭풍의 꼭대기를 넘었습니다.", "The convoy burns aether and clears the crown of the storm.");
            Add("result.ride", "공명자의 항법으로 오히려 에테르를 충전했습니다.", "Your resonator's course replenishes aether instead of consuming it.");
            Add("result.push", "선단은 통과했지만 일부 수송선이 구름바다로 떨어졌습니다.", "The convoy passes, but several transports fall into the cloud sea.");

            Add("log.combat_started", "제국 함선과 교전 개시.", "Engaging an imperial vessel.");
            Add("log.gate_battle", "천공문 기동 시작. 추격 함대를 막아야 합니다.", "Sky Gate activation begun. Hold off the pursuing fleet.");
            Add("log.convoy_starving", "보급이 바닥나 선단에 이탈자가 발생했습니다.", "Empty stores cause deaths and desertion across the convoy.");
            Add("log.player_miss", "주포 사격이 빗나갔습니다.", "The main battery misses.");
            Add("log.player_hit", "주포가 적 {0}을 타격했습니다.", "Main battery struck enemy {0}.");
            Add("log.enemy_miss", "적 포화가 기함을 비껴갔습니다.", "Enemy fire passes wide of the flagship.");
            Add("log.enemy_hit", "적 포화가 {0} 구획을 타격했습니다.", "Enemy fire struck the {0} compartment.");
            Add("log.overcharge", "{0} 공명 과부하 개시.", "Resonance overcharge initiated in {0}.");
            Add("log.resonance_accident", "과도한 공명으로 {0}에 에테르 화재 발생!", "Aether fire in {0} from resonance overload!");
            Add("log.altitude_changed", "기함이 {0} 고도로 이동합니다.", "Flagship changing to {0} altitude.");
            Add("log.squadron_launch", "{0} 출격.", "{0} launched.");
            Add("log.squadron_on_mission", "{0}이 임무 공역에 진입했습니다.", "{0} entered the mission area.");
            Add("log.squadron_returning", "{0}이 귀환 항로에 진입했습니다.", "{0} is on final approach.");
            Add("log.squadron_recovered", "{0} 귀환 완료.", "{0} recovered.");
            Add("log.squadron_damaged", "{0}에서 기체 손실 발생.", "{0} has lost an aircraft.");
            Add("log.squadron_destroyed", "{0} 전멸. 조종사 구조 신호 감지.", "{0} destroyed. Pilot beacon detected.");
            Add("log.intercept_ready", "{0}이 적 편대 요격 위치에 도달했습니다.", "{0} is in position to intercept.");
            Add("log.bombardment", "폭격대가 적 {0}을 타격했습니다.", "Bombers struck enemy {0}.");
            Add("log.escort_ready", "{0}이 기함 방공망을 보강합니다.", "{0} reinforces the flagship screen.");
            Add("log.recon_ready", "{0}이 적함의 사격 해법을 전송합니다.", "{0} transmits a firing solution.");
            Add("log.assault", "강습대가 적 {0}을 무력화합니다.", "Boarders sabotage enemy {0}.");
            Add("log.enemy_squadron_intercepted", "요격대가 적 공격편대를 격퇴했습니다.", "Interceptors broke the enemy strike.");
            Add("log.enemy_squadron_hit", "적 함재기가 비행갑판을 공습했습니다!", "Enemy aircraft struck the flight deck!");
            Add("log.weather_thunder", "낙뢰가 결계와 구획을 강타했습니다.", "Lightning lashed the ward and compartments.");
            Add("log.weather_turbulence", "난기류로 승무원이 부상했습니다.", "A crew member was injured by turbulence.");
            Add("log.weather_aether", "에테르류가 결계를 채우고 코어를 불안정하게 합니다.", "The current feeds the ward and destabilizes the core.");
            Add("log.weather_icing", "부양기관에 급속 결빙이 발생했습니다.", "Ice is forming across the lift array.");
            Add("log.weather_cloud", "짙은 구름이 정찰 정보를 지웁니다.", "Dense cloud erases the firing solution.");
            Add("log.support_hospital", "병원선이 긴급 의료진을 파견했습니다.", "The hospital ship has dispatched an emergency medical team.");
            Add("log.support_workshop", "공작선이 장갑과 설비를 긴급 보수합니다.", "The workshop ship patches armor and machinery.");
            Add("log.support_pathfinder", "정찰선이 안전한 사격·비행 경로를 표시합니다.", "The pathfinder marks safe firing and flight corridors.");
            Add("log.crew_lost", "{0} 사망.", "{0} has died.");
            Add("log.combat_victory", "적함 무력화. 인양반이 자재를 회수합니다.", "Enemy vessel neutralized. Salvage crews are moving in.");
            Add("log.gate_opened", "천공문 안정화. 선단 통과 개시.", "Sky Gate stabilized. Convoy transit beginning.");
            Add("log.defeat", "패배 원인: {0}", "Defeat: {0}");
            Add("log.emergency_aether", "수송선 동력핵을 해체해 에테르를 확보했습니다.", "Transport cores were dismantled for emergency aether.");
            Add("log.emergency_ordnance", "인양물을 분해해 전투용 군수품을 긴급 조립했습니다.", "Salvage was stripped into emergency combat ordnance.");

            Add("alert.resonance_fire", "공명 사고: {0}에 에테르 화재!", "RESONANCE ACCIDENT: aether fire in {0}!");
            Add("alert.squadron_on_mission", "{0}: 임무 수행 개시", "{0}: mission underway");
            Add("alert.squadron_returning", "{0}: 귀환 중", "{0}: returning to carrier");
            Add("alert.squadron_recovered", "{0}: 착함 완료·재출격 가능", "{0}: recovered and ready");
            Add("alert.squadron_destroyed", "{0} 전멸 — 조종사 구조 필요!", "{0} destroyed — pilot rescue required!");
            Add("alert.enemy_airstrike", "적 공습이 비행갑판을 타격했습니다!", "Enemy strike hit the flight deck!");
            Add("alert.thunder_strike", "낙뢰가 결계와 함내 구획을 강타했습니다!", "Lightning struck the ward and compartments!");
            Add("alert.icing", "{0} 급속 결빙 — 동력과 수리 인원을 확인하십시오.", "Rapid icing in {0} — check power and repairs.");
            Add("alert.hull_breached", "{0} 피격 — 장갑 관통·선체 손상!", "Hit in {0} — armor breached, hull damaged!");

            Add("command.ok", "명령 완료", "Command complete");
            Add("command.invalid", "유효하지 않은 명령입니다.", "Invalid command.");
            Add("command.route_unavailable", "연결되지 않았거나 폭풍에 봉쇄된 항로입니다.", "That route is disconnected or stormed.");
            Add("command.travelled", "다음 공역으로 이동했습니다.", "The convoy entered the next airspace.");
            Add("command.choice_unavailable", "조건이나 자원이 부족합니다.", "Requirements or resources are missing.");
            Add("command.invalid_phase", "현재 상황에서는 실행할 수 없습니다.", "That command is unavailable in the current phase.");
            Add("command.invalid_system", "이 설비에는 적용할 수 없습니다.", "That system cannot receive this command.");
            Add("command.no_power", "에테르 코어의 가용 출력이 부족합니다.", "The aether core has no available output.");
            Add("command.power_changed", "동력 배분을 변경했습니다.", "Power allocation changed.");
            Add("command.need_resonator", "해당 구획에 활동 가능한 공명자가 필요합니다.", "An active resonator must be stationed in that room.");
            Add("command.already_overcharged", "이미 공명 과부하 중입니다.", "That system is already overcharged.");
            Add("command.overcharged", "공명 과부하를 시작했습니다.", "Resonance overcharge engaged.");
            Add("command.crew_unavailable", "승무원을 이동시킬 수 없습니다.", "That crew member cannot move.");
            Add("command.crew_moved", "승무원이 새 구획으로 이동합니다.", "Crew member moving to the selected room.");
            Add("command.weapons_unpowered", "무장실에 동력이 공급되지 않습니다.", "The weapons room has no power.");
            Add("command.weapon_cooldown", "주포가 아직 충전 중입니다.", "The main battery is still charging.");
            Add("command.weapon_fired", "주포 사격 명령을 실행했습니다.", "Main battery fired.");
            Add("command.altitude_cooldown", "부양기관이 아직 고도 변경을 준비 중입니다.", "The lift array is still recovering.");
            Add("command.lift_unpowered", "부양기관에 동력이 공급되지 않습니다.", "The lift array has no power.");
            Add("command.altitude_same", "이미 해당 고도에 있습니다.", "The flagship is already at that altitude.");
            Add("command.altitude_changed", "고도 변경을 시작했습니다.", "Altitude change initiated.");
            Add("command.deck_unpowered", "비행갑판에 동력이 공급되지 않습니다.", "The flight deck has no power.");
            Add("command.squadron_unavailable", "해당 편대는 출격할 수 없습니다.", "That squadron cannot launch.");
            Add("command.no_ordnance", "군수품이 부족합니다.", "Not enough ordnance.");
            Add("command.invalid_mission", "유효하지 않은 편대 임무입니다.", "Invalid squadron mission.");
            Add("command.squadron_launched", "편대가 출격했습니다.", "Squadron launched.");
            Add("command.support_cooldown", "지원선이 아직 다음 호출을 준비 중입니다.", "The support ship is still recovering.");
            Add("command.support_used", "지원선 호출을 실행했습니다.", "Support ship called.");
            Add("command.no_salvage", "인양물이 부족합니다.", "Not enough salvage.");
            Add("command.field_repair", "기함을 현장 수리했습니다.", "Field repairs completed.");
            Add("command.no_squadron_damage", "보충이 필요한 편대가 없습니다.", "No squadron requires replacement craft.");
            Add("command.squadron_refit", "손실된 함재기 한 대를 보충했습니다.", "One lost aircraft has been replaced.");
            Add("command.ordnance_remaining", "아직 사용할 군수품이 남아 있습니다.", "Usable ordnance remains.");
            Add("command.emergency_ordnance", "비상 군수품 3을 조립했습니다.", "Three emergency ordnance assembled.");
            Add("command.not_stranded", "아직 이용 가능한 항로가 있습니다.", "An affordable route is still available.");
            Add("command.emergency_aether", "희생을 치르고 비상 에테르를 확보했습니다.", "Emergency aether secured at a cost.");

            Add("enum.difficulty.story", "이야기", "Story");
            Add("enum.difficulty.standard", "표준", "Standard");
            Add("enum.difficulty.harsh", "가혹", "Harsh");
            Add("enum.altitudeband.low", "저고도", "Low");
            Add("enum.altitudeband.medium", "중고도", "Medium");
            Add("enum.altitudeband.high", "고고도", "High");
            Add("enum.supportshiptype.hospital", "병원선", "Hospital Ship");
            Add("enum.supportshiptype.workshop", "공작선", "Workshop Ship");
            Add("enum.supportshiptype.pathfinder", "정찰선", "Pathfinder Ship");
            Add("enum.crewlineage.human", "인간", "Human");
            Add("enum.crewlineage.elf", "엘프", "Elf");
            Add("enum.crewlineage.dwarf", "드워프", "Dwarf");
            Add("enum.crewlineage.orc", "오크", "Orc");
            Add("enum.crewlineage.goblin", "고블린", "Goblin");
            Add("enum.crewlineage.avian", "조인계 수인", "Avian");
            Add("enum.squadronstatus.ready", "출격 가능", "Ready");
            Add("enum.squadronstatus.launching", "이륙 중", "Launching");
            Add("enum.squadronstatus.onmission", "임무 수행", "On mission");
            Add("enum.squadronstatus.recovering", "귀환 중", "Recovering");
            Add("enum.squadronstatus.destroyed", "전멸", "Destroyed");
            Add("enum.squadronmission.intercept", "요격", "Intercept");
            Add("enum.squadronmission.bombard", "폭격", "Bombard");
            Add("enum.squadronmission.escort", "호위", "Escort");
            Add("enum.squadronmission.recon", "정찰", "Recon");
            Add("enum.squadronmission.assault", "강습", "Assault");
            Add("enum.defeatreason.flagshipdestroyed", "기함 파괴", "Flagship destroyed");
            Add("enum.defeatreason.captainlost", "함장 사망", "Captain lost");
            Add("enum.defeatreason.convoylost", "생존자 전멸", "Convoy lost");
            Add("enum.defeatreason.moralecollapsed", "선단 사기 붕괴", "Morale collapsed");
        }
    }
}
