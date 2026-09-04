# Generates EnemyLibrary.cs and LocalizationService.Enemies.cs from one table.
# Power arrays are in ShipSystemType order: Bridge, AetherCore, LiftArray, Engines, Ward, Weapons, FlightDeck, Sensors, Infirmary, LifeSupport.
SIL = {  # silhouette -> (plan id, ko family, en family)
 "cutter": ("enemy_cutter", "커터", "Cutter"), "carrier": ("enemy_carrier", "항모", "Carrier"), "scout": ("enemy_scout", "정찰 프리깃", "Scout Frigate"),
 "boarder": ("enemy_boarder", "강습 바지선", "Boarding Barge"), "cruiser": ("enemy_cruiser", "순양함", "Cruiser"), "monitor": ("enemy_monitor", "감시함", "Monitor"),
 "lancer": ("enemy_lancer", "창기병 구축함", "Lancer Destroyer"), "minelayer": ("enemy_minelayer", "기뢰 부설함", "Minelayer"), "firebrand": ("enemy_firebrand", "소이함", "Firebrand"),
 "dreadnought": ("enemy_dreadnought", "드레드노트", "Dreadnought"), "hive": ("enemy_hive", "벌집 항모", "Hive Carrier"), "wraith": ("enemy_wraith", "망령함", "Wraith"),
}
def e(id, sil, tier, weight, region, hull, armor, ward, core, power, maxp, weapons, ko=None, en=None, boarding=False, name_key=None):
    assert sum(power) <= core, id
    return dict(id=id, sil=sil, tier=tier, weight=weight, region=region, hull=hull, armor=armor, ward=ward, core=core, power=power, maxp=maxp, weapons=weapons, ko=ko, en=en, boarding=boarding, name_key=name_key)

