# 생애주기 진행 (Lifecycle Progression)

> **Status**: Designed (pending review)
> **Author**: user + systems-designer + qa-lead + art-director
> **Last Updated**: 2026-07-16
> **Implements Pillar**: Pillar 2 (지켜주고 싶은 마음)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode

## Overview

생애주기 진행은 감자가 씨앗에서 수확까지 거치는 4일짜리 자동 성장 타이머이자 상태
머신이다. 날씨 시스템이 벽시계 기반으로 "지금 무슨 날씨인가"를 결정론적으로
재구성하듯, 이 시스템은 "지금 어느 생장 단계(발아→성장→비대→수확)인가"를 심은
시각(`plantedUtc`)으로부터 항상 재구성 가능해야 한다 — 매 프레임 시뮬레이션하는
카운트다운이 아니라, 임의 시각을 넣으면 그 시각의 단계를 즉시 돌려주는 순수 함수다.

플레이어는 이 시스템을 직접 조작하지 않는다 — 터치 케어(창문/난로/물뿌리개)가
감자의 *상태*(수분/온도/습도)를 조정하는 것과 달리, 생애주기는 전적으로 시간이
진행시킨다. 플레이어의 역할은 관찰뿐이다: 앱을 열 때마다 "오늘은 얼마나 자랐지?"를
확인하는 것 자체가 Pillar 2(지켜주고 싶은 마음)가 매일 반복되는 습관으로 굳어지는
지점이며, `game-concept.md`가 MVP 핵심 가설로 못박은 "4일 루프가 다시 들어오고
싶게 만드는가"의 답이 실질적으로 이 시스템에 달려 있다.

이 시스템이 없다면 감자는 정적인 스탯 덩어리로 영원히 정지해 있을 뿐, 자라남도
수확이라는 정서적 정점(art-bible Section 2 §5, "졸업식의 톤")도 존재하지 않는다 —
코스튬·이름짓기&수확카드·감자 마을 등 모든 다운스트림 진행/수집 시스템이 이 시스템의
"수확 완료" 신호 하나에 매달려 있다.

## Player Fantasy

