# Generates WeaponLibrary.cs and LocalizationService.Weapons.cs from one table.
CAN, LAN, PIE, MIS, FLK, INC, BRE = "Cannon", "Lance", "Piercer", "Missile", "Flak", "Incendiary", "Breacher"
COST = {1: 10, 2: 18, 3: 28}

def w(id, fam, tier, ko, en, dko, den, power, damage, cooldown, **fx):
    return dict(id=id, fam=fam, tier=tier, ko=ko, en=en, dko=dko, den=den, power=power, damage=damage, cooldown=cooldown, fx=fx)

WEAPONS = [
 w("aether_cannon", CAN, 1, "에테르 포", "Aether Cannon", "균형 잡힌 기본 함포.", "The balanced standard gun.", 2, 4.6, 4.0),
 w("heavy_cannon", CAN, 2, "중포", "Heavy Cannon", "느리지만 강한 함포.", "Slow, hard-hitting gun.", 2, 6.4, 5.6),
 w("siege_cannon", CAN, 3, "공성포", "Siege Cannon", "구획을 부수는 대구경포. 시스템 피해 1.5배.", "A great gun that wrecks compartments; 1.5x system damage.", 3, 9.5, 6.4, systemDamageMultiplier=1.5),
 w("ward_lance", LAN, 1, "결계 랜스", "Ward Lance", "결계에 2배 피해, 장갑은 뚫지 못함.", "Double damage to wards; cannot pierce armor.", 1, 3.0, 3.4, wardMultiplier=2.0),
 w("resonance_lance", LAN, 2, "공명 랜스", "Resonance Lance", "결계에 2배 피해, 명중 +6%.", "Double damage to wards, +6% accuracy.", 2, 4.4, 3.6, wardMultiplier=2.0, accuracyBonus=0.06),
 w("sky_lance", LAN, 3, "창공 랜스", "Sky Lance", "결계에 2.5배 피해.", "2.5x damage to wards.", 2, 5.8, 3.8, wardMultiplier=2.5),
 w("bolt_thrower", PIE, 1, "볼트 투사기", "Bolt Thrower", "피해의 60%가 장갑을 관통. 결계에는 절반.", "60% of the hit bypasses armor; half damage to wards.", 1, 3.4, 4.2, armorPiercing=0.6, wardMultiplier=0.5),
 w("rail_harpoon", PIE, 2, "레일 작살", "Rail Harpoon", "피해의 70%가 장갑을 관통. 결계에는 절반.", "70% of the hit bypasses armor; half damage to wards.", 2, 5.2, 4.6, armorPiercing=0.7, wardMultiplier=0.5),
 w("gate_piercer", PIE, 3, "천공 관통포", "Gate Piercer", "피해의 80%가 장갑을 관통, 파공 확률 30%.", "80% of the hit bypasses armor; 30% breach chance.", 3, 7.4, 5.0, armorPiercing=0.8, wardMultiplier=0.5, breachChance=0.3),
 w("rocket_pod", MIS, 1, "로켓 포드", "Rocket Pod", "결계 무시. 발당 군수품 1. 화재 확률 30%.", "Ignores wards. 1 ordnance per shot. 30% fire chance.", 1, 6.0, 6.0, ignoresWard=True, ordnancePerShot=1, fireChance=0.3),
 w("storm_missiles", MIS, 2, "폭풍 미사일", "Storm Missiles", "결계 무시. 발당 군수품 1. 화재 확률 45%.", "Ignores wards. 1 ordnance per shot. 45% fire chance.", 1, 8.5, 6.5, ignoresWard=True, ordnancePerShot=1, fireChance=0.45),
 w("ruin_missiles", MIS, 3, "파멸 미사일", "Ruin Missiles", "결계 무시. 발당 군수품 2. 시스템 피해 1.5배.", "Ignores wards. 2 ordnance per shot. 1.5x system damage.", 2, 12.0, 7.5, ignoresWard=True, ordnancePerShot=2, fireChance=0.4, systemDamageMultiplier=1.5),
 w("flak_battery", FLK, 1, "플랙 포대", "Flak Battery", "피해는 낮지만 발사마다 요격 충전 +1.", "Light damage, but every shot grants an intercept charge.", 1, 1.6, 3.0, interceptCharge=1),
 w("flak_curtain", FLK, 2, "플랙 장막", "Flak Curtain", "발사마다 요격 충전 +1. 명중 +8%.", "Every shot grants an intercept charge; +8% accuracy.", 2, 2.6, 2.8, interceptCharge=1, accuracyBonus=0.08),
 w("ember_mortar", INC, 1, "잿불 박격포", "Ember Mortar", "화재 확률 80%, 시스템 피해 1.3배.", "80% fire chance, 1.3x system damage.", 1, 2.4, 4.4, fireChance=0.8, systemDamageMultiplier=1.3),
 w("hellfire_mortar", INC, 3, "지옥불 박격포", "Hellfire Mortar", "화재 확률 100%, 시스템 피해 1.6배.", "Always starts a fire; 1.6x system damage.", 2, 4.2, 4.8, fireChance=1.0, systemDamageMultiplier=1.6),
 w("breacher_charges", BRE, 1, "파공 폭약", "Breacher Charges", "파공 확률 70%.", "70% breach chance.", 1, 2.8, 4.6, breachChance=0.7),
 w("hull_ripper", BRE, 2, "선체 파쇄기", "Hull Ripper", "파공 확률 80%, 장갑 40% 관통.", "80% breach chance; 40% of the hit bypasses armor.", 2, 4.6, 5.2, breachChance=0.8, armorPiercing=0.4),
]

