# 함선 비주얼 개선 — 0.9

기준일: 2026-09-05. 내장 ImageGen으로 기함 3종을 새로 제작하고 주요 적함 3종을 다시 제작했다. 전투는 기존 2D 스프라이트/구획 방식이며, 이번 '모델링' 작업은 입체적인 하드서피스 외형 표현과 선체에 맞춘 갑판 투영을 뜻한다. 3D 메시·리깅·Blender 모델은 포함하지 않는다. 생성 도구의 개별 모델 버전은 호출 인터페이스에서 지정하거나 확인하지 않았다.

## 적용 파일

- `Assets/_Project/Resources/Art/Ships/ship_vanguard.png` — 여명의 피난처: 흰색·청록 장갑, 네 부양 아웃리거와 함교.
- `Assets/_Project/Resources/Art/Ships/ship_bastion.png` — 철벽: 중장갑, 세 대형 포대와 황색 코어.
- `Assets/_Project/Resources/Art/Ships/ship_zephyr.png` — 서풍 연: 쌍 비행갑판과 후퇴익, 청록 추진기.
- `Assets/_Project/Resources/Art/Ships/enemy_cutter.png` — 제국 커터: 붉은 추격함.
- `Assets/_Project/Resources/Art/Ships/enemy_cruiser.png` — 제국 순양함: 대형 현측 포대와 네 추진기.
- `Assets/_Project/Resources/Art/Ships/enemy_warden.png` — 수호함: 백색·남색 장갑과 고대 반원 동력기관.
- `Assets/_Project/Resources/Art/Backgrounds/ark_title.png` — 여명의 피난처가 피난선단을 이끄는 메인 화면.

선체 6장 모두 1536×1024 RGBA, 이미지별 완전 투명 픽셀 41~54%를 확인했다. 원본 알파를 유지한다. 기존 적함 이미지의 이전 버전은 Git 이력에 남는다. 나머지 적 실루엣 10종과 공역 배경 7장은 이전 제작분을 사용한다.

## 표시 방식

- 기함의 비어 있는 `deckPlanId`는 실제 `DeckPlan.shipId`로 해석한다. 따라서 새 기함 아트가 구형 세이브에서도 로드된다.
- `ShipArtLayout`은 선체별 중앙 갑판 영역을 정규화 좌표로 정의한다. 이미지 비율을 유지하면서 방을 이 영역에 투영하고 게임 규칙상의 방 배치는 그대로 사용한다.
- 내부 구획/선체 외형 전환 및 구획 확대를 제공한다. 확대는 선체와 구획에 같은 배율을 적용하며 전용 영역 바깥은 잘라 낸다. 승무원 선택 시 아군 내부 구획을 확대한다. 보기 전환은 일시정지를 해제하지 않는다.
- 금속 바닥의 이음새, 벽 조명, 설비 디테일은 방마다 하나의 `DeckSurfaceGraphic` 메시로 그린다. 장식은 마우스 클릭을 받지 않는다. 상태 색상·화재·파공·승무원·체력은 실제 런 상태에서 그린다.
- 작은 방에서는 설비 장식을 생략해 라벨 공간을 확보한다. 선택한 방의 전체 명칭과 상태는 하단 상세 패널에 유지된다.
- 전투 중 `Z`로 양쪽 갑판을 함께 확대/축소할 수 있다.
- 준비 화면에는 선택 기함의 실제 외형과 선체/장갑/결계 수치를 표시한다. 메뉴는 왼쪽 조작부·오른쪽 기함 구도로 변경했다.
- 임포터는 원본 비율을 유지하고 스프라이트를 FullRect로 가져온다. 배경은 최대 2048, 아이콘은 256 설정을 유지한다.

## 프롬프트

내장 ImageGen 사용. 기함과 주요 적함은 아래 공통 프롬프트에 각 Subject를 붙인 독립 요청으로 제작했다.

```text
Use case: stylized-concept
Asset type: production-ready transparent PNG hull sprite for a 2D sky-carrier strategy game, Aether Ark.
Camera: exact orthographic TOP DOWN looking vertically down on a horizontally oriented ship, bow to the RIGHT, stern to the LEFT. No isometric perspective, no side view.
Composition: landscape 3:2 canvas. Whole ship entirely visible with transparent padding of 4 percent; hull fills 92 percent width and 80 percent height. Broad, continuous armored central body from x=18% to 80%, y=24% to 76% so a playable rectangular cutaway room layout can be composited there. Distinct ship silhouette, not a flat diagram. Along the outer edges show modeled stepped armor, outriggers, propulsion nacelles, flight deck rails and weapon sponsons that remain visible around the UI.
Visual direction: premium hand-painted 2D game sprite rendered with dimensional hard-surface model quality, coherent functional magitech engineering, matte painted steel, brushed metal, restrained brass edging, subtle wear, legible big forms and purposeful small details. Soft neutral overhead lighting, crisp contour. The CENTRAL DECK must remain relatively quiet metal plating with fine panel seams, no room labels and no baked room grids.
Output requirements: genuinely transparent background alpha=0 outside ship; no opaque black/white background, no backdrop glow or cast shadow, no checkerboard pattern, no sky, no floor, no text, no labels, no logo, no watermarks, no separate aircraft or people. Emissive accents confined within the hull.
```

### ship_vanguard