E = [
 # cutter
 e("enemy_cutter", "cutter", 1, 40, 1, 24, 10, 8, 9, [1,0,1,2,1,3,0,0,0,1], [2,0,3,3,3,4,2,2,1,2], ["aether_cannon"], name_key="ship.enemy_cutter"),
 e("enemy_cutter_lance", "cutter", 1, 20, 2, 24, 10, 10, 9, [1,0,1,2,1,3,0,0,0,1], [2,0,3,3,3,4,2,2,1,2], ["ward_lance"], "제국 랜스 커터", "Imperial Lance Cutter"),
 e("enemy_cutter_veteran", "cutter", 1, 15, 3, 27, 12, 9, 10, [1,0,1,2,1,4,0,0,0,1], [2,0,3,3,3,4,2,2,1,2], ["heavy_cannon"], "제국 노병 커터", "Imperial Veteran Cutter"),
 # carrier
 e("enemy_carrier", "carrier", 1, 20, 1, 28, 12, 10, 10, [1,0,1,1,1,1,3,1,0,1], [2,0,3,3,3,3,4,2,1,2], ["flak_battery"], name_key="ship.enemy_carrier"),
 e("enemy_carrier_strike", "carrier", 1, 15, 2, 28, 12, 10, 11, [1,0,1,1,1,1,4,1,0,1], [2,0,3,3,3,3,4,2,1,2], ["aether_cannon"], "제국 타격 항모", "Imperial Strike Wing Carrier"),
 e("enemy_carrier_veteran", "carrier", 1, 10, 3, 31, 14, 11, 11, [1,0,1,1,1,2,3,1,0,1], [2,0,3,3,3,3,4,2,1,2], ["flak_curtain"], "제국 노병 항모", "Imperial Veteran Carrier"),
 # scout
 e("enemy_scout", "scout", 1, 20, 1, 20, 8, 8, 11, [1,0,1,3,1,2,0,2,0,1], [2,0,3,4,3,3,1,3,1,2], ["bolt_thrower"], name_key="ship.enemy_scout"),
 e("enemy_scout_lance", "scout", 1, 15, 2, 20, 8, 10, 11, [1,0,1,3,1,2,0,2,0,1], [2,0,3,4,3,3,1,3,1,2], ["ward_lance"], "제국 랜스 프리깃", "Imperial Lance Frigate"),
 e("enemy_scout_hunter", "scout", 1, 15, 3, 22, 9, 9, 12, [1,0,1,3,1,2,0,3,0,1], [2,0,3,4,3,3,1,3,1,2], ["rail_harpoon"], "제국 사냥꾼 프리깃", "Imperial Hunter Frigate"),
 # boarder
 e("enemy_boarder", "boarder", 1, 20, 1, 26, 12, 6, 9, [1,0,1,1,1,1,2,0,1,1], [2,0,3,3,3,3,3,2,1,2], ["ember_mortar"], boarding=True, name_key="ship.enemy_boarder"),
 e("enemy_boarder_assault", "boarder", 1, 15, 2, 26, 12, 6, 10, [1,0,1,1,1,1,3,0,1,1], [2,0,3,3,3,3,3,2,1,2], ["ember_mortar"], "제국 강습 함대선", "Imperial Assault Barge", boarding=True),
 e("enemy_boarder_veteran", "boarder", 1, 10, 3, 29, 13, 7, 11, [1,0,1,1,1,2,3,0,1,1], [2,0,3,3,3,3,3,2,1,2], ["breacher_charges"], "제국 노병 바지선", "Imperial Veteran Barge", boarding=True),
 # lancer
 e("enemy_lancer", "lancer", 1, 20, 2, 22, 8, 10, 10, [1,0,1,2,2,2,0,1,0,1], [2,0,3,3,3,3,1,2,1,2], ["ward_lance"], "제국 창기병 구축함", "Imperial Lancer Destroyer"),
 e("enemy_lancer_twin", "lancer", 1, 15, 3, 24, 9, 11, 11, [1,0,1,2,2,3,0,1,0,1], [2,0,3,3,3,3,1,2,1,2], ["sky_lance"], "제국 쌍창 구축함", "Imperial Twin-Lance Destroyer"),
 # minelayer
 e("enemy_minelayer", "minelayer", 1, 15, 2, 22, 12, 6, 9, [1,0,1,1,1,2,0,1,1,1], [2,0,3,3,3,3,1,2,1,2], ["breacher_charges"], "제국 기뢰 부설함", "Imperial Minelayer"),
 e("enemy_minelayer_ripper", "minelayer", 1, 15, 3, 24, 13, 7, 10, [1,0,1,1,1,3,0,1,1,1], [2,0,3,3,3,3,1,2,1,2], ["hull_ripper"], "제국 파쇄 부설함", "Imperial Ripper Minelayer"),
 # firebrand
 e("enemy_firebrand", "firebrand", 1, 15, 2, 24, 10, 8, 9, [1,0,1,2,1,2,0,1,0,1], [2,0,3,3,3,3,1,2,1,2], ["ember_mortar"], "제국 소이함", "Imperial Firebrand"),
 e("enemy_firebrand_hellfire", "firebrand", 1, 15, 4, 26, 11, 9, 10, [1,0,1,2,1,3,0,1,0,1], [2,0,3,3,3,3,1,2,1,2], ["hellfire_mortar"], "제국 지옥불 소이함", "Imperial Hellfire Firebrand"),
 # cruiser
 e("enemy_cruiser", "cruiser", 2, 60, 1, 34, 18, 12, 11, [1,0,1,2,2,3,1,0,0,1], [2,0,3,3,3,4,2,2,1,2], ["heavy_cannon"], name_key="ship.enemy_cruiser"),
 e("enemy_cruiser_missile", "cruiser", 2, 25, 2, 34, 18, 12, 11, [1,0,1,2,2,3,1,0,0,1], [2,0,3,3,3,4,2,2,1,2], ["storm_missiles"], "제국 미사일 순양함", "Imperial Missile Cruiser"),
 e("enemy_cruiser_veteran", "cruiser", 2, 20, 3, 37, 20, 13, 12, [1,0,1,2,2,4,1,0,0,1], [2,0,3,3,3,4,2,2,1,2], ["siege_cannon"], "제국 노병 순양함", "Imperial Veteran Cruiser"),
 # monitor
 e("enemy_monitor", "monitor", 2, 40, 1, 30, 22, 16, 11, [1,0,1,1,3,2,0,2,0,1], [2,0,3,3,4,4,1,3,1,2], ["aether_cannon"], name_key="ship.enemy_monitor"),
 e("enemy_monitor_lance", "monitor", 2, 20, 2, 30, 22, 16, 11, [1,0,1,1,3,2,0,2,0,1], [2,0,3,3,4,4,1,3,1,2], ["resonance_lance"], "제국 랜스 감시함", "Imperial Lance Monitor"),
 e("enemy_monitor_veteran", "monitor", 2, 15, 3, 33, 24, 18, 12, [1,0,1,1,3,3,0,2,0,1], [2,0,3,3,4,4,1,3,1,2], ["heavy_cannon"], "제국 노병 감시함", "Imperial Veteran Monitor"),
 # dreadnought
 e("enemy_dreadnought", "dreadnought", 2, 20, 2, 34, 22, 12, 11, [1,0,1,1,2,3,1,1,0,1], [2,0,3,3,3,5,2,2,1,2], ["heavy_cannon"], "제국 드레드노트", "Imperial Dreadnought"),
 e("enemy_dreadnought_bastion", "dreadnought", 2, 15, 4, 40, 26, 14, 12, [1,0,1,1,3,3,1,1,0,1], [2,0,3,3,4,5,2,2,1,2], ["siege_cannon"], "제국 요새 드레드노트", "Imperial Bastion Dreadnought"),
 # hive
 e("enemy_hive", "hive", 2, 20, 2, 32, 16, 12, 11, [1,0,1,1,1,1,3,1,1,1], [2,0,3,3,3,3,4,2,1,2], ["flak_battery"], "제국 벌집 항모", "Imperial Hive Carrier", boarding=True),
 e("enemy_hive_swarm", "hive", 2, 15, 4, 34, 17, 13, 13, [1,0,1,1,1,2,4,1,1,1], [2,0,3,3,3,3,4,2,1,2], ["rocket_pod"], "제국 떼벌 항모", "Imperial Swarm Carrier", boarding=True),
 # wraith
 e("enemy_wraith", "wraith", 2, 20, 3, 26, 12, 14, 12, [1,0,1,4,1,2,0,2,0,1], [2,0,3,4,3,3,1,3,1,2], ["rail_harpoon"], "제국 망령함", "Imperial Wraith"),
 e("enemy_wraith_ghost", "wraith", 2, 15, 4, 28, 13, 15, 13, [1,0,1,4,1,2,0,3,0,1], [2,0,3,4,3,3,1,3,1,2], ["gate_piercer"], "제국 유령 망령함", "Imperial Ghost Wraith"),
]
assert len(E) >= 30 and len({x["sil"] for x in E}) == 12, (len(E), len({x["sil"] for x in E}))

