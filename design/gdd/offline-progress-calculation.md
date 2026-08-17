# 오프라인 진행 계산 (Offline Progress Calculation)

> **Status**: Designed (pending review)
> **Author**: user + systems-designer + qa-lead
> **Last Updated**: 2026-07-18
> **Implements Pillar**: Pillar 3 (벌주지 않는 힐링)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode

## Overview

오프라인 진행 계산은 앱이 백그라운드/종료 상태였던 동안 감자에게 일어난 일을 앱 재개
시점에 단 한 번의 결정론적 배치 연산으로 재구성하는 순수 계산 계층이다. 날씨 시스템이
"임의 시각의 날씨"를, 생애주기 진행이 "임의 시각의 생장 단계"를 각각 프레임 누적이
아니라 벽시계 기반 폐형식(closed-form) 함수로 재구성하듯, 이 시스템은 그 두 함수
(`GetWeatherAt`/`GetWeatherSegments`, `Tick`)와 감자 상태 관리의 사다리꼴 배치 공식
(§4)을 시간순으로 엮어 호출하는 **단일 진입점**이다 — 4개 Foundation/Core 시스템이
각자 노출해둔 "오프라인 안전" 폐형식 계산을 실제로 한자리에서 조립하는 곳이 이
시스템 하나뿐이다.

플레이어는 이 시스템을 직접 조작하지 않으며 존재조차 인지하지 못한다 — 앱을 열면
이미 계산이 끝나 있고, 감자는 마치 그 시간 내내 실시간으로 지켜본 것과 동일한 최종
상태로 나타난다. 트리거는 전적으로 자동(앱 재개/로드 시점)이다.

이 시스템이 없다면 "방치형(Idle)"이라는 장르 정체성 자체가 성립하지 않는다 —
날씨·수분/온습도·성장이 앱이 닫힌 동안 멈춰버리거나(방치형 실패), 혹은 4개 시스템이
각자 임시방편적인 캐치업 로직을 중복 구현해야 한다(정합성 위험). 또한 이 재구성이
정확하고 공정해야만 Pillar 3(벌주지 않는 힐링)의 "돌아왔을 때 부당하게 시들어 있지
않다"는 약속과, game-concept.md가 명시한 리텐션 훅("8시간 온실막을 미리 설치하고
안심하고 나가기")이 실제로 지켜진다.

## Player Fantasy

> *(`creative-director` 미상담 — lean 모드, Section D/H 아님. 프로덕션 전 수동 검토 권장.)*

플레이어가 이 시스템에서 직접 느끼는 감정은 없다 — 대신 이 시스템의 정확성이 두 가지
다운스트림 감정의 **전제조건**이 된다.

첫째, **"내가 없어도 부당하게 당하지 않는다"는 조용한 신뢰**. game-concept.md의 Key
Dynamics가 이미 "오프라인 안전 최적화 행동"(취침·업무 전 온실막 설치)을 핵심 플레이어
행동으로 명명했는데, 이 행동 자체가 성립하려면 온실막을 설치하고 나간 뒤 실제로 그
보호가 정확히 적용됐다는 확신이 있어야 한다. 계산이 틀리면(과하게 가혹하거나, 반대로
아무 결과도 없다면) 이 신뢰가 곧바로 깨진다.

