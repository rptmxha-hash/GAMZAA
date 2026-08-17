# 감자 상태 관리 (Potato Stat System)

> **Status**: In Design
> **Author**: user + game-designer/systems-designer
> **Last Updated**: 2026-07-09
> **Implements Pillar**: Pillar 1 (손끝의 위안), Pillar 3 (벌주지 않는 힐링)

## Overview

감자 상태 관리 시스템은 플레이어가 직접 보는 화면이 아니라, 창문을 밀고 난로를 누르고
물을 줄 때마다 "감자가 지금 어떤 상태인지"를 결정하는 보이지 않는 저울이다. 플레이어는
이 시스템을 직접 들여다보지 않는다 — 대신 감자의 표정이 편안한지 처져 있는지, 화면의
색감이 따뜻한지 차가운지, 볼이 발그레한지 창백한지로 이 시스템의 결과를 느낀다. 습도·
온도·수분이라는 세 축의 수치가 날씨·터치·시간 경과에 따라 오르내리며, 그 값이 임계값을
넘으면 표정·색상·나아가 위기(시듦)까지 촉발한다. 이 시스템이 없다면 날씨도, 케어
제스처도, 생장도, 위기도 서로 아무 관계 없는 독립된 이벤트로 흩어져버린다 — 감자를
"돌본다"는 감각 자체가 바로 이 세 수치가 서로 얽혀 움직이는 데서 나온다.

## Player Fantasy

이 시스템 자체에는 고유한 플레이어 판타지가 없다 — 플레이어는 습도·온도·수분이라는
숫자를 결코 보지 않는다. 대신 이 시스템은 Pillar 1(손끝의 위안)과 Pillar 3(벌주지 않는
힐링)이 실제로 작동하기 위한 엔진이다: 터치가 "즉각 반응한다"는 감각은 이 시스템의
수치가 지체 없이 바뀌기 때문이고, "죄책감 없이 되돌릴 수 있다"는 안심은 이 시스템이
되돌림 가능한 형태로 상태를 설계했기 때문이다. 이 시스템이 잘 만들어졌다는 증거는
역설적으로 "플레이어가 이 시스템의 존재를 전혀 의식하지 못한 채, 그저 감자가 살아
있는 것처럼 반응한다"고 느끼는 것이다.

> *(`creative-director` 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 lean에서도 상담 필수.)*

### Core Rules

**세 가지 코어 스탯**: 수분(Moisture)·습도(Humidity)·온도(Temperature) — 각 0~100 범위,
각자 "안전 구간(safe band)"을 가진다.

**통합 방치 지표(Neglect Meter)**: 3개 스탯을 각각 별도로 위기 판정하지 않고, 하나의
통합 수치(0~100)로 관리한다 — 아트 바이블의 무드 설계가 "시듦"을 날씨 종류와 무관하게
단일 상태로 정의했기 때문이다(폭염으로 인한 시듦과 한파로 인한 시듦이 시각적으로 다르지
않음). 세 스탯 중 하나라도 안전 구간을 벗어나면 이 지표가 쌓이고, 셋 다 안전 구간 안에
있으면 서서히 줄어든다. 이 지표가 100에 도달하면 시듦(Wither)이 트리거된다.

**온실막 보호(`shieldActiveUntil`)**: 시스템 인덱스의 순환 의존성 해소안대로, 이 필드는
별도 시스템이 아니라 이 시스템 자체에 귀속된다. 이 타임스탬프가 미래인 동안은 방치 지표
누적이 완전히 정지된다(오프라인 위험 0%).

**되돌림(Rewind)**: 시듦 이후 광고 시청으로 트리거되면, 3개 스탯 전부를 이상적 값으로
리셋하고 방치 지표를 0으로 되돌리며 `shieldActiveUntil = 지금 + 8시간`을 설정한다.

### States and Transitions

| 상태 | 진입 조건 | 이탈 조건 |
|---|---|---|
| **Healthy(건강)** | 기본값 — 3개 스탯 모두 안전 구간 내 | 스탯 하나라도 이탈 → AtRisk |
| **AtRisk(위험 진행)** | 스탯 1개 이상 안전 구간 이탈, 방치 지표 누적 중 | 전부 복귀 시 → Healthy / 방치 지표 100 도달 시 → Wither |
| **Wither(시듦)** | 방치 지표 100 도달(온실막 비활성 시에만) | 시듦/위기 & 회복 시스템의 `TriggerRecovery()` 호출 → Recovered (부활 광고 시청 또는 위기 중 누적 30분 포그라운드 체류 경로, `wither-crisis-recovery-system.md` 확정, 2026-07-18) |
| **Recovered/Shielded(회복·보호)** | 되돌림 트리거 — 스탯 리셋, `shieldActiveUntil` 설정 | 보호 시간 만료 → `isShielded=false`로 전환. **스탯 자체는 보호 중에도 계속 드리프트하며 멈추지 않는다** — 온실막이 차단하는 것은 방치 지표(`neglectMeter`) 누적뿐이다(Formulas §2·§4, Edge Cases, Visual/Audio Requirements와 일치). 만료 시점 스탯이 이미 구간 밖이면 즉시 AtRisk, 전부 구간 안이면 Healthy |

### Interactions with Other Systems

이 시스템은 **Foundation**이므로 아무것도 의존하지 않고, 공개 API(스탯 조정·조회
메서드)만 노출한다. 다른 시스템들이 이 API를 통해 읽고 쓴다.

| 시스템 | 관계 | 인터페이스 |
|---|---|---|
| 날씨 시스템 | 이 시스템에 의존(호출자) | 현재 날씨에 따라 `AdjustMoisture/Humidity/Temperature(delta)` 호출 |
| 터치 케어 인터랙션 | 이 시스템에 의존(호출자) | 제스처 성공 시 동일한 스탯 조정 메서드 호출 |
| 오프라인 진행 계산 | 이 시스템에 의존(호출자) | `lastUpdatedUtc`·`shieldActiveUntil` 읽어 경과 시간만큼 일괄 감쇠 적용 |
| 시듦/위기 & 회복 | 이 시스템에 의존(호출자+구독자) | `NeglectMeter`·`IsWithering()` 조회, 되돌림 시 `ResetToIdeal()` + `ActivateShield(8h)` 호출 |
| 표정/애니메이션 | 이 시스템에 의존(구독자) | 매 프레임 현재 스탯/상태를 읽어 표정 결정 |
| 온실막 보호 | 이 시스템에 의존(호출자) | `ActivateShield(SHIELD_DURATION)` 단독 호출(능동 설치, `ResetToIdeal()` 없이) + `isWithered`/`isShielded` 조회로 설치 게이팅(`greenhouse-shield-system.md` 확정, 2026-07-18) |

## Formulas

> *(`systems-designer` 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션.)*

### 0. 공통 원시 함수 — 스탯 조정

날씨 감쇠(틱 단위/오프라인 일괄 계산)와 터치 보정(1회성 완료 이벤트)이 모두 이 함수를
거쳐 적용된다 — Detailed Design의 `AdjustMoisture/Humidity/Temperature(delta)`가 내부적으로
호출하는 함수.

`V' = clamp(V + Δ, 0, 100)`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 현재 스탯값 | `V` | float | 0–100 | 수분·습도·온도 중 하나의 현재 값 |
| 조정량 | `Δ` | float | 무제한 입력, 효과는 제한됨 | 날씨 감쇠 또는 터치로부터 오는 델타 (+/-) |
| 결과값 | `V'` | float | 0–100 | 조정 후 최종 스탯값 |

**Output Range:** 항상 [0, 100]으로 클램프 — 스탯이 음수가 되거나 100을 초과할 수 없어,
과도한 방치나 과도한 터치 모두 그냥 포화될 뿐 별도 페널티가 없다.