> *(`creative-director` 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

플레이어가 이 시스템에서 느껴야 할 감정은 **"내가 통제하지 않아도 자라고 있다"는
조용한 기대감**이다 — 게임 콘셉트의 Retention Hooks가 이미 "Investment: 정서가
누적된 내 감자"로 명명한 지점을, 4일이라는 물리적 시간의 흐름이 뒷받침한다. 이건
육성 시뮬레이션의 "내가 최적으로 키워낸다"는 숙련 판타지가 아니라 — 터치 케어가
이미 그 역할을 맡고 있다 — **여행을 떠난 반려의 편지를 기다리는 마음**에 가깝다
(art-bible Reference Direction #4, 여행 개구리와 동일한 정서 축). 오늘 열었을 때
어제와 조금 달라진 실루엣을 발견하는 것 자체가 보상이며, 그 발견에는 어떤 조작도
개입하지 않는다 — 순수하게 "시간이 지나갔고, 내가 없는 동안에도 이 아이는 자라고
있었다"는 확인이다.

수확이 다가올수록(비대 단계) 이 기대감은 art-bible 콘셉트가 명명한 "졸업식의
톤"(대견함+아쉬움)으로 서서히 기울어야 한다 — 마지막 날은 "얼마나 컸는지 확인"에서
"곧 떠나보낼 준비"로 감정의 무게중심이 옮겨간다. Pillar 2(지켜주고 싶은 마음)가 이
시스템에서 취하는 구체적 형태는 "매일 들여다보는 습관"이며, Pillar 4(짧게 자주
부담없이)와 충돌하지 않도록 이 확인 자체에는 어떤 압박도 없다 — 오늘 안 열어봐도
성장은 멈추지 않고 기다려준다(이 보장은 Formulas/Edge Cases에서 구체화).

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 lean에서도 상담 필수.)*

### Core Rules

1. **4단계, 24시간씩**: 발아(Sprout) → 성장(Growing) → 비대(Plump) → 수확(Harvest-ready),
   각 단계는 정확히 24시간 분량의 누적 성장 시간을 요구한다(art-bible이 이미 4단계
   실루엣 차등을 잠갔으므로 이 GDD는 지속시간만 확정).
2. **누적 카운터 기반, 순수 벽시계 재구성 아님**: 날씨 시스템의 `GetWeatherAt`과 달리,
   이 시스템은 `now - plantedUtc`만으로 단계를 재구성할 수 없다 — 아래 규칙 3(시듦 중
   일시정지) 때문에 실제 흐른 시간과 누적 성장 시간이 어긋날 수 있다. 대신
   `growthHoursAccumulated`(누적 성장시간)를 직접 영속 상태로 관리하고, 매 틱마다
   증가시킨다.
3. **시듦(Wither) 중 일시정지**: `감자 상태 관리`의 `neglectMeter ≥ 100`(Wither 상태)인
   동안은 `growthHoursAccumulated`가 증가하지 않는다 — 위기에 빠진 감자가 몰래 자라고
   있다는 인과적 모순을 피한다(사용자 확정). 되돌림(Recovery) 이후에는 멈췄던 지점부터
   그대로 재개된다(뒤로 밀리지 않음, 단순 일시정지).
4. **수확 단계는 종착점(hold)**: `growthHoursAccumulated ≥ 72h`에 도달하면 단계=
   Harvest-ready로 고정되고, 그 이상 누적해도 더 이상 아무 효과가 없다(4번째 단계를
   벗어나는 5번째 단계는 없음). 이 상태는 플레이어가 실제로 수확을 완료(`이름짓기 &
   수확 카드` 시스템의 몫)할 때까지 무기한 유지된다 — 방치해도 벌점 없음(Pillar 3·4).
5. **새 감자는 자동 시작 안 함**: 수확 완료 이벤트(다운스트림 시스템이 발행)를 받아야만
   이 시스템이 새 생애주기를 시작한다(`growthHoursAccumulated=0`, 새 `plantedUtc`
   기록). 수확 단계에 도달한 것만으로는 새 감자가 시작되지 않는다(사용자 확정).

### States and Transitions

| 상태 | 진입 조건 | 이탈 조건 |
|---|---|---|
| **Sprout(발아)** | 새 생애주기 시작(`growthHoursAccumulated=0`) | `growthHoursAccumulated ≥ 24h` → Growing |
| **Growing(성장)** | `growthHoursAccumulated ≥ 24h` | `growthHoursAccumulated ≥ 48h` → Plump |
| **Plump(비대)** | `growthHoursAccumulated ≥ 48h` | `growthHoursAccumulated ≥ 72h` → Harvest-ready |
| **Harvest-ready(수확)** | `growthHoursAccumulated ≥ 72h` | (자연 이탈 없음) 외부 이벤트 `OnHarvestCompleted` → 새 Sprout로 리셋 |

이 표에 "일시정지" 상태는 없다 — Wither은 `growthHoursAccumulated`의 증가만 멈출 뿐,
위 4단계 상태 머신 자체의 별도 상태가 아니다(어느 단계에 있든 Wither은 그 단계 안에서
그냥 시간이 멈춘 것처럼 작동).

### Interactions with Other Systems

| 시스템 | 관계 | 인터페이스(잠정) |
|---|---|---|
| 감자 상태 관리 | 조회(호출자) | `neglectMeter` 읽기 → `isWithered = (neglectMeter ≥ 100)` 로컬 파생(신규 필드 요청 없음, 기존 값 재사용) |
| 세이브/영속성 시스템 | 등록(호출자) | `Register("lifecycle", {plantedUtc, growthHoursAccumulated, lastTickUtc})` — `save-persistence-system.md` L67이 이미 이 키를 Hard 의존성 placeholder로 잡아둠, 이 GDD가 정확한 스키마 확정 |
| 오프라인 진행 계산 | 배치 갱신 요청(피호출자) | 오프라인 갭을 potato-stat의 Wither 구간 경계로 분할해 각 구간마다 `Tick(elapsedHours, isWithered)`를 순서대로 호출해야 함(Formulas §1) — 이 시스템 단독으로는 offline 배치를 수행하지 않는다. 세그먼트 분할 알고리즘은 `offline-progress-calculation.md` Formulas(`SegmentGap`)에서 확정됨(2026-07-18) |
| 표정/애니메이션 시스템 | 조회 | 현재 단계(enum)로 실루엣/포즈 선택 |
| 코스튬 시스템 | 조회 | `stage == HarvestReady`가 코스튬 착장 가능 여부의 게이트(art-bible 확정: 코스튬은 수확 이후만) |
| 이름짓기 & 수확 카드 | 피호출자 | `OnHarvestCompleted()` — 이 시스템은 이 호출을 받아야만 새 생애주기를 시작 |

## Formulas

> *(`systems-designer` 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션. 핵심 발견:
> potato-stat의 되돌림(Recovery)은 광고 시청이라는 라이브 행동으로만 트리거되고 오프라인
> 자동 복구 경로가 없다 — 따라서 한 오프라인 갭 안에서 시듦은 최대 1회만 진입 가능하고,
> 일단 진입하면 그 갭이 끝날 때까지 유지된다. 이 사실이 §2의 배치 재구성을 단순한
> 합산으로 만든다.)*

### 1. 실시간(포그라운드) 틱 갱신

앱이 열려 있는 동안 매 틱(프레임 또는 수 초 폴링)마다 실제 경과시간을 그대로 더한다 —
시듦 중에는 더하지 않는다. Harvest-ready 이후에도 이 공식은 특별 취급 없이 계속 돌아간다
("계속 누적해도 효과 없음"은 §3의 클램프가 보장하므로 이 공식이 알 필요 없음).

`G' = G + (1 - W) · Δt`

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 누적 성장시간 | `G` | float (hours) | 0–∞ | `growthHoursAccumulated` — 이 시스템의 성장 계산을 좌우하는 핵심 영속 상태값(`plantedUtc`·`lastTickUtc`와 함께 저장됨, Dependencies 참고) |
| 시듦 여부 | `W` | bool {0,1} | {0,1} | `neglectMeter ≥ 100`에서 로컬 파생(Detailed Design 표에 이미 확정, 신규 필드 아님) |
| 경과 실시간 | `Δt` | float (hours) | ≥0 | 이번 틱과 직전 틱 사이 실제 경과 시간 |
| 결과 | `G'` | float (hours) | `G` 이상 | 갱신된 누적 성장시간 |

**`lastTickUtc` 갱신 시점**: `G`와 정확히 같은 주기로 갱신된다 — 매 라이브 틱마다
`G'`를 반영한 직후 `lastTickUtc = 이번 틱 시각`으로 즉시 갱신한다. 별도의 갱신
주기(예: 백그라운드 전환 시점, 세이브 체크포인트 시점)를 두지 않는다 — `Δt_gap`(§2)이
"마지막으로 성장을 계산한 시각"과 정확히 일치해야 하므로, `G`의 갱신과 `lastTickUtc`의
갱신은 항상 원자적으로 같이 일어난다.

**Output Range:** `G`는 단조 비감소, 상한 없음(§3에서 단계 조회 시에만 실질적으로
클램프됨) — 이 공식 자체는 아무것도 클램프하지 않는다.

**Example:** `G=40h`, 30초 폴링(`Δt=30/3600≈0.00833h`). `W=0`(정상):
`G'=40+0.00833=40.00833h`. 같은 틱에서 `W=1`(시듦 중)이면: `G'=40+0=40h` — 완전히 정지.

---

### 2. 오프라인/배치 재구성

이 시스템 단독으로는 오프라인 배치를 수행하지 않는다(Detailed Design Interactions 표
확정) — 오프라인 진행 계산기가 갭을 시간순 세그먼트 리스트로 분할해 공급하면, 이
시스템은 각 세그먼트에 §1을 그대로 반복 적용한다. 즉 오프라인 배치는 별도의 닫힌 형식이
아니라 §1의 합이다.

`G' = G + Σᵢ₌₁ⁿ (1 - Wᵢ) · Δtᵢ`, 세그먼트 `(Δtᵢ, Wᵢ)`는 시간순으로 오프라인 계산기가 공급

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 세그먼트 수 | `n` | int | ≥1 | 오프라인 계산기가 갭을 분할한 세그먼트 개수 |
| 세그먼트 경과시간 | `Δtᵢ` | float (hours) | ≥0, `Σ Δtᵢ = Δt_gap` | i번째 세그먼트의 실시간 길이 |
| 세그먼트 시듦 여부 | `Wᵢ` | bool {0,1} | {0,1} | i번째 세그먼트 동안 `isWithered`가 참이었는가(세그먼트 경계 자체가 Wither 전이 시각으로 정의되므로 세그먼트 내부는 상수) |
| 갭 길이 | `Δt_gap` | float (hours) | ≥0 | `max(0, now - lastTickUtc)` — potato-stat AC-EC-04와 동일한 시계 조작 방지 클램프 재사용 |
| 갱신 전/후 누적 성장시간 | `G`, `G'` | float (hours) | 0–∞ | §1과 동일 필드 |

**Output Range:** `G'-G ∈ [0, Δt_gap]`, 전 구간이 `Wᵢ=0`일 때만 상한(`Δt_gap`)에 도달,
갭 시작 시점부터 이미 시듦이었으면 하한(0).

**설계 노트(엔지니어링 판단)**: potato-stat의 되돌림(Recovery)은 오직 광고 시청(라이브
행동)으로만 트리거되고 오프라인 자동 경로가 없으므로, 한 오프라인 갭 안에서 Wither는
**최대 1회만 진입**할 수 있고 일단 진입하면 그 갭이 끝날 때까지(앱 재개 시점까지)
이탈할 수 없다 — "여러 번 진입·이탈"은 현재 잠긴 규칙상 구조적으로 불가능하다. 따라서
`n`은 실무상 항상 1(시듦 없음) 또는 2(비-시듦 세그먼트 + 갭 끝까지 이어지는 시듦
세그먼트 하나)로 수렴한다. 위 일반식(Σ)은 안전하게 그대로 두되, 오프라인 계산기
구현체는 "neglectMeter가 100에 도달하는 단 하나의 시각(있다면)"만 찾으면 충분하며,
그 이후는 자동으로 시듦 세그먼트로 간주해도 된다 — 별도 이탈 시각 탐색이 필요 없다.
이는 잠긴 Interactions 표의 "구간 경계로 분할" 문구를 바꾸지 않으며, 구현 난이도만
크게 낮춰준다.

**Example (§4 종합 예시로도 사용):** 앱 종료 시점 `G=40h`(성장/Growing 중반), 갭
`Δt_gap=30h`. 오프라인 계산기가 2세그먼트로 분할해 공급: 세그먼트1 `(Δt₁=27h, W₁=0)`,
세그먼트2 `(Δt₂=3h, W₂=1)` — neglectMeter가 갭 시작 27시간 후 100에 도달해 남은
3시간(갭이 끝날 때까지) 시듦 유지. 계산: `G' = 40 + 1×27 + 0×3 = 67h`. 앱 재개 순간
감자는 여전히 시듦 상태(`W=1`)로 표시된다 — 부활(광고 시청)은 재개 이후 포그라운드에서
§1로 별도 처리되는 그다음 사건이지, 이 오프라인 갭의 일부가 아니다.

---

### 3. 단계 조회

`stageIndex(G) = clamp(⌊G / 24⌋, 0, 3)`, `stage(G) = STAGES[stageIndex(G)]`

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 누적 성장시간 | `G` | float (hours) | 0–∞ | §1·§2가 갱신하는 영속 상태값 |
| 단계 인덱스 | `stageIndex` | int | {0,1,2,3} | 0=Sprout, 1=Growing, 2=Plump, 3=HarvestReady |
| 결과 | `stage(G)` | enum | {Sprout, Growing, Plump, HarvestReady} | 조회 시점의 생장 단계 |

**Output Range:** 항상 4개 enum 값 중 정확히 하나 — floor+clamp가 전역에서 총함수(total
function)임을 보장, "5번째 단계"나 "단계 사이" 값은 존재하지 않는다.

**Example (§2 예시 이어서):** `G=67h` → `⌊67/24⌋=2` → `clamp(2,0,3)=2` → **Plump**
(48≤67<72). 경계 확인: `G=72.0h` → `⌊72/24⌋=3` → **HarvestReady**. 장기 방치 확인:
`G=500h` → `⌊500/24⌋=20` → `clamp(20,0,3)=3` → 여전히 **HarvestReady**(Core Rule 4와
정합, 무기한 유지).

---

**상수 요약(레지스트리 등록 후보)**: `STAGE_DURATION_HOURS=24h`(4단계 공유, 신규 상수);
`growthHoursAccumulated`(필드, 세이브 등록 대상); Wither은 최대 1회/갭 진입이라는
구조적 보장(potato-stat Recovery=라이브 광고 시청 전제, 신규 상수 아님).

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드.)*