둘째, **앱을 다시 열 때의 안도감**(lifecycle-progression의 "여행을 떠난 반려의 편지를
기다리는 마음"과 같은 축) — "어제와 조금 달라진 모습을 발견"하는 그 순간이 감정적으로
성립하려면, 그 발견의 *내용*이 이 시스템의 계산 결과다. 이 시스템은 판타지를 만들지
않지만, 다른 시스템이 만드는 판타지가 거짓이 되지 않도록 지킨다.

*Design test*: 계산 결과가 플레이어의 사전 기대(온실막을 켜고 잤으니 안전할 것)와
어긋나면 Pillar 3 위반 — 항상 보수적으로(플레이어에게 유리한 쪽으로) 실패해야 한다는
원칙이 이후 Edge Cases 설계를 관통해야 한다.

## Detailed Design

> *(Specialist agents 미상담 — lean 모드, Section D/H 아님. 프로덕션 전 수동 검토 권장.)*

### Core Rules

1. **트리거**: save-persistence가 Ready(전 등록자 로드 완료)를 보고한 직후(콜드 스타트)
   또는 앱이 포그라운드로 복귀할 때(`OnApplicationFocus(true)`) 정확히 1회 실행된다.
   실행 중 재진입 요청은 무시한다(단일 실행 보장 — 동시에 두 배치가 같은 상태를 이중
   적용하는 것을 방지).

2. **갭 산정**: `gapStart = lastUpdatedUtc`(potato-stat 소유, save-persistence §5의
   `IsTimestampValid` 게이트 통과 후 값), `gapEnd = now = DateTime.UtcNow`.
   `Δt_total = max(0, gapEnd - gapStart)`. potato-stat과 lifecycle의 타임스탬프는 이
   시스템과 라이브 틱 드라이버 양쪽에서 항상 같은 세그먼트에 함께 진행되므로(Core Rule
   4), 정상 동작 시 두 값은 구조적으로 항상 동일하다 — 어긋나는 경우의 방어 규칙은
   Edge Cases로 이관.

3. **세그먼트 경계 수집**: `(gapStart, gapEnd]` 구간을 다음 세 종류의 분할점으로 잘라
   시간순 정렬한다 — (a) 날씨 전환 시각(`GetWeatherSegments(gapStart, gapEnd)`),
   (b) 온실막 만료 시각(`shieldActiveUntil`, 구간 내에 있을 때만), (c) 방치 지표가
   100에 도달하는 시각(있다면 단 1개, 산출식은 Formulas 참고). 결과로 각 부분구간은
   날씨·온실막 상태·시듦 여부가 전부 상수인 "단조 구간"이 된다.

4. **세그먼트별 순차 적용(락스텝)**: 각 단조 구간에 대해, potato-stat의 배치 공식(§4)
   으로 수분/온도/습도/방치 지표를 갱신하고, **같은 구간에 대해 곧바로** lifecycle의
   `Tick(Δtᵢ, Wᵢ)`를 호출한다 — 두 시스템이 항상 같은 세그먼트 경계로, 같은 순서로
   함께 전진하며 이 사이에는 어떤 저장도 발생하지 않는다.

5. **커밋**: 모든 세그먼트 적용이 끝난 뒤(즉 두 시스템 모두 `now`에 도달한 뒤) 단 한 번
   `RequestSave()`를 호출한다. 배치 도중에는 주기 자동저장(30초)이 끼어들지 않도록
   계산 전체가 동기적으로 완료된다(날씨 시스템 §3이 이미 증명한 최악 100년 갭
   ≈146,000회 반복 = 수 밀리초 상한 근거).

6. **완료 신호**: 커밋 후 `OnOfflineProgressApplied` 이벤트(또는 동등한 콜백)를
   발행한다 — 시듦/위기&회복, 온실막 보호, 알림 시스템 등 다운스트림이 "이제 상태가
   확정됐다"를 알 수 있는 유일한 시점.

### States and Transitions

이 시스템은 지속 상태를 소유하지 않는다(순수 함수형 배치 연산 — 세션 사이에 남기는
자체 데이터 없음). 단일 실행 내부의 전이만 존재한다:

| State | 설명 | 다음 State |
|---|---|---|
| `Idle` | 트리거 대기 | `Segmenting` (트리거 발생 시) |
| `Segmenting` | Core Rule 3 — 경계 수집·정렬 | `Applying` |
| `Applying` | Core Rule 4 — 세그먼트 순차 락스텝 적용 | `Committing` (전 세그먼트 완료 시) |
| `Committing` | Core Rule 5~6 — 저장 + 이벤트 발행 | `Idle` |

`Applying` 도중에는 재진입 트리거를 무시(Core Rule 1)하고, 라이브 틱 드라이버도 이
시스템이 `Idle`로 돌아올 때까지 대기한다(둘이 같은 필드에 동시에 쓰지 않도록).

**조회 가능한 상태 플래그(`IsApplying`)**: 위 표의 State는 내부 구현 상세가 아니라
`IsApplying`(bool, `Segmenting`/`Applying`/`Committing` 동안 true) 하나로 외부에
노출된다 — 주기 자동저장(30초 타이머)과 라이브 틱 드라이버는 매 실행 전에 이 플래그를
확인해 true면 대기(또는 스킵 후 다음 주기 재시도)한다. 이 플래그가 곧 "배치 도중
저장/라이브 틱이 끼어들지 않는다"는 Core Rule 5의 주장을 테스트 가능하게 만드는
유일한 접점이다.

### Interactions with Other Systems

| System | 방향 | 데이터 |
|---|---|---|
| 감자 상태 관리 | 읽기(`lastUpdatedUtc`, `shieldActiveUntil`) + 쓰기(배치 조정 적용) | §4 배치 공식 소비 |
| 날씨 시스템 | 조회(호출자) | `GetWeatherSegments(gapStart, gapEnd)` |
| 생애주기 진행 | 호출(세그먼트별 `Tick(Δtᵢ, Wᵢ)`) | AC-INT-07 계약 이행 |
| 세이브/영속성 시스템 | 읽기(로드된 타임스탬프, `IsTimestampValid` 게이트 통과분) +
  쓰기(`RequestSave()` 단일 호출) | Ready 신호 이후에만 트리거 |
| 시듦/위기 & 회복 | 신호 발행(`OnOfflineProgressApplied`) | 다운스트림 소비 |
| 온실막 보호 | 신호 발행 | 다운스트림 소비 |
| 알림 시스템 (미설계) | 신호 발행 | 다운스트림 소비 |

## Formulas

> *(`systems-designer` 상담 완료 — 이 GDD의 핵심 미해결 항목이었던 세그먼트 분할
> 알고리즘을 여기서 해소함. lifecycle-progression-system.md Open Questions 항목
> "오프라인 진행 계산기의 세그먼트 분할 알고리즘" 해소.)*

**핵심 구조적 사실**: `BASELINE_RATEᵢ`와 `ACTIVE_RATEᵢ`는 항상 같은 방향(potato-stat
§2 "기본 방향 통일 규칙")이고 오프라인 갭에는 터치 이벤트가 없으므로, 각 스탯의
`Vᵢ`는 갭 전체에서 단조롭게 한 방향으로만 움직인다 — 날씨 경계에서 바뀌는 건 속도의
"크기"뿐, 방향은 절대 바뀌지 않는다. 따라서 스탯당 갭 전체에서 최대 2개의 분기점
(안전 구간 이탈 1개 + 클램프 도달 1개)만 존재하며, 이것이 아래 알고리즘의 저비용을
보장한다.

### Formula 1 — 스탯 경계 도달 시각 (`t_cross`)

The `t_cross` formula is defined as:

`t_cross(X, V₀, rate, t₀) = t₀ + (X - V₀) / rate`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 목표값 | X | float | {SAFE_MINᵢ, SAFE_MAXᵢ, 0, 100} | 안전 구간 경계 또는 클램프 경계 |
| 구간 시작값 | V₀ | float | 0–100 | 해당 상수-rate 구간 시작 시점의 Vᵢ |
| 상수 감쇠율 | rate | float | {±0.5, ±2.5, ±4.0} | 이 구간의 ACTIVE_RATEᵢ 또는 BASELINE_RATEᵢ (0 아님, 나눗셈 항상 안전) |
| 구간 시작 절대시각 | t₀ | timestamp | — | 이 상수-rate 구간의 시작 |

**유효성 필터**: `t₀ < t_cross ≤ t_window_end` **이고** `sign(X-V₀) == sign(rate)`일
때만 유효(진행 방향에 실제로 놓인 경계만 채택). 스탯이 이미 구간 밖이면 안전 경계
대신 클램프(0/100) 경계만 검사한다 — 스탯당 항상 최대 1개의 대기 중인 목표만 존재.

**Output Range:** `(t₀, t_window_end]` 내 시각 1개, 또는 "이 창에는 없음"(다음 창으로
목표 이월).
**Example:** 한파 구간 `t₀=0h`, 온도 `V₀=32`, `rate=-2.5/hr`, 목표 `SAFE_MIN=30` →
`t_cross = 0 + (30-32)/(-2.5) = 0.8h`.

### Formula 2 — 조각별 폐형식 ΔN (다중 스탯)

The `ΔN_piece` formula is defined as:

`ΔN_piece = NEGLECT_K · (1 - isShielded) · [O_start·Δt + (m/2)·Δt²]`

potato-stat §4 사다리꼴 공식(`ΔN = k·[(o_start+o_end)/2]·Δt`)과 대수적으로 동일 —
`o_end = o_start + m·Δt` 대입 전개형. 새 공식이 아니라 τ 풀이(Formula 3)에 필요한
형태로 재정리한 것.

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 구간 시작 합산 초과량 | O_start | float | 0–150 | Σᵢ overshootᵢ(t_piece_start) |
| 합산 초과량 기울기 | m | float | 0–9.0 | Σᵢ \|rateᵢ\|(이 구간에서 밖에 있는 스탯만 합산 — 무터치이므로 항상 증가만) |
| 구간 길이 | Δt | float | ≥0 (hr) | 이 완전 선형 하위 조각의 길이 |
| 차폐 여부 | isShielded | bool | {0,1} | potato-stat §4와 동일 |

**Output Range:** `ΔN_piece ≥ 0` 항상(무터치 오프라인 불변식 — 갭 중엔 초과량이 줄어들
방법이 없음).
**Example:** `Δt=2h`, 온도만 밖(`O_start=5.5, m=2.5`) → `ΔN = 0.5·(5.5·2+1.25·4) = 8`.

### Formula 3 — 시듦 진입 시각 (`τ_wither`)

The `τ_wither` formula is defined as:

`τ_local = (-b + √(b²-4ac)) / (2a)`, where `a=NEGLECT_K·m/2`, `b=NEGLECT_K·O_start`,
`c=N_start-100` (m=0이면 선형 퇴화: `τ_local = (100-N_start)/(NEGLECT_K·O_start)`)

`N_start<100`이고 `N_start+ΔN_piece≥100`인 **단 하나의** 조각에서만 풀이한다(비차폐
구간에서 N은 단조 비감소이므로 그런 조각은 유일하게 존재).

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 조각 시작 방치 지표 | N_start | float | 0 ≤ N_start < 100 | 크로싱 조각 진입 시점의 N |
| O_start, m | — | float | Formula 2와 동일 | 이 조각의 값 |
| 결과(상대) | τ_local | float | [0, Δt_piece) | 조각 시작 후 N=100 도달까지 걸린 시간 |
| 결과(절대) | τ_absolute | timestamp | (t_piece_start, t_piece_end) | `t_piece_start + τ_local` — 세그먼트 분할점 (c) |

**Root 선택**: 항상 `+√`를 취한다 — `N_start<100`(c<0)이고 `a,b≥0`이므로 판별식이
`b²`보다 항상 크며, `+`근이 유일한 비음수 해다(`-`근은 항상 음수라 별도 검사 불필요).

**Output Range:** `τ_local ∈ [0, Δt_piece)`, 조각 선택 조건 자체가 보장하므로 항상
실수·범위 내.
**Example:** `N_start=71.625, O_start=22.0, m=1.0` → `a=0.25, b=11, c=-28.375` →
`τ_local = (-11+√(121+28.375))/0.5 = 2.444h`.

### Formula 4 — 세그먼트 분할 절차 (`SegmentGap`)

The `SegmentGap` procedure is defined as:

`SegmentGap(gapStart, gapEnd) → [(Δtⱼ, weatherⱼ, isShieldedⱼ, Wⱼ)]` — Core Rules §3이
요구하는 정렬된 단조 세그먼트 리스트를 만드는 절차.

**절차(우선순위 순):**
1. `GetWeatherSegments(gapStart, gapEnd)`(weather-system §3) → 날씨 전환 경계.
2. `shieldActiveUntil`이 갭 내부에 있으면 분할점으로 삽입.
3. (1)+(2)를 정렬해 만든 각 "외곽 조각"을 시간순으로 처리:
   - **차폐 중(`isShielded=1`)**: 세분 없이 `V(t)=clamp(V₀+rate·Δt,0,100)`만 적용,
     `Tick(Δt, W=0)` 호출 후 다음 조각으로.
   - **비차폐(`isShielded=0`)**: 각 스탯의 대기 중인 목표에 Formula 1을 적용해 유효한
     경계를 찾아 조각을 추가로 쪼갠다. 결과 하위 조각마다 Formula 2로 `ΔN_piece`를
     순차 적용하며 누적 `N`을 갱신한다. `N`이 100을 넘는 순간 Formula 3으로 τ를 풀어
     그 조각을 τ에서 자르고 `Tick(τ_local, W=0)`을 호출한 뒤 **나머지 갭 전체를
     `W=1`인 세그먼트 하나로 종료**한다(τ 이후엔 날씨·스탯 재계산을 완전히 생략 —
     시듦 중엔 성장 정지·방치 지표 100 고정·복귀는 라이브 전용이라 추적할 것이 없음).
4. 어떤 조각에서도 100을 넘지 않으면 τ 없이 정상 종료(감자는 갭 내내 시들지 않음).

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 갭 경계 | gapStart, gapEnd | timestamp | — | Core Rule 2 정의 |
| 날씨 구간 수 | K | int | 1 ~ ⌈Δt_total/DURATION_MIN⌉ | weather-system §3의 기존 상한 |

**Output Range(최악 상한):** 세그먼트 수 ≤ `K + 9`(날씨 K + 온실막 1 + 스탯 경계
최대 6 + τ 1) — weather-system §3과 같은 오더(`O(Δt_total/DURATION_MIN)`). 100년
갭도 ~146,000 + 상수, 현실적 갭(수 시간~2주)은 수~수십 개 세그먼트. 실제로 시드는
갭은 τ 조기 종료 덕에 `Δt_total`이 아니라 시듦까지 걸리는 시간(potato-stat/weather
§6 최악치 기준 약 15~27h)에 의해 더 일찍 끝난다.

**Worked Example(20시간 갭)**: `T0` 수분=40/습도=50/온도=32, `N₀=8`,
`shieldActiveUntil=T0+3h`. 날씨: 한파`[0,5h)`→폭우/안개`[5,13h)`→맑음`[13,20h]`.

| # | 구간 | Δt | 상태 | N 종료 | 비고 |
|---|------|----|------|--------|------|
| 1 | [0,3h) | 3h | 한파, 차폐 | 8(고정) | 세분 없음 |
| 2 | [3,5h) | 2h | 한파, 비차폐 | 16 | 온도만 밖 |
| 3 | [5,10h) | 5h | 폭우/안개, 비차폐 | 45.375 | 습도가 10h째 SAFE_MAX 도달 |
| 4 | [10,13h) | 3h | 폭우/안개, 비차폐 | 71.625 | 온도+습도 둘 다 밖 |
| 5 | [13,15.444h) | 2.444h | 맑음, 비차폐 | **100(τ)** | 수분의 자체 경계(16.4h)는 τ가 먼저 와서 계산 안 함 |
| 6 | [15.444h,20h] | 4.556h | 맑음, **시듦** | 100(고정) | `Tick(4.556h,1)` 하나로 종료 |

τ≈15.44h(15h27m), 총 6세그먼트.

**시듦 이후 확인**: `neglectMeter`는 100에서 고정(potato-stat 확정), Recovery는
`ResetToIdeal()`로 이전 값을 무조건 덮어쓰므로 τ 이후 구간의 정확한 Vᵢ 추적은
의미 없다 — Formula 1~3을 완전히 생략해도 안전하며, 이것이 Formula 4의 조기 종료
근거다.

## Edge Cases

> *(Specialist agent 미상담 — lean 모드, Section D/H 아님. 프로덕션 전 수동 검토 권장.)*

- **If 갭 시작 시 이미 시듦 상태(`neglectMeter=100`)였고 그 뒤로도 계속 시듦이었다면**:
  `SegmentGap`은 스탯 계산을 전혀 하지 않고 단일 세그먼트 `(Δt_total, W=1)`을 즉시
  만들어 `Tick(Δt_total, 1)`만 호출한다 — lifecycle-progression Edge Cases의 기존
  규칙(`G'=G`)과 정확히 일치.

- **If 로드된 `lastUpdatedUtc`(potato-stat)와 `lastTickUtc`(lifecycle)가 서로
  다르다면**(정상 흐름에서는 Core Rule 4의 락스텝 커밋 때문에 발생하지 않아야
  하지만, 저장 손상·마이그레이션 버그에 대한 방어): `gapStart = min(lastUpdatedUtc,
  lastTickUtc)`를 취해 더 이른 시각부터 재처리한다. 짧게 처리해 결손을 남기는 대신
  겹치는 구간을 다시 처리하는 쪽을 택한다 — 재처리 결과는 항상 클램프·상한(스탯
  0–100, `neglectMeter` 100, `stageIndex` 3)으로 유계이므로 겹침 재처리가 무한
  발산하지 않는다. 처리 후 두 타임스탬프는 다시 `now`로 동일하게 갱신되어 자가
  치유된다.

- **If `now < gapStart`(기기 시계가 과거로 조작되거나 되돌아감)**:
  `Δt_total = max(0, now-gapStart)`로 0 클램프 — potato-stat AC-EC-04·lifecycle
  동등 규칙과 동일한 방어를 이 시스템 레벨에서도 반복 적용.

- **If 오프라인 갭 도중 온실막이 만료된다면**: `shieldActiveUntil`이 그 자체로
  세그먼트 분할점이므로(Formula 4 §2), 만료 전 조각은 `isShielded=1`(방치 지표
  누적 0), 만료 후 조각은 `isShielded=0`(정상 누적)으로 각각 계산 — potato-stat
  AC-EC-02와 동일한 계약.

- **If 게임 버전 업데이트로 날씨 가중치가 `cutoverUtc` 기준으로 바뀌었고, 그 시점이
  오프라인 갭 내부에 있다면**: 이 시스템은 별도 처리를 하지 않는다 —
  `GetWeatherSegments`가 이미 컷오버를 내부에서 반영해 정확한 구간을 반환하므로
  (weather-system Edge Cases), 이 시스템은 그 결과를 그대로 소비만 한다.

- **If 오프라인 갭이 극단적으로 길다면(수년~100년급, 세이브 파일을 오래 방치)**:
  Formula 4의 `K+9` 상한에 의해 여전히 유한 시간(수 밀리초~수십 밀리초)에 종료한다
  — weather-system §3이 이미 증명한 상한을 그대로 상속.

- **If 갭이 이미 수확 단계(`stageIndex=3`, HarvestReady)에 도달한 뒤에도
  계속된다면**: `Tick`이 내부적으로 `G`를 계속 누적해도
  `stageIndex(G)=clamp(⌊G/24⌋,0,3)`이 3에서 고정되므로 관측 가능한 변화가 없다 —
  lifecycle-progression 자체 규칙(hold, 방치 벌점 없음)을 그대로 상속, 이 시스템은
  특별 처리 불필요.

- **If 앱이 아주 짧게(수 초) 백그라운드/포그라운드를 오갔다면**: 트리거(Core Rule 1)가
  매번 실행되지만 `Δt_total`이 미미해 `SegmentGap`이 1개 이하의 세그먼트를 반환하며
  비용은 사실상 0에 수렴한다 — 별도의 최소 갭 임계값(threshold)을 두지 않는다
  (불필요한 복잡도).

## Dependencies

**Upstream (이 시스템이 의존):**

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 감자 상태 관리 | Hard | `lastUpdatedUtc`·`shieldActiveUntil` 읽기, 배치 조정 적용(§4 공식) |
| 날씨 시스템 | Hard | `GetWeatherSegments(fromT, toT)` |
| 생애주기 진행 | Hard | `Tick(Δtᵢ, Wᵢ)` 세그먼트별 순차 호출 |
| 세이브/영속성 시스템 | Hard | 로드된 타임스탬프(`IsTimestampValid` 게이트 통과분) 소비, `RequestSave()` 단일 커밋 |

**Downstream (이 시스템에 의존):**

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 시듦/위기 & 회복 | Hard | `OnOfflineProgressApplied` 신호 이후에만 정확한 위기 판정 가능 |
| 온실막 보호 | Soft | 동일 신호 — 없어도 콜드 스타트 직후 설치 게이팅이 잠깐 stale할 뿐 코어 설치는 작동(강도 정정: greenhouse-shield-system.md가 이 의존을 Soft로 확정, 2026-07-19 양방향 정합) |
| 알림 시스템 (미설계) | Soft | 동일 신호, 없어도 알림만 지연될 뿐 코어 루프는 유지 |

**교차 문서 수정(사용자 일괄 승인, 2026-07-18)**:
- `lifecycle-progression-system.md`: "잠정 — 그 GDD 미작성" 태그 2곳 제거, 이 GDD로 링크.
  Open Questions의 세그먼트 분할 알고리즘 항목 해결됨으로 갱신.
- `potato-stat-system.md` + `save-persistence-system.md`: 이 GDD 작성 중 발견된 실제
  gap 보완 — `neglectMeter`·`shieldActiveUntil`이 어느 문서에도 등록 키가 없어 앱
  재시작 시 시듦/온실막 상태가 리셋될 실제 버그 위험이 있었음. 양쪽 문서에 명시적
  `Register`/`RegisterUtcTimestamp` 스키마 추가로 해소.

## Tuning Knobs

이 시스템은 자체 소유 튜닝 노브가 없다 — 4개 상위 시스템(감자 상태 관리·날씨 시스템·
생애주기 진행·세이브/영속성)이 이미 튜닝한 상수를 그대로 소비하는 순수 오케스트레이션
계층이다. 참조만 하는 값(변경 시 소유 문서를 수정할 것):
`NEGLECT_K`, `NEGLECT_DECAY`(potato-stat) · `SAFE_MIN/MAXᵢ`(potato-stat) ·
`ACTIVE_RATE`/`BASELINE_RATE` 계열(potato-stat/weather) · `DURATION_MIN/MAX`(weather) ·
`SHIELD_DURATION`(game-concept, potato-stat 소비) · `STAGE_DURATION_HOURS`(lifecycle).

## Visual/Audio Requirements

해당 없음 — 이 시스템은 순수 인프라 계층으로 자체 시각/오디오 표현이 없다. 계산
결과가 감자에게 어떻게 보이는지(포즈, 색조, 실루엣)는 전적으로 potato-stat·
lifecycle-progression 자체의 Visual/Audio Requirements가 소유한다. 이 시스템은
그 시스템들이 최종 상태에 도달하게만 만들 뿐이다.

## UI Requirements

해당 없음 — 신규 HUD/화면 없음. `OnOfflineProgressApplied` 신호를 실제 플레이어
대상 UI(알림·요약 카드 등)로 표현할지는 알림 시스템(미설계)·시듦/위기&회복의
소관(Open Questions에 페이로드 스키마 확정 필요 항목으로 이미 등록됨).

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션.)*