**Example:** `V=85`, `Δ=+30`(물뿌리개 완료) → `V' = clamp(115, 0, 100) = 100`. 초과분
(+15)은 그냥 흡수됨 — 이미 최댓값 근처인 스탯을 만져도 손해가 없다.

---

### 1. 안전 구간 경계값 (스탯별 차등)

3개 스탯이 같은 중심값(`IDEAL=50` — `ResetToIdeal()`이 쓰는 값)을 공유하되, 폭은 스탯마다
다르다.

`IsInBand(Vᵢ) = (SAFE_MINᵢ ≤ Vᵢ ≤ SAFE_MAXᵢ)`, 심각도: `overshoot(Vᵢ) = max(0, SAFE_MINᵢ - Vᵢ, Vᵢ - SAFE_MAXᵢ)`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 이상값 | `IDEAL` | int (const) | 50 | 3개 스탯 공유 중심값, 회복 시 리셋 목표 |
| 안전 구간 경계 | `SAFE_MINᵢ`, `SAFE_MAXᵢ` | int (const, 스탯별) | 아래 표 참고 | 스탯별 안전 구간 |
| 현재값 | `Vᵢ` | float | 0–100 | 스탯 `i ∈ {수분, 습도, 온도}`의 현재 값 |
| 이탈 정도 | `overshoot(Vᵢ)` | float | 0 ~ (50 − 반폭) | 구간 밖으로 벗어난 거리 |

| 스탯 | 반폭 | 안전 구간 | 근거 |
|---|---|---|---|
| **수분** | 25 | **[25, 75]** (가장 넓음) | 물주기는 가장 습관적이고 자주 손이 가는 제스처(§2 기본 엔트로피도 항상 흐르는 스탯) — 관대하지 않으면 물주기가 위안이 아니라 잡일이 됨 |
| **온도** | 20 | **[30, 70]** (중간) | 한파 때만 간헐적으로 압박받고, 난로 롱프레스는 프로토타입에서 유일하게 "그럴듯하다"는 검증을 받은 제스처 — 이미 신뢰받는 피드백 루프라 다소 타이트해도 부당하게 느껴지지 않음 |
| **습도** | 15 | **[35, 65]** (가장 좁음) | 아트 바이블상 폭우/안개가 3개 날씨 중 가장 무겁고 차가운 무드로 설계됨(무드 긴장과 수치 긴장을 맞춤) + 창문 드래그가 프로토타입에서 가장 검증이 부족했던 제스처("열리는 느낌이 안 난다") — 좁은 구간으로 환기를 계속 신경 쓰게 유지 |

**Output Range:** 세 구간 모두 `IDEAL=50`을 중심으로 대칭이며 폭(50/40/30, 최대 1.67배 차)이
극단적이지 않아 "적당한 긴장" 톤을 세 스탯 모두에서 유지한다.

**Example:** `습도=68` → `68 > SAFE_MAX(65)` → 구간 이탈, `overshoot = 68-65 = 3`. 같은
값(68)이 수분이었다면 여전히 구간 안 — 습도의 좁은 구간 때문에 같은 절대 드리프트가 다른
스탯보다 먼저 위기로 등록된다.

---

### 2. 날씨 기반 수동 드리프트 (기본 엔트로피 포함)

**규칙**: 각 스탯은 한 시점에 정확히 하나의 감쇠율만 쓴다 — 현재 날씨가 그 스탯을 겨냥하면
활성 날씨 감쇠율, 아니면 기본 엔트로피율.

`rate(Vᵢ, t) = ACTIVE_RATEᵢ (날씨가 i를 겨냥할 때) else BASELINE_RATEᵢ`
**닫힌 형식(오프라인 일괄 계산용):** `V(t) = clamp(V₀ + rate · t, 0, 100)`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 활성 날씨 감쇠율 | `ACTIVE_RATEᵢ` | float (const) | ±2.5 pts/hr | 해당 날씨가 이 스탯을 겨냥할 때의 전체 압박 |
| 기본 엔트로피율 | `BASELINE_RATEᵢ` | float (const) | ±0.5 pts/hr | 그 스탯의 날씨가 비활성일 때도 항상 흐르는 미약한 배경 드리프트(활성 감쇠율의 1/5) |
| 시작값/경과시간/결과값 | `V₀`, `t`, `V(t)` | 상동 | — | — |

**기본 방향 통일 규칙**: 각 스탯의 "자연 엔트로피 방향"은 항상 그 스탯 고유의 활성 날씨
위기와 **같은 방향**이고(날씨는 같은 흐름을 가속할 뿐), 그 스탯 고유의 터치 제스처로만
**상쇄**된다 — 세 스탯 모두 스스로와 충돌하지 않는다.

| 스탯 | 기본 방향 | 근거 | 활성 날씨와 일치? | 상쇄 수단 |
|---|---|---|---|---|
| **수분** | -0.5/hr (감소) | 흙은 날씨와 무관하게 서서히 마름 — 맑음은 같은 과정을 가속할 뿐 | ✓ 맑음도 -2.5/hr | 물뿌리개 (+30) |
| **습도** | +0.5/hr (증가) | 환기 안 된 공기는 저절로 눅눅해짐 — 폭우/안개는 같은 경향에 습기를 더 얹을 뿐 | ✓ 폭우/안개도 +2.5/hr | 창문 드래그 (-30) |
| **온도** | -0.5/hr (감소) | 열원 없는 방은 저절로 식음 — 한파는 같은 냉각을 가속할 뿐 | ✓ 한파도 -2.5/hr | 난로 롱프레스 (+30) |

**Output Range:** 0–100 클램프. 기본 엔트로피만으로는 이상값에서 각 스탯 구간 경계까지
30~50시간이 걸려(가장 관대→가장 좁음 순) 정상적인 방치 범위에서는 지배적 위협이 아님을
확인 — 스탯이 "가만히 얼어있지 않고 살아 숨 쉬는" 느낌을 주는 목적이 크다.

**Example:** 현재 날씨가 맑음(수분만 겨냥). 습도는 비겨냥이므로 기본율 적용:
`V₀=50`, `rate=+0.5`. `t=10h` 후: `V(10) = clamp(50+5, 0, 100) = 55` — 여전히
`[35,65]` 안, 작지만 눈에 보이는 무해한 변화.

---

### 3. 터치 보정량

프로토타입 발견에 따라, 제스처 피드백은 진행형이지만 **스탯 조정 자체는 완료 시점에
한 번, 원자적으로** 발생한다 — 진행 중 비례 조정이 아니다.

`V' = clamp(V + TOUCH_Δ · sign_gesture, 0, 100)` — 완료 시 1회 적용

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 보정 크기 | `TOUCH_Δ` | int (const) | 30 | 제스처 완료 1회당 스탯 변화량 |
| 방향 | `sign_gesture` | int (const, 제스처별) | {+1,-1} | 물뿌리개(+수분) / 창문 드래그(-습도) / 난로(+온도) |
| 이전/이후값 | `V`, `V'` | float | 0–100 | 완료 직전/직후 스탯값 |

**Output Range:** 0–100 클램프. `TOUCH_Δ=30`은 수분·온도 안전 구간 반폭(25/20)보다 크게
설계되어, 최악값(0)에서도 터치 한 번이면 구간 경계까지 확실히 복귀한다.

**Example:** 수분이 `V=15`(구간 밖, `overshoot = SAFE_MIN(25) - 15 = 10`)까지 떨어진 상태.
물뿌리개 완료: `V' = clamp(15+30, 0, 100) = 45` — 안전 구간 `[25,75]` 안으로 한 번에 복귀.

> **알려진 루즈엔드(습도 한정)**: 습도 구간이 좁아진 결과(`[35,65]`), 습도 절대 최댓값
> (100)에서 창문 드래그 1회(`TOUCH_Δ=30`)로는 70까지만 회복되어 구간 경계(65)를 살짝
> 넘긴다. 실제로는 습도가 절대 최댓값에 도달하기 전(약 18.65시간)에 이미 시듦이
> 트리거되므로 실무 영향은 미미하지만, `Asset Standards`나 밸런스 튜닝 단계에서
> 재확인할 항목으로 남긴다.