```text
Subject: EAS Dawn Refuge, balanced refugee command sky carrier and battleship. A broad ivory and muted petrol-teal plated body with brushed brass mechanical ribs, twin rear cyan aether engines, a long functional launch rail along the lower outside edge, command bridge pod near the upper bow, compact paired turrets on bow-side sponsons. Naval practical design, a battered but dependable ark protecting civilians. Recognizable silhouette with a tapering reinforced bow and four short lift outriggers.
```

### ship_bastion

```text
Subject: EAS Iron Bastion, heavy gun platform and armored sky carrier. Massive broad slate-blue and charcoal steel citadel body, thick overlapping armor slabs, restrained amber/brass edge details, three prominent gun emplacements arranged on external forward sponsons, recessed small flight strip on a flank, two huge guarded rear engines with amber aether cores. Compact blunt reinforced prow to the right, blocky shoulders and strong rectangular silhouette. Central deck stays continuous and broad, guns outside it.
```

### ship_zephyr

```text
Subject: EAS Zephyr Kite, agile light sky carrier specialized in air wings. Streamlined ivory and desaturated teal hull with slim brushed brass edging, elegant swept outer stabilizers, twin long narrow launch decks along TOP and BOTTOM edges, four compact cyan aether lift pods, rear twin engines. Broad central fuselage still continuous enough for a cutaway interior. Long tapered bow to right. Lighter angular silhouette than a battleship, coherent compact machinery; small mounted gun at the forward edge.
```

### enemy_cutter

```text
Subject: Imperial pursuit cutter. Lean charcoal gunmetal hull, oxblood armor shoulders, subtle brass riveted edging, two swept rear fins, compact twin violet aether engine bells at left, sharpened bow to right with a forward gun and two small external sponson cannons. Keep broad continuous central fuselage for the playable cutaway. Practical menacing aerial interceptor, crisp hard-surface materials, much simpler silhouette than a capital ship.
```

### enemy_cruiser

```text
Subject: Imperial storm cruiser. Heavy blue-black steel hull with deep crimson armor sections, warm brass ribs, stepped front shoulder plates and two paired heavy broadside turrets on outer sponsons, reinforced pointed prow on right, four armored engine outlets at rear left with restrained violet light. A broad continuous central deck plate accommodates the playable cutaway. Powerful navy cruiser with no giant wings, unmistakably larger and sturdier than a pursuit cutter.
```

### enemy_warden

```text
Subject: The Gate Warden Undying Oath, an ancient magitech guardian capital ship. Monumental pale stone-ceramic and dark navy armored hull, restrained antique gold mechanical filigree and inset violet crystalline channels. One great segmented half-ring propulsion crown behind the stern on the LEFT, symmetrical long outer buttresses and heavy siege cannon emplacements above and below the hull, long reinforced spear-like prow RIGHT. Central deck stays BROAD and rectangular for interior overlay. A relic battleship, distinguishable from ordinary imperial fleet with regal architectural mass and solid model-like detail.
```

### ark_title

새로 생성한 여명의 피난처 스프라이트를 참조 이미지로 사용했다.

```text
Use case: stylized-concept
Asset type: high-resolution cinematic 16:9 title-screen key art for Aether Ark, a science-fantasy sky-carrier game.
Input image: Image 1 is the identity reference for the EAS Dawn Refuge carrier: preserve its ivory/teal armor, brass ribs, cyan aether engines, four lift outriggers and carrier/battleship hybrid design. Show this same vessel from a dramatic three-quarter elevated side perspective, NOT top-down.
Scene: the giant ark flying ABOVE a deep ocean of clouds on a magitech planet. On the RIGHT two-thirds, the carrier has believable armored depth, an operational side flight deck, delicate bridge windows and small navigation lights, accompanied by a few much smaller distant refugee support vessels to communicate scale. Far background floating cliffs and ruined monumental gate arches; a violent violet storm is behind them on the extreme right, and narrow amber dawn breaks ahead.
Composition: wide cinematic landscape 16:9, visually quiet dark navy atmosphere throughout the entire LEFT third for title/buttons; hero carrier occupies the RIGHT half, bow angled slightly toward upper right. Keep ship entirely in frame. Not centered behind the menu.
Style: premium painted game key art with tangible hard-surface modeling, physically coherent ship construction, confident brushwork, atmospheric depth, restrained cyan and amber accent lights. Dark but hopeful, sophisticated moody film composition.
Constraints: no text, no letters, no logo, no watermark, no UI, no planet seen from space, no sea underneath (only clouds), no exaggerated bloom.
```

## 검수

- 최종 결과: EditMode 124/124, PlayMode 6/6 통과, macOS 개발 빌드 성공. 메인·준비·일반 전투·영어/고대비 최종전과 `Z` 확대 화면을 실제 빌드에서 확인했다.
- 실제 리소스 임포트, 전체 EditMode/PlayMode 테스트, macOS 개발 빌드.
- UI 테스트는 새 장식 위에서 실제 GraphicRaycaster로 방 버튼이 먼저 선택되는지, 외형 전환 후 내부 조작 복귀와 확대 크기 증가, 일시정지 유지 여부를 검증한다.
- 개발 빌드 검수용 조합: `-debug-setup -debug-flagship ship_zephyr`, `-debug-combat gate_warden -debug-flagship ship_bastion -debug-english -debug-high-contrast`. 이 플래그들은 개발 빌드에서만 적용된다.