### Core Rules (AC-ST-##)

- **AC-ST-01 (단일 트리거 실행)** **GIVEN** 오케스트레이터가 `Idle`이고 콜드 스타트(세이브 Ready 직후) 또는 포그라운드 복귀(`OnApplicationFocus(true)`) 조건이 충족 **WHEN** 트리거 발생 **THEN** 배치가 정확히 1회 실행되어 `Idle→Segmenting→Applying→Committing→Idle`로 전이한다.
- **AC-ST-02 (Applying 중 재진입 무시)** **GIVEN** 오케스트레이터가 `Applying` 중 **WHEN** 두 번째 트리거 요청 도착 **THEN** 무시됨 — `SegmentGap`·`RequestSave()` 모두 추가 호출 없이 진행 중이던 배치만 끝까지 완료.
- **AC-ST-03 (갭 산정)** **GIVEN** `lastUpdatedUtc=T0`, 주입된 시계 `now=T0+5h` **WHEN** 갭 계산 **THEN** `gapStart=T0, gapEnd=T0+5h, Δt_total=5h`.
- **AC-ST-04 (세그먼트 경계 수집 — 3종 병합)** **GIVEN** 날씨 전환 2개·온실막 만료 1개·시듦 진입 1개가 갭 내부에 존재 **WHEN** 경계 수집 **THEN** 3종이 하나의 시간순 정렬 리스트로 병합되고, 결과 각 부분구간은 날씨·차폐·시듦 여부가 전부 상수.
- **AC-ST-05 (락스텝 순차 적용)** **GIVEN** 2세그먼트 갭 `[(27h,W=0),(3h,W=1)]` **WHEN** 적용 **THEN** 세그먼트1의 potato-stat 갱신 직후 곧바로 `Tick(27h,0)`이 호출되고, 세그먼트2가 시작되기 전에는 `RequestSave()`가 호출되지 않는다.
- **AC-ST-06 (단일 커밋)** **GIVEN** 전 세그먼트 적용 완료 **WHEN** Applying 종료 **THEN** `RequestSave()`가 정확히 1회만 호출되고, Applying 도중에는 0회.
- **AC-ST-07 (완료 신호 순서)** **GIVEN** Committing이 `RequestSave()`를 호출한 직후 **WHEN** 저장 완료 **THEN** `OnOfflineProgressApplied`가 정확히 1회, `RequestSave()` 호출 이전에는 발생하지 않음.
- **AC-ST-08 (IsApplying 플래그 배타성)** **GIVEN** `IsApplying=true`(Segmenting/Applying/Committing 중) **WHEN** 주기 자동저장 타이머 또는 라이브 틱 드라이버가 실행 시도 **THEN** 둘 다 대기(또는 스킵)하며, `IsApplying=false`(Idle)로 돌아온 뒤에야 재개한다.