*Design note*: 스탯은 원시 숫자로 플레이어에게 노출되지 않으므로, 이 "확실한 점프"는
표정/애니메이션 시스템이 30점 변화를 뚜렷한 시각·무드 변화로 반드시 번역해야 한다는
계약이기도 하다.

---

### 4. 방치 지표 누적

`dN/dt = (1 - isShielded) · k · Σᵢ overshoot(Vᵢ)`, `i ∈ {수분, 습도, 온도}`

**구간별 닫힌 형식(오프라인 일괄 계산 안전):** `ΔN = (1 - isShielded) · k · Σᵢ [(oᵢ,start + oᵢ,end)/2] · Δt`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 방치 지표 | `N` | float | 0–100 | 통합 방치 수치 |
| 누적 계수 | `k` | float (const) | **0.5** | 스탯 1개, 이탈 1점당, 시간당 누적되는 방치 점수 |
| 이탈 정도 | `overshoot(Vᵢ)` | float | §1 참고 | 3개 스탯 각각 |
| 온실막 활성 여부 | `isShielded` | bool {0,1} | {0,1} | `now < shieldActiveUntil`이면 1 — 누적을 완전히 차단 |
| 구간 시작/끝 이탈치 | `oᵢ,start`, `oᵢ,end` | float | §1 참고 | 선형(단조) 구간의 시작/끝 이탈값 |
| 경과 시간 | `Δt` | float | 0–∞ (시간) | 해당 구간 지속 시간 |

**Output Range:** `N`은 [0,100] 클램프, `isShielded=0`일 때만 100 도달 시 시듦 트리거.

**배치 계산 참고**: `rate_weather`가 날씨 구간마다 상수이므로 `overshoot(t)`는 구간별로
정확히 조각별 선형이다. 오프라인 계산기는 구간 경계 이탈 시각·날씨 전환 시각에서 대수적으로
시각을 구해 방치 갭을 단조 구간들로 나누고, 구간마다 사다리꼴 공식을 적용하면 된다 —
프레임 단위 시뮬레이션 루프가 며칠짜리 오프라인 갭에도 필요 없다.

**Example (심각도 스케일링):** 수분만 구간 밖(`overshoot=15`) → `dN/dt = 0.5×15 = 7.5`
방치/시간. 습도도 동시에 이탈(`overshoot=15`)이면 → `dN/dt = 0.5×(15+15) = 15` —
정확히 2배, 별도 계수 없이 "이탈 스탯이 많을수록 빨라짐" 요구를 충족.

**Example (사다리꼴 닫힌 형식):** 수분이 `t=10h`에 구간 이탈(overshoot=0)해서
`t=14h`까지 overshoot=10으로 드리프트(`Δt=4h`, 단일 스탯, 온실막 없음):
`ΔN = 0.5 × ((0+10)/2) × 4 = 10`.

---

### 5. 방치 지표 감소

3개 스탯이 동시에 안전 구간 안에 있을 때만 작동. 온실막과 무관하게 무조건 작동한다(보호
중엔 스탯이 리셋되어 이미 구간 안이므로 자연히 이 경로를 탄다).

`N(t) = max(0, N₀ - D · t)`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 시작 방치값 | `N₀` | float | 0–100 | 3개 스탯이 동시에 구간 복귀한 순간의 방치값 |
| 감소율 | `D` | float (const) | 25 | 전부 구간 안일 때 시간당 감소되는 방치 점수 |
| 경과시간/결과값 | `t`, `N(t)` | 상동 | — | — |

**Output Range:** 0에서 클램프(음수 불가). 누적보다 훨씬 빠르게 설계 — "고쳤으니 이제
안전하다"는 감각을 주지, 처벌의 꼬리표가 남지 않는다.

**Example:** 마지막 이탈 스탯을 터치로 해결한 순간 `N₀=10`(위 §4 예시). 감소:
`N(t) = max(0, 10-25t)`. `t = 10/25 = 0.4h = 24분` 후 0 도달.

---

### 6. 시듦까지의 시간 — 정합성 검증 (3개 스탯 개별 재검증)

각 스탯을 자신의 활성 날씨가 무기한 지속된다는 최악 시나리오로 개별 검증(Healthy,
`N=0`, `IDEAL=50`에서 시작, 개입 없음).

| 스탯 / 날씨 | 반폭 | 구간 이탈 시각(AtRisk 시작) | 극값 도달 시각 | **시듦 시각(T)** | 경고 구간 |
|---|---|---|---|---|---|
| **수분 / 맑음** | 25 | 10.0h | 20.0h | **23.0h** | 13.0h |
| **온도 / 한파** | 20 | 8.0h | 20.0h | **20.7h** | 12.7h |
| **습도 / 폭우안개** | 15 | 6.0h | (도달 전 시듦) | **18.65h** | 12.65h |

> 세 스탯 모두 `t=20h`에 극값(0 또는 100)에 도달한다 — `IDEAL=50`과 `ACTIVE_RATE=2.5`의
> 불변 성질(`50/2.5=20`)로, 구간 폭과 무관. 습도만 극값 도달 전에 이미 방치 100에 먼저
> 도달한다(좁은 구간 때문에 overshoot이 더 가파르게 쌓임).

**체크인 주기별 검증(3개 스탯 전부)**:

| 주기 | 수분(최악 방치) | 온도 | 습도 |
|---|---|---|---|
| 6시간마다(1일 4회) | 0(이탈 안 함) | 0(이탈 안 함) | 0(이탈 안 함) |
| 8시간마다(1일 3회) | 0(10h까지 안 이탈) | 0(정확히 경계, 안전) | 2.5/100 |
| 12시간마다(1일 2회) | 2.5/100 | 10/100 | 22.5/100 |
| 24시간마다(1일 1회) | **23.0h에 시듦 — 실패** | **20.7h에 시듦 — 실패** | **18.65h에 시듦 — 실패** |

**검증 결과**: "하루 2번 체크인은 항상 안전"은 3개 스탯 전부에서 유지된다(최악인 습도도
22.5/100로 여유). "하루치를 통째로 방치하면 시듦하되 명확한 경고가 있다"도 유지된다 —
시듦 시각은 18.65~23h 범위(모두 "대략 하루"로 읽힘)에 몰려 있고, **어느 스탯이든 12.65~
13.0시간의 일관된 사전 경고**를 준다(방치한 스탯이 무엇이든 예측 가능한 경고 유예 시간을
받는다는 뜻 — 유지할 가치가 있는 성질).

**의도된 비대칭**: 같은 12시간 방치에도 습도(22.5/100)가 수분(2.5/100)보다 눈에 띄게
"빡빡하다" — 이는 §1에서 습도를 가장 좁은 구간으로 설계한 의도된 결과다(폭우/안개 날에는
조금 더 신경 써야 한다). 세 스탯이 더 이상 동등하게 관대하지 않다는 뜻이지만, 사용자
확정을 거친 의도된 밸런스다.

**Example (습도, 가장 타이트한 경우):** `V₀=50`, 폭우/안개 지속(`rate=+2.5/hr`,
구간 `[35,65]`). `t=6h`에 이탈(`V=65`). Overshoot이 선형 증가; `1.25×0.5×(t-6)²=100`을
풀면 `(t-6)²=160 → t≈18.65h`. 방치가 100을 넘는 시각은 **t≈18시간 39분**, AtRisk
시작(t=6h) 이후 약 **12.6시간**의 경고 구간.

---

### 7. 온실막 활성화 (`ActivateShield`)

되돌림(회복)과 능동 설치(온실막 보호 시스템) 두 경로가 공유하는 온실막 부여 mutator.
항상 SET이며 절대 기존 잔여에 누적하지 않는다(`greenhouse-shield-system.md` 작성 중
독립 항목으로 정식화, 2026-07-18 — 그 전까지는 §Detailed Design 산문에 회복 프로세스와
묶여서만 서술됐음).

