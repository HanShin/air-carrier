# Generates EncounterLibrary.cs and LocalizationService.Encounters.cs from one data table,
# so choice ids and localization keys can never drift apart.
R, S, T, C, W = "Rescue", "Salvage", "Trade", "Checkpoint", "Storm"

def ch(id, ko, en, rko, ren, **fx):
    return dict(id=id, ko=ko, en=en, rko=rko, ren=ren, fx=fx)

EVENTS = [
# ---------------- RESCUE ----------------
dict(id="burning_ferry", type=R, tko="불타는 연락선", ten="THE BURNING FERRY",
 bko="엔진 화재가 번진 연락선이 구름 위를 헤맵니다. 갑판에 승객이 몰려 있고, 불길은 몇 분 안에 연료고에 닿을 것입니다.",
 ben="A ferry drifts above the clouds with an engine fire spreading toward its fuel bunker. Passengers crowd the deck; you have minutes.",
 choices=[
  ch("teams","소화반을 보내 차분히 구조한다 (보급 1)","Send fire teams and evacuate methodically (1 supply)","소화반이 불길을 잡는 동안 승객 40명이 옮겨 탔습니다.","Fire teams hold the blaze while forty passengers cross.",suppliesCost=1,survivorDelta=40,moraleDelta=5),
  ch("dash","선체를 붙이고 전원 탈출시킨다 — 위험한 접근","Lash hulls and take everyone — a dangerous approach","무모했지만 통했습니다. 70명이 살았고 선단이 환호합니다.","Reckless, but it works. Seventy are saved and the convoy cheers.",survivorDelta=70,moraleDelta=6,successChance=0.6,failureChoiceId="dash_fail"),
  ch("dash_fail","연료고 폭발","The bunker detonates","연료고가 터지며 선체가 그을렸습니다. 20명만 구했습니다.","The bunker blows; the hull is scorched and only twenty are pulled clear.",hidden=True,hullDelta=-4,survivorDelta=20,moraleDelta=-3),
  ch("leave","거리를 둔다","Keep your distance","폭발의 빛이 구름을 물들이고, 선단은 침묵합니다.","The explosion lights the clouds, and the convoy falls silent.",moraleDelta=-5)]),
dict(id="ice_locked_lifeboats", type=R, tko="결빙된 구명정", ten="ICE-LOCKED LIFEBOATS",
 bko="얼어붙은 구명정 세 척이 빙운 속에 갇혀 있습니다. 안에서 두드리는 소리가 들립니다.",
 ben="Three lifeboats hang frozen inside an ice cloud. Something knocks from within.",
 choices=[
  ch("thaw","[드워프] 용광로 배관으로 천천히 녹인다 (보급 1)","[Dwarf] Thaw them with forge lines (1 supply)","드워프 기술자들이 얼음을 녹여 30명을 무사히 데려왔습니다.","Dwarven hands melt the ice away and thirty come aboard unharmed.",requiredTag="lineage.dwarf",suppliesCost=1,survivorDelta=30,moraleDelta=4),
  ch("blast","에테르로 얼음을 깨뜨린다 (에테르 1)","Crack the ice with an aether pulse (1 aether)","얼음이 깨지고 구명정과 장비를 회수했습니다.","The ice shatters; you recover the boats and their gear.",aetherCost=1,survivorDelta=28,salvageDelta=3,moraleDelta=1),
  ch("leave","두드림을 뒤로하고 떠난다","Leave the knocking behind","두드리는 소리가 잦아들 때까지 아무도 말을 하지 않았습니다.","No one speaks until the knocking fades.",moraleDelta=-4)]),
dict(id="mutiny_transport", type=R, tko="반란 수송선", ten="THE MUTINOUS TRANSPORT",
 bko="수송선의 승무원이 선장을 가두고 선단 합류를 요구합니다. 승객들은 겁에 질려 있습니다.",
 ben="A transport's crew has locked up their captain and demand to join the convoy. The passengers are terrified.",
 choices=[
  ch("intimidate","[오크] 무장 승무원을 보내 질서를 잡는다","[Orc] Send armed crew to restore order","반란자들은 오크 해병을 보자마자 무기를 내렸습니다.","The mutineers drop their weapons the moment the orc marines board.",requiredTag="lineage.orc",survivorDelta=55,moraleDelta=2),
  ch("negotiate","보급 2를 주고 협상한다","Negotiate with 2 supplies","보급을 나누자 양측이 합류에 동의했습니다.","Shared stores bring both sides to the table.",suppliesCost=2,survivorDelta=55,moraleDelta=6),
  ch("refuse","반란선을 선단에 들이지 않는다","Refuse the mutineers","수송선은 홀로 구름 속으로 사라졌습니다.","The transport vanishes alone into the clouds.",moraleDelta=-3)]),
dict(id="plague_barge", type=R, tko="역병 바지선", ten="THE PLAGUE BARGE",
 bko="격리 깃발을 단 바지선이 도움을 청합니다. 생존자들은 열에 시달리지만 아직 걸을 수 있습니다.",
 ben="A barge flying quarantine flags pleads for help. Its survivors are feverish but still on their feet.",
 choices=[
  ch("quarantine","[병원선] 격리 병동으로 받아들인다","[Hospital ship] Take them into the isolation ward","병원선이 환자를 받았고 역병은 번지지 않았습니다.","The hospital ship takes the sick, and the fever spreads no further.",requiredTag="support.hospital",survivorDelta=60,moraleDelta=5),
  ch("take","그냥 받아들인다 — 절반의 도박","Take them anyway — a coin toss","열이 가라앉았습니다. 60명이 합류했습니다.","The fever breaks. Sixty join the convoy.",survivorDelta=60,moraleDelta=4,successChance=0.5,failureChoiceId="take_fail"),
  ch("take_fail","역병 확산","The fever spreads","역병이 선단에 번져 30명을 잃었습니다.","The fever spreads through the convoy and thirty are lost.",hidden=True,survivorDelta=-30,moraleDelta=-8),
  ch("refuse","떠난다","Leave them","바지선의 신호가 점점 약해집니다.","The barge's signal grows fainter.",moraleDelta=-4)]),
dict(id="child_choir", type=R, tko="떠도는 성가대", ten="THE WANDERING CHOIR",
 bko="음악학교 비행선이 연료를 다 쓰고 표류합니다. 아이들이 갑판에서 노래하며 신호를 보냅니다.",
 ben="A music-school airship has run dry. Children on its deck sing to signal you.",
 choices=[
  ch("take","모두 태운다 (보급 1)","Take them all aboard (1 supply)","성가대의 노래가 선단 통신망에 퍼지자 사기가 치솟았습니다.","Their song spreads across the convoy channel and morale soars.",suppliesCost=1,survivorDelta=18,moraleDelta=10),
  ch("fuel","에테르 1을 나눠 준다","Share 1 aether","비행선은 다시 떠올랐고, 아이들이 손을 흔들었습니다.","The airship lifts again; the children wave.",aetherCost=1,moraleDelta=6),
  ch("leave","지나친다","Pass them by","노래가 뒤에서 잦아듭니다.","The song fades behind you.",moraleDelta=-6)]),
dict(id="imperial_deserters", type=R, tko="제국 탈영병", ten="IMPERIAL DESERTERS",
 bko="제국 순찰정 한 척이 항복 깃발을 올렸습니다. 승무원은 탈영을 원하며 무기고를 대가로 내놓습니다.",
 ben="An imperial picket raises a surrender flag. Its crew want to desert and offer their magazine in return.",
 choices=[
  ch("accept","받아들인다","Take them in","군수품을 받았지만 선단 일부는 제국 제복을 불신합니다.","You gain ordnance, but parts of the convoy distrust the uniforms.",survivorDelta=25,ordnanceDelta=2,moraleDelta=-3),
  ch("interrogate","[인간] 옛 동료로서 항로 정보를 얻는다","[Human] Trade on old ties for route intelligence","탈영병이 제국 연료고 좌표를 넘겼습니다.","The deserters hand over the coordinates of an imperial fuel cache.",requiredTag="lineage.human",survivorDelta=25,aetherDelta=2),
  ch("refuse","거절한다","Turn them away","순찰정은 항복 깃발을 내리고 멀어졌습니다.","The picket lowers its flag and drifts away.",moraleDelta=-1)]),
dict(id="stranded_engineers", type=R, tko="조난 기술자", ten="STRANDED ENGINEERS",
 bko="부서진 정비선의 기술자들이 공구를 든 채 구조를 기다립니다.",
 ben="Engineers from a wrecked tender wait for rescue, tools in hand.",
 choices=[
  ch("take","태우고 수리를 맡긴다","Take them aboard and put them to work","기술자들이 선체 균열을 메웠습니다.","The engineers weld the flagship's cracks shut.",survivorDelta=12,hullDelta=6,moraleDelta=2),
  ch("pay","인양물 3을 주고 정식 수리를 부탁한다","Pay 3 salvage for a proper refit","자재를 받은 기술자들이 장갑판까지 손봤습니다.","With materials in hand they patch armor as well as hull.",salvageCost=3,survivorDelta=12,hullDelta=10,armorDelta=4,moraleDelta=3),
  ch("leave","떠난다","Leave them","정비선의 불빛이 꺼졌습니다.","The tender's lights go dark.",moraleDelta=-3)]),
dict(id="sky_whale_calf", type=R, tko="다친 하늘고래 새끼", ten="THE WOUNDED SKY-WHALE CALF",
 bko="그물에 얽힌 하늘고래 새끼가 선단 곁을 맴돕니다. 어미는 보이지 않습니다.",
 ben="A sky-whale calf tangled in netting circles the convoy. Its mother is nowhere in sight.",
 choices=[
  ch("calm","[조인] 노래로 달래며 그물을 푼다","[Avian] Sing it calm and cut the net","풀려난 새끼가 에테르 결정을 떨어뜨리고 떠났습니다.","Freed, the calf sheds an aether crystal and swims away.",requiredTag="lineage.avian",moraleDelta=9,aetherDelta=1),
  ch("free","보급 1을 미끼로 그물을 푼다","Lure it with 1 supply and cut the net","새끼가 선단 주위를 한 바퀴 돌고 사라졌습니다.","The calf circles the convoy once and vanishes.",suppliesCost=1,moraleDelta=5),
  ch("harvest","사냥해 자원으로 쓴다","Harvest it","자원은 얻었지만 선단은 그 울음을 잊지 못합니다.","The convoy gains salvage and cannot forget the sound it made.",salvageDelta=8,moraleDelta=-9)]),
dict(id="wreck_signal_trap", type=R, tko="부르는 난파선", ten="THE WRECK THAT CALLED",
 bko="구조 신호가 나오는 난파선 주위로 기류가 이상하게 고요합니다.",
 ben="A wreck broadcasts a distress call. The air around it is strangely still.",
 choices=[
  ch("detect","[정찰선] 주변을 먼저 훑는다","[Pathfinder] Sweep the approach first","정찰선이 매복 기뢰를 찾아냈고, 난파선은 조용히 인양됐습니다.","The pathfinder finds the ambush mines; the wreck is quietly salvaged.",requiredTag="support.pathfinder",salvageDelta=6,moraleDelta=2),
  ch("approach","그대로 접근한다 (55%)","Approach directly (55%)","진짜 생존자였습니다. 50명이 합류했습니다.","The call was real. Fifty survivors join you.",survivorDelta=50,moraleDelta=5,successChance=0.55,failureChoiceId="ambush"),
  ch("ambush","매복","Ambush","난파선 뒤에서 제국 커터가 튀어나옵니다!","An imperial cutter bursts from behind the wreck!",hidden=True,startsBattle=True,moraleDelta=-2),
  ch("ignore","무시한다","Ignore the call","신호가 뒤에서 되풀이됩니다.","The signal repeats behind you.",moraleDelta=-2)]),
# ---------------- SALVAGE ----------------
dict(id="derelict_cruiser", type=S, tko="표류 순양함", ten="THE DERELICT CRUISER",
 bko="제국 순양함 한 척이 등을 꺼트린 채 표류합니다. 무장은 남아 있고 코어는 불안정하게 웅웅거립니다.",
 ben="An imperial cruiser drifts dark. Its guns are intact and its core hums unsteadily.",
 choices=[
  ch("strip","무장고를 뜯어낸다","Strip the magazines","군수품과 부품을 확보했습니다.","Ordnance and parts secured.",ordnanceDelta=4,salvageDelta=4),
  ch("core","[드워프] 코어를 추출한다 (70%)","[Dwarf] Extract the core (70%)","코어가 안전하게 빠져나왔습니다.","The core comes free cleanly.",requiredTag="lineage.dwarf",aetherDelta=3,salvageDelta=3,successChance=0.7,failureChoiceId="core_fail"),
  ch("core_fail","코어 폭주","Core surge","코어가 폭주하며 선체를 태웠습니다.","The core surges and scorches the hull.",hidden=True,hullDelta=-5,instabilityDelta=15),
  ch("leave","손대지 않는다","Leave it","순양함은 계속 표류합니다.","The cruiser drifts on.")]),
dict(id="aether_geyser", type=S, tko="에테르 간헐천", ten="THE AETHER GEYSER",
 bko="구름 아래에서 에테르 기둥이 주기적으로 솟구칩니다.",
 ben="Columns of aether erupt from beneath the clouds at intervals.",
 choices=[
  ch("harvest","코어를 열어 최대한 흡수한다","Open the core and drink deep","에테르는 넘치지만 코어가 불안하게 떨립니다.","Aether overflows; the core shudders.",aetherDelta=4,instabilityDelta=20),
  ch("careful","가장자리에서 조심스럽게 채집한다","Skim the edges carefully","안전하게 에테르를 채웠습니다.","Aether gathered safely.",aetherDelta=2,instabilityDelta=5),
  ch("pass","지나간다","Pass it by","간헐천이 뒤에서 솟구칩니다.","The geyser erupts behind you.")]),
dict(id="floating_monastery", type=S, tko="부유 수도원", ten="THE FLOATING MONASTERY",
 bko="버려진 수도원이 조용히 떠 있습니다. 성유물과 낡은 기도문이 남아 있습니다.",
 ben="An abandoned monastery floats in silence, relics and worn prayers still inside.",
 choices=[
  ch("relics","성유물을 가져간다","Take the relics","성유물은 값지지만 순례자들이 눈을 돌립니다.","The relics are valuable; the pilgrims look away.",salvageDelta=10,moraleDelta=-6),
  ch("prayers","기도문을 선단에 낭독한다","Read the prayers to the convoy","오래된 기도가 선단을 달랬습니다.","Old words steady the convoy.",moraleDelta=7),
  ch("resonate","[엘프] 성가의 공명으로 코어를 채운다","[Elf] Tune the core to the chant","성가가 코어에 에테르를 불어넣었습니다.","The chant breathes aether into the core.",requiredTag="lineage.elf",aetherDelta=2,moraleDelta=4)]),
dict(id="mine_field", type=S, tko="옛 기뢰 지대", ten="THE OLD MINEFIELD",
 bko="옛 전쟁의 기뢰가 항로를 막습니다. 기뢰 사이에 침몰선 잔해가 보입니다.",
 ben="Mines from an old war block the lane. Wreckage glints between them.",
 choices=[
  ch("chart","[정찰선] 안전한 길을 그린다","[Pathfinder] Chart a safe path","정찰선이 기뢰를 해체해 군수품으로 만들었습니다.","The pathfinder defuses mines into ordnance.",requiredTag="support.pathfinder",ordnanceDelta=3,salvageDelta=5),
  ch("thread","기뢰 사이를 통과한다 (60%)","Thread the mines (60%)","잔해를 챙겨 무사히 빠져나왔습니다.","You pull salvage from the wreck and slip out.",salvageDelta=9,successChance=0.6,failureChoiceId="thread_fail"),
  ch("thread_fail","기뢰 접촉","Mine strike","기뢰가 터져 장갑이 찢겼습니다.","A mine detonates against the armor.",hidden=True,hullDelta=-6,armorDelta=-4,salvageDelta=3),
  ch("avoid","우회한다 (에테르 1)","Go around (1 aether)","연료를 태워 기뢰 지대를 돌아갔습니다.","You burn fuel to skirt the field.",aetherCost=1)]),
dict(id="crashed_courier", type=S, tko="추락한 급사선", ten="THE CRASHED COURIER",
 bko="암호 장치가 켜진 채 추락한 제국 급사선이 있습니다.",
 ben="An imperial courier has crashed with its cipher engine still running.",
 choices=[
  ch("decode","암호를 풀어 보급 좌표를 찾는다","Decode it for depot coordinates","근처 보급 좌표를 얻었습니다.","You find a nearby depot's coordinates.",aetherDelta=1,suppliesDelta=2,moraleDelta=1),
  ch("sell","장치를 뜯어 팔 준비를 한다","Rip out the engine to sell","암호 장치는 값이 나갈 것입니다.","The engine will fetch a good price.",salvageDelta=7),
  ch("burn","급사선을 불태워 흔적을 지운다","Burn the courier to hide your trail","추격군이 며칠은 헤맬 것입니다.","The pursuit will chase ghosts for days.",moraleDelta=3,ordnanceDelta=1)]),
dict(id="cloud_farm", type=S, tko="구름 농장 잔해", ten="THE CLOUD FARM",
 bko="부서진 수경 농장이 떠다닙니다. 작물은 아직 살아 있고, 농부들도 일부 남았습니다.",
 ben="A broken hydroponic farm drifts by. Its crops still live, and so do a few farmers.",
 choices=[
  ch("harvest","작물을 거둔다","Harvest the crops","보급이 늘었습니다.","Supplies replenished.",suppliesDelta=6),
  ch("settle","농부들과 작물을 함께 태운다","Take the farmers and their crops","농부들이 선단 식량 관리를 맡았습니다.","The farmers take charge of the convoy's rations.",survivorDelta=20,suppliesDelta=3,moraleDelta=3),
  ch("leave","지나친다","Leave it","농장이 뒤로 멀어집니다.","The farm drifts behind.")]),
dict(id="ordnance_cache", type=S, tko="봉인된 저장고", ten="THE SEALED CACHE",
 bko="제국 군수 저장고가 봉인된 채 절벽에 박혀 있습니다.",
 ben="A sealed imperial ordnance cache is wedged into a cliff.",
 choices=[
  ch("careful","천천히 봉인을 푼다","Work the seal open slowly","군수품 일부를 회수했습니다.","Some ordnance recovered.",ordnanceDelta=3),
  ch("blast","폭파해서 연다 (65%)","Blow it open (65%)","저장고가 통째로 열렸습니다.","The whole cache is yours.",ordnanceDelta=6,salvageDelta=2,successChance=0.65,failureChoiceId="blast_fail"),
  ch("blast_fail","유폭","Sympathetic detonation","저장고가 유폭해 선체가 흔들렸습니다.","The cache cooks off and rattles the hull.",hidden=True,ordnanceDelta=1,hullDelta=-3),
  ch("leave","건드리지 않는다","Leave it sealed","저장고는 절벽에 남았습니다.","The cache stays in the cliff.")]),
dict(id="sky_kelp_forest", type=S, tko="하늘 켈프 숲", ten="THE SKY-KELP FOREST",
 bko="부유 켈프 숲이 항로를 덮습니다. 켈프 사이에 새 둥지와 오래된 잔해가 있습니다.",
 ben="Floating kelp blankets the lane, nests and old wreckage hidden in its fronds.",
 choices=[
  ch("gather","켈프를 거둔다","Gather kelp","식용 켈프를 저장했습니다.","Edible kelp stored.",suppliesDelta=5),
  ch("scavenge","[고블린] 둥지와 잔해를 뒤진다","[Goblin] Pick through the nests and wreckage","고블린들이 켈프 속에서 값진 부품을 찾았습니다.","The goblins find valuable parts in the fronds.",requiredTag="lineage.goblin",salvageDelta=6,suppliesDelta=2),
  ch("push","밀고 나간다 (에테르 1)","Push through (1 aether)","켈프를 헤치고 나왔습니다.","You force a way through.",aetherCost=1,moraleDelta=1)]),
dict(id="gate_shard", type=S, tko="천공문 파편", ten="THE GATE SHARD",
 bko="고대 천공문의 파편이 공중에 떠 있습니다. 코어가 그 진동에 반응합니다.",
 ben="A shard of an ancient sky gate hangs in the air. Your core answers its hum.",
 choices=[
  ch("study","파편을 연구한다","Study the shard","파편의 진동에서 에테르를 얻었습니다.","The shard's hum yields aether.",aetherDelta=2,moraleDelta=3),
  ch("sell","파편을 잘라 판다","Cut it up to sell","파편은 상인들에게 큰 값을 받을 것입니다.","The fragments will fetch a fortune.",salvageDelta=12),
  ch("attune","[엘프] 파편에 코어를 공명시킨다","[Elf] Attune the core to the shard","코어가 넘치도록 채워졌지만 불안정해졌습니다.","The core brims — and trembles.",requiredTag="lineage.elf",aetherDelta=4,instabilityDelta=10)]),
# ---------------- TRADE ----------------
dict(id="smuggler_flotilla", type=T, tko="밀수단 선단", ten="THE SMUGGLER FLOTILLA",
 bko="깃발 없는 밀수선들이 거래를 제안합니다. 가격은 좋지만 질문은 받지 않습니다.",
 ben="Flagless smugglers offer a deal. The prices are good and no questions are taken.",
 choices=[
  ch("ordnance","인양물 5로 군수품 4","4 ordnance for 5 salvage","군수품이 조용히 옮겨졌습니다.","The ordnance changes hands quietly.",salvageCost=5,ordnanceDelta=4),
  ch("aether","인양물 7로 에테르 4","4 aether for 7 salvage","연료 탱크가 채워졌습니다.","Your tanks are filled.",salvageCost=7,aetherDelta=4),
  ch("sell","보급 2를 팔아 인양물 9","Sell 2 supplies for 9 salvage","밀수단이 식량에 후한 값을 쳤습니다.","The smugglers pay well for food.",suppliesCost=2,salvageDelta=9),
  ch("depart","거래하지 않는다","Decline","밀수선들이 구름 속으로 흩어졌습니다.","The smugglers scatter into the clouds.")]),
dict(id="guild_caravan", type=T, tko="길드 상단", ten="THE GUILD CARAVAN",
 bko="무장한 상단이 폭풍을 피해 호위를 원합니다.",
 ben="An armed guild caravan wants an escort through the storm.",
 choices=[
  ch("escort","군수품 1을 쓰며 호위한다","Escort them (1 ordnance)","상단이 호위비를 후하게 치렀습니다.","The caravan pays handsomely for the escort.",ordnanceCost=1,salvageDelta=8,moraleDelta=3),
  ch("buy","인양물 5로 보급 6","6 supplies for 5 salvage","보급이 늘었습니다.","Supplies replenished.",salvageCost=5,suppliesDelta=6),
  ch("depart","떠난다","Move on","상단은 다른 호위를 찾아 떠났습니다.","The caravan seeks another escort.")]),
dict(id="refit_yard", type=T, tko="개장 조선소", ten="THE REFIT YARD",
 bko="떠돌이 조선소가 개장 서비스를 광고합니다.",
 ben="A roaming refit yard advertises its services.",
 choices=[
  ch("refit","인양물 6으로 편대 전면 보충","Full air-wing refit for 6 salvage","편대가 정원을 채웠습니다.","Both wings are back to full strength.",salvageCost=6,refitSquadrons=True),
  ch("plating","인양물 9로 장갑판 보강","Armor plating for 9 salvage","새 장갑판이 선체를 감쌌습니다.","Fresh plating wraps the hull.",salvageCost=9,armorDelta=8),
  ch("depart","떠난다","Move on","조선소가 다음 손님을 부릅니다.","The yard hails its next customer.")]),
dict(id="black_market", type=T, tko="암시장", ten="THE BLACK MARKET",
 bko="부유 암시장이 싼 에테르를 팝니다. 출처는 묻지 않는 편이 낫습니다.",
 ben="A floating black market sells cheap aether. Better not to ask where it came from.",
 choices=[
  ch("cheap","인양물 4로 에테르 3","3 aether for 4 salvage","출처 불명의 에테르가 탱크에 들어갔습니다.","Aether of uncertain origin fills the tanks.",salvageCost=4,aetherDelta=3,moraleDelta=-2),
  ch("haggle","[고블린] 흥정한다","[Goblin] Haggle","고블린의 흥정으로 덤까지 챙겼습니다.","Goblin haggling wins a bonus.",requiredTag="lineage.goblin",salvageCost=3,aetherDelta=3,suppliesDelta=2),
  ch("depart","떠난다","Leave","시장의 불빛이 멀어집니다.","The market's lights fade.")]),
dict(id="pilgrim_bazaar", type=T, tko="순례자 시장", ten="THE PILGRIM BAZAAR",
 bko="순례선단이 임시 시장을 열었습니다. 사기 진작 물품과 성물이 오갑니다.",
 ben="A pilgrim fleet has opened a bazaar of comforts and holy trinkets.",
 choices=[
  ch("comforts","보급 1로 위문품 구입","Buy comforts for 1 supply","선단에 온기가 돌았습니다.","Warmth spreads through the convoy.",suppliesCost=1,moraleDelta=8),
  ch("sell","성물을 판다","Sell holy trinkets","돈은 벌었지만 순례자들의 시선이 차갑습니다.","You profit; the pilgrims' eyes are cold.",salvageDelta=5,moraleDelta=-3),
  ch("depart","떠난다","Leave","시장이 문을 닫습니다.","The bazaar closes.")]),
dict(id="mercenary_wing", type=T, tko="용병 편대", ten="THE MERCENARY WING",
 bko="용병 편대가 정비와 호위를 제안합니다. 값은 비쌉니다.",
 ben="A mercenary wing offers maintenance and escort. It is not cheap.",
 choices=[
  ch("hire","인양물 10으로 계약","Hire them for 10 salvage","용병들이 편대를 보충하고 군수품을 나눴습니다.","The mercenaries refit your wings and share ordnance.",salvageCost=10,ordnanceDelta=3,refitSquadrons=True),
  ch("ordnance","인양물 4로 군수품 2","2 ordnance for 4 salvage","군수품을 구입했습니다.","Ordnance purchased.",salvageCost=4,ordnanceDelta=2),
  ch("depart","거절한다","Decline","용병들이 경례하고 떠났습니다.","The mercenaries salute and depart.")]),
dict(id="fuel_barge", type=T, tko="연료 바지선", ten="THE FUEL BARGE",
 bko="연료 바지선이 에테르를 팝니다. 식량도 받습니다.",
 ben="A fuel barge sells aether and accepts food in trade.",
 choices=[
  ch("small","보급 3으로 에테르 3","3 aether for 3 supplies","연료를 보충했습니다.","Fuel topped up.",suppliesCost=3,aetherDelta=3),
  ch("big","인양물 8로 에테르 6","6 aether for 8 salvage","탱크가 가득 찼습니다.","Tanks full.",salvageCost=8,aetherDelta=6),
  ch("depart","떠난다","Leave","바지선 승무원이 손을 흔듭니다.","The barge crew wave you off.")]),
dict(id="quartermaster", type=T, tko="부패한 보급관", ten="THE CROOKED QUARTERMASTER",
 bko="제국 보급관이 몰래 물자를 팔겠다고 합니다.",
 ben="An imperial quartermaster offers to sell stores under the table.",
 choices=[
  ch("bribe","인양물 7로 군수품 5","5 ordnance for 7 salvage","군수품이 조용히 넘어왔습니다.","The ordnance arrives quietly.",salvageCost=7,ordnanceDelta=5,moraleDelta=-2),
  ch("pose","[인간] 감찰관 행세로 물자를 압수한다","[Human] Pose as inspectors and seize stores","보급관은 서류를 보고 물자를 내줬습니다.","One look at the papers and the stores are yours.",requiredTag="lineage.human",suppliesCost=1,ordnanceDelta=4,aetherDelta=1),
  ch("depart","떠난다","Leave","보급관이 어깨를 으쓱합니다.","The quartermaster shrugs.")]),
dict(id="shipwright", type=T, tko="떠돌이 조선공", ten="THE WANDERING SHIPWRIGHT",
 bko="은퇴한 조선공이 수리 대가로 인양물을 원합니다.",
 ben="A retired shipwright will patch you up for salvage.",
 choices=[
  ch("repair","인양물 7로 기함 수리","Repair the flagship for 7 salvage","조선공이 선체와 구획을 손봤습니다.","The shipwright patches hull and compartments.",salvageCost=7),
  ch("reinforce","인양물 5로 선체 보강","Reinforce the hull for 5 salvage","보강재가 선체를 감쌌습니다.","Bracing wraps the hull.",salvageCost=5,hullDelta=5,armorDelta=3),
  ch("depart","떠난다","Leave","조선공이 다음 항구로 향합니다.","The shipwright heads for the next port.")]),
# ---------------- CHECKPOINT ----------------
dict(id="customs_inspection", type=C, tko="세관 검사", ten="THE CUSTOMS INSPECTION",
 bko="세관선이 화물 검사를 요구합니다. 선단에 숨겨 둔 피난민이 있습니다.",
 ben="A customs cutter demands to inspect cargo. Refugees are hidden in your holds.",
 choices=[
  ch("submit","보급 2를 '관세'로 낸다","Pay 2 supplies in 'duties'","세관은 서류를 보지도 않고 떠났습니다.","Customs leaves without reading a page.",suppliesCost=2,moraleDelta=-1),
  ch("hide","피난민을 숨기고 통과한다 (60%)","Hide the refugees and bluff (60%)","세관은 아무것도 찾지 못했습니다.","Customs finds nothing.",moraleDelta=4,successChance=0.6,failureChoiceId="hide_fail"),
  ch("hide_fail","발각","Discovered","피난민이 발각됐습니다. 세관선이 포문을 엽니다!","The refugees are found. The cutter opens fire!",hidden=True,startsBattle=True,moraleDelta=-3),
  ch("fight","먼저 포문을 연다","Open fire first","세관선이 결계를 올립니다.","The cutter raises its ward.",startsBattle=True)]),
dict(id="loyalty_oath", type=C, tko="충성 서약", ten="THE LOYALTY OATH",
 bko="제국 선전선이 통과 조건으로 공개 충성 서약을 요구합니다.",
 ben="An imperial broadcast ship demands a public loyalty oath as the price of passage.",
 choices=[
  ch("swear","서약하고 연료를 받는다","Swear and take the fuel","서약이 방송됐습니다. 선단은 부끄러워합니다.","The oath is broadcast. The convoy is ashamed.",aetherDelta=2,moraleDelta=-8),
  ch("recite","[인간] 옛 공화국의 서약을 대신 읊는다","[Human] Recite the old republic's oath instead","장교는 미소를 지으며 통과를 허락했습니다.","The officer smiles and waves you through.",requiredTag="lineage.human",moraleDelta=2,aetherDelta=1),
  ch("refuse","거부하고 돌파한다","Refuse and break through","선전선이 호위함을 부릅니다.","The broadcast ship calls its escort.",startsBattle=True,moraleDelta=3)]),
dict(id="bounty_hunters", type=C, tko="현상금 사냥꾼", ten="THE BOUNTY HUNTERS",
 bko="현상금 사냥꾼들이 함장의 목에 걸린 값을 들먹입니다.",
 ben="Bounty hunters name the price on your captain's head.",
 choices=[
  ch("pay","인양물 8로 매수한다","Buy them off for 8 salvage","사냥꾼들이 돈을 세며 떠났습니다.","The hunters leave counting coin.",salvageCost=8),
  ch("intimidate","[오크] 해병을 갑판에 세운다","[Orc] Line the deck with marines","사냥꾼들은 다시 생각했습니다.","The hunters reconsider.",requiredTag="lineage.orc",moraleDelta=4),
  ch("fight","싸운다","Fight","사냥꾼선이 무장을 펼칩니다.","The hunters run out their guns.",startsBattle=True)]),
dict(id="blockade_toll", type=C, tko="봉쇄 통행세", ten="THE BLOCKADE TOLL",
 bko="제국 봉쇄선이 통행세를 요구합니다. 뒤에는 순양함이 대기 중입니다.",
 ben="An imperial blockade demands a toll. A cruiser waits behind it.",
 choices=[
  ch("pay","에테르 1과 인양물 4를 낸다","Pay 1 aether and 4 salvage","봉쇄선이 길을 열었습니다.","The blockade opens.",aetherCost=1,salvageCost=4),
  ch("run","전속으로 돌파한다 (50%)","Run the blockade (50%)","봉쇄선이 미처 반응하지 못했습니다.","The blockade fails to react in time.",moraleDelta=5,successChance=0.5,failureChoiceId="run_fail"),
  ch("run_fail","차단","Intercepted","순양함이 앞을 막아섭니다!","The cruiser cuts you off!",hidden=True,startsBattle=True,battleTier=2),
  ch("fight","순양함과 정면으로 싸운다","Fight the cruiser head-on","순양함이 전투 태세에 들어갑니다.","The cruiser clears for action.",startsBattle=True,battleTier=2)]),
dict(id="propaganda_broadcast", type=C, tko="선전 방송", ten="THE PROPAGANDA BROADCAST",
 bko="제국 방송이 선단 통신망을 점령해 항복을 종용합니다.",
 ben="Imperial propaganda floods the convoy channel, urging surrender.",
 choices=[
  ch("jam","군수품 1을 써 방해 전파를 쏜다","Spend 1 ordnance to jam it","방송이 끊기자 환호가 터졌습니다.","The broadcast dies and the convoy cheers.",ordnanceCost=1,moraleDelta=5),
  ch("counter","함장이 직접 연설한다","The captain answers on the open channel","함장의 목소리가 선단을 붙잡았습니다.","The captain's voice holds the convoy together.",moraleDelta=2),
  ch("ignore","무시한다","Ignore it","방송이 며칠간 이어졌습니다.","The broadcast drones on for days.",moraleDelta=-6)]),
dict(id="reformist_courier", type=C, tko="개혁파 급사", ten="THE REFORMIST COURIER",
 bko="개혁파 급사선이 추격을 받으며 도움을 청합니다.",
 ben="A reformist courier, pursued, asks for cover.",
 choices=[
  ch("help","보급 1을 주고 숨겨 준다","Hide them for 1 supply","급사는 비밀 연료고 좌표로 보답했습니다.","The courier repays you with the coordinates of a hidden fuel cache.",suppliesCost=1,aetherDelta=2,moraleDelta=4),
  ch("refuse","관여하지 않는다","Stay out of it","급사선이 홀로 사라졌습니다.","The courier vanishes alone.",moraleDelta=-2)]),
dict(id="hostage_exchange", type=C, tko="인질 교환", ten="THE HOSTAGE EXCHANGE",
 bko="제국 초계함이 선단 출신 인질을 군수품과 바꾸자고 합니다.",
 ben="An imperial picket offers convoy hostages in exchange for ordnance.",
 choices=[
  ch("trade","군수품 2로 교환한다","Trade 2 ordnance","인질들이 돌아왔습니다.","The hostages come home.",ordnanceCost=2,survivorDelta=40,moraleDelta=6),
  ch("assault","강습대로 구출한다 (50%)","Storm the picket (50%)","강습대가 인질을 구출했습니다.","The boarders free the hostages.",survivorDelta=40,moraleDelta=8,successChance=0.5,failureChoiceId="assault_fail"),
  ch("assault_fail","강습 실패","Assault fails","강습이 막혔고 초계함이 반격합니다!","The assault is repulsed and the picket fights back!",hidden=True,startsBattle=True,moraleDelta=-3),
  ch("refuse","거절한다","Refuse","초계함이 인질을 데리고 떠났습니다.","The picket leaves with the hostages.",moraleDelta=-4)]),
dict(id="spy_aboard", type=C, tko="함내 첩자", ten="THE SPY ABOARD",
 bko="제국 검문관이 선단에 첩자가 있다고 경고합니다. 진심일까요?",
 ben="An imperial inspector warns that a spy hides in your convoy. Is it true?",
 choices=[
  ch("search","보급 1을 쓰며 전수 조사한다","Search every hold (1 supply)","첩자와 숨겨진 폭약을 찾았습니다.","You find the spy and a hidden charge.",suppliesCost=1,moraleDelta=-2,ordnanceDelta=1),
  ch("spot","[조인] 눈으로 찾아낸다","[Avian] Pick the spy out by sight","조인의 눈이 첩자를 골라냈습니다.","Avian eyes single out the spy.",requiredTag="lineage.avian",moraleDelta=5),
  ch("ignore","무시한다 (50%)","Ignore the warning (50%)","경고는 헛소리였습니다.","The warning was nothing.",moraleDelta=1,successChance=0.5,failureChoiceId="sabotage"),
  ch("sabotage","파괴 공작","Sabotage","첩자가 연료를 빼돌리고 코어를 흔들었습니다.","The spy bleeds fuel and rattles the core.",hidden=True,aetherDelta=-2,instabilityDelta=10)]),
dict(id="pilgrim_blockade", type=C, tko="문 앞의 순례자", ten="THE PILGRIMS AT THE GATE",
 bko="검문소가 순례자들을 막고 있습니다. 그들은 선단의 호위를 청합니다.",
 ben="A checkpoint is holding pilgrims back. They ask to travel under your protection.",
 choices=[
  ch("escort","에테르 1을 쓰며 호위한다","Escort them (1 aether)","순례자들이 선단에 합류했습니다.","The pilgrims join the convoy.",aetherCost=1,survivorDelta=30,moraleDelta=7),
  ch("leave","지나친다","Pass on","순례자들이 뒤에 남았습니다.","The pilgrims stay behind.",moraleDelta=-2)]),
# ---------------- STORM ----------------
dict(id="ion_squall", type=W, tko="이온 스콜", ten="THE ION SQUALL",
 bko="이온 스콜이 전 계기를 흔듭니다. 코어가 비명을 지릅니다.",
 ben="An ion squall rattles every instrument. The core screams.",
 choices=[
  ch("calm","에테르 1을 태워 코어를 진정시킨다","Burn 1 aether to calm the core","코어가 진정됐습니다.","The core settles.",aetherCost=1,instabilityDelta=-15),
  ch("push","그대로 밀어붙인다 (60%)","Push through (60%)","스콜을 뚫었고 잔류 에테르를 회수했습니다.","You clear the squall and recover stray aether.",aetherDelta=1,successChance=0.6,failureChoiceId="push_fail"),
  ch("push_fail","방전","Discharge","방전이 선체를 태우고 코어를 흔들었습니다.","A discharge scorches the hull and rattles the core.",hidden=True,hullDelta=-4,instabilityDelta=15)]),
dict(id="static_fog", type=W, tko="정전기 안개", ten="THE STATIC FOG",
 bko="정전기 안개가 시야를 지웁니다.",
 ben="Static fog erases all sight.",
 choices=[
  ch("chart","[정찰선] 앞장서게 한다","[Pathfinder] Send the pathfinder ahead","정찰선이 안전한 길을 찾았습니다.","The pathfinder finds a clear line.",requiredTag="support.pathfinder",moraleDelta=2,aetherDelta=1),
  ch("slow","속도를 늦춘다 (에테르 1)","Slow down (1 aether)","천천히, 무사히 빠져나왔습니다.","Slow and safe.",aetherCost=1),
  ch("fast","전속으로 돌파한다 (50%)","Full speed (50%)","안개를 단숨에 벗어났습니다.","You burst out of the fog.",moraleDelta=2,successChance=0.5,failureChoiceId="fast_fail"),
  ch("fast_fail","충돌","Collision","잔해와 충돌해 장갑이 찢겼습니다.","You clip wreckage and lose armor.",hidden=True,armorDelta=-6)]),
dict(id="hail_front", type=W, tko="우박 전선", ten="THE HAIL FRONT",
 bko="주먹만 한 우박이 선단을 두드립니다.",
 ben="Fist-sized hail hammers the convoy.",
 choices=[
  ch("brace","[드워프] 장갑을 세워 버틴다","[Dwarf] Brace the plating","드워프들이 장갑을 지켰습니다.","The dwarves hold the plating.",requiredTag="lineage.dwarf",armorDelta=-1,moraleDelta=2),
  ch("climb","에테르 2로 전선 위로 오른다","Climb above it (2 aether)","우박 위의 하늘은 고요했습니다.","Above the front, the sky is still.",aetherCost=2),
  ch("endure","버틴다","Endure it","우박이 장갑을 두들겼습니다.","The hail dents the armor.",armorDelta=-5,hullDelta=-2)]),
dict(id="aether_bloom", type=W, tko="에테르 개화", ten="THE AETHER BLOOM",
 bko="폭풍 속에 에테르 꽃이 피었습니다. 눈부시고 위험합니다.",
 ben="An aether bloom opens inside the storm — dazzling and dangerous.",
 choices=[
  ch("harvest","가운데로 들어가 채집한다","Fly into the bloom and harvest","에테르가 넘치지만 코어가 비명을 지릅니다.","Aether floods in; the core howls.",aetherDelta=4,instabilityDelta=20),
  ch("skirt","가장자리만 스친다","Skirt the edge","약간의 에테르를 얻었습니다.","A little aether gained.",aetherDelta=1),
  ch("avoid","피한다","Avoid it","개화가 뒤에서 사그라듭니다.","The bloom fades behind you.")]),
dict(id="lightning_choir", type=W, tko="번개 성가", ten="THE LIGHTNING CHOIR",
 bko="번개가 규칙적인 화음으로 칩니다. 공명자가 귀를 기울입니다.",
 ben="Lightning strikes in regular chords. Your resonator listens.",
 choices=[
  ch("conduct","[엘프] 화음을 지휘해 코어를 채운다","[Elf] Conduct the chords into the core","번개가 코어를 채웠습니다.","Lightning fills the core.",requiredTag="lineage.elf",aetherDelta=3,moraleDelta=3),
  ch("ground","군수품 1을 피뢰침으로 쓴다","Sacrifice 1 ordnance as a lightning rod","번개가 미끼를 쳤습니다.","The lightning takes the bait.",ordnanceCost=1),
  ch("endure","버틴다 (50%)","Ride it out (50%)","번개가 선단을 비껴갔습니다.","The lightning passes you by.",moraleDelta=1,successChance=0.5,failureChoiceId="strike"),
  ch("strike","낙뢰","Struck","낙뢰가 선체를 강타했습니다.","Lightning hammers the hull.",hidden=True,hullDelta=-5,moraleDelta=-2)]),
dict(id="updraft_chasm", type=W, tko="상승기류 협곡", ten="THE UPDRAFT CHASM",
 bko="협곡에서 거대한 상승기류가 솟습니다.",
 ben="A vast updraft rises from the chasm.",
 choices=[
  ch("high","기류를 타고 고고도로 오른다","Ride it to high altitude","선단이 힘들이지 않고 고고도에 올랐습니다.","The convoy climbs high without burning fuel.",aetherDelta=2),
  ch("avoid","우회한다 (에테르 1)","Go around (1 aether)","협곡을 돌아갔습니다.","You skirt the chasm.",aetherCost=1)]),
dict(id="cloud_reef", type=W, tko="구름 암초", ten="THE CLOUD REEF",
 bko="굳은 구름 암초가 항로를 막습니다. 암초 속에 난파선이 박혀 있습니다.",
 ben="A reef of hardened cloud blocks the lane. Wrecks are lodged inside it.",
 choices=[
  ch("climb","[고블린] 암초를 타고 잔해를 챙긴다","[Goblin] Climb the reef for salvage","고블린들이 잔해를 뜯어 왔습니다.","The goblins strip the wrecks.",requiredTag="lineage.goblin",salvageDelta=5),
  ch("around","우회한다 (에테르 1)","Go around (1 aether)","암초를 돌아갔습니다.","You go around.",aetherCost=1),
  ch("risk","틈으로 통과하며 인양한다 (55%)","Squeeze through and salvage (55%)","틈을 통과하며 잔해를 챙겼습니다.","You slip through and take salvage.",salvageDelta=8,successChance=0.55,failureChoiceId="scrape"),
  ch("scrape","긁힘","Scraped","암초가 선체를 긁었습니다.","The reef tears at the hull.",hidden=True,hullDelta=-6,salvageDelta=2)]),
dict(id="whiteout", type=W, tko="화이트아웃", ten="THE WHITEOUT",
 bko="눈보라가 모든 것을 지웁니다. 수송선이 대열을 잃기 시작합니다.",
 ben="A blizzard erases everything. Transports begin losing formation.",
 choices=[
  ch("anchor","보급 1을 쓰며 정박해 기다린다","Anchor and wait (1 supply)","눈보라가 지나갈 때까지 기다렸습니다.","You wait out the storm.",suppliesCost=1,moraleDelta=1),
  ch("press","밀고 나간다 (60%)","Press on (60%)","대열을 유지한 채 빠져나왔습니다.","Formation holds.",moraleDelta=2,successChance=0.6,failureChoiceId="lost"),
  ch("lost","이탈","Lost transports","수송선 한 척이 대열에서 사라졌습니다.","A transport is lost from the formation.",hidden=True,survivorDelta=-20,moraleDelta=-5)]),
dict(id="storm_leviathan", type=W, tko="폭풍 리바이어던", ten="THE STORM LEVIATHAN",
 bko="폭풍 속에서 거대한 그림자가 선단을 따라옵니다.",
 ben="Something vast follows the convoy inside the storm.",
 choices=[
  ch("flee","에테르 2로 전속 이탈","Flee at full burn (2 aether)","그림자가 뒤처졌습니다.","The shadow falls behind.",aetherCost=2),
  ch("sing","[조인] 노래로 달랜다","[Avian] Sing to it","리바이어던이 노래를 따라 부르더니 멀어졌습니다.","The leviathan answers the song and drifts away.",requiredTag="lineage.avian",moraleDelta=8,aetherDelta=1),
  ch("harpoon","작살을 쏜다 (40%)","Harpoon it (40%)","리바이어던이 쓰러졌고 선단이 자원을 챙겼습니다.","The leviathan falls and the convoy strips it.",salvageDelta=15,moraleDelta=5,successChance=0.4,failureChoiceId="harpoon_fail"),
  ch("harpoon_fail","반격","It turns on you","리바이어던이 선체를 들이받았습니다.","The leviathan rams the hull.",hidden=True,hullDelta=-8,moraleDelta=-4)]),
]