- **If 신규 세이브(최초 실행, 심어진 감자 없음)**: potato-stat의 "최초 실행=전 스탯
  IDEAL" 관대한 시작 철학과 동일선상으로, 자동으로 Sprout 단계의 새 생애주기를
  시작한다(`growthHoursAccumulated=0`, `plantedUtc=lastTickUtc=지금`) — 플레이어가
  "씨앗 심기" 같은 별도 액션을 할 필요 없음(MVP 스코프: 감자 1마리 상시 존재).
- **If `now < lastTickUtc`(기기 시계가 과거로 조작되거나 되돌아감)**: `Δt_gap =
  max(0, now - lastTickUtc)`로 클램프해 0으로 처리한다(§2 공식에 이미 명시) — 시계를
  되돌려도 성장이 감소하지 않고, 그냥 이번 틱에 아무 성장도 더해지지 않을 뿐이다.
- **If `OnHarvestCompleted()`가 `stage ≠ HarvestReady`인 상태에서 호출됨(다운스트림
  버그·잘못된 순서)**: 이 시스템은 호출을 무시한다(no-op) — `growthHoursAccumulated`를
  리셋하지 않는다. 이 시스템은 자신의 불변조건(수확은 72h 이후에만 유효)을 스스로
  지키며, 호출자를 신뢰하지 않는다.