`ActivateShield(duration): shieldActiveUntil' = t_call + duration`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 보호 지속 | `duration` | float, h | >0 | 요청 보호 길이. 두 호출자 모두 `SHIELD_DURATION(8h)`을 전달 |
| 부여 시각 | `t_call` | float, UTC | — | `ActivateShield`이 실행되는 순간 |
| 새 만료시각 | `shieldActiveUntil'` | float, UTC | — | 항상 덮어쓰기(SET), 누적 아님 |

**두 호출자(신규 명시, 2026-07-18)**: (a) 시듦/위기 & 회복의 `TriggerRecovery()` —
`ResetToIdeal()`과 원자적으로 번들. (b) 온실막 보호 시스템의 능동 설치 — 단독 호출(스탯
리셋 없음). 부여된 `shieldActiveUntil`은 §4의 `isShielded = now < shieldActiveUntil`을
통해 방치 지표 누적을 차단한다.

**Output Range:** `shieldActiveUntil'`은 항상 `t_call`보다 `duration`만큼 미래. 재호출 시
기존 잔여를 무시하고 그 시각 기준으로 다시 SET한다(스택 불가).

**Example:** `t_call=14:00`, `duration=8h` → `shieldActiveUntil'=22:00`. 이후 20:00에
재호출(`duration=8h`) → `shieldActiveUntil'=04:00`(누적 아니라 그 시각 기준 8h로 SET).

---

**상수 요약**(레지스트리 등록용): `IDEAL=50`(공유); 수분 `[25,75]`, 온도 `[30,70]`, 습도
`[35,65]`; `ACTIVE_RATE=±2.5 pts/hr`; `BASELINE_RATE=±0.5 pts/hr`(§2 방향 표 참고);
`NEGLECT_K=0.5`; `NEGLECT_DECAY=25`; `TOUCH_Δ=30`(습도 한정 루즈엔드 있음, §3 참고).

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드.)*

- **If 터치 제스처가 완료 전에 중단된다면(앱 강제 종료·화면 전환 등)**: 스탯 조정은
  완료 시점에만 1회 적용되므로(§3), 미완료 제스처는 스탯에 어떤 영향도 남기지 않는다.
  진행 중이던 시각 피드백만 소실된다.

- **If 오프라인 갭 도중 온실막(`shieldActiveUntil`)이 만료된다면**: 배치 계산은 날씨
  전환·구간 경계 이탈 시각뿐 아니라 `shieldActiveUntil` 시각도 분할 지점으로 추가해야
  한다 — 만료 전 구간은 `isShielded=1`(누적 0), 만료 후 구간은 `isShielded=0`(정상
  누적)으로 각각 계산한다.

- **If 방치 지표가 100(시듦)에 도달한 이후에도 오프라인 갭이 계속된다면**: 방치 지표는
  100에서 정지하며 더 이상 누적되지 않는다. 시듦은 이진 상태이지 정도의 문제가 아니다 —
  Pillar 3(벌주지 않는 힐링)에 따라 "더 심한 시듦"이라는 개념 자체가 존재하지 않는다.

- **If 부활 광고 시청을 기다리는 동안(시듦 트리거 후 회복 완료 전)**: 같은 이유로 방치
  지표 누적이 정지된 상태이므로, 광고 로딩·시청 시간이 길어져도 상태가 더 나빠지지
  않는다.

- **If 기기 시계가 과거로 조작되거나 타임존이 변경된다면**: `경과시간 = max(0, now -
  lastUpdatedUtc)`로 항상 0 이상으로 클램프한다. 음수 경과시간은 0으로 처리해, 시계를
  되돌려 방치를 "취소"하는 악용을 막는다.

- **If 최초 실행(신규 세이브)이라면**: 모든 스탯 = `IDEAL(50)`, 방치 지표 = 0,
  `shieldActiveUntil` = 이미 만료된 값(예: epoch) — 기본적으로 보호는 비활성 상태로
  시작한다.

- **If 동일 제스처를 짧은 시간 내 반복 실행(스팸)한다면**: 별도 쿨다운을 두지 않는다.
  §0의 클램프로 인해 이미 최댓값인 스탯에 터치해도 초과분은 그냥 흡수되므로 스팸해도
  이득도 손해도 없다 — 의도적으로 제한을 두지 않는 것이 Pillar 3와 일치한다.

## Dependencies

**이 시스템이 의존하는 것**: 없음 — Foundation 레이어, 시스템 인덱스 최상위.

**이 시스템에 의존하는 것** (전부 hard dependency — 아래 시스템들은 이 시스템 없이는
기능할 수 없음):

| 시스템 | 의존 강도 | 인터페이스 |
|---|---|---|
| 날씨 시스템 | Hard | `AdjustMoisture/Humidity/Temperature(delta)` 호출 |
| 터치 케어 인터랙션 | Hard | 동일 조정 메서드 호출 + `IsWithering()` 조회(시듦 시 세 제스처 비활성화 게이팅 — touch-care-interaction.md 확정, 2026-07-18) |
| 오프라인 진행 계산 | Hard | `lastUpdatedUtc`·`shieldActiveUntil` 읽기 + 배치 조정 적용 |
| 생애주기 진행 | Hard | `neglectMeter` 조회 → `isWithered` 로컬 파생(lifecycle-progression-system.md Detailed Design Interactions 표 확정, 2026-07-16. `/consistency-check`(2026-07-18)에서 이 표의 역방향 누락 발견해 보완) |
| 시듦/위기 & 회복 | Hard | `NeglectMeter`·`IsWithering()` 조회, `ResetToIdeal()` + `ActivateShield(8h)` 호출 |
| 표정/애니메이션 | Hard | 매 프레임 스탯값 조회(구독) |
| 온실막 보호 | Hard(이 시스템에 의존) | `ActivateShield(SHIELD_DURATION)`의 2번째 호출자(능동 설치 시 단독, `ResetToIdeal()` 없이) + `isWithered`/`isShielded` 조회로 설치 게이팅·top-up 판정. `shieldActiveUntil` 필드 자체는 여전히 이 시스템(Foundation)에 귀속되어 순환 의존성 해소 구조를 유지(`greenhouse-shield-system.md` 확정, 2026-07-18) |
| ASMR 적응형 오디오 | Soft | `OnWitherEntered`/`OnRecovered` 이벤트(§5 오디오 범위 메모가 이미 노출) 구독 — 이 시스템이 응답하지 않아도 시듦/회복 판정 자체는 정상 작동, 오디오만 무음이 됨(`asmr-adaptive-audio.md` 확정, 2026-07-21 — 이전까지 이 표에서 누락돼 있던 실제 갭) |

**양방향 일관성 확인**: 시스템 인덱스의 Dependency Map에 위 5개 시스템이 모두 "감자 상태
관리"를 의존 대상으로 명시하고 있음 — 일치. 아직 이들 시스템의 GDD가 작성되지 않았으므로,
위 인터페이스는 **잠정 계약(provisional contract)**이며 각 시스템 GDD 작성 시 재확인
필요.

**세이브/영속성 등록 스키마**(`offline-progress-calculation.md` 작성 중 발견된 gap 보완,
2026-07-18 — lifecycle-progression-system.md L94의 명시적 `Register(...)` 문장과 동일한
패턴으로 이 시스템도 확정):
- `Register("potato_stats", {moisture, humidity, temperature, neglectMeter})` — 4개
  수치 필드를 하나로 묶어 일반 등록(UTC 타임스탬프 아님).
- `RegisterUtcTimestamp("potato_lastUpdatedUtc", getter, setter)`
- `RegisterUtcTimestamp("potato_shieldActiveUntil", getter, setter)` — `shieldActiveUntil`도
  시계 조작 방어가 필요한 UTC 값이므로 `lastUpdatedUtc`와 동일하게 save-persistence §5의
  `IsTimestampValid` 게이트를 통과한다.