### Formulas (AC-F-##)

- **AC-F-01 (t_cross 기본 계산)** **GIVEN** 한파 구간 `t0=0h, V0=32, rate=-2.5/hr, X=SAFE_MIN=30` **WHEN** `t_cross` 평가 **THEN** `=0.8h`, 두 유효성 조건 모두 충족.
- **AC-F-02 (t_cross 유효성 필터)** **GIVEN** 스탯이 이미 구간 밖(`V0=20, SAFE_MIN=25, rate=+0.5`, 구간 쪽으로 이동 중) **WHEN** `X=SAFE_MIN=25`로 평가 **THEN** 그 경계는 배제되고 클램프(0/100) 경계만 검사 대상.
- **AC-F-03 (ΔN_piece — 비차폐)** **GIVEN** `Δt=2h, O_start=5.5, m=2.5, isShielded=0` **WHEN** 평가 **THEN** `ΔN=8`.
- **AC-F-04 (ΔN_piece — 차폐 시 정확히 0)** **GIVEN** AC-F-03과 동일 입력, `isShielded=1` **WHEN** 평가 **THEN** `ΔN=0`(O_start·m 무관).
- **AC-F-05 (τ_wither — 이차방정식 근 선택)** **GIVEN** `N_start=71.625, O_start=22.0, m=1.0` **WHEN** 풀이 **THEN** `a=0.25,b=11,c=-28.375`, `+√`근 선택, `τ_local≈2.444h`(오차 ±0.001h).
- **AC-F-06 (τ_wither — m=0 선형 퇴화)** **GIVEN** `m=0, O_start=20, N_start=90` **WHEN** 풀이 **THEN** 선형식 `(100-N_start)/(NEGLECT_K·O_start)` 사용, 결과가 `[0,Δt_piece)` 내.
- **AC-F-07 (SegmentGap — 차폐 구간 세분 생략)** **GIVEN** `isShielded=1`이고 2개 스탯이 구간 밖 **WHEN** `SegmentGap` 처리 **THEN** `t_cross` 세분 없이 단일 클램프 갱신 + 단일 `Tick(Δt,0)`.
- **AC-F-08 (SegmentGap — 전체 정합성, 20h 픽스처)** **GIVEN** Formulas §의 20시간 예시 픽스처 **WHEN** `SegmentGap` 실행 **THEN** 문서화된 6세그먼트와 정확히 일치(N값·τ≈15.44h 포함, 오차 ±1분).
- **AC-F-09 (SegmentGap — 크로싱 없음, 정상 종료)** **GIVEN** N이 갭 내내 100에 도달하지 않는 12h 갭(potato-stat AC-F-17 안전 시나리오) **WHEN** 실행 **THEN** Formula 3 미호출, 전 세그먼트 `W=0`, 마지막 세그먼트 종료가 `gapEnd`와 정확히 일치.
- **AC-F-10 (SegmentGap — 유한 상한 K+9)** **GIVEN** 합성 100년 갭 **WHEN** 실행 **THEN** 세그먼트 수 `≤K+9`로 유한 종료, 예외·행 없음(엄격한 밀리초 상한은 단정하지 않음 — Testability Notes 참고).