def cs_str(s): return '"' + s.replace('"', '\\"') + '"'
lib = ["using System.Collections.Generic;", "using AetherArk.Core;", "", "namespace AetherArk.Content", "{",
       "    /// <summary>Enemy configs. Generated from tools/gen_enemies.py; edit the table, not this file.</summary>",
       "    public static class EnemyLibrary", "    {", "        public static void AddAll(List<EnemyDefinition> result)", "        {"]
for x in E:
    plan = SIL[x["sil"]][0]
    key = x["name_key"] or f'ship.{x["id"]}'
    weapons = ", ".join(cs_str(w) for w in x["weapons"])
    lib.append(f'            result.Add(new EnemyDefinition {{ id = "{x["id"]}", silhouette = "{plan}", nameKey = "{key}", displayName = {cs_str(x["en"] or x["id"])}, tier = {x["tier"]}, weight = {x["weight"]}, minRegion = {x["region"]}, hull = {x["hull"]}f, armor = {x["armor"]}f, ward = {x["ward"]}f, coreOutput = {x["core"]}, boarding = {"true" if x["boarding"] else "false"}, weapons = new[] {{ {weapons} }}, power = new[] {{ {", ".join(map(str, x["power"]))} }}, maxPower = new[] {{ {", ".join(map(str, x["maxp"]))} }} }});')
lib += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/EnemyLibrary.cs", "w", encoding="utf-8").write("\n".join(lib))

loc = ["namespace AetherArk.Content", "{", "    /// <summary>Enemy config names. Generated from tools/gen_enemies.py.</summary>",
       "    public sealed partial class LocalizationService", "    {", "        private void AddEnemyStrings()", "        {"]
for x in E:
    if x["name_key"]: continue  # base configs reuse the existing ship.* strings
    loc.append(f'            Add("ship.{x["id"]}", {cs_str(x["ko"])}, {cs_str(x["en"])});')
loc += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/LocalizationService.Enemies.cs", "w", encoding="utf-8").write("\n".join(loc))
print("configs:", len(E), "silhouettes:", len({x["sil"] for x in E}))