## Tuning Knobs

Formulas 섹션의 상수들이 곧 이 시스템의 조정 노브다. 각 노브의 안전 범위와 극단값에서
무엇이 깨지는지 정리한다.

| 노브 | 현재값 | 권장 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `IDEAL` | 50 | 사실상 고정 | 3개 스탯이 공유하는 상수라 변경 시 안전구간 표 전체 재조정 필요 | 상동 |
| 안전구간 반폭(스탯별) | 수분25/온도20/습도15 | 10~30 | 사소한 드리프트에도 즉시 위험 진입 — Pillar 4(부담없음) 위반 | 안전구간이 전체 범위를 거의 잠식해 "위험" 상태 자체가 무의미해짐 |
| `ACTIVE_RATE` | ±2.5/hr | 1.5~4.0/hr | 날씨 이벤트가 압박감을 전혀 안 줘서 대처할 이유가 사라짐 | §6 정합성 검증이 깨져 짧은 방치에도 시듦 — 예측 불가능한 기습 위기 발생 |
| `BASELINE_RATE` | ±0.5/hr | ACTIVE_RATE의 1/5~1/3 유지 | (하한 없음 — 0에 가까우면 그냥 스탯이 안 움직이는 것뿐) | ACTIVE_RATE에 근접하면 "날씨가 중요하다"는 설계 의도가 흐려짐(날씨 무관하게 항상 위험) |
| `TOUCH_Δ` | 30 | 20~40, 가장 좁은 스탯 반폭의 2배 이상 유지 | 터치해도 구간 복귀가 안 돼 "만졌는데 소용없다"는 무력감 — Pillar 1 정면 위반 | 한 번으로 항상 최댓값까지 튀어 반복 손맛의 의미가 사라짐 |
| `NEGLECT_K` | 0.5 | 변경 시 §6 시듦 시각 재검증 필수 | 방치해도 좀처럼 시듦하지 않아 부활 광고 트리거 빈도 저하(수익모델 영향) | 짧은 방치에도 시듦해 "기습"처럼 느껴짐 — Pillar 3 위반 |
| `NEGLECT_DECAY` | 25 | ACTIVE_RATE 관련 누적 속도보다 항상 크게 유지 | 회복해도 한참 방치 지표가 남아 "고쳤는데 왜 아직도"라는 답답함 | 방치가 사실상 무의미해짐(항상 즉시 리셋) |
| 온실막 지속시간 | 8시간 | 게임 콘셉트 문서에서 이미 확정된 값 — 이 GDD는 소비만 함 | 재조정은 콘셉트 변경 사안(이 GDD 범위 밖) | 상동 |

## Visual/Audio Requirements

> 이 섹션은 새로운 아트 방향을 정의하지 않는다 — `art-bible.md`가 이미 잠근 무드(Section 2)·
> 형태(Section 3)·색(Section 4)·표정 규칙(Character Design Direction §3)을, 이 시스템이
> 노출하는 숫자값에 정확히 매핑하는 **데이터 계약**이다. 아직 작성되지 않은 표정/애니메이션
> GDD는 이 계약만으로 구현 가능해야 한다 — 날씨 시스템이 지금 무엇을 하고 있는지 알 필요가
> 없다.
>
> **범위 밖**: 맑음(Sunny-content, 생기 포즈)은 "현재 날씨"라는 이 시스템에 없는 신호로
> 트리거되므로 여기서 다루지 않는다 — 날씨 시스템 GDD의 몫이다. 이 계약은 세 스탯의 결핍
> 방향 위기 포즈, Wither, 그리고 Recovery가 재사용하는 생기 포즈만 소유한다.

### 0. 노출값 정리

| 필드 | 출처 | 비고 |
|---|---|---|
| `moisture`/`humidity`/`temperature` (`Vᵢ`) | Formulas §0 | 원시값 0–100, 이미 매 프레임 구독 대상(Dependencies) |
| `overshootMoisture`/`Humidity`/`Temperature` | Formulas §1 | 방향 없는 절대 이탈량 |
| `neglectMeter` | Formulas §4 | 0–100 |
| `isShielded` | Detailed Design | `now < shieldActiveUntil` |

**방향 파생(신규 필드 아님)**: `overshootᵢ(low) = max(0, SAFE_MINᵢ - Vᵢ)`,
`overshootᵢ(high) = max(0, Vᵢ - SAFE_MAXᵢ)` — Formulas §1의 `overshoot(Vᵢ) = max(low, high)`를
방향별로 풀어 쓴 것뿐, §1의 정의를 바꾸지 않는다.

### 1. 포즈 선택 (Layer 2 — 이산 카테고리)

위에서 아래로 평가, 먼저 참인 조건에서 멈춘다.

1. `neglectMeter ≥ 100` → **Wither-sagging** (아래 모든 조건 무시, 회복 전까지 고정)
2. 세 스탯 모두 `overshoot = 0` → **일상 케어(Baseline)**
3. `overshoot > 0`인 스탯이 하나 이상 → **최대 overshoot 스탯**이 승자(AtRisk는 별도
   시각 상태를 갖지 않는다 — 승자의 포즈가 곧 AtRisk의 시각이다). 동률이면 **습도 > 온도 >
   수분** 순으로 재정한다(Formulas §1이 이미 확정한 안전구간 반폭 좁은 순 재사용, 신규
   우선순위 아님).
   - 승자 = 습도, high 방향 → **Rain-huddled**
   - 승자 = 온도, low 방향 → **Cold-curled**
   - 승자 = 수분, low 방향 → **Baseline 유지**(Layer 2 신규 카테고리 없음, §2 참고)
   - 승자 = 수분 high / 습도 low / 온도 high("과잉 케어" 방향) → **Baseline 유지**
     (art bible에 전용 포즈 없음 — 의도된 결정: 과도한 물주기/환기/난로질을 시각적으로
     벌하지 않는다, Pillar 3. 사용자 확정 2026-07-10)

`isShielded=true`이면 위 결과 **위에** §4의 온실막 오버레이를 얹는다(포즈 계산 자체는
대체하지 않음).

### 2. 윤곽선 굴곡 (Layer 1 — 연속 보간, 매 프레임)

| 스탯·방향 | 블렌드 `b ∈ [0,1]` | b=0 | b=1 |
|---|---|---|---|
| 온도 low | `clamp(overshootTemperature(low)/20, 0, 1)` | 눈매 살짝 감김 | 눈매 꽉 감김(Cold-curled 최대) |
| 습도 high | `clamp(overshootHumidity(high)/15, 0, 1)` | 눈꺼풀 살짝 낮아짐 | 축축하게 처진 눈매(Rain-huddled 최대) |
| 수분 low | `clamp(overshootMoisture(low)/25, 0, 1)` | 눈매 미세하게 무거워짐 | 반쯤 감긴 나른함 + 크라운 끝 처짐 최대 |

분모는 각 스탯의 안전구간 반폭(Formulas §1, 신규 상수 아님) — 스탯이 절대극값(0/100)에
도달하는 시각과 `b=1.0` 도달 시각이 자동으로 일치한다(§6 정합성 검증과 정합).

**Heatwave-drooping 라벨 임계값(이 섹션 소유, 신규)**: `HEATWAVE_LABEL_RATIO = 0.5`.
`b(수분 low) ≥ 0.5`일 때만 "Heatwave-drooping" 이산 라벨을 적용한다(개별 자산/블렌드
앵커가 필요한 경우의 명명 편의용) — 렌더링 분기점이 아니라 연속 곡선 위의 QA 표기일 뿐이다.

### 3. Blush 색 강조

