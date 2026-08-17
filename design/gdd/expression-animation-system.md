# 표정 / 애니메이션 시스템 (Expression / Animation System)

> **Status**: Designed (pending review)
> **Author**: user + systems-designer + art-director + qa-lead
> **Last Updated**: 2026-07-18
> **Implements Pillar**: Pillar 2 (지켜주고 싶은 마음), Pillar 1 (손끝의 위안)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode

## Overview

표정/애니메이션 시스템은 감자 상태 관리·날씨 시스템·생애주기 진행이라는 세 개의 독립된
데이터 소스가 각각 노출하는 신호(스탯 기반 포즈/윤곽선 계약, 현재 날씨 enum, 생장 단계)를
매 프레임 하나의 일관된 렌더링 상태로 합성하는 조정 계층이다. 이 시스템 자체는 새로운
감정 규칙을 발명하지 않는다 — art-bible과 potato-stat이 이미 잠근 포즈 우선순위·윤곽선
블렌드·색 계약을 그대로 소비하되, 그 문서들이 명시적으로 비워둔 세 이음매(맑음/생기 포즈
트리거 소유권, 동시 이벤트 중재 순서, 생장 4단계 실루엣 간 포즈 호환성)를 채운다.

플레이어는 이 시스템을 직접 조작하지 않는다 — 앱을 열 때마다 감자의 표정·자세·색조가
이미 그 순간의 날씨·상태·생장 단계를 반영한 하나의 그림으로 보일 뿐이다. 하지만 그 결과는
지극히 직접적으로 느껴진다: 감자가 편안해 보이는지 지쳐 보이는지가 화면을 여는 그 찰나에
즉시 읽혀야 하기 때문이다. 이 시스템이 없다면 potato-stat의 숫자, weather의 enum,
lifecycle의 카운터는 각자 정확하더라도 화면에는 아무것도 나타나지 않는다 — 셋 중 하나만
바뀌어도 감자가 다르게 "보여야" 한다는 이 게임의 핵심 약속이 이 시스템 하나에 전적으로
의존한다.

## Player Fantasy

> *(`creative-director` 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

플레이어가 이 시스템을 통해 느껴야 할 감정은 **감자가 지금 이 순간 어떤 마음인지를, 말이
아니라 얼굴 없는 몸짓 하나로 즉시 알아채는 감각**이다 — art-bible이 이미 못박은 대로
감자는 눈동자도 입도 팔도 없으므로(베이비 스키마), 이 시스템이 표현할 수 있는 것은 오직
윤곽선의 굴곡과 볼의 홍조뿐이다. 이 제약은 결핍이 아니라 이 시스템의 플레이어 판타지
자체다: 화려한 얼굴 연기 없이도, 웅크린 어깨선 하나 처진 눈매 하나만으로 "아, 얘가 지금
힘들구나"를 직감하는 것 — 이것이 Pillar 2(지켜주고 싶은 마음)가 이 시스템에서 취하는
구체적 형태다.

기준점은 art-bible이 이미 확정한 여행 개구리(旅かえる) 축 — 과장된 리액션이 아니라
맥락과 몸짓에서 나오는 절제된 감정이다. 플레이어는 이 시스템을 조작하지 않지만, 그
결과는 매번 앱을 열 때 가장 먼저 눈에 들어오는 것이라 조작감 못지않게 직접적으로
느껴진다 — 날씨가 궂어지면 감자가 먼저 웅크리고, 손끝으로 돌봐주면 그 자리에서 곧바로
다시 펴지는 것을 보는 순간, 손끝의 위안(Pillar 1)이 시각적으로 완성된다. 이 시스템이
실패하는 방식은 "표정이 안 예뻐서"가 아니라 "감자의 현재 상태를 한눈에 못 읽어서"다 —
표현력보다 즉각적 판독성이 이 시스템의 진짜 성공 기준이다.

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 lean에서도 상담 필수. 이 시스템은
> systems-index 고위험 시스템 표에 없어 기본 lean 규칙을 그대로 적용.)*

### Core Rules

1. **세 독립 신호 구독**: 이 시스템은 매 프레임 세 개의 업스트림 신호를 읽는다 —
   감자 상태 관리의 포즈/윤곽선/Blush/이벤트 계약(Visual/Audio Requirements §0-4),
   날씨 시스템의 `GetCurrentWeather()`, 생애주기 진행의 `stageIndex(G)`. 이 시스템은
   새 신호를 만들지 않고 오직 세 신호를 합성한다.

2. **최종 포즈 카테고리 해석(2단계)**: potato-stat §1이 반환하는 1차 결과
   `{Wither-sagging, Baseline, Rain-huddled, Cold-curled}`를 그대로 받되, 이 시스템이
   2차 승격 규칙 하나를 추가한다 — `1차 결과 = Baseline` **그리고** `GetCurrentWeather() = Sunny`
   일 때만 최종 포즈를 **Sunny-content(생기)**로 승격한다. 그 외 모든 경우 1차 결과를
   그대로 최종 포즈로 채택한다. (art-bible §3 표가 "맑음"을 "일상 케어"와 별도 행으로
   구별해뒀지만, 그 트리거 신호를 potato-stat도 weather-system도 명시적으로 청구하지
   않았던 공백을 이 규칙이 채운다.)

3. **Wither-sagging은 무조건 최우선**: potato-stat §1 규칙을 그대로 상속 — 날씨나 생장
   단계와 무관하게 `neglectMeter≥100`이면 다른 모든 조건을 무시한다.

4. **수확(Harvest-proud)은 상시 포즈 축에 속하지 않는다(사용자 확정)**: `stage=HarvestReady`
   도달 자체는 위 포즈 해석 순서에 어떤 영향도 주지 않는다 — HarvestReady 상태에서도
   방치하면 여전히 Wither-sagging이 정상적으로 나타난다(Pillar 3: 수확 준비가 됐다고
   방치 안전망이 사라지지 않음). Harvest-proud 포즈는 오직 실제 수확 액션이 트리거하는
   1회성 퇴장 연출(art-bible Mood & Atmosphere §5)에서만 등장하는 **외부 요청 override**
   이며, 정확한 호출 시점·인터페이스는 `이름짓기 & 수확 카드` 시스템(아직 미설계)의
   몫이다 — 이 시스템은 그 요청을 받아 재생할 수 있는 훅만 노출한다.

5. **생장 실루엣과 포즈는 독립된 두 축**: `stageIndex(G)`가 결정하는 4단계 실루엣
   (art-bible §3(a), lifecycle Visual/Audio §1)과 위 포즈 카테고리·윤곽선 블렌드값은
   서로 독립적으로 합성된다 — 어떤 생장 단계에 있든 동일한 포즈 파라미터가 그 단계의
   실루엣 위에 적용된다(예: Rain-huddled는 Sprout에서도 Plump에서도 같은 압축 축 규칙을
   따르되, 각 단계 고유의 케이지 위에서 해석된다). 정확한 리그/케이지 메커니즘은 Unity
   6.5 Sprite BlendShape API 채택 여부에 달려 있으며 `/create-architecture` 단계의
   ADR로 넘긴다 — 이 GDD는 "포즈 파라미터는 생장 단계에 무관하다"는 설계 계약까지만
   잠근다.

6. **동시 원샷 전환 우선순위**: potato-stat의 `OnWitherEntered`와 lifecycle의
   `OnStageAdvanced`가 같은 프레임(또는 같은 배치 세그먼트 경계)에 동시 발생하면,
   `OnWitherEntered`가 우선한다 — 그 프레임의 성장전환 애니메이션(`GROWTH_BREATH_DURATION`/
   `HARVEST_ARRIVAL_DURATION`)은 재생을 건너뛰고 실루엣만 즉시 새 단계로 스냅한다(art-bible이
   이미 "라이브 전환에만 재생, 오프라인 후엔 즉시 스냅"이라는 선례를 남겨뒀으므로 신규
   규칙이 아니라 그 선례의 연장). 근거: art-bible이 Wither을 "조용한 다급함"(이 문서
   긴장 상한선)으로, `OnStageAdvanced`를 "가장 조용한 이벤트"로 이미 상대적 무게를
   확정해뒀다 — 동시 발생 시 더 무거운 이벤트가 이긴다. `OnRecovered`는 구조적으로
   `OnStageAdvanced`와 동시 발생할 수 없다(회복은 그 프레임에 시간을 흐르게 하지
   않으므로 같은 틱에 생장 경계를 넘을 수 없음) — 별도 규칙 불필요.

7. **터치 접촉 번짐 애니메이션은 이 시스템 범위 밖**: art-bible Mood & Atmosphere §1의
   "접촉점 번짐" 예외 모션은 `touch-care-interaction.md`가 제스처 진행률에 직결해
   소유한다 — 이 시스템은 그 애니메이션을 트리거하지도, 알 필요도 없다(레이어가 다름,
   weather-system §Visual/Audio Requirements의 "터치 반응 항상 우선" 규칙과 동일 원리).

### States and Transitions

| 최종 상태(포즈) | 진입 조건 | 이탈 조건 |
|---|---|---|
| **Baseline(일상 케어)** | 1차 결과=Baseline **그리고** 날씨≠Sunny | 날씨→Sunny 전환 시 → Sunny-content / 임의 스탯 이탈 → 위기 포즈 / `neglectMeter=100` → Wither-sagging |
| **Sunny-content(맑음/생기)** | 1차 결과=Baseline **그리고** 날씨=Sunny | 날씨가 Sunny 아닌 것으로 전환 → Baseline(또는 그 사이 스탯 이탈이 있었다면 위기 포즈로 직행) / 스탯 이탈 → 위기 포즈 / `neglectMeter=100` → Wither-sagging |
| **Rain-huddled(폭우/안개)** | potato-stat §1: 습도 overshoot 최대 승리 | 습도 복귀 & 다른 이탈 없음 → Baseline/Sunny-content(날씨 재확인) / `neglectMeter=100` → Wither-sagging |
| **Cold-curled(한파)** | potato-stat §1: 온도(low) overshoot 최대 승리 | 상동(온도 기준) |
| **Wither-sagging(시듦)** | `neglectMeter≥100`(모든 조건 무시, 최우선) | `OnRecovered` → 위 표를 즉시 재평가(potato-stat AC-VA-10과 동일 원칙) |
| **Recovery 전환(통과 이벤트)** | `OnRecovered` 이벤트 발생 프레임 | 전환 애니메이션 종료 즉시 위 표의 정적 상태로 귀속(자체 지속 상태 아님) |
| **Harvest-proud(수확 퇴장, 통과 이벤트)** | 외부(이름짓기&수확카드) 요청 | 연출 종료 → 새 Sprout의 Baseline(다음 생애주기 첫 프레임) |

