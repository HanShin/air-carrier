# Generates ModuleLibrary.cs and LocalizationService.Modules.cs from one table.
H, C, W, D, S, E, B, M = "Hull", "Core", "Weapons", "Deck", "Sensors", "Engineering", "Bridge", "Marines"
COST = {1: 8, 2: 14, 3: 22}

def m(id, cat, tier, ko, en, dko, den, **fx):
    return dict(id=id, cat=cat, tier=tier, ko=ko, en=en, dko=dko, den=den, fx=fx)

MODULES = [
 m("reinforced_ribs", H, 1, "보강 늑골", "Reinforced Ribs", "선체 상한 +6.", "+6 maximum hull.", maxHull=6),
 m("ablative_plating", H, 2, "소모성 장갑판", "Ablative Plating", "장갑 상한 +8.", "+8 maximum armor.", maxArmor=8),
 m("ward_lattice", H, 2, "결계 격자", "Ward Lattice", "결계 상한 +6.", "+6 maximum ward.", maxWard=6),
 m("storm_keel", H, 3, "폭풍 용골", "Storm Keel", "선체 상한 +8, 장갑 상한 +6.", "+8 hull and +6 armor.", maxHull=8, maxArmor=6),
 m("resonance_dampers", C, 1, "공명 감쇠기", "Resonance Dampers", "불안정 감소 속도 1.5배.", "Instability decays 1.5x faster.", instabilityDecay=1.5),
 m("aether_capacitor", C, 2, "에테르 축전기", "Aether Capacitor", "코어 출력 +1.", "+1 core output.", coreOutput=1),
 m("twin_core_bypass", C, 3, "쌍코어 우회로", "Twin-Core Bypass", "코어 출력 +2.", "+2 core output.", coreOutput=2),
 m("ward_harmonizer", C, 2, "결계 조율기", "Ward Harmonizer", "결계 재생 1.35배.", "Ward regeneration x1.35.", wardRegen=1.35),
 m("rifled_barrels", W, 1, "강선 포신", "Rifled Barrels", "주포 피해 1.12배.", "Main battery damage x1.12.", weaponDamage=1.12),
 m("autoloader", W, 2, "자동 장전기", "Autoloader", "주포 재장전 시간 0.85배.", "Main battery cooldown x0.85.", weaponCooldown=0.85),
 m("aether_shells", W, 3, "에테르 포탄", "Aether Shells", "주포 피해 1.25배.", "Main battery damage x1.25.", weaponDamage=1.25),
 m("gunnery_computer", W, 2, "사격 계산기", "Gunnery Computer", "명중률 +6%.", "+6% accuracy.", accuracy=0.06),
 m("extended_hangar", D, 1, "확장 격납고", "Extended Hangar", "모든 편대 최대 전력 +1.", "+1 maximum strength for every wing.", squadronStrength=1),
 m("rapid_catapult", D, 2, "고속 사출기", "Rapid Catapult", "편대 임무 시간 0.8배.", "Wing mission time x0.8.", squadronTime=0.8),
 m("escort_doctrine", D, 2, "호위 교리", "Escort Doctrine", "전투 시작 시 요격 충전 +1.", "+1 intercept charge at the start of every battle.", interceptCharges=1),
 m("veteran_pilots", D, 3, "베테랑 조종사", "Veteran Pilots", "편대 최대 전력 +1, 임무 시간 0.9배.", "+1 wing strength and mission time x0.9.", squadronStrength=1, squadronTime=0.9),
 m("long_range_array", S, 1, "장거리 안테나", "Long-Range Array", "명중률 +4%.", "+4% accuracy.", accuracy=0.04),
 m("storm_eyes", S, 2, "폭풍의 눈", "Storm Eyes", "기상으로 인한 명중 감소를 절반으로.", "Halves weather accuracy penalties.", weatherResistance=True),
 m("recon_uplink", S, 3, "정찰 중계기", "Recon Uplink", "전투 시작 시 정찰 보너스 10초.", "10 s of recon bonus at the start of every battle.", reconSeconds=10),
 m("damage_control_teams", E, 1, "손상 통제반", "Damage Control Teams", "승무원 수리 속도 1.3배.", "Crew repair rate x1.3.", repairRate=1.3),
 m("fire_suppression", E, 2, "소화 체계", "Fire Suppression", "화재 확산과 화상 피해 절반.", "Halves fire spread and burn damage.", fireResistance=True),
 m("oxygen_reserves", E, 1, "예비 산소", "Oxygen Reserves", "산소 손실 절반.", "Halves oxygen loss.", oxygenReserve=True),
 m("auto_repair_drones", E, 3, "자동 수리 드론", "Auto-Repair Drones", "승무원이 없어도 모든 구획을 초당 0.5 수리.", "Repairs every room by 0.5 per second even with no crew present.", autoRepair=0.5),
 m("medical_bay_upgrade", E, 2, "의무실 개량", "Medical Bay Upgrade", "의무실 치료 속도 1.5배.", "Infirmary healing x1.5.", healRate=1.5),
 m("navigator_charts", B, 1, "항법 해도", "Navigator's Charts", "도약 에테르 비용 −1 (최소 1).", "Jumps cost 1 less aether (minimum 1).", aetherDiscount=True),
 m("salvage_cranes", B, 2, "인양 크레인", "Salvage Cranes", "전투 승리 인양물 +3.", "+3 salvage per battle won.", salvageReward=3),
 m("salvage_refinery", B, 3, "인양물 정제소", "Salvage Refinery", "전투 승리 인양물 +6.", "+6 salvage per battle won.", salvageReward=6),
 m("boarding_armory", M, 1, "강습 무기고", "Boarding Armoury", "침입자 제압 속도 1.6배.", "Crew repel boarders 1.6x faster.", boardingDefense=1.6),
 m("marine_barracks", M, 2, "해병 막사", "Marine Barracks", "침입자 제압 1.3배, 승무원 체력 +10.", "Repel boarders 1.3x faster and +10 crew health.", boardingDefense=1.3, crewHealth=10),
 m("shock_troops", M, 3, "충격 강습대", "Shock Troops", "강습 임무 파괴 공작 +16.", "+16 sabotage on assault missions.", assaultBonus=16),
]