- **If Harvest-ready 상태에서 감자가 시듦(Wither)에 빠짐(스탯 방치로 인한 별도 위기 —
  생장 단계와 무관하게 발생 가능)**: `growthHoursAccumulated` 증가가 멈추지만 이미
  `stage=HarvestReady`(§3 클램프)라 관찰 가능한 효과가 전혀 없다 — 별도 처리 불필요,
  공식이 자연히 흡수한다.
- **If 오프라인 갭 동안 `neglectMeter`가 100에 전혀 도달하지 않음(정상적인 대다수
  케이스)**: 오프라인 계산기는 세그먼트를 1개(`W=0` 전체)로 공급하면 충분하다 —
  §2의 일반식이 `n=1`로 자연히 축소되는 경우.
- **If 오프라인 갭이 시작되기 *이전*부터 이미 시듦 상태였고, 갭 내내 복귀하지 않음**:
  세그먼트가 `(Δt_gap, W=1)` 하나로 공급되어 `G'=G`(전혀 증가 없음) — §2 Output Range의
  하한 케이스.

## Dependencies

### Upstream (이 시스템이 의존)

| 시스템 | 방향 | 강도 | 인터페이스 |
|---|---|---|---|
| 감자 상태 관리 | 조회(이 시스템 → potato-stat) | Hard | `neglectMeter` 읽기 → `isWithered` 로컬 파생(Formulas §1) |
| 세이브/영속성 시스템 | 등록(이 시스템 → save-persistence) | Hard | `Register("lifecycle", {plantedUtc, growthHoursAccumulated, lastTickUtc})` |

### Downstream (이 시스템에 의존)

| 시스템 | 방향 | 강도 | 인터페이스 |
|---|---|---|---|
| 오프라인 진행 계산 | 호출(오프라인 계산기 → 이 시스템) | Hard | 갭을 세그먼트로 분할해 `Tick(Δtᵢ, Wᵢ)` 순서대로 호출(Formulas §2) — 이 방향이 맞다: 날씨 시스템의 `GetWeatherAt` 선례와 동일하게, 호출자(오프라인 계산기)가 피호출자(이 시스템)에 의존한다. 이 시스템 자체는 오프라인 계산기 없이도(온라인 틱만으로) 독립적으로 작동한다. `offline-progress-calculation.md`(2026-07-18) 확정 |
| 표정/애니메이션 시스템 | 조회 | Soft | 현재 단계(enum)로 실루엣 선택 — 응답 없어도 핵심 케어 루프는 유지, 연출만 밋밋해짐 |
| ASMR 적응형 오디오 | 조회 | Soft | `OnStageAdvanced` 이벤트(Visual/Audio §3이 이미 노출한 훅) 구독 — 응답 없어도 생장 전이 자체는 정상 작동, 생장 전환 SFX만 무음(`asmr-adaptive-audio.md` 확정, 2026-07-21 — 이전까지 이 표에서 누락돼 있던 실제 갭) |
| 코스튬 시스템 | 조회 | Hard | `stage == HarvestReady` 게이팅 — 이 신호 없이는 코스튬 시스템 자체가 작동 불가 |
| 이름짓기 & 수확 카드 | 조회 + 호출 | Hard | `stage == HarvestReady` 확인 후 수확 액션 허용, 완료 시 `OnHarvestCompleted()`로 콜백(Detailed Design 확정) |
| 성장 부스터(Rain) 시스템(Alpha) | 호출 | Hard | 부스터 발동 시 `growthHoursAccumulated`에 보너스 시간 직접 가산(정확한 가산량은 이 시스템 범위 밖 — 그 GDD가 확정) |

**Hard vs Soft 판정 기준**: weather-system.md와 동일 — 이 시스템이 응답하지 않을 때 대상
시스템이 아예 작동을 멈추면 Hard, 연출만 밋밋해지고 핵심 루프는 유지되면 Soft.

## Tuning Knobs

| 노브 | 기본값 | 안전 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `STAGE_DURATION_HOURS` | 24h(4단계 공유) | 12~48h | 하루에 여러 단계를 건너뛰어 "매일 조금씩 자란다"는 관찰 리듬이 깨짐, 총 주기가 짧아져 `game-concept.md`의 "4일 생애주기" 핵심 가설(마케팅 피치·MVP 가설)과 어긋남 | 하루를 열어도 눈에 띄는 변화가 없어 Pillar 2("얼마나 자랐는지 확인하는 습관")가 무의미해짐, 총 주기가 늘어나 코스튬/마을 컬렉션 진행 속도가 지나치게 느려짐 |

**노브 간 상호작용**: `STAGE_DURATION_HOURS`를 바꾸면 총 생애주기 길이(`4 ×
STAGE_DURATION_HOURS`)도 함께 바뀐다 — 이 값은 `game-concept.md` Elevator
Pitch·MVP Core Hypothesis가 "4일"로 문서 전역에 못박아둔 값이라, 변경 시 그 문서
자체의 정정이 필요할 수 있다(이 GDD 단독 튜닝이 아님, 콘셉트 변경 사안). 단일
노브만 존재하는 이유: 4단계가 동일 길이를 공유하도록 Core Rules에서 이미
확정했으므로(art-bible 실루엣 차등은 지속시간이 아니라 형태로만 구현), 단계별
개별 지속시간 노브는 불필요한 자유도다.

## Visual/Audio Requirements

