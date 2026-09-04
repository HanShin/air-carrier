# 적 함선 실루엣 아트

기준일: 2026-09-04. 내장 ImageGen으로 생성한 투명 PNG 13종을 `Assets/_Project/Resources/Art/Ships/`에 저장한다. 각 파일명은 `ShipState.deckPlanId`와 같고, `ShipBlueprintView`가 `Resources.Load<Sprite>`로 갑판 UI 뒤에 배치한다. 스프라이트가 없으면 기존 절차적 선체로 폴백한다.

## 파일 목록

`enemy_cutter`, `enemy_carrier`, `enemy_scout`, `enemy_boarder`, `enemy_lancer`, `enemy_minelayer`, `enemy_firebrand`, `enemy_cruiser`, `enemy_monitor`, `enemy_dreadnought`, `enemy_hive`, `enemy_wraith`, `enemy_warden`.

## 공통 생성 프롬프트

```text
Use case: stylized-concept
Asset type: transparent 2D game ship silhouette used behind an FTL-style room grid
Scene/backdrop: genuinely transparent background
Style/medium: polished hand-painted 2D game asset, stylized realism, dark science-fantasy magitech navy, crisp cutout edges
Composition/framing: exact orthographic top-down view, bow points right, centered horizontally, ship fills about 88% canvas width and 55% height, transparent padding, no perspective
Lighting/mood: soft neutral overhead, restrained violet aether glow
Color palette: near-black blue steel, charcoal armor, aged brass trim, violet-magenta energy
Materials: riveted armor, engraved arcane plates, subtle wear; central room area relatively quiet
Constraints: isolated single ship; genuine alpha; no checkerboard; no floor/sky/clouds/shadow/border/text/letters/insignia/logo/watermark/room labels/people/aircraft/fire/cropped parts
Avoid: side view, three-quarter view, photorealism, bright background, excess bloom
```

계열별로 커터의 추격함 형태, 항모의 비행갑판, 정찰함의 센서 날개, 강습 바지선의 장갑 선수, 창기병 구축함의 장창형 선수, 기뢰함의 투하 셀, 화염함의 열 배출구, 순양함의 주력함 체급, 감시함의 방벽판, 드레드노트의 공성 포탑, 하이브 항모의 다중 격납고, 레이스의 가느다란 공명익, 수호함의 고대 천공문 문양을 subject에 추가해 변형했다. 화염함은 ember-orange 에너지 색을 추가했다.

## 제작·임포트 규칙

- 실제 알파 채널이 있는 PNG만 채택한다. 체크무늬가 이미지 픽셀에 포함된 출력은 사용하지 않는다.
- `ShipArtImporter`가 Sprite/Single, alpha transparency, mipmap off, clamp, bilinear, 최대 2048 설정을 일괄 적용한다.
- 함선 중앙은 갑판 구획 버튼이 덮으므로 외곽 형태와 선수 방향을 우선해 평가한다.
- 새 실루엣을 추가할 때 파일명을 deck plan id와 같게 지정하면 별도 매핑 코드 없이 로드된다.