def cs_str(s): return '"' + s.replace('\\', '\\\\').replace('"', '\\"') + '"'
def cs_val(v):
    if isinstance(v, bool): return "true" if v else "false"
    if isinstance(v, float): return f"{v}f"
    return str(v)

lib = ["using System.Collections.Generic;", "using AetherArk.Core;", "", "namespace AetherArk.Content", "{",
       "    /// <summary>Flagship modules. Generated from tools/gen_modules.py; edit the table, not this file.</summary>",
       "    public static class ModuleLibrary", "    {",
       "        public static void AddAll(Dictionary<string, ModuleDefinition> result)", "        {"]
for e in MODULES:
    fx = ", ".join(f"{k} = {cs_val(v)}" for k, v in e["fx"].items())
    lib.append(f'            result["{e["id"]}"] = new ModuleDefinition {{ id = "{e["id"]}", nameKey = "module.{e["id"]}", descriptionKey = "module.{e["id"]}.desc", category = ModuleCategory.{e["cat"]}, tier = {e["tier"]}, cost = {COST[e["tier"]]}, {fx} }};')
lib += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/ModuleLibrary.cs", "w", encoding="utf-8").write("\n".join(lib))

loc = ["namespace AetherArk.Content", "{",
       "    /// <summary>Module strings. Generated from tools/gen_modules.py.</summary>",
       "    public sealed partial class LocalizationService", "    {",
       "        private void AddModuleStrings()", "        {",
       '            Add("enum.modulecategory.hull", "선체", "Hull");',
       '            Add("enum.modulecategory.core", "코어", "Core");',
       '            Add("enum.modulecategory.weapons", "무장", "Weapons");',
       '            Add("enum.modulecategory.deck", "갑판", "Deck");',
       '            Add("enum.modulecategory.sensors", "센서", "Sensors");',
       '            Add("enum.modulecategory.engineering", "기관", "Engineering");',
       '            Add("enum.modulecategory.bridge", "함교", "Bridge");',
       '            Add("enum.modulecategory.marines", "해병", "Marines");']
for e in MODULES:
    loc.append(f'            Add("module.{e["id"]}", {cs_str(e["ko"])}, {cs_str(e["en"])});')
    loc.append(f'            Add("module.{e["id"]}.desc", {cs_str(e["dko"])}, {cs_str(e["den"])});')
loc += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/LocalizationService.Modules.cs", "w", encoding="utf-8").write("\n".join(loc))
print("modules:", len(MODULES))