| 조건 | Blush |
|---|---|
| 기본(아래 어느 것도 아님) | Potato Blush Base |
| 온도 low | **Potato Blush (Chilled)** — 난로 롱프레스 완료(`TOUCH_Δ=+30`) 즉시 Base로 복귀. 별도 임계값 불필요: `TOUCH_Δ=30 ≥ 온도 반폭(20)`이 항상 구간 복귀를 보장하므로 "터치 완료"와 "`overshootTemperature(low)`가 0이 됨"은 항상 같은 프레임에 일어난다 |
| 습도 high | Base 유지, 재채색 없이 워시로만 옅어짐(art bible 잠금) |
| Wither | Potato Blush **Dark**만 잔흔(다른 톤 금지) |
| Recovery 전환 중(§4, 1회성) | Blush Light → Amber Ochre Light 블룸(일시 상승, 전환 종료 후 Base) |

### 4. 이벤트/오버레이

| 트리거 | 발생 시점 | 재생 | 종료 후 |
|---|---|---|---|
| `OnWitherEntered` | `neglectMeter`가 100을 최초로 넘는 프레임(`isShielded=false`에서만 가능) | 1회 논루프 "잎이 늘어지는" 전환 | Wither-sagging 정지 포즈 고정, §1 규칙 무시(회복까지) |
| `OnRecovered` | `ResetToIdeal()+ActivateShield()` 원자적 실행 프레임 | 1회 ~1초+ 중심→바깥 채도 번짐 + Blush 블룸 | 세 스탯=50(전부 구간 중앙)이므로 §1 재평가 → Baseline |
| `isShielded==true` (이벤트 아님, 매 프레임 조건) | `now < shieldActiveUntil` | 정지 반투명 온실막 오버레이(루프 없음, Section 1 원칙 3) — §1 포즈 위에 얹힘 | `isShielded`가 거짓이 되는 순간 오버레이만 제거, 포즈는 이미 그 시점 실제 스탯을 반영 중이라 별도 스냅 불필요 |

### 5. 오디오 (범위 메모)

이 시스템은 사운드를 직접 재생하지 않는다 — 위 표의 이벤트(`OnWitherEntered`,
`OnRecovered`, `isShielded` 변화)와 §3의 터치 완료 순간만 오디오가 구독 가능한 훅으로
노출한다. 실제 SFX 큐 설계는 별도 오디오 GDD의 몫이다.

## UI Requirements

이 시스템은 직접적인 UI를 갖지 않는다 — Player Fantasy 섹션에 명시한 대로, 습도·온도·
수분·방치 지표는 원시 숫자로 플레이어에게 노출되지 않는다. 이 시스템의 모든 "UI"는
Visual/Audio Requirements의 데이터 계약을 통해 하위 시스템이 간접적으로 시각화한다:

- **표정/애니메이션 시스템**: 매 프레임 스탯값을 구독해 감자의 표정으로 번역(직접 소유)
- **홈 화면 위젯 시스템**(UI/HUD 아트 바이블 §6): 이 시스템의 원시 스탯값을 직접 읽지
  않는다 — 표정/애니메이션 시스템이 날씨·생장 단계까지 반영해 계산한 **최종 포즈+실루엣**을
  축소 LOD로 그대로 미러링한다(같은 `overshoot=0`이라도 날씨에 따라 미러링될 실루엣이
  다를 수 있어, 원시 스탯만으로는 정확한 미러링이 불가능하다 — `expression-animation-system.md`
  Interactions 확정, 2026-07-18). 배지·경고 문구 없음 원칙(art bible 잠금 사항)은 그대로 유지.
- **설정/접근성 시스템**: 이 시스템의 데이터를 직접 노출하지 않음, 해당 없음

이 GDD는 위 하위 시스템들이 구현할 **데이터 인터페이스**(Visual/Audio Requirements)까지만
책임진다 — 실제 화면 레이아웃·위젯 디자인은 각 하위 시스템 GDD의 몫이다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션.)*

아래 기준은 Detailed Design·Formulas·Edge Cases·Visual/Audio Requirements에서 정의된
필드·상수명을 그대로 사용한다. 각 항목은 GDD 전체를 읽지 않은 QA 테스터도 독립적으로
재현·판정할 수 있도록 정확한 시작 조건과 기대값을 명시한다. "상태(State)"는 이 GDD가
별도로 저장하는 필드가 아니라 `neglectMeter`·`overshoot*`·`isShielded`로부터 파생되는
값이다 — 아래 기준은 그 파생 조건을 기준으로 검증한다.

### 상태 전이

- **AC-ST-01 (Healthy 진입)**
  **GIVEN** 수분·습도·온도가 각자의 안전 구간 안(예: 수분∈[25,75], 습도∈[35,65], 온도∈[30,70])
  **WHEN** 상태를 조회한다
  **THEN** `overshootMoisture = overshootHumidity = overshootTemperature = 0`이고 상태 = Healthy로 판정된다.

- **AC-ST-02 (Healthy → AtRisk)**
  **GIVEN** 상태 = Healthy, `neglectMeter = 0`
  **WHEN** 드리프트로 습도가 `SAFE_MAX(65)`를 초과한다
  **THEN** `overshootHumidity > 0`이 되어 상태가 AtRisk로 전이되고, 다음 틱부터 §4 공식에 따라 `neglectMeter`가 증가하기 시작한다.

- **AC-ST-03 (AtRisk → Healthy)**
  **GIVEN** 상태 = AtRisk, 온도만 이탈(`overshootTemperature > 0`), 다른 두 스탯은 0
  **WHEN** 난로 롱프레스 완료로 온도가 안전 구간에 복귀한다(`overshootTemperature = 0`)
  **THEN** 세 overshoot 값이 모두 0이 되는 즉시 상태 = Healthy로 전이된다. `neglectMeter`는 0으로 즉시 리셋되지 않고 §5 공식에 따라 점진적으로 감소를 시작한다.

- **AC-ST-04 (AtRisk → Wither)**
  **GIVEN** 상태 = AtRisk, `isShielded = false`
  **WHEN** `neglectMeter`가 누적되어 100에 도달한다
  **THEN** 상태 = Wither로 전이되고, `OnWitherEntered` 이벤트가 그 프레임에 정확히 1회 발생한다.

- **AC-ST-05 (`isShielded`는 Wither 진입을 완전 차단)**
  **GIVEN** 상태 = AtRisk, 하나 이상의 스탯 이탈(`overshoot > 0`) 지속
  **WHEN** `now < shieldActiveUntil`(`isShielded = true`)인 동안 임의 시간이 경과한다
  **THEN** §4의 `(1-isShielded)` 계수에 의해 `dN/dt = 0`이 유지되어 `neglectMeter`가 증가하지 않고, Wither로 전이되지 않는다.

- **AC-ST-06 (Wither → Recovered/Shielded)**
  **GIVEN** 상태 = Wither (`neglectMeter = 100`)
  **WHEN** `ResetToIdeal()` + `ActivateShield(8h)`가 원자적으로 실행된다(되돌림 트리거)
  **THEN** 세 스탯 = 50(`IDEAL`), `neglectMeter = 0`, `shieldActiveUntil = 실행시각 + 8h`, `isShielded = true`, `OnRecovered` 이벤트가 정확히 1회 발생한다.

- **AC-ST-07 (Recovered/Shielded → Healthy 또는 AtRisk)**
  **GIVEN** 상태 = Recovered/Shielded, `now < shieldActiveUntil`
  **WHEN** `now ≥ shieldActiveUntil`이 된다
  **THEN** `isShielded = false`로 전환된다. 보호 중에도 스탯은 계속 드리프트했으므로, 그 시점 세 스탯이 모두 안전 구간 안이면 Healthy로, 하나라도 구간 밖이면 즉시 AtRisk로 전이된다.

### 공식 검증

- **AC-F-01 (클램프 상한)** **GIVEN** `V=85` **WHEN** `Δ=+30` 적용 **THEN** `V' = clamp(115,0,100) = 100`.
- **AC-F-02 (클램프 하한)** **GIVEN** `V=10` **WHEN** `Δ=-30` 적용 **THEN** `V' = clamp(-20,0,100) = 0`.

