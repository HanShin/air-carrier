# 장비·편대 UI 아이콘

기준일: 2026-09-04. 내장 ImageGen으로 생성한 투명 PNG 20종을 `Assets/_Project/Resources/Art/Icons/`에 저장한다. `GameIconLibrary`가 enum 이름을 리소스 경로에 대응시키며 항구 카드, 전투 무기 슬롯, 편대 카드와 다섯 임무 버튼이 같은 아이콘을 공유한다.

## 구성

- 모듈 8분류: `hull`, `core`, `weapons`, `deck`, `sensors`, `engineering`, `bridge`, `marines`
- 무기 7계열: `cannon`, `lance`, `piercer`, `missile`, `flak`, `incendiary`, `breacher`
- 편대 5병과: `interceptor`, `bomber`, `escort`, `recon`, `assault`

개별 장비가 아니라 규칙 분류별 아이콘을 사용한다. 따라서 티어가 올라가도 플레이어가 포격·결계 파괴·장갑 관통·탄약 소모·요격·화재·감압 규칙을 같은 시각 언어로 알아볼 수 있다.

## 공통 생성 프롬프트

```text
Use case: stylized-concept
Asset type: square transparent PNG game UI category icon, readable at 24–64 pixels
Primary request: create one isolated dark science-fantasy magitech emblem
Scene/backdrop: no backdrop; every pixel outside the badge has alpha 0; never paint a white, gray, black, or checkerboard background
Style/medium: polished hand-painted 2D game UI icon, crisp cutout edges, bold silhouette, high contrast
Composition/framing: one centered brass-rimmed octagonal badge filling 78% of a square canvas, one large simple symbol, wide transparent margin, straight-on
Color palette: near-black navy steel, aged brass, cyan-violet aether with one role-specific accent
Materials/textures: riveted steel, engraved brass, dark enamel, crystal energy
Output constraint: genuine RGBA transparent PNG
Constraints: no text, letters, numbers, logo, watermark, people, scene, floor, drop shadow, checkerboard pixels, cropped parts, or multiple badges
```

Subject만 분류별로 바꿨다. 예를 들어 선체는 보강 방패, 코어는 에테르 결정 터빈, 광창은 보라색 에너지 창, 파공은 갈라진 장갑판, 요격기는 상승하는 전투기, 강습기는 장갑 강습정으로 지정했다.

## 임포트·UI 규칙

- `ShipArtImporter`가 함선과 아이콘 폴더 모두 Sprite/Single, alpha transparency, mipmap off, clamp, bilinear로 가져온다. 원본은 보존하되 아이콘의 런타임 최대 크기는 256px, 함선은 2048px이다.
- 아이콘은 장식이 아니라 규칙 식별자이므로 이름·수치 텍스트를 대체하지 않는다.
- 비활성 전투 버튼의 아이콘에는 회색 틴트를 적용하고 클릭 레이캐스트는 끈다.
- 신규 enum 값을 추가하면 동일한 소문자 파일명의 PNG와 EditMode 테스트를 함께 추가한다.