def cs_str(s): return '"' + s.replace('\\', '\\\\').replace('"', '\\"') + '"'
def cs_val(v):
    if isinstance(v, bool): return "true" if v else "false"
    if isinstance(v, float): return f"{v}f"
    return str(v)

lib = ["using System.Collections.Generic;", "using AetherArk.Core;", "", "namespace AetherArk.Content", "{",
       "    /// <summary>Mounted weapons. Generated from tools/gen_weapons.py; edit the table, not this file.</summary>",
       "    public static class WeaponLibrary", "    {",
       "        public static void AddAll(Dictionary<string, WeaponDefinition> result)", "        {"]
for e in WEAPONS:
    fx = "".join(f", {k} = {cs_val(v)}" for k, v in e["fx"].items())
    lib.append(f'            result["{e["id"]}"] = new WeaponDefinition {{ id = "{e["id"]}", nameKey = "weapon.{e["id"]}", descriptionKey = "weapon.{e["id"]}.desc", family = WeaponFamily.{e["fam"]}, tier = {e["tier"]}, cost = {COST[e["tier"]]}, powerCost = {e["power"]}, damage = {e["damage"]}f, cooldown = {e["cooldown"]}f{fx} }};')
lib += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/WeaponLibrary.cs", "w", encoding="utf-8").write("\n".join(lib))

loc = ["namespace AetherArk.Content", "{",
       "    /// <summary>Weapon strings. Generated from tools/gen_weapons.py.</summary>",
       "    public sealed partial class LocalizationService", "    {",
       "        private void AddWeaponStrings()", "        {",
       '            Add("enum.weaponfamily.cannon", "함포", "Cannon");',
       '            Add("enum.weaponfamily.lance", "랜스", "Lance");',
       '            Add("enum.weaponfamily.piercer", "관통", "Piercer");',
       '            Add("enum.weaponfamily.missile", "미사일", "Missile");',
       '            Add("enum.weaponfamily.flak", "플랙", "Flak");',
       '            Add("enum.weaponfamily.incendiary", "소이", "Incendiary");',
       '            Add("enum.weaponfamily.breacher", "파공", "Breacher");']
for e in WEAPONS:
    loc.append(f'            Add("weapon.{e["id"]}", {cs_str(e["ko"])}, {cs_str(e["en"])});')
    loc.append(f'            Add("weapon.{e["id"]}.desc", {cs_str(e["dko"])}, {cs_str(e["den"])});')
loc += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/LocalizationService.Weapons.cs", "w", encoding="utf-8").write("\n".join(loc))
print("weapons:", len(WEAPONS))