### Edge Cases (AC-EC-##)

- **AC-EC-01 (갭 시작 시 이미 시듦 — 고속 경로)** **GIVEN** `neglectMeter=100`이 `gapStart`부터 유지, 갭 내내 회복 없음 **WHEN** 배치 실행 **THEN** 단일 세그먼트 `(Δt_total,W=1)`, `Tick(Δt_total,1)` 1회, `G'=G` — lifecycle AC-EC-06과 일치.
- **AC-EC-02 (타임스탬프 불일치 — 방어적 gapStart)** **GIVEN** `lastUpdatedUtc=T+2h, lastTickUtc=T` **WHEN** gapStart 계산 **THEN** `gapStart=min(T+2h,T)=T`, 배치 후 두 타임스탬프 모두 동일한 `now`로 재설정.
- **AC-EC-03 (시계 역행)** **GIVEN** `lastUpdatedUtc=T`, 주입 시계 `now=T-5h` **WHEN** 갭 계산 **THEN** `Δt_total=0`, 세그먼트·스탯·성장 변화 없음, 그러나 `RequestSave()`/`OnOfflineProgressApplied`는 각각 정확히 1회 발생.
- **AC-EC-04 (온실막 만료 중간)** **GIVEN** 갭 시작 시 `isShielded=1`, 갭 도중 `shieldActiveUntil` 경과, 한 스탯 `overshoot=10` 유지 **WHEN** 배치 실행 **THEN** 만료 전 `ΔN=0`, 만료 후만 `ΔN=NEGLECT_K×10×(만료후경과시간)`.
- **AC-EC-05 (날씨 컷오버 — 패스스루)** **GIVEN** `GetWeatherSegments`가 이미 컷오버 전/후 구간을 올바르게 반환하는 스텁 **WHEN** 오케스트레이터가 소비 **THEN** 반환값을 그대로 적용, 자체 컷오버 분기 없음.
- **AC-EC-06 (극단적으로 긴 갭 — 유한 종료)** **GIVEN** 100년 갭 **WHEN** 배치 실행 **THEN** 예외·행 없이 완료, AC-F-10의 유계 세그먼트 리스트 생성.
- **AC-EC-07 (수확 단계 이후 지속 — 관측 가능 변화 없음)** **GIVEN** 갭 시작 시 `stage=HarvestReady`, 이후 50h `W=0`으로 지속 **WHEN** 배치 실행 **THEN** `Tick`은 세그먼트마다 정상 호출되지만 `stage`는 `HarvestReady` 유지, 별도 분기 불필요.
- **AC-EC-08 (초단기 포그라운드 전환 — 최소 임계값 없음)** **GIVEN** 앱이 실제 3초 후 백그라운드/포그라운드 전환 **WHEN** 트리거 발생 **THEN** 전체 상태 머신(`Segmenting→Applying→Committing`)이 정상 실행되어 0~1개 세그먼트 생성, 조기 스킵 없음.