Heatwave는 별도 행이 없다 — art-bible·potato-stat이 이미 확정한 대로 Baseline 카테고리
안에서 윤곽선 블렌드(Layer 1, `overshootMoisture(low)`)만으로 표현된다(Core Rule 5의
"포즈-실루엣 독립"과 같은 이유로 신규 카테고리 불필요).

### Interactions with Other Systems

| 시스템 | 관계 | 인터페이스 |
|---|---|---|
| 감자 상태 관리 | 이 시스템이 의존(구독자) | 포즈 1차 결과·윤곽선 블렌드값·Blush·`neglectMeter`·`isShielded`·`OnWitherEntered`/`OnRecovered` 이벤트 구독(Visual/Audio Requirements §0-4 전체) |
| 날씨 시스템 | 이 시스템이 의존(조회자) | `GetCurrentWeather()` — Sunny-content 승격 판정(Core Rule 2)에만 사용, 값 자체를 저장하지 않음 |
| 생애주기 진행 | 이 시스템이 의존(조회자) | `stageIndex(G)` — 4단계 실루엣 선택, `OnStageAdvanced` 이벤트 구독 |
| 터치 케어 인터랙션 | 관계 없음(명시적 경계) | 접촉 번짐 애니메이션은 별도 레이어·별도 소유 — 데이터 교환 없음 |
| 홈 화면 위젯 시스템(Alpha, 미설계) | 이 시스템에 의존(피조회자) | 이 시스템이 계산한 최종 포즈+실루엣 축소 LOD를 그대로 미러링(art-bible LOD §4) |
| 이름짓기 & 수확 카드(미설계) | 이 시스템에 의존(호출자) | Harvest-proud 퇴장 연출 트리거(정확한 호출 시점은 잠정 계약, Open Questions) |

## Formulas

> *(`systems-designer` 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션. 이 섹션이
> `potato-stat-system.md` Formulas §3의 명시적 계약을 이행한다.)*

이 섹션은 `potato-stat-system.md` Formulas §3이 명시적으로 표정/애니메이션 시스템에 넘긴
계약 — "터치로 발생하는 30점 원자적 점프를 뚜렷한 시각·무드 변화로 반드시 번역해야
한다" — 을 이행한다. 새로운 포즈·색·이벤트 규칙은 발명하지 않는다. 오직 이미 잠긴
업스트림 신호(포즈 1차 결과, Layer 1 연속 블렌드값, 날씨 enum, `isLive` 라이브/오프라인
구분)가 프레임 하나 안에서 불연속으로 바뀔 때 그것을 화면에 "어떻게" 그릴지의 타이밍/보간
계약만 정의한다.

### 0. 신규 상수 — `POSE_BLEND_DURATION`

`POSE_BLEND_DURATION = 0.2` (const, 초)

라이브 목격 중 원자적/즉시 트리거로 발생하는 포즈 전환(Layer 2 카테고리 스왑 + Layer 1
연속값 재타겟팅, §2)에 공용으로 쓰이는 크로스페이드 길이.

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 포즈 블렌드 지속시간 | `POSE_BLEND_DURATION` | float (const) | 0.2 (초) | §2의 `v_rendered(t)` 보간이 공유하는 단일 지속시간 상수 |

**값 선정 근거** (기존 3개 duration 상수와의 대조):

- **`CROSSFADE_DURATION`(3.0s)** — 배경 무드 톤이라 여유가 있고, weather-system.md가
  이미 "3초는 팝/플래시를 피할 만큼 충분히 길되 '정적인 포근함'의 배경 등급을 벗어나지
  않을 만큼 짧다"고 명시적으로 근거를 남겼다. 하지만 이 크기는 손끝 직결 피드백(Pillar 1
  "즉각 반응")에는 너무 느리다 — 감자 본인의 자세가 3초씩 걸려 바뀌면 "만졌는데 한참
  있다 반응한다"는 인상을 준다.
