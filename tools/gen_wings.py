# Generates WingLibrary.cs and LocalizationService.Wings.cs from one table.
INT, BOM, ESC, REC, ASL = "Interceptor", "Bomber", "Escort", "Recon", "Assault"
COST = {1: 12, 2: 20, 3: 30}

def w(id, type, tier, ko, en, dko, den, strength, ordnance, **fx):
    return dict(id=id, type=type, tier=tier, ko=ko, en=en, dko=dko, den=den, strength=strength, ordnance=ordnance, fx=fx)

WINGS = [
 w("kestrel_interceptors", INT, 1, "황조롱이 요격대", "Kestrel Interceptors", "요격 출격 시 요격 충전 2.", "Intercept sorties grant 2 charges.", 4, 1, interceptCharges=2),
 w("ember_bombers", BOM, 1, "잿불 폭격대", "Ember Bombers", "폭격 피해 6 + 기체 수.", "Bombard for 6 + strength.", 3, 2),
 w("gale_lancers", INT, 2, "돌풍 창기병대", "Gale Lancers", "요격 충전 3, 손실 확률 0.8배.", "3 intercept charges; losses x0.8.", 4, 1, interceptCharges=3, lossResistance=0.8),
 w("ghost_kites", INT, 3, "유령 연대", "Ghost Kites", "5기, 임무 시간 0.7배, 손실 확률 0.7배, 요격 충전 2.", "Five craft, mission time x0.7, losses x0.7, 2 charges.", 5, 1, interceptCharges=2, missionTime=0.7, lossResistance=0.7),
 w("thunder_bombers", BOM, 2, "천둥 폭격대", "Thunder Bombers", "폭격 피해 1.5배, 목표 구획 화재. 군수품 3.", "Bombard x1.5 and sets the target room afire. 3 ordnance.", 3, 3, bombardDamage=1.5, bombardFire=20),
 w("sky_wardens", ESC, 2, "창공 수호대", "Sky Wardens", "호위 시 결계 +8, 요격 충전 2.", "Escort restores 8 ward and grants 2 charges.", 3, 1, escortWard=8, escortCharges=2),
 w("far_eyes", REC, 1, "먼눈 정찰대", "Far Eyes", "2기, 군수품 0, 정찰 25초, 손실 확률 0.5배.", "Two craft, no ordnance, 25 s recon, losses x0.5.", 2, 0, reconSeconds=25, lossResistance=0.5),
 w("storm_marines", ASL, 2, "폭풍 해병대", "Storm Marines", "강습 파괴 공작 48, 적 선체 -3.", "Assault sabotage 48 and 3 hull.", 3, 2, assaultSabotage=48, assaultHull=3),
 w("ruin_dropships", ASL, 3, "파멸 강하정", "Ruin Dropships", "4기, 강습 파괴 공작 64, 적 선체 -5.", "Four craft, assault sabotage 64 and 5 hull.", 4, 3, assaultSabotage=64, assaultHull=5),
]

def cs_str(s): return '"' + s.replace('\\', '\\\\').replace('"', '\\"') + '"'
def cs_val(v):
    if isinstance(v, bool): return "true" if v else "false"
    if isinstance(v, float): return f"{v}f"
    return str(v)

lib = ["using System.Collections.Generic;", "using AetherArk.Core;", "", "namespace AetherArk.Content", "{",
       "    /// <summary>Air wings. Generated from tools/gen_wings.py; edit the table, not this file.</summary>",
       "    public static class WingLibrary", "    {",
       "        public static void AddAll(Dictionary<string, WingDefinition> result)", "        {"]
for e in WINGS:
    fx = "".join(f", {k} = {cs_val(v)}" for k, v in e["fx"].items())
    lib.append(f'            result["{e["id"]}"] = new WingDefinition {{ id = "{e["id"]}", nameKey = "wing.{e["id"]}", descriptionKey = "wing.{e["id"]}.desc", type = SquadronType.{e["type"]}, tier = {e["tier"]}, cost = {COST[e["tier"]]}, strength = {e["strength"]}, ordnanceCost = {e["ordnance"]}{fx} }};')
lib += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/WingLibrary.cs", "w", encoding="utf-8").write("\n".join(lib))

loc = ["namespace AetherArk.Content", "{",
       "    /// <summary>Wing strings. Generated from tools/gen_wings.py.</summary>",
       "    public sealed partial class LocalizationService", "    {",
       "        private void AddWingStrings()", "        {"]
for e in WINGS:
    loc.append(f'            Add("wing.{e["id"]}", {cs_str(e["ko"])}, {cs_str(e["en"])});')
    loc.append(f'            Add("wing.{e["id"]}.desc", {cs_str(e["dko"])}, {cs_str(e["den"])});')
loc += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/LocalizationService.Wings.cs", "w", encoding="utf-8").write("\n".join(loc))
print("wings:", len(WINGS))