### Integration (AC-INT-##)

- **AC-INT-01 (전체 파이프라인 — 다중 세그먼트, 협력자 호출 검증)** **GIVEN** AC-F-08 픽스처, `GetWeatherSegments`/potato-stat 배치 함수/`lifecycle.Tick` 스파이 **WHEN** 배치 실행 **THEN** `GetWeatherSegments`는 정확히 1회, potato-stat 배치 함수는 세그먼트당 1회(해당 `Δt,weather,isShielded`로), `lifecycle.Tick`은 세그먼트당 1회씩 각 potato-stat 호출 직후 문서화된 시간순으로 호출(각 6회).
- **AC-INT-02 (이미 시듦 고속 경로 — 호출 검증)** **GIVEN** `neglectMeter=100` at gap start, 회복 없음 **WHEN** 실행 **THEN** `GetWeatherSegments` 미호출, Formula 1~3 미호출, `lifecycle.Tick`은 `(Δt_total,1)`로 정확히 1회.
- **AC-INT-03 (락스텝 커밋 — 배치 중 부분 저장 없음)** **GIVEN** 3세그먼트 이상 갭 **WHEN** 배치 실행 **THEN** 세그먼트 적용 중에는 `RequestSave()` 0회, 마지막 세그먼트 이후 정확히 1회.
- **AC-INT-04 (lifecycle Tick 순차 호출 계약)** **GIVEN** 3세그먼트 갭 `[(Δt1,W1),(Δt2,W2),(Δt3,W3)]` **WHEN** 호출 **THEN** `Tick`이 정확히 3회, 항상 시간순, 하나로 합쳐 호출되지 않음 — `lifecycle-progression-system.md` AC-INT-07을 호출자 측에서 충족.