> 이 섹션은 새로운 실루엣 방향을 정의하지 않는다 — `art-bible.md` Section 3(a)(생장
> 4단계)가 이미 잠근 종횡비·크라운 비중 차등을 그대로 인용한다. 이 섹션이 신규로
> 확정하는 것은 "단계가 바뀌는 순간"의 연출 계약(`OnStageAdvanced`)뿐이다(art-director
> 상담, 2026-07-15 — qa-lead가 발견한 갭에 대한 답).

### 1. 단계별 실루엣 (art-bible Section 3(a) 인용, 신규 아님)

| 단계 | 종횡비 성격 | 크라운 비중 |
|---|---|---|
| Sprout(발아) | 가장 둥글고 작음(원에 가까움) | 아주 작은 싹 하나 |
| Growing(성장) | 세로로 길쭉해짐 | 눈에 띄게 커짐, 옆으로 살짝 벌어짐 |
| Plump(비대) | 가장 둥글고 통통함(가로 폭 최대) | 몸통 대비 상대적으로 작아 보임 |
| HarvestReady(수확) | 둥글되 아랫배가 살짝 평평(유일하게 바닥 평평) | 가장 풍성함 |

### 2. `OnStageAdvanced` 연출 계약 (신규)

Pillar 2(지켜주고 싶은 마음)에는 성장이 "목격된 사건"이어야 하는 정서적 요구가
있지만, 4일에 3번 발생하는 이 이벤트를 Wither/Recovery급 정서적 정점으로 다루면
"정적인 포근함" 원칙과 충돌한다 — 이 게임에서 **가장 조용한 이벤트**로 설계한다.

| 상수 | 값 | 설명 |
|---|---|---|
| `GROWTH_BREATH_DURATION` | 0.4–0.6s | Sprout→Growing, Growing→Plump 전환 시 감자 자신의 윤곽선이 기존 비율에서 새 비율로 눌렸다 펴지는 단일 모프. 색 변화·전체화면 효과 없음, 오버슛 없는 ease-out만(오버슛=레벨업 징글처럼 읽혀 Pillar 3 위반) |
| `HARVEST_ARRIVAL_DURATION` | 0.8–1.0s | Plump→HarvestReady 전환 전용. 동일한 모프 언어를 쓰되 살짝 늘리고 끝에 짧은 정지(hold)를 둬 "바뀜"이 아니라 "도착함"으로 읽히게 함(Wither=스냅/Recovery=느긋함과 같은 비대칭 페이싱 원칙 재사용) |

**핵심 규칙**:
1. **라이브 전환에만 재생**: 날씨 시스템과 동일한 규칙 — 경계를 실시간으로 통과하는
   순간 앱이 열려 있을 때만 재생한다. 앱이 닫혀 있던 동안 경계를 지났다면 다음 재개
   시 새 실루엣으로 **즉시 스냅**한다(재생 없음). 성장은 순수하게 경과 실시간에 의해
   결정되므로(날씨와 동일 성질, Wither/Recovery처럼 플레이어 행동으로 트리거되지
   않음), 대부분의 전환은 애초에 목격되지 않는다 — 배터리 낭비 없음.
2. **수확 단계에 신규 시각 마커 추가 금지**: art-bible Section 3(a)가 4단계를
   실루엣만으로 차등화하도록 이미 잠갔다 — 여기에 색/글로우/배지를 얹으면 문서에
   없는 5번째 차등 축이 생긴다. 대부분의 플레이어가 라이브 도착 순간을 놓치는 만큼,
   수확 가능함을 부각하려면 캐릭터 애셋이 아니라 **UI 레이어**(CTA·배지 상태)가
   담당해야 한다 — UI Requirements 참고.

### 3. 오디오 (범위 메모)

이 시스템은 사운드를 직접 재생하지 않는다. `OnStageAdvanced` 이벤트가 오디오가 구독
가능한 유일한 훅이다 — 실제 SFX 설계는 별도 오디오 GDD(ASMR 적응형 오디오 시스템,
아직 미설계)의 몫이다.

## UI Requirements

이 시스템은 두 개의 UI 표면과 맞닿는다 — 둘 다 art-bible이 이미 잠근 어휘를 그대로
재사용하며, 신규 UI 패턴을 만들지 않는다.

1. **일상 진행 표시(passive)**: 별도의 숫자 카운트다운("D-2" 등)이나 진행바를 두지
   않는다 — Pillar 4(부담없이)와 충돌하기 때문이다. 현재 실루엣 자체(Visual/Audio
   Requirements §1)가 곧 진행 상황 표시다. 홈 화면 위젯에 노출되는 축소 LOD
   실루엣(art-bible UI/HUD §3, 날씨 시스템과 동일 재사용 원칙)도 자동으로 현재 생장
   단계를 반영한다 — 이 시스템이 신규로 할 일은 없다.
2. **수확 CTA(active, `stage==HarvestReady`에서만 등장)**: art-bible UI/HUD §4가
   "화면당 히어로 액션은 정확히 하나, Amber Ochre 전용"이라고 이미 잠가뒀다 — 수확
   준비가 된 순간 뜨는 "수확하기" 버튼이 이 화면의 유일한 히어로 액션이 된다(이전
   3단계 동안은 이 버튼 자체가 존재하지 않음, 터치 케어 제스처만 있음). 위기 알림
   금지 원칙(붉은 배지·점멸·카운트다운)은 이 CTA에는 적용되지 않는다 — 이건 위기가
   아니라 좋은 소식이므로, 다만 "지금 안 하면 손해"류의 압박 문구는 Pillar 4에 따라
   여전히 금지한다(예: "언제든 편하게 수확하세요" 톤 권장, 정확한 카피는
   `writer`/`ux-designer` 후속 과제).

Downstream `이름짓기 & 수확 카드` 시스템이 실제 수확 화면/플로우를 소유하며, 이
시스템은 오직 `stage==HarvestReady` 게이트 신호(AC-INT-04~06)만 제공한다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — 2026-07-15. lean 모드에서도 Acceptance Criteria는 상담 필수
> 섹션. 상태 전이 기준은 `potato-stat-system.md`와 동일하게 `AC-ST-##` 접두사를 쓴다.)*