- **`GROWTH_BREATH_DURATION`(0.4–0.6s)** — 감자 자신의 실루엣이 바뀐다는 점에서 가장
  가까운 선례지만, 이 이벤트는 **수동적**(플레이어 행동과 무관하게 경과 시간만으로
  트리거)이라 "여유롭게 목격되는" 페이싱이 허용된다. 이번 전환은 반대로 플레이어 자신의
  제스처 완료가 원인이므로, 그보다 명백히 빨라야 인과관계("내가 만졌더니 바로
  바뀌었다")가 즉시 읽힌다.
- **`HARVEST_ARRIVAL_DURATION`(0.8–1.0s)**, **`OnRecovered` 번짐(~1초+)** — 둘 다
  의도적으로 무거운 서사적 순간(도착/부활)이다. 일상적 포즈 전환에 이 무게를 주면 매
  터치마다 과장된 이벤트처럼 느껴져 "정적인 포근함" 원칙과 art-bible의 "잦은 이벤트에
  정점급 무게를 주지 않는다"는 선례(lifecycle-progression-system.md)에 위배된다.

`0.2s`(200ms)는 위 세 상수의 최저값(`GROWTH_BREATH_DURATION` 0.4s)보다도 명백히 짧게
잡아, "이 전환은 성장처럼 여유로운 수동 이벤트가 아니라 손끝 결과에 곧바로 이어지는
되먹임"이라는 위계를 duration 값 자체로 표현한다. 동시에 0초(순간 스냅)는 아니므로 전신
실루엣이 한 프레임에 통째로 바뀌는 "플래시" 위험(art-bible Section 1 원칙 3)을 피한다.
`technical-preferences.md`가 검토 중인 idle 화면 30fps 스로틀을 가정해도 `0.2s`는
6프레임을 확보해 부드러운 블렌드로 읽히기에 충분하다(4~5프레임 이하로 더 줄이면 스로틀
상황에서 다시 컷처럼 보일 위험이 있다).

범위가 아니라 **단일 고정값**으로 제안한다 — `GROWTH_BREATH_DURATION`/
`HARVEST_ARRIVAL_DURATION`이 범위인 이유는 전환 쌍마다 아트가 살짝 다른 타이밍을 가질
수 있는 키프레임 애니메이션이기 때문이지만, 이 전환은 `CROSSFADE_DURATION`과 같은
성격의 **절차적(procedural) 크로스페이드**라 모든 카테고리 쌍에 동일한 값을 쓰는 것이
자연스럽다(`CROSSFADE_DURATION`도 단일값).

**Output Range:** 고정 상수, 튜닝 시 안전 범위는 대략 0.15–0.3s로 예상(Tuning Knobs
섹션에서 정식 확정) — 하한은 30fps 프레임 수 확보, 상한은 `GROWTH_BREATH_DURATION`
최저값(0.4s)을 넘지 않는 것.

**Example:** 습도 위기(Rain-huddled) 상태에서 창문 드래그가 완료되는 순간, 화면은 그
프레임에 즉시 블렌드를 "시작"하되(입력 지연 없음), 시각적으로 다 펴지기까지 0.2초에
걸쳐 부드럽게 이어진다 — 순간 팝도 아니고 3초짜리 배경 크로스페이드도 아닌, 그 사이의
별도 티어.

---

### 1. 전환 모드 판정 — `BlendMode(isLive, source)`

`BlendMode(isLive, source) = Snap` (if `isLive = false`); 그 외에는 `source`에 따라
`PoseBlend | PassThrough | Owned`

`isLive`는 이 시스템이 새로 발명하는 신호가 아니다 — weather-system.md
`CROSSFADE_DURATION`과 lifecycle-progression-system.md `GROWTH_BREATH_DURATION`이 이미
확립한 "라이브 전환에만 재생, 오프라인 갭 후 재개는 즉시 스냅" 게이트를 그대로
상속한다.

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 라이브 여부 | `isLive` | bool | {0,1} | 앱이 열려 실시간으로 경계를 통과했는지(1) vs 오프라인 갭 재구성으로 도달했는지(0) |
| 전환 원인 | `source` | enum | {`TouchDelta`, `WeatherEnumStep`, `AmbientDrift`, `BespokeEvent`} | 아래 결정표 참고 |
| 전환 방식 | `BlendMode` | enum | {`Snap`, `PoseBlend`, `PassThrough`, `Owned`} | §2의 렌더링 방식을 결정하는 출력 |

**결정표:**

| `isLive` | `source` | `BlendMode` | 근거 |
|---|---|---|---|
| `false` | (무관) | `Snap` | 오프라인 갭 재개 — weather/lifecycle 선례와 동일, 예외 없음(아무도 보지 않은 전환에 애니메이션 소비 안 함) |
| `true` | `TouchDelta`(`TOUCH_Δ=30` 원자적 적용, potato-stat §3) | `PoseBlend` | 한 프레임 안에서 overshoot이 최대 30점 뛰어 Layer 2 승자가 바뀔 수 있음 — 이 GDD가 채워야 할 원래 계약 |
| `true` | `WeatherEnumStep`(Sunny↔비Sunny 전환에 의한 Sunny-content 승격/강등, Core Rule 2) | `PoseBlend` | **날씨가 원인이지만 gradual하지 않다** — 날씨 enum 자체는 weather-system §5에 의해 구간 경계에서 계단형으로 즉시 바뀐다(스탯 overshoot 드리프트와는 다른 신호). Sunny-content 게이트는 이 overshoot 값이 아니라 enum 자체를 직접 참조하므로 touch와 동일하게 "라이브 목격 중 순간 스텝"이다 |
| `true` | `AmbientDrift`(`BASELINE_RATE`/`ACTIVE_RATE`에 의한 overshoot의 연속 드리프트가 Layer 2 최댓값 승자를 바꾸는 경우) | `PassThrough` | overshoot가 시간에 대해 연속(§4 참고)이므로 승자가 바뀌는 교차점에서 두 후보 모두 `b≈0` 근방 — 이미 저진폭이라 추가 블렌드가 불필요 |
| `true` | `BespokeEvent`(`OnWitherEntered`/`OnRecovered`/`OnStageAdvanced`/Harvest-proud) | `Owned` | 이 4개는 이미 자기 소유의 1회성 전환 애니메이션이 확정돼 있음(potato-stat Visual/Audio §4, lifecycle `GROWTH_BREATH_DURATION`/`HARVEST_ARRIVAL_DURATION`, art-bible) — `POSE_BLEND_DURATION`을 얹으면 중복 지연만 생김. 이 표는 이 4개를 명시적으로 범위 밖으로 배제한다 |

**Output Range:** `{Snap, PoseBlend, PassThrough, Owned}` 4값 열거형, 상호 배타적, 항상
정확히 하나 반환.

**Example:** 습도가 폭우/안개 활성 날씨 하에서 `ACTIVE_RATE=+2.5/hr`로 서서히 상승해
`overshootHumidity`가 온도 overshoot을 넘어서는 순간 → `source=AmbientDrift` →
`BlendMode=PassThrough`(추가 블렌드 없음). 같은 상황에서 플레이어가 창문 드래그를
완료해 습도가 원자적으로 -30 되는 순간 → `source=TouchDelta` → `BlendMode=PoseBlend`
(§2 적용). 동일한 "Rain-huddled → Baseline" 결과라도 원인에 따라 렌더링 방식이 다르다.

---

### 2. 렌더링 값 보간 — `v_rendered(t)`

`v_rendered(t) = v_start + (v_target - v_start) · ease(clamp((t - t_retarget) / POSE_BLEND_DURATION, 0, 1))`,
`ease(x) = 1 - (1-x)²`

`BlendMode=PoseBlend`일 때만 적용되는 공용 크로스페이드 공식이다. **Layer 2(포즈
카테고리 크로스페이드 가중치)와 Layer 1(각 스탯의 연속 블렌드값 `b`) 양쪽 채널에
동일하게 적용**하며, 채널마다 독립적인 `(v_start, t_retarget)` 상태를 유지한다(§3에서
이 확장의 이유를 상세히 다룬다).

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 현재 시각 | `t` | float | ≥0 (초, 실경과시간) | 프레임 타임스탬프. 프레임 수가 아닌 초 단위 — 30fps 스로틀 하에서도 정확 |
| 재트리거 시각 | `t_retarget` | float | ≥0 (초) | 이 채널이 가장 최근 새 목표를 받은 시각(§4) |
| 블렌드 시작값 | `v_start` | float | 0–1 | `t_retarget` 순간 렌더링되고 있던 실제 값(재트리거 시 이전 렌더값을 그대로 승계) |
| 블렌드 목표값 | `v_target` | float | 0–1 | 그 프레임의 라이브 목표값. Layer 1: potato-stat이 그 프레임에 계산한 `b`(overshoot/halfwidth). Layer 2: `1`(신규 포즈로 완전 전환) |
| 완화 함수 | `ease(x)` | float→float | 0–1 | 2차 ease-out, 오버슛 없음 — `GROWTH_BREATH_DURATION`의 "오버슛=레벨업 징글처럼 읽혀 Pillar 3 위반" 원칙을 그대로 재사용, 별도 튜닝 노브로 노출하지 않음(스코프 최소화) |
| 렌더링 값 | `v_rendered(t)` | float | 0–1 | 그 프레임에 실제로 그리는 값 |

**Output Range:** `v_start`와 `v_target` 사이를 단조로 접근, `t ≥ t_retarget + POSE_BLEND_DURATION`
에서 정확히 `v_target`으로 클램프 고정(무한 점근 아님 — 정확히 종료).

**Example:** 습도 overshoot(high)=15 → Layer 1 `b_humidity=1.0`(Rain-huddled 최대 처짐)
상태에서 창문 드래그 완료(`TOUCH_Δ=-30`) → `overshootHumidity`가 그 프레임에 0으로
떨어져 `v_target=0`. `t_retarget=0`, `v_start=1.0`.
- `t=0.1s`(블렌드 중간): `ease(0.5) = 1-(0.5)² = 0.75` → `v_rendered = 1.0 + (0-1.0)×0.75 = 0.25`.
- `t=0.2s`: `v_rendered = 0`(완전 수렴 — 클램프에 의해 정확히 0, 그 이후 잔여 오차 없음).

같은 프레임에 Layer 2 크로스페이드 가중치도 동일한 `t_retarget=0`으로 시작해 `t=0.2s`에
신규 포즈(Baseline)로 완전히 전환된다 — 두 채널이 같은 시계를 쓰므로 "실루엣은 이미 다
펴졌는데 눈매만 아직 처져 있다" 같은 합성 불일치가 생기지 않는다.

---

### 3. Layer 1 적용 범위 — 원시 그대로 통과시키지 않는다

**결론: Layer 1도 §2의 `v_rendered(t)`를 그대로 적용해야 한다.** "이미 연속이니 매
프레임 그대로 통과시키면 된다"는 가설은 **원인이 `AmbientDrift`일 때만 맞고,
`TouchDelta`/`WeatherEnumStep`일 때는 틀렸다.**

`b = clamp(overshoot/halfwidth, 0, 1)`(potato-stat §2)는 값의 *모양*이 연속(구간 없는
매끈한 곡선)이라는 뜻이지, *시간에 대해* 매끄럽다는 뜻이 아니다 — 매 프레임 스탯값으로
부터 새로 계산될 뿐 이전 프레임 값에 대한 기억(관성)이 없다. `TOUCH_Δ=30`은 세 스탯의
반폭(25/20/15) 중 가장 넓은 것보다도 크므로, 터치 한 번이 `b`를 한 프레임 안에 `0→1`
또는 `1→0` 전 범위로 튕길 수 있다 — Layer 2 카테고리 라벨이 바뀌는 문제와 **정확히
동일한 종류의 팝**이 연속 파라미터(눈꺼풀 처짐 등) 위에서 일어난다.

만약 Layer 2만 블렌드하고 Layer 1을 원시로 통과시키면, 실루엣(전신 자세)은 0.2초에
걸쳐 부드럽게 펴지는데 그 위에 얹힌 눈매만 같은 프레임에 순간적으로 활짝 뜨는 **합성
레이어 어긋남**이 생겨 오히려 아무것도 안 하느니만 못한 "버그처럼 보이는" 결과를
낳는다. 따라서 두 레이어는 반드시 같은 트리거·같은 지속시간·같은 시계로 함께
블렌드되어야 한다.

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 적용 대상 채널 | `channel` | enum | {`Layer2_pose`, `Layer1_temperature`, `Layer1_humidity`, `Layer1_moisture`} | §2 보간을 독립적으로 적용받는 4개 렌더 채널 |
| 채널별 블렌드 적용 여부 | `AppliesBlend(channel, BlendMode)` | bool | {0,1} | `BlendMode=PoseBlend`이면 4개 채널 **전부** 1(예외 없음); `PassThrough`/`Snap`이면 전부 0(§1·§2 규칙 그대로 각자 적용) |

**Output Range:** `{true, false}` — `PoseBlend` 트리거 시 항상 4채널 전부 `true`(부분
적용 금지, 레이어 간 시계 불일치 방지).

**Example:** 습도·온도가 동시에 안전 구간 밖이었다가(습도가 근소하게 우세해
Rain-huddled 표시 중) 플레이어가 물뿌리개(수분 관련, 무관한 스탯)를 완료해도
`AmbientDrift` 중이던 습도/온도 채널은 그대로 `PassThrough` 유지 — `TOUCH_Δ`가 실제로
건드린 채널(이 예시에선 수분, Layer 1의 3번째 채널)만 `PoseBlend`로 전환된다. "터치가
발생했다"가 아니라 "그 채널의 목표값이 그 프레임에 원자적으로 바뀌었다"가 트리거
조건이다.

---

### 4. 재트리거(Retarget) 규칙 — 중첩 전환은 큐 없이 즉시 갱신

블렌드가 아직 끝나기 전(`t < t_retarget + POSE_BLEND_DURATION`)에 같은 채널에 새
목표가 도착하면(예: `touch-care-interaction.md`가 이미 확정한 "쿨다운 없음" 원칙에
따라 짧은 시간 안에 제스처가 연달아 완료되는 경우), **인터럽트 후 재타겟팅한다 —
큐잉하지 않는다.**

`(v_start, t_retarget) ← (v_rendered(t_now), t_now)`, 새 `v_target` 즉시 대입

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 재트리거 시점 | `t_now` | float | ≥0 (초) | 새 목표가 발행된 프레임의 시각 |
| 갱신 전 렌더값 | `v_rendered(t_now)` | float | 0–1 | 인터럽트 순간의 실제 화면값(§2) — 새 `v_start`로 승계, 텔레포트 없음 |

**근거:** touch-care-interaction.md가 이미 확립한 실시간 권위 원칙과 정합한다. 큐잉하면
"지금 상태"가 아니라 "몇 개 전 상태"를 보여주게 되어 Pillar 1(즉각 반응)에 정면으로
위배된다. 또한 스팸을 허용하는 게임(touch-care-interaction Edge Cases "쿨다운 도입은
Pillar 1 위반")이므로, 큐를 쓰면 연타 시 큐 길이가 무한정 자랄 위험도 있다 — 재타겟팅은
이 위험 자체를 구조적으로 제거한다.

**Output Range:** 항상 정확히 `POSE_BLEND_DURATION`만큼의 새 블렌드 창(누적 남은 시간이
아니라 매번 전체 창 재사용) — 연타해도 블렌드가 점점 빨라지거나 깜빡이지 않는다.

**Example:** `t=0.1s`에 `v_rendered=0.25`(§2 예시 이어서)인데, 같은 프레임에 다른
스탯이 동시에 위기로 전환돼 이 채널의 새 목표가 `v_target'=0.6`으로 바뀜 →
`v_start ← 0.25`, `t_retarget ← 0.1s`. `t=0.3s`(재트리거로부터 0.2초 후)에
`v_rendered=0.6` 도달 — 원래 목표(0)로 향하던 중간에 방향을 꺾어 매끄럽게 새 목표로
재수렴, 순간 점프 없음.

**예외(재타겟팅이 아니라 취소):** `BespokeEvent`(§1, `OnWitherEntered` 등)가 블렌드
도중 발생하면 재타겟팅이 아니라 **즉시 취소하고 넘긴다** — Core Rule 3·6이 이미 확정한
대로 Wither는 무조건 최우선이며 그 자신의 전환 애니메이션을 재생하므로, 진행 중이던
`PoseBlend`는 그 프레임에 폐기된다(별도 페이드아웃 없음, `BespokeEvent`가 화면을 완전히
인수).

---

**상수 요약(레지스트리 등록용):** `POSE_BLEND_DURATION=0.2s` — `CROSSFADE_DURATION`/
`GROWTH_BREATH_DURATION`/`HARVEST_ARRIVAL_DURATION`과 동일한 "전환 지속시간" 카테고리.

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드.)*

- **If 신규 세이브(최초 프레임, 이전 렌더값이 존재하지 않는다면)**: 블렌드할 "이전 상태"가
  없으므로 `v_start = v_target`, `t_retarget = 0`으로 초기화해 그 프레임에 즉시
  목표값으로 스냅한다(Formulas §2가 자연히 이 경우를 흡수 — `t=0`에서 이미
  `v_rendered=v_target`).

- **If 앱이 완전히 종료되지 않고 잠시 백그라운드로 전환됐다가(예: 알림 확인 후 복귀)
  블렌드가 진행 중이었다면**: `isLive` 판정과 별개로, 재개 시 벽시계 경과시간을 그대로
  `t`에 반영한다 — 백그라운드 시간이 `POSE_BLEND_DURATION(0.2s)`보다 길면 Formulas
  §2의 클램프가 이미 `v_target`에 도달한 것으로 자연히 처리하므로 별도 특별 처리가
  필요 없다(0.2초는 백그라운드 전환에 비해 항상 무시할 만큼 짧다).

- **If 같은 프레임에 서로 다른 두 채널이 동시에 `PoseBlend`를 트리거한다면(예: 두 스탯이
  동시에 안전 구간에 복귀)**: 각 채널은 Formulas §2·§4가 정의한 독립된
  `(v_start, t_retarget)` 상태를 가지므로 서로 간섭 없이 병렬로 블렌드된다 — 한 채널의
  전환이 다른 채널의 타이밍에 어떤 영향도 주지 않는다.

- **If `BespokeEvent`(`OnWitherEntered` 등)가 `PoseBlend` 진행 도중 발생한다면**:
  Formulas §4의 예외 규칙대로 진행 중이던 블렌드는 즉시 취소되고 화면을
  `BespokeEvent`의 자체 전환 애니메이션이 인수한다 — 별도 페이드아웃 없이 그 프레임에
  폐기된다(Core Rule 3·6과 정합).

- **If `OnWitherEntered`와 `OnStageAdvanced`가 정확히 같은 틱에 발생한다면(생장 경계
  통과와 방치 지표 100 도달이 우연히 같은 프레임에 겹치는 극히 드문 경우)**: Core
  Rule 6이 이미 확정한 대로 `OnWitherEntered`가 승리한다 — 그 프레임의 성장전환
  애니메이션(`GROWTH_BREATH_DURATION`)은 재생되지 않고 실루엣만 새 생장 단계로 즉시
  스냅되며, Wither 진입 연출만 재생된다. lifecycle의 `growthHoursAccumulated`는
  정상적으로 갱신됐지만(수치는 정확), 그 갱신을 알리는 시각 연출만 생략된다는 뜻이다.

- **If 홈 화면 위젯 시스템처럼 이 시스템의 출력을 구독하는 하위 시스템이 블렌드 도중
  순간값을 읽는다면**: `v_rendered(t)`는 정의상 항상 `[0,1]`(Layer 1) 또는 유효한
  포즈 가중치(Layer 2) 범위 안에 있으므로, 어느 시점에 읽더라도 구독자에게 미정의
  상태를 노출하지 않는다 — 별도 "블렌드 중" 플래그를 추가로 노출할 필요가 없다.

- **If 날씨 전환(`WeatherEnumStep`)과 터치 완료(`TouchDelta`)가 같은 채널에 같은
  프레임에 겹친다면(예: 날씨가 Sunny로 막 바뀐 순간 마침 물뿌리개도 완료됨)**: 둘 다
  `PoseBlend`를 요청하므로 Formulas §4의 재트리거 규칙이 그대로 적용된다 — 나중에
  도착한 요청이 먼저 도착한 요청의 진행 중이던 렌더값을 승계해 재수렴할 뿐, 특별한
  중재 규칙이 추가로 필요하지 않다(둘 다 같은 `PoseBlend` 계열이라 우선순위 충돌 자체가
  없음).

## Dependencies

### Upstream (이 시스템이 의존)

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 감자 상태 관리 | Hard | 포즈 1차 결과·윤곽선 블렌드값(`b`)·Blush·`neglectMeter`·`isShielded`·`OnWitherEntered`/`OnRecovered` 구독(양방향 확인: potato-stat Dependencies가 이미 "표정/애니메이션 | Hard"로 명시) |
| 날씨 시스템 | Soft | `GetCurrentWeather()` 조회(양방향 확인: weather-system Downstream 표가 이미 "표정/애니메이션 시스템 | 조회 | Soft"로 명시) |
| 생애주기 진행 | Soft | `stageIndex(G)`·`OnStageAdvanced` 구독(양방향 확인: lifecycle Downstream 표가 이미 "표정/애니메이션 시스템 | 조회 | Soft"로 명시) |

**세이브/영속성**: 이 시스템은 별도 저장 상태를 갖지 않는다 — 렌더링 값(`v_rendered`,
`t_retarget`, 진행 중인 블렌드 상태)은 전부 업스트림 세 시스템의 영속 데이터로부터 매
세션 재계산되는 파생값이며, 블렌드 자체가 최대 `POSE_BLEND_DURATION(0.2s)`로 세션
경계를 넘어 의미를 가질 필요가 없다(Edge Cases 참고). Foundation 레이어에 등록할 것이
없다.

### Downstream (이 시스템에 의존)

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 홈 화면 위젯 시스템(Alpha, 미설계) | Hard | 이 시스템이 계산한 최종 포즈+실루엣을 축소 LOD로 미러링 — 이 시스템 없이는 위젯이 미러링할 대상 자체가 없음 |
| 이름짓기 & 수확 카드(미설계) | Soft | Harvest-proud 퇴장 연출 트리거 — 응답 없어도 수확 액션(이름 기록·카드 생성) 자체는 정상 진행, 퇴장 연출만 생략됨 |

**Hard vs Soft 판정 기준**: weather-system.md·lifecycle-progression-system.md와 동일 —
이 시스템이 응답하지 않을 때 대상 시스템이 아예 작동을 멈추면 Hard, 연출만 밋밋해지고
핵심 루프는 유지되면 Soft.

## Tuning Knobs

| 노브 | 현재값 | 권장 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `POSE_BLEND_DURATION` | 0.2초 | 0.15~0.3초(Formulas §0에서 이미 도출) | 하한(0.15s) 미만: idle 30fps 스로틀 하에서 4프레임 이하로 줄어 다시 순간 팝처럼 보임 — Layer 1 반박(§3)이 막으려던 "레이어 어긋남 버그처럼 보임" 문제가 재발할 위험 | 상한(0.3s) 초과: `GROWTH_BREATH_DURATION` 최솟값(0.4s)에 근접해 "손끝 직결 피드백은 성장보다 명백히 빨라야 한다"는 위계(Formulas §0 근거)가 흐려짐. `touch-care-interaction.md`의 즉각 반응 기대와 충돌해 "만졌는데 반응이 느리다"는 인상 위험 |

**노브 간 상호작용**: `POSE_BLEND_DURATION`을 바꾸면 Formulas §2·§3·§4에서 정의한
모든 채널(Layer 2 포즈 + Layer 1 3개 스탯 윤곽선)이 동시에 영향을 받는다 — 채널별로
분리된 노브를 두지 않는 이유는 §3이 이미 확정한 "4채널 전부 같은 시계로 동시 블렌드"
원칙(레이어 어긋남 방지)이 개별 튜닝을 허용하면 깨지기 때문이다. `ease(x)` 함수의
정확한 형태(현재 2차 ease-out 고정)는 Formulas §2에서 의도적으로 노브화하지 않았다 —
스코프를 이 하나의 값으로 최소화하는 것이 lean 모드·솔로 개발 기준에 맞는다는 판단
(systems-designer 제안, 사용자 승인).

## Visual/Audio Requirements

> 이 섹션은 새로운 무드·색·형태를 정의하지 않는다 — art-bible이 이미 잠근 원칙들을
> 인용해 Formulas §0~§4가 신규 도입한 `POSE_BLEND_DURATION` 메커니즘 하나만 아트디렉션
> 관점에서 검수·확장한다 (art-director 상담, 2026-07-18).

### 1. `POSE_BLEND_DURATION=0.2s` 검수 — 확정, 조정 불필요

art-bible UI/HUD Visual Direction §4가 이미 이 판단에 쓸 두 준거점을 못박아 뒀다 —
버튼 프레스의 **스냅**(스케일 -4~6%, ~100ms, 오버슈트 없음)과 패널/모달 전환의
**소프트**(200-300ms, ease-out만, 스프링·오버슈트 금지). `POSE_BLEND_DURATION`
(200ms)은 산술적으로 소프트 대역 하한과 겹치지만, 이 겹침은 결함이 아니라 의도된
안무(choreography)로 읽어야 한다.

터치되는 오브젝트(창문·난로·물뿌리개) 자체의 즉각 반응은 이미 버튼과 동일한 스냅
티어(~100ms)에서 먼저 완료된다(touch-care-interaction.md 소관, 이 시스템은 관여하지
않음 — Core Rule 7). 감자의 전신 포즈가 그 뒤 0.2초에 걸쳐 반응하는 것은 "오브젝트를
만짐 → 그 결과로 살아있는 존재가 한 박자 늦게 반응함"이라는 인과관계를 물리적으로
그럴듯하게 만드는 장치다. 만약 감자의 전신 포즈까지 버튼과 동일한 ~100ms로 스냅되면,
Shape Language §1이 감자에게 유일하게 부여한 지위 — Tier 1 Hero, "표정이 실루엣
자체에서 나오는 살아있는" 존재 — 가 Tier 2(버튼 등 정적 오브젝트)와 동일한 기계적
리듬으로 눌리는 느낌을 줄 위험이 있다.

ease 함수 선택도 같은 방향을 가리킨다. 포즈 블렌드는 2차 ease-out(오버슛 없음)을
쓰는데, 이는 하드 컷·즉시 스냅(버튼 프레스)이 아니라 패널 전환·`GROWTH_BREATH_DURATION`과
동일한 "소프트" 계열 곡선이다. 지속시간이 소프트 대역 하단에 걸쳐 있어도 커브 자체가
"스냅"이 아니므로, 우려한 "스냅한 UI 위젯처럼 기계적으로 느껴짐" 위험은 커브 선택
단계에서 이미 차단돼 있다.

### 2. 블렌드의 시각적 질감 — 순수 케이지 보간, 추가 워시 불필요

Character Design Direction §4(LOD 철학)가 이미 이 질문의 답이 되는 채널 분리를
확정해뒀다 — "지오메트리(윤곽선·Blush 위치)는 모든 확대율에서 보존, 재질(결·글레이즈·
그라디언트 부드러움)만 확대율에 따라 감소". `POSE_BLEND_DURATION`이 다루는 두 채널
(Layer 2 포즈 카테고리 스왑, Layer 1 윤곽선 굴곡값)은 정의상 지오메트리 채널이다 —
"윤곽선이 어디 있는가"의 문제이지 "표면에 무엇이 칠해져 있는가"의 문제가 아니다.
종이결·글레이즈는 그 케이지 위에 항상 이미 렌더링되고 있는 상시 배경 재질이므로,
블렌드가 진행되는 동안에도 자연히 화면에 남아 있다 — 새로 얹을 필요가 없다.