### Testability Notes

- **주입 가능한 시계 필수**: `now`를 `DateTime.UtcNow` 직접 호출이 아니라 주입된 clock provider로 받아야 AC-ST-03·AC-EC-03 등 시간 기반 AC가 결정론적 EditMode 테스트가 된다.
- **로드된 타임스탬프도 주입 가능해야** — 실제 세이브/영속성 싱글턴이 아니라 추상화를 통해 임의 값 설정 가능해야 함.
- **4개 협력자(GetWeatherSegments, potato-stat 배치 함수, lifecycle.Tick, RequestSave) 모두 모킹 가능한 인터페이스/델리게이트여야** AC-INT 항목들이 EditMode에서 성립.
- **트리거 소스(OnApplicationFocus 등)는 직접 단위테스트 대상이 아님** — 내부 진입점 메서드(예: `TryRunOfflineProgress()`)를 직접 호출하는 방식으로 AC-ST-01/02를 테스트하고, 엔진 콜백이 실제로 그 메서드를 부르는지는 수동/PlayMode 확인으로 분리.
- **float 비교는 epsilon 허용 오차 필요**(`t_cross`/`τ_wither`/`ΔN_piece`) — 워크드 예시에 이미 쓰인 ±0.001h 수준 권장.
- **AC-F-10/AC-EC-06은 엄격한 밀리초 상한을 검증하지 않는다** — 유한성·유계 세그먼트 수·예외 없음만 `tests/unit/`에서 단정, 실제 벽시계 성능 예산은 `tests/performance/`로 분리.
- **AC-EC-05는 블랙박스 패스스루만 검증** — "컷오버 인식 분기가 아예 없다"는 것은 런타임 관측이 아니라 코드 리뷰 관심사.