아래 기준은 Detailed Design·Formulas·Edge Cases에서 정의된 필드·상수명을 그대로
사용한다. 각 항목은 GDD 전체를 읽지 않은 QA 테스터도 독립적으로 재현·판정할 수
있도록 정확한 시작 조건과 기대값을 명시한다. "단계(stage)"는 이 시스템이 별도로
저장하는 필드가 아니라 `growthHoursAccumulated`(G)로부터 `stageIndex(G)`가 파생하는
값이다 — 아래 기준은 그 파생 조건을 기준으로 검증한다.

### 단계 전이 (Core Rules)

- **AC-ST-01 (4단계 경계 — 상태 전이 표 검증)**
  **GIVEN** `growthHoursAccumulated`(G)가 각각 `{0h, 23.99h, 24.0h, 47.99h, 48.0h, 71.99h, 72.0h}`
  **WHEN** 각 값에서 `stage(G)`를 조회한다
  **THEN** 순서대로 `{Sprout, Sprout, Growing, Growing, Plump, Plump, HarvestReady}` —
  States and Transitions 표의 진입 조건과 일치한다.

- **AC-ST-02 (누적 카운터는 벽시계 경과시간과 다름 — Core Rule 2)**
  **GIVEN** 감자 A는 `plantedUtc`로부터 실제 30시간이 경과했고 그동안 한 번도 Wither가
  없었다(`G_A=30h`). 감자 B도 마찬가지로 실제 30시간이 경과했으나 그중 10시간 동안
  Wither 상태였다(`G_B=30-10=20h`)
  **WHEN** 두 감자의 `stage`를 동시에 조회한다
  **THEN** `stage(A)=Growing`(G≥24h), `stage(B)=Sprout`(G<24h) — `now - plantedUtc`가
  동일(30h)해도 단계가 다르다. `growthHoursAccumulated`가 유일한 권위이지 벽시계
  경과시간이 아님을 확인한다.

- **AC-ST-03 (Wither 중 성장 정지 — Core Rule 3 전반부)**
  **GIVEN** `G=30h`(Growing), `isWithered=true`(`neglectMeter≥100`)
  **WHEN** 5시간 경과(`Δt=5h`)
  **THEN** `G`는 여전히 `30h`(변화 없음), `stage`도 여전히 Growing.

- **AC-ST-04 (Recovery 후 멈춘 지점에서 재개, 되감기 없음 — Core Rule 3 후반부)**
  **GIVEN** `G=30h`에서 Wither 진입, 10시간 동안 Wither 유지(`G`는 30h로 고정) 후
  Recovery(`isWithered`가 다시 `false`로 전환)
  **WHEN** Recovery 이후 추가로 2시간 경과(`Δt=2h`, `W=0`)
  **THEN** `G=32h`(30+2) — Wither 진입 시점(30h)이나 그 이전으로 되돌아가지 않고,
  정지됐던 지점에서 그대로 이어서 증가한다.

- **AC-ST-05 (Harvest-ready는 종착점 — 5번째 단계 없음, Core Rule 4)**
  **GIVEN** `G=72h`(HarvestReady 최초 도달)
  **WHEN** 이후 428시간 추가 경과(`W=0` 가정, `G=500h`)
  **THEN** `stage`는 여전히 HarvestReady — 5번째 단계나 별도 "초과 성숙" 상태로
  전이되지 않는다.

- **AC-ST-06 (새 생애주기는 자동 시작되지 않음 — Core Rule 5 전반부)**
  **GIVEN** `stage=HarvestReady`(G≥72h), `OnHarvestCompleted()` 호출 없음
  **WHEN** `G`가 계속 증가한다(예: `G=72h→G=150h`)
  **THEN** `plantedUtc`는 최초 심은 시각으로 불변, `growthHoursAccumulated`도 리셋되지
  않고 계속 누적되며, 새 Sprout 단계로 전환되지 않는다.

- **AC-ST-07 (OnHarvestCompleted 호출 시 새 생애주기 시작 — Core Rule 5 후반부)**
  **GIVEN** `stage=HarvestReady`(예: `G=90h`)
  **WHEN** 다운스트림 시스템이 시각 `T`에 `OnHarvestCompleted()`를 호출한다
  **THEN** 호출 즉시 `growthHoursAccumulated=0`, `plantedUtc=T`(새 값), `stage=Sprout`로
  전이한다.

### 실시간 틱 갱신 (Formulas §1)

- **AC-F-01 (정상 틱 — W=0)** **GIVEN** `G=40h`, 폴링 간격 30초(`Δt=30/3600≈0.00833h`),
  `isWithered=false`(`W=0`) **WHEN** 1틱 적용 **THEN** `G'=40+0.00833=40.00833h`.
- **AC-F-02 (시듦 중 틱 — W=1, 완전 정지)** **GIVEN** 동일 조건(`G=40h`, `Δt=0.00833h`),
  `isWithered=true`(`W=1`) **WHEN** 1틱 적용 **THEN** `G'=40h`(변화 없음).
- **AC-F-03 (Harvest-ready 이후에도 특별취급 없이 계속 누적)** **GIVEN** `G=72h`(이미
  HarvestReady), `W=0` **WHEN** `Δt=5h` 경과 **THEN** `G'=77h` — 공식 자체는 클램프하지
  않으며, 표시상 HarvestReady 고정은 §3의 `stageIndex` 클램프가 담당한다.

### 오프라인/배치 재구성 (Formulas §2)

- **AC-F-04 (2세그먼트 순차 적용 — 문서 종합 예시)** **GIVEN** `G=40h`, 오프라인 갭
  `Δt_gap=30h`, 계산기가 세그먼트 `[(Δt1=27h, W1=0), (Δt2=3h, W2=1)]`로 분할 공급
  **WHEN** `Tick(27h, W=0)` 다음 `Tick(3h, W=1)`을 시간순으로 적용 **THEN**
  `G'=40+27+0=67h`, 앱 재개 시점 `isWithered=true`로 표시된다.

### 단계 조회 (Formulas §3)