오히려 블렌드 자체에 추가 번짐/워시 레이어를 얹으면 두 문제가 생긴다.

1. **신호 충돌**: 접촉점 번짐(Mood & Atmosphere §1)은 이미 별도 시스템
   (touch-care-interaction.md)이 소유한 확정된 시각 언어다. 포즈 블렌드에도 같은
   번짐을 얹으면 같은 화면에 "무엇이 왜 번지고 있는가"를 나타내는 두 개의 겹치는
   신호가 생겨, 접촉점 번짐 고유의 "만짐의 예고"라는 의미(Section 1 원칙 1)가
   희석된다.
2. **절제 원칙 위반**: Character Design Direction §3("절제가 만드는 표현")이 이미
   감자의 표현 진폭을 좁게 유지하기로 확정했다. 일상적으로 반복되는 모든 포즈 전환에
   장식적 워시 플러리시를 매번 더하면, 여행 개구리(旅かえる) 축을 기준점으로 삼은
   이 절제 원칙에 정면으로 반한다.

기술 참고 노트가 언급한 Unity 6.5 Sprite BlendShape API(케이지 기반 FFD)를 채택한다면,
케이지 정점 간 직선 보간 그 자체가 이 요구를 정확히 충족한다 — 별도 셰이더 레이어
없이도 이미 종이결/글레이즈가 입혀진 표면 위에서 지오메트리만 움직이는 결과가
나온다. 정확한 구현 타당성은 technical-artist와 별도 확인이 필요하지만, 아트디렉션
방향은 "얹지 않는다"로 확정한다.