## Open Questions

| 질문 | 상황 | 담당 | 해소 시점 |
|---|---|---|---|
| `K+9` 세그먼트 상한의 실측 성능 검증 | 이론적 상한(Formula 4)은 증명됐으나 실제 저사양 모바일 기기에서의 밀리초 단위 실측은 아직 없음 | performance-analyst | Vertical Slice 프로파일링 단계 |
| `lastUpdatedUtc`/`lastTickUtc` 불일치(AC-EC-02) 발생 시 텔레메트리 기록 여부 | 정상 흐름에서는 발생하지 않아야 하는 방어적 케이스 — 실제 발생 시 버그 신호로 로깅할지 결정 안 됨 | analytics-engineer | 분석/텔레메트리 시스템 설계 시 |
| `OnOfflineProgressApplied`가 갭 요약 페이로드를 포함해야 하는가(예: 시들었는지, 온실막이 만료됐는지, 어떤 날씨를 거쳤는지) | 알림 시스템(미설계)·시듦/위기&회복이 이 정보를 UI/카피에 활용할 수 있으나 페이로드 스키마 미확정 — 시듦/위기&회복은 설계됐으나 그 GDD도 이 페이로드에 의존하지 않는 형태로 완결됐으므로, 실제 스키마는 알림 시스템 설계 시 확정 | game-designer/ux-designer | 알림 시스템 설계 시 |
| 세이브 포맷 마이그레이션이 이 시스템이 신뢰하는 신규 필드(`potato_shieldActiveUntil`/`potato_lastUpdatedUtc`)에 미치는 영향 | save-persistence 자체의 마이그레이션 전략이 아직 열린 질문 | lead-programmer | save-persistence-system.md 마이그레이션 전략 확정 시 |
| PRNG 해시 알고리즘 구체 선정 | weather-system의 기존 열린 질문 — 이 시스템은 `GetWeatherSegments`를 통해서만 간접 소비하므로 직접 해소 대상 아님, 상속 확인만 필요 | technical-director | `/create-architecture` 단계 |