- **AC-F-05 (중간값 조회)** **GIVEN** `G=67h` **WHEN** `stageIndex(G)` 계산 **THEN**
  `⌊67/24⌋=2`, `clamp(2,0,3)=2` → Plump.
- **AC-F-06 (경계값 포함 — 하한 inclusive)** **GIVEN** `G=72.0h`(정확히 경계) **WHEN**
  `stageIndex(G)` 계산 **THEN** `⌊72/24⌋=3` → HarvestReady. 대조: `G=71.99h` **WHEN**
  같은 계산 **THEN** `⌊71.99/24⌋=2` → Plump(아직 미도달) — 각 단계 하한이 floor
  컨벤션상 포함(inclusive)됨을 확인한다.
- **AC-F-07 (상한 클램프 — 장기 방치)** **GIVEN** `G=500h` **WHEN** `stageIndex(G)` 계산
  **THEN** `⌊500/24⌋=20`, `clamp(20,0,3)=3` → HarvestReady(20이 아닌 3으로 클램프,
  무기한 유지).
- **AC-F-08 (총함수 보장 — 5번째 단계·미정의 값 없음)** **GIVEN** 임의의 `G≥0`(예: `0,
  12, 24, 36.5, 72, 9999`) **WHEN** `stageIndex(G)`를 반복 계산 **THEN** 매번 정확히
  `{0,1,2,3}` 중 하나만 반환 — 예외·null·"단계 사이" 값이 발생하지 않는다.

### 엣지 케이스

- **AC-EC-01 (신규 세이브 — 자동 Sprout 시작)** **GIVEN** 세이브 데이터 없음(최초 실행,
  심어진 감자 없음) **WHEN** 시스템 초기화 **THEN** `growthHoursAccumulated=0`,
  `plantedUtc=lastTickUtc=현재 시각`, `stage=Sprout` — 플레이어의 별도 "씨앗 심기"
  액션 없이 자동 시작된다.
- **AC-EC-02 (기기 시계 되돌림 — Δt_gap 0으로 클램프)** **GIVEN** `lastTickUtc=T`
  **WHEN** 기기 시계가 `now=T-5h`로 조작/되돌아감(`now < lastTickUtc`) **THEN**
  `Δt_gap=max(0, now-T)=0`으로 클램프되어, `growthHoursAccumulated`는 감소하지도
  이번 틱에 증가하지도 않는다(불변).
- **AC-EC-03 (조기 OnHarvestCompleted 호출 — no-op)** **GIVEN** `stage=Growing`
  (`G=30h`, 72h 미도달) **WHEN** 다운스트림 버그로 `OnHarvestCompleted()`가 호출됨
  **THEN** 호출은 무시(no-op)된다 — `growthHoursAccumulated=30h` 그대로, `plantedUtc`도
  갱신되지 않고, `stage`도 Growing 그대로 유지된다.
- **AC-EC-04 (Harvest-ready 중 Wither — 관찰 가능한 효과 없음)** **GIVEN**
  `stage=HarvestReady`(`G=72h`), 이후 별도 위기로 `isWithered=true`로 전환 **WHEN**
  20시간 경과(`W=1` 유지) **THEN** `growthHoursAccumulated` 증가는 멈추지만(§1 W=1
  규칙), 이미 `stageIndex` 클램프(§3)로 인해 `stage`는 여전히 HarvestReady — Wither
  진입 전후로 관찰 가능한 단계 표시 차이가 전혀 없다.
- **AC-EC-05 (정상적인 대다수 케이스 — 갭 내내 시듦 없음, 1세그먼트)** **GIVEN**
  오프라인 갭 `Δt_gap=15h` 동안 `neglectMeter`가 한 번도 100에 도달하지 않음 **WHEN**
  오프라인 계산기가 세그먼트를 `[(15h, W=0)]` 1개로 공급 **THEN** `G'=G+15h` — Σ
  일반식이 `n=1`로 자연히 축소되는 경우.
- **AC-EC-06 (갭 시작 전부터 시듦, 갭 내내 미복귀 — 성장 0)** **GIVEN** 오프라인 갭이
  시작되기 이전부터 이미 `isWithered=true`였고, 갭 내내(`Δt_gap=8h`) 복귀하지 않음
  **WHEN** 오프라인 계산기가 세그먼트를 `[(8h, W=1)]` 1개로 공급 **THEN** `G'=G`(전혀
  증가 없음) — §2 Output Range의 하한(0) 케이스.

### 연동 계약 (Cross-System Interactions)

- **AC-INT-01 (isWithered 파생 — neglectMeter 임계값)** **GIVEN** potato-stat의
  `neglectMeter=99` **WHEN** `isWithered` 로컬 파생 평가 **THEN** `isWithered=false`
  (`W=0`). **GIVEN** `neglectMeter=100` **WHEN** 같은 평가 **THEN** `isWithered=true`
  (`W=1`) — 신규 필드 추가 없이 potato-stat의 기존 값을 그대로 재사용한다.
- **AC-INT-02 (세이브 등록 스키마)** **GIVEN** 세이브 트리거 **WHEN** 영속성 시스템이
  이 시스템의 상태를 직렬화 **THEN** 키 `"lifecycle"` 아래 정확히
  `{plantedUtc, growthHoursAccumulated, lastTickUtc}` 3개 필드만 등록된다 — `stage`는
  별도 필드로 저장하지 않는다(항상 `stageIndex(G)`로 재계산, Overview의 "순수 함수"
  원칙과 일치).
- **AC-INT-03 (로드 후 재구성 일관성)** **GIVEN** 세이브된
  `{plantedUtc, growthHoursAccumulated=G, lastTickUtc}` **WHEN** 로드 직후(추가 틱
  적용 전) `stage` 조회 **THEN** `stage=stageIndex(G)` — 세이브 직전과 동일한 값,
  저장/로드를 거쳐도 재계산 결과가 달라지지 않는다.