- **AC-F-03 (수분 안전 구간)** **GIVEN** 수분 = 20 **WHEN** `IsInBand`/`overshoot` 평가 **THEN** `IsInBand = false`(`SAFE_MIN=25` 미만), `overshootMoisture = 5`.
- **AC-F-04 (습도 안전 구간·스탯별 반폭 차이 확인)** **GIVEN** 습도 = 68 **WHEN** 평가 **THEN** `IsInBand(습도) = false`(`SAFE_MAX=65` 초과), `overshootHumidity = 3`. 동일 절대값 68이 수분이었다면 `IsInBand(수분) = true`(수분 `SAFE_MAX=75`) — 같은 드리프트가 스탯마다 다르게 판정됨을 확인.
- **AC-F-05 (경계값 포함 여부)** **GIVEN** 온도 = 30(정확히 `SAFE_MIN`) **WHEN** `IsInBand` 평가 **THEN** `IsInBand = true`(폐구간, 경계 포함).

- **AC-F-06 (활성 날씨 감쇠율)** **GIVEN** 수분=50, 날씨=맑음(수분 겨냥) **WHEN** 1시간 경과 **THEN** 수분 = `clamp(50-2.5,0,100) = 47.5`(`ACTIVE_RATE` 적용).
- **AC-F-07 (기본 엔트로피)** **GIVEN** 습도=50, 날씨=맑음(습도 비겨냥) **WHEN** 10시간 경과 **THEN** 습도 = `clamp(50+0.5×10,0,100) = 55`(`BASELINE_RATE` 적용).
- **AC-F-08 (기본 방향-활성 날씨 방향 일치)** **GIVEN** 스탯이 비겨냥 상태로 자연 드리프트 중 **WHEN** 그 스탯을 겨냥하는 날씨가 활성화된다 **THEN** 드리프트 부호는 그대로, 크기만 `BASELINE_RATE`→`ACTIVE_RATE`로 커진다(방향 역전 없음).

- **AC-F-09 (터치 보정 — 원자적 적용)** **GIVEN** 수분 `V=15`(안전 구간 밖, `overshootMoisture = 10`) **WHEN** 물뿌리개 완료 **THEN** `V' = clamp(45,0,100) = 45`, `overshootMoisture = 0`.
- **AC-F-10 (진행 중 조정 없음 — 원자성)** **GIVEN** 제스처 진행률 0→99% **WHEN** 완료 이벤트 미발생 **THEN** 스탯 변화 없음(진행률 비례 조정 없음).
- **AC-F-11 (습도 최댓값 근처 루즈엔드 — 회귀 기준선)** **GIVEN** 습도 = 100 **WHEN** 창문 드래그 1회 완료(`TOUCH_Δ=30`) **THEN** `V' = clamp(70,0,100) = 70`, `overshootHumidity = 5`(안전 구간 [35,65] 밖에 남음) — GDD가 명시한 "알려진 루즈엔드"와 일치해야 정상.

- **AC-F-12 (단일 스탯 이탈 시 누적률)** **GIVEN** `overshootMoisture=15`, 나머지 0, `isShielded=false` **WHEN** `dN/dt` 평가 **THEN** `dN/dt = NEGLECT_K(0.5) × 15 = 7.5`.
- **AC-F-13 (복수 스탯 가산 확인)** **GIVEN** `overshootMoisture=15`, `overshootHumidity=15` 동시, `isShielded=false` **WHEN** `dN/dt` 평가 **THEN** `dN/dt = 0.5×(15+15) = 15`(단일 스탯 대비 정확히 2배).
- **AC-F-14 (오프라인 사다리꼴 닫힌 형식)** **GIVEN** 수분이 `t=10h`에 이탈(`overshoot=0`)→`t=14h`까지 선형으로 `overshoot=10`까지 드리프트(`Δt=4h`, 단일 스탯, `isShielded=false`) **WHEN** 배치 계산 **THEN** `ΔN = 0.5×((0+10)/2)×4 = 10`.

- **AC-F-15 (감소 개시 및 목표값)** **GIVEN** `neglectMeter=10`, 세 스탯이 방금 동시에 구간 복귀(`N₀=10`) **WHEN** 24분(0.4h) 경과 **THEN** `neglectMeter = max(0, 10 - NEGLECT_DECAY(25)×0.4) = 0`.
- **AC-F-16 (0에서 클램프)** **GIVEN** `neglectMeter=5` **WHEN** 1시간 경과(`25×1=25 > 5`) **THEN** `neglectMeter = max(0, 5-25) = 0`(음수 불가).

- **AC-F-17 (2x/일 체크인은 항상 안전 — 최악 스탯 기준)** **GIVEN** 습도를 겨냥하는 날씨(폭우/안개)가 12시간 무기한 지속, 개입 없음, `Healthy·N=0·IDEAL=50`에서 시작(3개 스탯 중 습도가 최악 시나리오) **WHEN** 12시간 경과 시점 확인 **THEN** `neglectMeter = 22.5`(<100), 상태는 AtRisk 유지 — Wither 미발생. (수분 2.5/100, 온도 10/100은 이보다 여유로워 별도 검증 불필요.)
- **AC-F-18 (24시간 방치 — 수분/맑음)** **GIVEN** 맑음 무기한 지속, 개입 없음, `Healthy·N=0` 시작 **WHEN** 시간 경과 추적 **THEN** `t=10.0h` AtRisk 진입, `t=23.0h` `neglectMeter=100` 도달·Wither 전이 — 경고 구간 13.0시간.
- **AC-F-19 (24시간 방치 — 온도/한파)** **GIVEN** 한파 무기한 지속 **WHEN** 시간 경과 추적 **THEN** `t=8.0h` AtRisk 진입, `t=20.7h` Wither — 경고 구간 12.7시간.
- **AC-F-20 (24시간 방치 — 습도/폭우안개, 가장 타이트)** **GIVEN** 폭우/안개 무기한 지속 **WHEN** 시간 경과 추적 **THEN** `t=6.0h` AtRisk 진입, `t≈18.65h`(18h39m) Wither — 경고 구간 약 12.65시간, 세 스탯 중 가장 이른 시듦.
- **AC-F-21 (24시간 방치 — 수분/폭염, post-MVP, 네 날씨 중 가장 급격)** **GIVEN** 폭염 무기한 지속(`ACTIVE_RATE_HEATWAVE=-4.0`, weather-system.md §4 확정), 개입 없음, `Healthy·N=0` 시작 **WHEN** 시간 경과 추적 **THEN** `t=6.25h` AtRisk 진입, `t≈17.375h` Wither — 경고 구간 약 11.125시간, 네 날씨 중 가장 이른 시듦(습도/폭우안개보다 약 1.3h 빠름).
- **AC-F-22 (`ActivateShield` 단독 호출 — 스탯 무변경 producer-side 계약, 2026-07-18 추가)** **GIVEN** `moisture=40`, `humidity=60`, `temperature=45`, `neglectMeter=30`인 임의 상태 **WHEN** `ActivateShield(SHIELD_DURATION)`이 `ResetToIdeal()` 없이 **단독**으로 호출된다(능동 설치 경로, `greenhouse-shield-system.md`) **THEN** `shieldActiveUntil`만 `t_call+8h`로 갱신되고 `moisture`/`humidity`/`temperature`/`neglectMeter`는 전부 비트단위로 불변 — 스탯 리셋은 `ActivateShield` 자체가 아니라 호출자(`TriggerRecovery`)가 `ResetToIdeal()`로 번들링하는 책임임을 검증(§7). 향후 리팩터링이 리셋을 `ActivateShield` 내부로 옮기면 이 기준이 실패해야 정상.

### 엣지 케이스