def cs_str(s): return '"' + s.replace('\\', '\\\\').replace('"', '\\"') + '"'
def cs_val(k, v):
    if isinstance(v, bool): return "true" if v else "false"
    if isinstance(v, float): return f"{v}f"
    if isinstance(v, str): return cs_str(v)
    return str(v)

lib = ["using System.Collections.Generic;", "using AetherArk.Core;", "", "namespace AetherArk.Content", "{",
       "    /// <summary>Authored events beyond the five baseline encounters. Generated from tools/gen_events.py data.</summary>",
       "    public static class EncounterLibrary", "    {",
       "        public static void AddAll(Dictionary<string, EncounterDefinition> result)", "        {"]
for e in EVENTS:
    lib.append(f'            Event(result, "{e["id"]}", EncounterType.{e["type"]},')
    for i, c in enumerate(e["choices"]):
        fx = ", ".join(f"{k} = {cs_val(k, v)}" for k, v in c["fx"].items())
        parts = [f'id = "{c["id"]}"'] + ([fx] if fx else [])
        comma = "," if i < len(e["choices"]) - 1 else ");"
        lib.append(f'                new EncounterChoiceDefinition {{ {", ".join(parts)} }}{comma}')
lib += ["        }", "",
        "        private static void Event(Dictionary<string, EncounterDefinition> result, string id, EncounterType type, params EncounterChoiceDefinition[] choices)",
        "        {",
        "            var encounter = new EncounterDefinition { id = id, type = type, titleKey = \"enc.\" + id + \".title\", bodyKey = \"enc.\" + id + \".body\" };",
        "            for (var i = 0; i < choices.Length; i++)",
        "            {",
        "                choices[i].textKey = \"enc.\" + id + \".\" + choices[i].id;",
        "                choices[i].resultKey = \"enc.\" + id + \".\" + choices[i].id + \".r\";",
        "                encounter.choices.Add(choices[i]);",
        "            }",
        "            result[id] = encounter;",
        "        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/EncounterLibrary.cs", "w", encoding="utf-8").write("\n".join(lib))

loc = ["namespace AetherArk.Content", "{",
       "    /// <summary>Event strings. Generated from tools/gen_events.py data; keep in sync with EncounterLibrary.</summary>",
       "    public sealed partial class LocalizationService", "    {",
       "        private void AddEncounterStrings()", "        {"]
for e in EVENTS:
    loc.append(f'            Add("enc.{e["id"]}.title", {cs_str(e["tko"])}, {cs_str(e["ten"])});')
    loc.append(f'            Add("enc.{e["id"]}.body", {cs_str(e["bko"])}, {cs_str(e["ben"])});')
    for c in e["choices"]:
        loc.append(f'            Add("enc.{e["id"]}.{c["id"]}", {cs_str(c["ko"])}, {cs_str(c["en"])});')
        loc.append(f'            Add("enc.{e["id"]}.{c["id"]}.r", {cs_str(c["rko"])}, {cs_str(c["ren"])});')
loc += ["        }", "    }", "}", ""]
open("/Users/hanshin/workspace/air-carrier/Assets/_Project/Scripts/Content/LocalizationService.Encounters.cs", "w", encoding="utf-8").write("\n".join(loc))
from collections import Counter
print("events:", len(EVENTS), Counter(e["type"] for e in EVENTS))