- **AC-INT-04 (코스튬 게이팅 — 차단)** **GIVEN** `stage≠HarvestReady`(예: Growing)
  **WHEN** 코스튬 시스템이 착장 가능 여부를 조회 **THEN** `gate=false`(착장 불가) —
  `isShielded`나 `neglectMeter` 등 다른 상태와 무관하게 오직 `stage`만으로 판정된다.
- **AC-INT-05 (코스튬 게이팅 — 개방 및 유지)** **GIVEN** `G`가 72h에 최초 도달
  (`stage=HarvestReady` 진입) **WHEN** 코스튬 시스템이 조회 **THEN** `gate=true`.
  이후 `G=500h`까지 계속 증가해도 `gate`는 계속 `true`로 유지되다가,
  `OnHarvestCompleted()` 호출로 `stage=Sprout`로 리셋되는 즉시 `gate=false`로 다시
  닫힌다.
- **AC-INT-06 (수확 카드 시스템 — 코스튬과 동일 게이트 공유)** **GIVEN**
  `stage=HarvestReady` **WHEN** 이름짓기&수확카드 시스템이 수확 액션 허용 여부를 조회
  **THEN** `allowed=true`. **GIVEN** `stage=Plump`(예: `G=60h`) **WHEN** 동일 조회
  **THEN** `allowed=false` — 코스튬 게이트(AC-INT-04/05)와 동일한
  `stage==HarvestReady` 조건을 공유한다.
- **AC-INT-07 (오프라인 계산기 호출 계약 — 세그먼트 순차 호출)** **GIVEN** 오프라인
  진행 계산기가 갭을 시간순 세그먼트 리스트 `[(Δt1,W1), (Δt2,W2), ...]`로 분할 완료
  **WHEN** 이 시스템에 갱신을 요청 **THEN** 각 세그먼트에 대해 `Tick(Δtᵢ, Wᵢ)`을
  시간순으로 순차 호출해야 한다 — 이 시스템은 배치 갭 전체를 자체적으로 세그먼트
  분할하지 않는다(Formulas §2, Interactions 표 확정). 계산기가 세그먼트 분할 없이
  `(Δt_gap, 단일 W)` 하나만 통짜로 넘기면, 그 갭 내부에 있었을 수 있는 Wither 구간이
  무시되어 부정확한 결과가 나온다.

### 이 GDD 범위 밖으로 명시적으로 남긴 것 (테스트 불가 항목)

- **성장 부스터(Rain) 보너스의 정확한 가산량·트리거 조건**은 그 GDD가 아직 작성되지
  않았으므로(Alpha 스코프) 테스트 대상에서 제외한다 — Dependencies Downstream 표가
  이미 이 경계를 명시.
- **오프라인 진행 계산기 자체의 세그먼트 분할 로직**(예: `neglectMeter`가 정확히 100에
  도달하는 시각을 찾는 알고리즘)은 그 계산기 GDD 소관 — 이 GDD는 "세그먼트가 올바르게
  주어졌을 때" 이 시스템이 올바르게 적용하는지만 검증한다(AC-F-04, AC-INT-07).
- **`OnStageAdvanced` 연출·수확 CTA의 정확한 시각 판정 기준**은 Visual/Audio·UI
  Requirements가 이 AC 작성 이후에 추가되었으므로 이번 패스의 기준에는 포함되지
  않았다 — potato-stat의 AC-VA 패턴을 따라 후속 기준(예: 라이브 vs 스냅 판정, 앰버
  히어로 버튼 등장 조건)을 추가하는 것을 권장한다.
- **실시간 틱의 정확한 폴링 주기**(프레임 단위 vs 수 초)는 엔지니어링 결정으로 이 GDD가
  잠그지 않는다(weather-system.md와 동일 컨벤션) — AC-F-01/02는 예시값(30초)을 쓸 뿐
  그 값 자체를 요구사항으로 고정하지 않는다.

## Open Questions

| 질문 | 배경 | 담당 | 해소 시점 |
|---|---|---|---|
| 수확 시 감자 상태 관리(수분/습도/온도/neglectMeter) 리셋 주체·시점 | `OnHarvestCompleted()`는 이 시스템의 필드만 리셋한다고 확정됨 — potato-stat 쪽 리셋이 원자적으로 함께 일어나는지 미정의 | game-designer/systems-designer | `이름짓기 & 수확 카드` 시스템 `/design-system` 진행 시 반드시 해결(**BLOCKING**) |
| `OnHarvestCompleted()` 호출 시점 — 이름짓기 완료 후인가, 수확 버튼 탭 즉시인가 | 정확한 시퀀싱이 두 시스템(이 GDD·이름짓기&수확카드) 경계에 걸쳐 있어 어느 쪽도 아직 확정하지 않음 | game-designer | `이름짓기 & 수확 카드` 시스템 설계 시 |
| 성장 부스터(Rain)의 정확한 가산량·트리거 조건 | 이 GDD는 "보너스 시간을 직접 가산" 인터페이스만 열어두고 값은 위임(Alpha 스코프) | economy-designer/systems-designer | 성장 부스터 시스템 `/design-system` 진행 시 |
| ~~오프라인 진행 계산기의 세그먼트 분할 알고리즘(neglectMeter=100 도달 시각 탐색)~~ | **해결됨(2026-07-18)** — `offline-progress-calculation.md` Formulas가 `t_cross`/`ΔN_piece`/`τ_wither`/`SegmentGap` 4개 공식으로 확정. 이 GDD는 계약(세그먼트를 순서대로 받아 적용)만 계속 소비 | — | 해소됨 |
| ~~`OnStageAdvanced` 이벤트의 실제 SFX~~ | **설계 단계 해결됨(2026-07-21)** — `asmr-adaptive-audio.md`가 이벤트 티어 중 가장 얕은 덕킹("게임 전체에서 가장 조용한 사운드")으로 확정. 실제 오디오 에셋 제작은 별도 프로덕션 과제로 남음 | — | 설계 해소됨(에셋 제작 대기) |
| 수확 CTA 정확한 카피 문구 | UI Requirements에서 톤만 권장, 확정 카피 없음 | writer/ux-designer | UX 스펙 작성 시 |