### 3. Sunny-content 승격 순간 — 표준 블렌드로 충분, 별도 플러리시는 반려 대상

이 GDD 스스로가 이미 `WeatherEnumStep`을 `Owned`(4개 bespoke 이벤트 전용)가 아니라
`TouchDelta`와 동급인 `PoseBlend`로 분류했다(§1 결정표) — "이 전환은 조용히 반복되는
일상적 사건이지 정점급 서사가 아니다"라는 판단을 formulas 단에서 이미 내린 셈이다.
art-bible이 정점급 무게를 예약해둔 대상은 명확히 한정적이다 — 되돌리기/회복의
중심→바깥 골든 블룸(Mood & Atmosphere §4), 위기 진입의 잎-늘어짐 1회(§3), 수확의
표면 하이라이트(Character Design §2(a), "의미 예약(semantic reservation)" 원칙으로
명시). 이들은 생애주기 전체에서 극히 드물게 발생하지만, 맑음↔비맑음 전환은 날씨
순환 주기(weather-system.md 기준 6~10시간 세그먼트)마다 반복될 수 있는 훨씬 잦은
사건이다. 여기에 캐릭터 레벨 플러리시까지 더하면 이 시스템 §0 근거("잦은 이벤트에
정점급 무게를 주지 않는다")와 art-bible의 의미 예약 원칙 둘 다와 충돌한다 — 반복해서
쓰면 정말 특별한 순간(회복·수확)의 특별함이 희석된다.

배경 레이어의 "정지된 옅은 햇살 번짐 워시"(Mood & Atmosphere §2-1)는 이미 그 자체로
"맑아짐"을 announce하는 신호이며, `CROSSFADE_DURATION=3.0s`라는 훨씬 느린 별도
시계로 움직인다(weather-system.md Visual/Audio Requirements §2). 캐릭터 레벨에
0.2초짜리 신호를 추가로 얹으면 같은 사건을 서로 다른 두 속도로 두 번 announce하는
꼴이 되어, art-bible Mood & Atmosphere의 Cross-State Quick Reference가 요구하는
"한 그림책의 한 페이지" 통일감과 어긋나는 시각적 잡음을 만든다. **결론: 표준 포즈
블렌드(§2 formula)만으로 충분하며, 추가 플러리시는 반려한다.**

### 4. LOD 상호작용 — 두 컨텍스트를 분리해서 답한다

art-bible Character Design Direction §4의 Small-scale(축소) 행이 "감자 마을 선반,
위젯/알림 아이콘"을 한 행에 묶어뒀지만, 이 GDD의 관점에서는 두 컨텍스트가 실제로는
서로 다른 질문이다.

**(a) 감자 마을 선반 — 범위 밖.** Character Design Direction §2(c)와 §3이 이미
확정한 대로, 마을에 전시된 감자는 항상 고정된 **수확-자랑스러움(Harvest-proud)**
포즈만 취하며(코스튬만 다름), 날씨·스탯에 반응해 포즈가 바뀌는 살아있는 개체가
아니다. 즉 마을 화면에는 애초에 포즈 *전환*이라는 사건 자체가 존재하지 않으므로
`POSE_BLEND_DURATION`이 적용될 대상이 없다. 이 GDD의 Interactions/Downstream 표가
"홈 화면 위젯 시스템"만 다운스트림으로 적고 감자 마을을 명시하지 않은 것은 정확한
설계였음을 이 자리에서 재확인한다.

**(b) 홈 화면 위젯 — 즉시 스냅(settled end-state만 미러링) 권장.** 이쪽은 실제로
라이브 포즈가 바뀌는 "현재 감자"를 축소 미러링하는 대상이 맞다. 여기서는 이 GDD가
이미 세운 `isLive=false → Snap` 선례(Formulas §1 결정표)의 자연스러운 연장을
제안한다 — 네이티브 홈 위젯은 통상 OS 자체 갱신 주기로 스냅샷을 다시 그리는 방식이지
연속 프레임 렌더링이 아니므로, "앱을 열어 실시간으로 목격"하는 상황과 구조적으로
다르다. 오프라인 갭 재개와 동일한 논리 — "아무도 실시간으로 지켜보지 않는 전환에
애니메이션을 소비하지 않는다" — 가 그대로 적용될 수 있다.

다만 이 권고는 순수 아트디렉션 판단이며, 실제 OS 위젯 프레임워크(iOS/Android)가 몇
ms 단위 갱신·애니메이션을 허용하는지는 이 문서가 결정할 사안이 아니다 —
technical-artist·ui-programmer와 별도 확인이 필요하다(Open Questions로 넘기는 것을
권장).

한 가지 짚어둘 점: Character Design §4의 "지오메트리는 모든 확대율에서 보존"이라는
규칙은 이 질문에 직접 답하지 않는다 — 그 규칙은 "한 프레임에 어떤 정보가 그려지는가"
(그라디언트·결의 유무)를 규정할 뿐, "그 정보가 시간에 걸쳐 애니메이션되는가"는 별개의
축이다. 위젯이 블렌드 없이 최종 상태로 스냅하더라도, 그 최종 상태의 실루엣 자체는
여전히 축소 크기에서도 표정이 읽히는 윤곽선과 Blush 위치 정보를 완전히 보존한다 —
즉 스냅 처리가 Section 1 원칙 2("아주 작은 크기에서도 표정이 실루엣에서 읽혀야
한다")를 위반하지 않는다.

## UI Requirements

이 시스템은 직접적인 화면 UI를 갖지 않는다 — 감자 자체의 렌더링은 게임 화면의 중심
오브젝트이지 UI 위젯이 아니며, 이 시스템의 산출물(최종 포즈+실루엣+블렌드 진행 상태)은
하위 UI 표면이 소비하는 **데이터 계약**일 뿐이다.

- **홈 화면 위젯 시스템**(Alpha, 미설계): Visual/Audio Requirements §4(b)가 권고한
  대로, 이 시스템의 `v_rendered` 라이브 블렌드가 아니라 settled end-state(블렌드
  완료 후 값)만 미러링해야 한다 — 정확한 갱신 주기·애니메이션 지원 여부는 Open
  Questions로 이월.
- **감자 마을 화면**(미설계): 이 시스템을 전혀 호출하지 않는다(AC-INT-08 확정) — 고정
  Harvest-proud 포즈 + 코스튬만 표시하는 별도 경로.
- **메인 게임 화면**(터치 케어 인터랙션 소유): 감자 렌더링 자체는 이 화면의 시각적
  중심이지만, 화면 레이아웃·터치 히트박스·CTA 버튼 배치 등은 art-bible UI/HUD와 각
  소유 시스템(터치 케어 인터랙션, 이름짓기 & 수확 카드)의 몫이다 — 이 GDD는 그
  중심에 놓일 감자가 무엇을 보여줄지만 책임진다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션.)*

아래 기준은 Detailed Design·Formulas·Edge Cases에서 정의된 필드·상수명을 그대로 사용한다.
이 시스템은 새 감정 규칙을 발명하지 않으므로(Overview), 아래 기준도 potato-stat·weather·
lifecycle이 이미 잠근 값(포즈 1차 결과, `b`, `GetCurrentWeather()`, `stageIndex(G)` 등)은
재검증하지 않고 **그 값들을 입력으로 받았을 때 이 시스템 고유의 합성/블렌드 계약**만
검증한다. 각 항목은 GDD 전체를 읽지 않은 QA 테스터도 독립적으로 재현·판정할 수 있도록
정확한 시작 조건과 기대값을 명시한다.

### 상태 전이

- **AC-ST-01 (Baseline 진입)**
  **GIVEN** potato-stat 1차 결과 = Baseline, `GetCurrentWeather() ≠ Sunny`(예: Rainy)
  **WHEN** 최종 포즈를 평가한다
  **THEN** 최종 포즈 = Baseline(일상 케어), Sunny-content로 승격되지 않는다.

- **AC-ST-02 (Baseline → Sunny-content)**
  **GIVEN** 최종 포즈 = Baseline, 1차 결과 = Baseline 유지
  **WHEN** `GetCurrentWeather()`가 Sunny로 전환된다
  **THEN** 그 프레임에 최종 포즈 = Sunny-content로 전이하고, `source=WeatherEnumStep`이므로 `BlendMode=PoseBlend`가 적용된다(§1).

- **AC-ST-03 (Baseline → 위기 포즈, 승격 규칙 미개입)**
  **GIVEN** 최종 포즈 = Baseline, `GetCurrentWeather() ≠ Sunny`
  **WHEN** 임의 스탯이 안전 구간을 이탈해 1차 결과가 위기 카테고리(예: Cold-curled)로 바뀐다
  **THEN** 최종 포즈 = Cold-curled로 즉시 전이한다 — 1차 결과가 Baseline이 아니므로 Core Rule 2 승격 규칙 자체가 애초에 평가되지 않는다.

- **AC-ST-04 (Sunny-content 진입, AND 조건 동시 충족)**
  **GIVEN** 1차 결과 = Baseline **그리고** `GetCurrentWeather() = Sunny`가 동시에 성립
  **WHEN** 최종 포즈를 평가한다
  **THEN** 최종 포즈 = Sunny-content.

- **AC-ST-05 (Sunny-content → Baseline, 순수 날씨 이탈)**
  **GIVEN** 최종 포즈 = Sunny-content, 1차 결과 = Baseline 유지(스탯 이탈 없음)
  **WHEN** `GetCurrentWeather()`가 Sunny 아닌 값으로 전환된다
  **THEN** 최종 포즈 = Baseline(위기 포즈로 가지 않는다).

- **AC-ST-06 (Sunny-content → 위기 포즈 직행, 날씨 이탈 + 그 사이 스탯 이탈)**
  **GIVEN** 최종 포즈 = Sunny-content
  **WHEN** 날씨가 Sunny를 벗어나는 것과 같은 구간 사이에 스탯 이탈이 먼저 발생해 1차 결과가 위기 카테고리(예: Rain-huddled)로 바뀌어 있었다
  **THEN** 최종 포즈는 Baseline을 거치지 않고 곧바로 Rain-huddled로 직행한다.

- **AC-ST-07 (Sunny-content → 위기 포즈, 날씨 유지 상태에서 스탯만 이탈)**
  **GIVEN** 최종 포즈 = Sunny-content, `GetCurrentWeather() = Sunny` 유지
  **WHEN** 임의 스탯이 안전 구간을 이탈해 1차 결과가 위기 카테고리로 바뀐다
  **THEN** 최종 포즈 = 해당 위기 포즈로 즉시 전이한다 — 1차 결과 ≠ Baseline이 된 순간 승격 규칙은 더 이상 적용되지 않는다.

- **AC-ST-08 (Rain-huddled 진입/이탈, 날씨 무관 채택)**
  **GIVEN** potato-stat 1차 결과 = Rain-huddled(습도 overshoot 최댓값 승리)
  **WHEN** 최종 포즈를 평가한다
  **THEN** `GetCurrentWeather()` 값과 무관하게 최종 포즈 = Rain-huddled로 그대로 채택된다. **WHEN** 이후 습도가 복귀하고 다른 이탈이 없다 **THEN** 그 시점 `GetCurrentWeather()`를 재조회해 Baseline 또는 Sunny-content로 재평가한다.

- **AC-ST-09 (Cold-curled 진입/이탈, 날씨 무관 채택)**
  Cold-curled에 대해 AC-ST-08과 동일한 패턴을 적용한다(온도 overshoot 최댓값 승리 기준).

- **AC-ST-10 (Wither-sagging 무조건 최우선, Core Rule 3)**
  **GIVEN** `neglectMeter ≥ 100`, `GetCurrentWeather()`가 임의값, 1차 결과가 임의의 위기 카테고리(예: Rain-huddled)
  **WHEN** 최종 포즈를 평가한다
  **THEN** 최종 포즈 = Wither-sagging — 날씨·1차 결과 값과 무관하게 다른 모든 조건을 무시한다.

- **AC-ST-11 (`OnRecovered` → 즉시 재평가)**
  **GIVEN** 최종 포즈 = Wither-sagging
  **WHEN** `OnRecovered` 이벤트가 발생한다
  **THEN** 표 전체를 즉시 재평가한다(potato-stat AC-VA-10과 동일 원칙) — 재평가 결과는 그 순간 1차 결과·날씨 조합에 따라 Baseline 또는 Sunny-content로 결정된다.

- **AC-ST-12 (Recovery 전환은 통과 이벤트, 지속 상태 아님)**
  **GIVEN** `OnRecovered` 발생 프레임
  **WHEN** 전환 애니메이션 재생이 종료된다
  **THEN** 화면은 즉시 위 표의 정적 상태(Baseline 또는 Sunny-content)로 귀속된다 — "Recovery 전환"이 프레임 간에 머무를 수 있는 자체 지속 상태로 존재하지 않는다.

- **AC-ST-13 (Harvest-proud 진입/퇴장)**
  **GIVEN** `stage=HarvestReady`, 외부(이름짓기&수확카드) 시스템이 Harvest-proud 퇴장 연출을 요청한다
  **WHEN** 요청을 수신한다
  **THEN** 그 즉시 화면을 Harvest-proud 연출이 인수(override)한다. **WHEN** 연출이 종료된다 **THEN** 최종 포즈 = 새 Sprout의 Baseline(다음 생애주기 첫 프레임)으로 전이한다.

- **AC-ST-14 (`HarvestReady` 도달은 포즈 해석에 영향 없음, Core Rule 4)**
  **GIVEN** `stage=HarvestReady`, `neglectMeter`가 방치로 100에 도달한다
  **WHEN** 최종 포즈를 평가한다
  **THEN** `HarvestReady` 도달과 무관하게 Wither-sagging이 정상적으로 나타난다 — 수확 준비 완료가 방치 안전망을 해제하지 않는다.

- **AC-ST-15 (Heatwave는 별도 상태 행 없음)**
  **GIVEN** 1차 결과 = Baseline, `overshootMoisture(low) > 0`(폭염에 의한 수분 low 이탈), `GetCurrentWeather() ≠ Sunny`
  **WHEN** 최종 포즈를 평가한다
  **THEN** 최종 포즈는 Baseline 카테고리 그대로 유지되고 Layer 1 윤곽선 블렌드(`overshootMoisture(low)` 기반 `b`)만으로 표현된다 — 신규 Layer 2 카테고리가 생성되지 않는다.

### 공식 검증

- **AC-F-01 (`POSE_BLEND_DURATION` 상수값)** **GIVEN** 시스템 초기화 **WHEN** `POSE_BLEND_DURATION` 조회 **THEN** 정확히 `0.2`(초), const.
- **AC-F-02 (`BlendMode=Snap` — `isLive=false`, `source` 무관)** **GIVEN** `isLive=false`, `source=TouchDelta`(임의값) **WHEN** `BlendMode` 평가 **THEN** `BlendMode=Snap` — `source` 값과 무관하게 항상 `Snap`.
- **AC-F-03 (`BlendMode=PoseBlend` — `TouchDelta`)** **GIVEN** `isLive=true`, `source=TouchDelta`(예: 창문 드래그 완료로 `TOUCH_Δ=30` 원자적 적용되어 Layer 2 승자가 바뀜) **WHEN** `BlendMode` 평가 **THEN** `BlendMode=PoseBlend`.
- **AC-F-04 (`BlendMode=PoseBlend` — `WeatherEnumStep`)** **GIVEN** `isLive=true`, `source=WeatherEnumStep`(Sunny↔비Sunny 전환) **WHEN** `BlendMode` 평가 **THEN** `BlendMode=PoseBlend` — 날씨가 원인이지만 gradual하지 않으므로 `Owned`가 아닌 `PoseBlend`로 분류된다.
- **AC-F-05 (`BlendMode=PassThrough` — `AmbientDrift`)** **GIVEN** `isLive=true`, `source=AmbientDrift`(`ACTIVE_RATE=+2.5/hr`로 습도가 서서히 상승해 `overshootHumidity`가 온도 overshoot을 넘어서는 교차점) **WHEN** `BlendMode` 평가 **THEN** `BlendMode=PassThrough` — 추가 블렌드 없이 원시값 통과.
- **AC-F-06 (`BlendMode=Owned` — `BespokeEvent` 4종)** **GIVEN** `isLive=true`, `source=BespokeEvent`(`OnWitherEntered`/`OnRecovered`/`OnStageAdvanced`/Harvest-proud 중 하나) **WHEN** `BlendMode` 평가 **THEN** 4개 이벤트 전부 동일하게 `BlendMode=Owned` — `POSE_BLEND_DURATION` 크로스페이드가 얹히지 않고 각 이벤트 고유 전환 애니메이션만 재생.
- **AC-F-07 (결정표 배타성)** **GIVEN** 임의의 `(isLive, source)` 조합 **WHEN** `BlendMode` 평가 **THEN** `{Snap, PoseBlend, PassThrough, Owned}` 중 정확히 하나만 반환(상호 배타적, 미정의 값 없음).
- **AC-F-08 (동일 결과·다른 원인 → 다른 `BlendMode`)** **GIVEN** [경로 A] 습도가 폭우/안개 활성 날씨 하에서 서서히 상승해 온도 overshoot을 넘어서는 상황(`AmbientDrift`)과 [경로 B] 동일한 "Rain-huddled→Baseline" 결과가 창문 드래그 완료(`TouchDelta`)로 발생하는 상황 **WHEN** 각각 `BlendMode` 평가 **THEN** 최종 포즈 결과는 동일(Baseline)해도 `BlendMode`는 다르다(경로 A=`PassThrough`, 경로 B=`PoseBlend`) — 렌더링 방식이 원인에 따라 분기됨을 확인.
- **AC-F-09 (`t=t_retarget` 경계 — 블렌드 시작점은 점프 없음)** **GIVEN** `v_start=1.0`, `v_target=0`, `t_retarget=0` **WHEN** `t=0`(=`t_retarget`) **THEN** `ease(0)=1-(1-0)²=0`, `v_rendered=1.0+(0-1.0)×0=1.0=v_start` — 블렌드 시작 순간 `v_start`와 정확히 일치.
- **AC-F-10 (블렌드 중간값, `t=0.1s`)** **GIVEN** 위와 동일 시작조건, `POSE_BLEND_DURATION=0.2` **WHEN** `t=0.1s` **THEN** `clamp((0.1-0)/0.2,0,1)=0.5`, `ease(0.5)=1-(0.5)²=0.75`, `v_rendered=1.0+(0-1.0)×0.75=0.25`.
- **AC-F-11 (`t=t_retarget+POSE_BLEND_DURATION` 경계 — 정확한 종료, 무한 점근 아님)** **GIVEN** 위와 동일 시작조건 **WHEN** `t=0.2s`(=`t_retarget+POSE_BLEND_DURATION`) **THEN** `v_rendered=0`(`v_target`)과 정확히 일치하도록 클램프되며, `t>0.2s` 이후에도 값이 `v_target`에 고정 유지된다(잔여 오차 없음).
- **AC-F-12 (Layer 1·Layer 2 공유 시계 — 4채널 동시 종료)** **GIVEN** 습도 `overshoot(high)=15`로 Layer 1 `b_humidity=1.0`(Rain-huddled 최대) 상태에서 창문 드래그 완료(`TOUCH_Δ=-30`)로 `overshootHumidity`가 그 프레임 0으로 떨어짐(Layer 1 `v_target=0`), 동시에 Layer 2 크로스페이드 가중치도 동일 `t_retarget=0`으로 시작(Baseline로 완전 전환, `v_target=1`) **WHEN** `t=0.2s` **THEN** Layer 1(`b_humidity`)과 Layer 2(포즈 가중치) 둘 다 정확히 같은 시각에 각자의 목표값(0과 1)에 동시 도달 — "실루엣은 다 펴졌는데 눈매만 처져 있다"는 식의 합성 불일치가 관찰되지 않는다.
- **AC-F-13 (`PoseBlend` 시 4채널 전부 적용, 예외 없음)** **GIVEN** `BlendMode=PoseBlend`가 결정된 프레임 **WHEN** `AppliesBlend(channel, PoseBlend)`를 4개 채널(`Layer2_pose`, `Layer1_temperature`, `Layer1_humidity`, `Layer1_moisture`) 각각에 대해 평가 **THEN** 4개 전부 `true` — 하나라도 `false`가 되어 원시값을 그대로 통과시키는 부분 적용은 발생하지 않는다.
- **AC-F-14 (터치가 실제로 건드린 채널만 전환, 무관 채널은 유지)** **GIVEN** 습도·온도가 동시에 안전 구간 밖(습도가 근소 우세해 Rain-huddled 표시 중, 둘 다 `AmbientDrift`로 서서히 드리프트 중) **WHEN** 플레이어가 물뿌리개(수분 관련, 무관한 스탯)를 완료 **THEN** `Layer1_moisture` 채널만 `PoseBlend`로 전환되고 습도·온도 채널은 그대로 `PassThrough`를 유지한다 — 트리거 조건은 "터치 발생"이 아니라 "그 채널의 목표값이 그 프레임에 원자적으로 바뀌었는가"임을 확인.
- **AC-F-15 (재트리거 — 방향 전환 시 점프 없이 재수렴, 핵심 예시)** **GIVEN** 습도 위기 채널이 `v_start=1.0`, `t_retarget=0`, `v_target=0`으로 블렌드 진행 중, `t=0.1s` 시점 `v_rendered=0.25` **WHEN** 같은 프레임(`t=0.1s`)에 다른 스탯이 동시에 위기로 전환돼 이 채널의 새 목표가 `v_target'=0.6`으로 바뀐다 **THEN** `v_start←0.25`(직전 렌더값 그대로 승계, 텔레포트 없음), `t_retarget←0.1s`로 갱신되고, `t=0.3s`(재트리거로부터 정확히 0.2초 후)에 `v_rendered=0.6` 도달 — 원래 목표(0)로 향하던 값이 순간 점프 없이 방향을 꺾어 새 목표로 매끄럽게 재수렴한다.
- **AC-F-16 (재트리거는 항상 전체 `POSE_BLEND_DURATION` 재사용, 누적 잔여시간 아님)** **GIVEN** 채널이 짧은 간격으로 3회 연속 재트리거된 상황(매 재트리거 시점마다 새 목표 도착) **WHEN** 각 재트리거 이후 블렌드 소요시간을 측정 **THEN** 매 재트리거마다 정확히 `POSE_BLEND_DURATION`(0.2s) 전체 창이 새로 시작한다(이전 남은 시간이 아님) — 연타해도 블렌드가 점점 빨라지거나 깜빡이지 않는다.
- **AC-F-17 (`BespokeEvent`는 재타겟팅이 아니라 즉시 취소)** **GIVEN** 어떤 채널이 `PoseBlend` 진행 중(`t < t_retarget+POSE_BLEND_DURATION`) **WHEN** 그 도중 `BespokeEvent`(예: `OnWitherEntered`)가 발생한다 **THEN** 그 채널의 진행 중이던 `PoseBlend`는 §4 재트리거 로직(`v_start` 승계)을 타지 않고 그 프레임에 즉시 폐기되며, 별도 페이드아웃 없이 `BespokeEvent` 고유의 전환 애니메이션이 화면을 완전히 인수한다(§1 `Owned`와 정합).

### 엣지 케이스

- **AC-EC-01 (신규 세이브 — 즉시 스냅)** **GIVEN** 최초 프레임, 이전 렌더값이 존재하지 않음 **WHEN** 초기화 **THEN** `v_start=v_target`, `t_retarget=0`으로 설정되어 그 프레임(`t=0`)에 이미 `v_rendered=v_target`(스냅) — 별도 블렌드 대기가 없다.
- **AC-EC-02 (백그라운드 복귀, 경과시간이 `POSE_BLEND_DURATION` 초과)** **GIVEN** 블렌드 진행 중(`t_retarget=T`) 앱이 백그라운드로 전환됨 **WHEN** 벽시계 기준 0.2s보다 긴 시간 후 포그라운드로 복귀 **THEN** 복귀 시 `t`에 실제 경과시간을 그대로 반영하고, §2의 클램프에 의해 이미 `v_target`에 도달한 것으로 자연히 처리된다 — `isLive` 판정과 별개로 별도 특별 처리 로직이 필요 없다.
- **AC-EC-03 (동일 프레임 두 채널 동시 `PoseBlend`, 상호 독립)** **GIVEN** 두 스탯이 동시에 안전 구간에 복귀해 같은 프레임에 각각 `PoseBlend`가 트리거됨 **WHEN** 두 채널의 블렌드 진행을 관찰 **THEN** 각 채널은 독립된 `(v_start, t_retarget)` 상태를 가지며 서로 간섭 없이 병렬로 블렌드된다 — 한 채널의 진행 상황이 다른 채널의 타이밍에 영향을 주지 않는다.
- **AC-EC-04 (`BespokeEvent`가 `PoseBlend` 도중 발생 — 즉시 취소, 페이드아웃 없음)** **GIVEN** `PoseBlend`가 진행 중 **WHEN** `BespokeEvent`(`OnWitherEntered` 등)가 발생 **THEN** 진행 중이던 블렌드는 즉시 취소되고 화면은 `BespokeEvent`의 전환 애니메이션이 인수하며, 별도 페이드아웃 없이 그 프레임에 폐기된다(Core Rule 3·6과 정합).
- **AC-EC-05 (`OnWitherEntered`와 `OnStageAdvanced` 동시 발생, Core Rule 6)** **GIVEN** `neglectMeter`가 정확히 그 프레임에 100에 도달하는 것과 `stageIndex(G)` 경계 통과(`OnStageAdvanced` 발생 조건)가 정확히 같은 틱에 겹침 **WHEN** 두 이벤트를 동시 처리 **THEN** `OnWitherEntered`가 승리한다 — 그 프레임의 성장전환 애니메이션(`GROWTH_BREATH_DURATION`)은 재생되지 않고 실루엣만 새 생장 단계로 즉시 스냅되며 Wither 진입 연출만 재생된다. lifecycle의 `growthHoursAccumulated`는 수치상 정상적으로 갱신되지만, 그 갱신을 알리는 시각 연출만 생략된다(수치 갱신 자체는 취소되지 않는다).
- **AC-EC-06 (다운스트림 구독자가 블렌드 도중 순간값을 읽어도 항상 유효 범위)** **GIVEN** 홈 화면 위젯 시스템(또는 임의 구독자)이 `PoseBlend` 진행 중 임의 시점에 `v_rendered(t)`를 읽음 **WHEN** `t_retarget ≤ t < t_retarget+POSE_BLEND_DURATION` 구간 내 어느 시점에 조회하더라도 **THEN** `v_rendered(t)`는 항상 정의된 유효 범위(Layer 1: [0,1], Layer 2: 유효한 포즈 가중치) 안이다 — 미정의/범위 밖 값이 노출되지 않으며 별도 "블렌드 중" 플래그 없이도 안전하게 소비 가능하다.
- **AC-EC-07 (`WeatherEnumStep`과 `TouchDelta`가 같은 채널·같은 프레임에 겹침)** **GIVEN** 어떤 채널에 대해 날씨가 Sunny로 막 전환(`WeatherEnumStep`, `PoseBlend` 트리거)된 것과 정확히 같은 프레임에 터치 완료(`TouchDelta`, `PoseBlend` 트리거)도 발생 **WHEN** 두 요청의 처리 순서를 확인 **THEN** 별도의 우선순위/중재 로직 없이 §4 재트리거 규칙이 그대로 적용된다 — 나중에 도착한 요청이 먼저 도착한 요청의 진행 중이던 렌더값(`v_rendered(t_now)`)을 승계해 재수렴할 뿐, 두 `PoseBlend` 요청 사이의 충돌 자체가 존재하지 않는다.

### 시각 계약

- **AC-VA-01 (블렌드는 순수 지오메트리 보간, 추가 워시 레이어 없음)** **GIVEN** `PoseBlend`가 진행 중 **WHEN** 렌더링되는 채널을 확인 **THEN** Layer 1/Layer 2 지오메트리(윤곽선·케이지) 채널만 보간되고 있으며, 그 위에 별도 번짐/워시/글로우 효과 레이어가 추가로 얹히지 않는다(Visual/Audio Requirements §2).
- **AC-VA-02 (Sunny-content 승격 시 표준 블렌드만, 캐릭터 레벨 플러리시 없음)** **GIVEN** `WeatherEnumStep`으로 트리거된 `PoseBlend`가 Sunny-content로 전환 중 **WHEN** 재생되는 효과를 확인 **THEN** §2의 표준 `v_rendered` 블렌드만 재생되고, 별도 캐릭터 레벨 파티클/글로우 플러리시는 추가되지 않는다(배경 레이어의 `CROSSFADE_DURATION=3.0s` 햇살 워시는 weather-system 소관의 별도 채널이며 이 시스템 범위 밖).

### 크로스시스템 통합

- **AC-INT-01 (세 신호만 구독, 신규 신호 미발명, Core Rule 1)** **GIVEN** 시스템이 정상 동작 중 **WHEN** 매 프레임 입력 신호를 감사 **THEN** 이 시스템이 읽는 업스트림 신호는 정확히 potato-stat의 `{포즈 1차 결과, b, Blush, neglectMeter, isShielded, OnWitherEntered/OnRecovered}`, weather-system의 `{GetCurrentWeather()}`, lifecycle의 `{stageIndex(G), OnStageAdvanced}` 뿐이며, 이 시스템이 자체적으로 저장·발행하는 신규 세이브 필드가 존재하지 않는다(Dependencies "세이브/영속성" 문단과 정합).
- **AC-INT-02 (Sunny-content 승격은 정확히 AND 조건에서만 — 개별 조건 실패 확인)** **GIVEN** [케이스 A] 1차 결과=Baseline, 날씨≠Sunny(예: Rainy)와 [케이스 B] 1차 결과≠Baseline(예: Rain-huddled), 날씨=Sunny **WHEN** 각각 최종 포즈를 평가 **THEN** 두 케이스 모두 Sunny-content로 승격되지 않는다(케이스 A→Baseline 유지, 케이스 B→Rain-huddled 유지) — 두 조건 중 하나만 충족되는 경우 승격이 발생하지 않음을 개별로 확인.
- **AC-INT-03 (포즈-실루엣 독립 축, Core Rule 5)** **GIVEN** 동일한 포즈 파라미터(예: Rain-huddled, 습도 `b=0.8`)가 `stageIndex=0`(Sprout)와 `stageIndex=2`(Plump) 각각에 적용 **WHEN** 두 생장 단계에서 동일 포즈를 렌더링 **THEN** 두 경우 모두 같은 압축 축 규칙을 따르되 각 단계 고유의 실루엣/케이지 위에서 해석된다 — 생장 단계 값이 포즈 카테고리·블렌드값 계산 자체에 어떤 영향도 주지 않는다(포즈 계산과 실루엣 선택은 완전히 독립된 별도 조회).
- **AC-INT-04 (터치 접촉 번짐 애니메이션 경계, Core Rule 7)** **GIVEN** 플레이어가 터치 케어 제스처(창문/난로/물뿌리개)를 진행 중이라 접촉점 번짐 애니메이션이 재생 중 **WHEN** 이 시스템의 포즈/블렌드 계산을 감사 **THEN** 이 시스템은 접촉점 번짐 애니메이션을 트리거하지도, 그 상태를 조회하지도 않는다 — 두 시스템 사이에 데이터 교환이 전혀 없다(레이어 분리 확인).
- **AC-INT-05 (홈 화면 위젯 미러링, Hard dependency)** **GIVEN** 이 시스템이 최종 포즈+실루엣을 계산 완료한 프레임 **WHEN** 홈 화면 위젯 시스템이 값을 조회 **THEN** 위젯은 이 시스템이 계산한 최종 포즈+실루엣의 축소 LOD를 그대로 미러링하며 원시 potato-stat 스탯값을 직접 읽지 않는다(potato-stat-system.md UI Requirements 확정 사항과 정합) — 이 시스템이 값을 계산하지 못하면 위젯도 미러링할 대상이 없다(Hard dependency 판정 기준 충족 확인).
- **AC-INT-06 (Harvest-proud 훅, Soft dependency, 정확한 호출 시점은 범위 밖)** **GIVEN** 이름짓기&수확카드 시스템(미설계)이 Harvest-proud 퇴장 연출을 요청하는 훅을 호출 **WHEN** 이 시스템이 요청을 수신 **THEN** 요청받은 프레임에 Harvest-proud 연출을 재생할 수 있는 훅이 노출되어 있음을 확인한다 — 정확한 호출 시점·파라미터 시그니처는 이 GDD 범위 밖(Soft dependency: 응답 없어도 수확 액션 자체는 정상 진행, 퇴장 연출만 생략).
- **AC-INT-07 (`BespokeEvent` 4종 전부 `Owned`로 일관 취급)** **GIVEN** `OnWitherEntered`/`OnRecovered`/`OnStageAdvanced`/Harvest-proud 각각이 진행 중인 `PoseBlend` 도중 발생하는 4가지 케이스를 개별 확인 **WHEN** 각 이벤트 발생 시점을 확인 **THEN** 4종 이벤트 전부 동일하게 §1 `Owned` 분류를 받아 진행 중이던 `PoseBlend`를 즉시 취소시키며, `POSE_BLEND_DURATION` 크로스페이드가 중복으로 얹히지 않는다 — Wither 하나만의 특수 규칙이 아니라 4종 전체에 공통 적용됨을 개별로 확인.
- **AC-INT-08 (감자 마을 선반은 이 시스템에 의존하지 않음)** **GIVEN** 감자 마을 선반 컨텍스트(고정 Harvest-proud 전시 포즈, 코스튬만 다름) **WHEN** 그 화면의 렌더링 경로를 감사 **THEN** 이 시스템의 매 프레임 포즈/블렌드 계산 함수가 호출되지 않는다 — 포즈 *전환*이라는 사건 자체가 마을 화면에는 존재하지 않으므로(Visual/Audio Requirements §4(a)), Interactions/Downstream 표가 감자 마을을 명시하지 않은 설계와 정합.

### 이 GDD 범위 밖으로 명시적으로 남긴 것 (테스트 불가 항목)

- **BlendShape/케이지 리그의 실제 구현 타당성**: Unity 6.5 Sprite BlendShape API(케이지 기반
  FFD) 채택 여부는 `/create-architecture` 단계의 ADR 소관 — 이 GDD는 "포즈 파라미터는
  생장 단계에 무관하다"는 설계 계약까지만 잠근다(Core Rule 5). 실제 리그가 이 계약을
  기술적으로 구현 가능한지는 엔지니어링 관심사이지 이 GDD 기준 테스트 대상이 아니다.
- **Harvest-proud 트리거의 정확한 호출 시그니처**: 이름짓기 & 수확 카드 시스템이 아직
  미설계이므로, AC-INT-06은 "훅이 노출되어 있다"까지만 검증하며 정확한 파라미터·호출
  시점은 그 시스템 GDD가 확정할 사안이다.
- **홈 화면 위젯의 실제 OS 프레임워크 애니메이션 지원 여부**: Visual/Audio Requirements
  §4(b)가 "즉시 스냅(settled end-state만 미러링)"을 아트디렉션 관점에서 권고하지만, iOS/
  Android 네이티브 위젯 프레임워크가 실제로 몇 ms 단위 갱신·애니메이션을 허용하는지는
  이 문서가 결정할 사안이 아니다(technical-artist·ui-programmer 별도 확인 필요, Open
  Questions로 넘김).
- **접촉점 번짐 애니메이션 자체의 정확성**: `touch-care-interaction.md`가 소유한 별도
  시스템이며, 이 GDD는 그 애니메이션과 데이터 교환이 없다는 경계(AC-INT-04)만 검증한다 —
  번짐 애니메이션 자체의 품질·타이밍은 그 시스템의 AC 소관이다.
- **"즉각 판독성"이라는 주관적 성공 기준**(Player Fantasy): "감자의 현재 상태를 한눈에
  못 읽어서" 실패한다는 서술은 벤치마크 없는 정성적 기준이라 자동화 불가 — lean 모드로
  `creative-director` 미상담 상태이며, 프로덕션 전 수동 검토·플레이테스트로만 확인
  가능하다.
- **`ease(x)` 함수 형태 변경 가능성**: Tuning Knobs에서 의도적으로 노브화하지 않은
  고정값(2차 ease-out)이므로, 변형 커브에 대한 회귀 테스트는 대상이 없다 — 이 문서가
  잠그는 것은 현재 형태(§2)의 정확성뿐이다.

## Open Questions

| 질문 | 배경 | 담당 | 해소 시점 |
|---|---|---|---|
| 홈 화면 위젯이 실제로 iOS/Android 네이티브 위젯 프레임워크에서 몇 ms 단위 갱신·애니메이션을 지원하는지 | Visual/Audio Requirements §4(b)가 "즉시 스냅(settled end-state만 미러링)"을 아트디렉션 관점으로만 권고 — 실제 플랫폼 제약 미확인 | technical-artist/ui-programmer | 홈 화면 위젯 시스템 `/design-system` 진행 시 또는 실제 위젯 제작 전 |
| Harvest-proud 퇴장 연출의 정확한 호출 시점·파라미터 시그니처(이름짓기 완료 후인가, 수확 버튼 탭 즉시인가) | 이 GDD는 "요청을 받아 재생하는 훅"만 노출(Core Rule 4) — 정확한 호출 계약은 호출자 쪽 GDD가 미작성 | game-designer | 이름짓기 & 수확 카드 시스템 `/design-system` 진행 시 |
| Unity 6.5 Sprite BlendShape API(케이지 기반 FFD)가 이 GDD의 "포즈 파라미터는 생장 단계에 무관" 계약(Core Rule 5)을 실제로 구현 가능한지 | 학습 범위 밖 신규 기능(HIGH risk, `docs/engine-reference/unity/VERSION.md`) — 계약 자체만 잠그고 구현 메커니즘은 위임 | lead-programmer/technical-director | `/create-architecture` 단계 ADR |
| "즉각 판독성"(Player Fantasy 성공 기준)이 실제 플레이어에게 성립하는지 | 벤치마크 없는 정성적 기준, lean 모드라 creative-director 미상담 | creative-director / 플레이테스트 | 프로덕션 전 수동 검토·플레이테스트 |