- **AC-EC-01 (제스처 중단 시 무효)** **GIVEN** 물뿌리개 40% 진행 중 **WHEN** 완료 이벤트 발생 전 앱 강제 종료/화면 전환 **THEN** 재개 후 해당 스탯값 불변(완료 함수 미호출).
- **AC-EC-02 (오프라인 갭 중 온실막 만료 — 분할 계산)** **GIVEN** 갭 시작 시 `isShielded=true`, 갭 도중 `shieldActiveUntil` 경과, 갭 내내 한 스탯 `overshoot=10` 유지 **WHEN** 배치 계산 실행 **THEN** 만료 전 구간 `ΔN=0`, 만료 후 구간만 `ΔN=0.5×10×(만료후경과시간)` — `shieldActiveUntil`이 분할 지점으로 사용됨.
- **AC-EC-03 (Wither 중 방치 지표 동결)** **GIVEN** 상태=Wither(`neglectMeter=100`) **WHEN** 광고 대기/오프라인으로 임의 시간 추가 경과 **THEN** `neglectMeter`는 정확히 100 유지, 초과 없음.
- **AC-EC-04 (시계 조작 — 음수 경과시간 방지)** **GIVEN** `lastUpdatedUtc=T`, 시계가 `now = T-5h`로 조작 **WHEN** 배치 계산 실행 **THEN** 경과시간 `= max(0, now-T) = 0`, 스탯·`neglectMeter` 모두 불변.
- **AC-EC-05 (신규 세이브 초기값)** **GIVEN** 세이브 데이터 없음(최초 실행) **WHEN** 초기화 **THEN** `moisture=humidity=temperature=50`, `neglectMeter=0`, `shieldActiveUntil`=만료된 값(`isShielded=false`), 상태=Healthy.
- **AC-EC-06 (제스처 스팸 — 쿨다운 없음)** **GIVEN** 온도=100(포화) **WHEN** 난로 롱프레스 연속 3회 즉시 완료 **THEN** 매회 `V'=clamp(130,0,100)=100`, 변화 없음, 페널티도 추가 이득도 없음.

### 시각 계약

- **AC-VA-01 (Wither 최우선)** **GIVEN** `neglectMeter=100`, `overshootHumidity=20`(등 임의 이탈) **WHEN** 포즈 평가 **THEN** Wither-sagging 고정, 다른 조건 무시.
- **AC-VA-02 (전부 구간 안 → Baseline)** **GIVEN** `neglectMeter<100`, 세 overshoot 모두 0 **WHEN** 포즈 평가 **THEN** 일상 케어(Baseline).
- **AC-VA-03 (최대 overshoot 승자)** **GIVEN** `overshootTemperature(low)=8 > overshootHumidity(high)=3`, `overshootMoisture=0` **WHEN** 포즈 평가 **THEN** Cold-curled.
- **AC-VA-04 (동률 타이브레이크 — 습도>온도)** **GIVEN** `overshootHumidity(high) = overshootTemperature(low) = 5`(동률) **WHEN** 포즈 평가 **THEN** Rain-huddled(습도 승).
- **AC-VA-05 (동률 타이브레이크 — 온도>수분)** **GIVEN** `overshootTemperature(low) = overshootMoisture(low) = 5`(동률) **WHEN** 포즈 평가 **THEN** Cold-curled(온도 승) — 두 타이브레이크 결과를 합치면 습도>온도>수분 전순서가 성립.
- **AC-VA-06 ("과잉 케어" 방향은 Baseline)** **GIVEN** `overshootMoisture(high)`가 최대 overshoot **WHEN** 포즈 평가 **THEN** Baseline 유지(전용 포즈 없음) — 습도 low·온도 high도 동일 규칙.
- **AC-VA-07 (Chilled Blush 트리거 및 즉시 해제)** **GIVEN** `overshootTemperature(low) > 0`(Blush=Chilled) **WHEN** 난로 롱프레스 완료(`TOUCH_Δ=+30`) **THEN** 같은 프레임에 `overshootTemperature(low)=0`이 되며 Blush가 즉시 Base로 복귀.
- **AC-VA-08 (`isShielded`는 포즈를 대체하지 않음)** **GIVEN** `isShielded=true`, 보호 중 드리프트로 `overshootHumidity(high)=4` **WHEN** 포즈·오버레이 평가 **THEN** 기저 포즈=Rain-huddled(§1 그대로 계산), 그 위에 온실막 오버레이만 추가 — `isShielded`가 포즈를 Baseline으로 강제하지 않는다.
- **AC-VA-09 (Heatwave-drooping 라벨 — 렌더 분기 아님)** **GIVEN** `overshootMoisture(low)=12.5`(수분 반폭 25의 정확히 절반) **WHEN** `b=clamp(12.5/25,0,1)=0.5` 계산 **THEN** `b ≥ HEATWAVE_LABEL_RATIO(0.5)`이므로 라벨 적용(QA 표기용, 별도 렌더링 분기·스탯 변화 없음).
- **AC-VA-10 (OnRecovered 후 즉시 Baseline 재평가)** **GIVEN** 상태=Wither **WHEN** `ResetToIdeal()+ActivateShield()` 실행 **THEN** `OnRecovered` 1회 발생, 세 스탯=50→§1 재평가 결과=Baseline(온실막 오버레이는 §4에 따라 유지).

### 이 GDD 범위 밖으로 명시적으로 남긴 것 (테스트 불가 항목)

- **Rewind 트리거 자체**(광고 시청 완료 또는 무료 체류 임계값 도달)는
  `wither-crisis-recovery-system.md`(및 그 하위의 아직 미설계 부활 광고 시스템)
  소관 — AC-ST-06은 `ResetToIdeal()+ActivateShield()` 호출 시점부터만 검증한다.
- **터치 제스처의 "중단" 판정 로직**은 터치 케어 인터랙션 시스템 소관 — 이 시스템은 완료
  이벤트만 구독하므로 AC-EC-01은 그 경계까지만 검증한다.
- **폭염(Heatwave)·날씨 전환 경계의 감쇠율**은 Open Questions에 남은 미확정 값이라, 확정된
  맑음·한파·폭우안개 세 날씨(AC-F-18~20)로만 정합성 검증을 한정했다. 날씨 시스템 GDD가
  값을 확정하면 대응 AC를 추가해야 한다.
- **화면 렌더링 자체**는 아직 작성되지 않은 표정/애니메이션 시스템 GDD 소관 — AC-VA
  시리즈는 이 시스템이 노출하는 값이 우선순위 규칙에 따라 올바르게 판정되는지의 데이터
  계약 테스트이지, 픽셀 단위 렌더 검증이 아니다.

## Open Questions

| 질문 | 담당 | 목표 시점 |
|---|---|---|
| ~~폭염(Heatwave)의 정확한 `ACTIVE_RATE`는?~~ **해결됨(weather-system.md §4, 2026-07-12)**: `ACTIVE_RATE_HEATWAVE=-4.0 pts/hr`(수분 겨냥), 시듦 시각 17.375h — AC-F-21로 등록 | — | 해결됨 |
| ~~날씨가 전환되는 순간 감쇠율이 즉시 바뀌는지, 완만히 보간되는지?~~ **해결됨(weather-system.md §5, 2026-07-12)**: 즉시 전환(계단함수) 확정 — §2·§4 공식 무변경으로 보존됨 | — | 해결됨 |
| "과잉 케어" 방향(수분 과다/습도 과소/온도 과다)에 전용 표현을 추가할지 — 현재는 Baseline 유지로 확정(Pillar 3, 사용자 승인) | art-director | 낮은 우선순위, 향후 아트 바이블 개정 검토 |
| 습도가 절대 최댓값(100) 근처일 때 창문 드래그 1회로 안전구간까지 완전 복귀하지 못하는 루즈엔드(Formulas §3) — 실무 영향은 미미(Wither가 먼저 트리거) | systems-designer | 플레이테스트/밸런스 튜닝 단계 |
