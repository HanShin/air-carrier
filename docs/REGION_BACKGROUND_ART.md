# 공역별 배경 아트

기준일: 2026-09-04. 내장 ImageGen으로 생성한 16:9 계열 RGB PNG 7종을 `Assets/_Project/Resources/Art/Backgrounds/`에 저장한다. `BackgroundArt`가 현재 `regionIndex`와 `isFinalBattle`을 Resources 경로로 변환하고, `GameController`가 처음 요청된 텍스처를 캐시한다. 메뉴·원정 준비 화면과 누락된 파일은 기존 `sky_storm_background`로 폴백한다.

## 파일과 역할

| 파일 | 화면 | 시각 주제 |
|---|---|---|
| `dawn_archipelago.png` | 여명 군도 | 금빛 일출, 부유 군도, 길잡이 봉화 |
| `storm_corridor.png` | 폭풍 회랑 | 좁은 공중 협곡, 낙뢰운, 청색 유도탑 |
| `icefield_heights.png` | 빙운 고원 | 부유 빙붕, 결빙 유적, 청록 결정 |
| `imperial_cordon.png` | 제국 봉쇄권 | 검문 요새, 탐조등, 적색 결계와 사슬 |
| `abyssal_strait.png` | 심연 해협 | 수직 심연, 잔해 지대, 난류성 에테르류 |
| `sky_throne.png` | 천공 왕좌 | 백색 왕도, 금빛 고리 구조물, 맑은 상층운 |
| `throne_gate_finale.png` | 최종전·피날레 | 가동 중인 고대 천공문, 닫혀 오는 자색 폭풍, 안전한 반구의 빛 |

## 공통 생성 프롬프트

```text
Use case: stylized-concept
Asset type: widescreen 16:9 video-game environment background for Aether Ark, shown behind dense strategy UI
Input image: use Image 1 only as the visual benchmark for painterly finish, atmospheric depth, fantasy architecture language, and dark-hopeful science-fantasy mood. Create a new location, not a copy.
World: a magitech planet of floating continents above a cloud ocean, powered by crystalline aether.
Style/medium: polished hand-painted 2D fantasy concept art, cinematic but readable, restrained detail, subtle magitech structures, consistent with the reference
Composition/framing: wide establishing vista, horizon near upper third, major landmarks pushed toward left or right edge, central 60 percent and lower-middle kept lower-contrast and visually calm for game UI, strong layered depth
Constraints: environment only; no text, no letters, no UI, no logo, no watermark, no people, no characters, no foreground ship, no aircraft; no transparent background
Avoid: photorealism, modern Earth technology, outer space, clutter in the center, extreme bloom, oversaturated neon
```

기존 `sky_storm_background.png`는 화풍과 세계관의 기준 이미지로만 참조했다. 각 파일에는 위 공통문 뒤에 다음 지역별 요청을 붙였다.

## 지역별 요청

- 여명 군도: 환영하는 부유 섬 군도, 상아색 망루, 폭포, 호박색 에테르 봉화와 왼쪽의 금빛 첫 햇살. 긴 여정의 시작과 연약한 안전감.
- 폭풍 회랑: 들쭉날쭉한 부유 절벽 사이의 좁은 항로, 숯빛 낙뢰운, 자색 번개, 바람에 찢긴 구름 띠. 압박과 속도감.
- 빙운 고원: 고고도 빙붕, 서리 낀 천문대 유적, 거대한 고드름, 결빙 내부의 옅은 청록 에테르. 고독한 구조 개척지.
- 제국 봉쇄권: 암석 요새, 매달린 파일런과 사슬, 탐조등, 진홍색 신호 봉화, 공중 검문 통로를 가로지르는 기하학 결계. 감시와 통제.
- 심연 해협: 검은 부유 대륙 사이의 수직 구렁, 고대 인양 발판과 잔해, 나선형 청록 에테르류. 현기증과 위험한 기회.
- 천공 왕좌: 가장 높은 맑은 하늘, 금빛 첨탑과 백색 석조 왕도, 거대한 휴면 고리, 질서정연한 청록 에테르 선. 장엄하고 경계된 순례의 끝.
- 천공문 피날레: 부서진 기념비 사이에서 백금색·청록색으로 가동하는 거대한 동심원 천공문, 중심 너머의 평온한 안전지대, 바깥에서 조여 오는 자색 폭풍. 마지막 방어전과 절박한 희망.

## 제작·임포트 규칙

- 화면 중앙과 하단은 함선 평면도·카드·버튼이 올라오므로 주요 랜드마크와 강한 대비를 가장자리 또는 먼 배경에 둔다.
- `ShipArtImporter`가 배경 폴더를 Default texture, mipmap off, clamp, bilinear, 최대 2048로 일괄 임포트한다.
- 공역 파일명은 `RegionDefinition.id`와 같게 유지한다. 새 공역도 이 규칙을 따르면 매핑 테이블을 따로 만들 필요가 없다.
- 최종전은 공역 배경보다 우선하며 `isFinalBattle`인 동안 `throne_gate_finale`를 사용한다.
