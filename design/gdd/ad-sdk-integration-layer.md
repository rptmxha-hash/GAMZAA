# 광고 SDK 통합 계층 (Ad SDK Integration Layer)

> **Status**: In Design
> **Author**: user + systems-designer
> **Last Updated**: 2026-07-11
> **Implements Pillar**: Pillar 3 (벌주지 않는 힐링)

## Overview

광고 SDK 통합 계층은 부활·가챠·성장 부스터 세 광고 트리거가 실제 광고 네트워크(브랜드
미지정)와 대화하기 위한 단일 관문이다. 이 시스템은 세 상위 시스템 각각이 저마다 다른
SDK 호출 방식을 알 필요 없이, "이 종류의 광고를 요청한다 → 준비됐다/실패했다/사용자가
끝까지 봤다/중간에 닫았다"는 하나의 통일된 계약만 알면 되게 만든다. 이 계층이 없으면
세 시스템 모두가 각자 SDK 세부사항(네트워크 초기화, 재시도, 타임아웃)을 중복 구현해야
하고, 향후 SDK를 교체하거나 미디에이션을 추가할 때 세 곳을 전부 고쳐야 한다.

## Player Fantasy

이 시스템 자체에는 고유한 플레이어 판타지가 없다 — 플레이어는 광고 SDK가 어떻게
작동하는지 결코 의식하지 않는다. 대신 이 시스템은 Pillar 3(벌주지 않는 힐링)이
광고라는, 본질적으로 침입적일 수 있는 매체를 통해서도 성립하기 위한 전제조건이다:
광고 로드가 실패하거나 네트워크가 불안정해도 플레이어가 "광고 때문에 감자를 못
살렸다"는 좌절을 겪지 않아야 한다. 이 시스템이 잘 만들어졌다는 증거는 광고가 있다는
사실 자체가 매끄럽게 녹아들어, 실패했을 때조차 플레이어가 시스템 탓이 아니라 "잠깐
다시 해볼까" 정도로만 느끼는 것이다.

> *(`creative-director` 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 lean에서도 상담 필수.)*

### Core Rules

1. **종류별 요청 계약**: 상위 시스템(부활/가챠/부스터)은 광고 종류와 콜백만 넘기고 SDK
   세부사항을 모른다 — `RequestAd(adType, onReady, onFailed)`.
2. **사전 로드**: 광고는 필요한 순간이 아니라 미리 백그라운드에서 로드해 둔다 — 위기
   (시듦) 상황에서 로딩 지연 자체가 Pillar 3 위반이 될 수 있음.
3. **표시 및 결과 콜백**: `ShowAd(adType)` 호출 시 실제 광고가 뜨고, 완료/중도이탈/실패
   세 결과를 콜백으로 전달한다.
4. **빠른 실패 신호**: 오프라인이거나 로드 실패 시 무한 대기 없이 신속히 실패를 알려
   상위 시스템이 대안을 취하게 한다.
5. **단일 활성 광고 원칙**: 한 번에 하나만 활성 — 동시 요청은 큐잉하거나 거부.

### States and Transitions

| 상태 | 진입 조건 | 이탈 조건 |
|---|---|---|
| **Idle** | 초기 상태, 준비된 광고 없음 | 사전 로드 시작 → Preloading |
| **Preloading** | 백그라운드 로드 요청 | 성공 → Ready / 실패 → LoadFailed |
| **Ready** | 로드 완료, 표시 가능 | `ShowAd()` 호출 → Showing |
| **Showing** | 광고 표시 중 | 완료/중도이탈/오류 → Cooldown |
| **Cooldown** | 광고 소비 직후 | 다음 사전 로드 자동 트리거 → Preloading |
| **LoadFailed** | 로드 실패 | 재시도(Edge Cases) → Preloading 또는 계속 실패 상태 유지 |

### Interactions with Other Systems

| 시스템 | 관계 | 인터페이스(잠정) |
|---|---|---|
| 부활 광고 시스템 | 이 시스템에 의존(호출자) | `RequestAd("revive")`, `ShowAd("revive")` |
| 가챠/뽑기 시스템 | 이 시스템에 의존(호출자) | `RequestAd("gacha")`, `ShowAd("gacha")` |
| 성장 부스터 시스템 | 이 시스템에 의존(호출자) | `RequestAd("booster")`, `ShowAd("booster")` |
| 온실막 보호 | 이 시스템에 의존(호출자) | `RequestAd("shield")`, `ShowAd("shield")` — 온디맨드(사전로드 없음), `RANK_SHIELD=3` 최저(`greenhouse-shield-system.md` 확정, 2026-07-18) |

## Formulas

> *(`systems-designer` 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션.)*

이 시스템의 공식들은 게임플레이 밸런스가 아니라 **가용성(availability)과 응답성
(responsiveness)**을 위한 것이다. Pillar 3(벌주지 않는 힐링) 관점에서, 광고가
필요한 순간 준비돼 있지 않은 것 자체가 이 시스템이 낼 수 있는 최악의 실패이므로,
아래 모든 상수는 `save-persistence-system.md`가 데이터 유실 방지에 편향됐던 것과
같은 정신으로 "준비 완료 시점을 최대한 앞당기고, 실패는 최대한 빨리 알린다" 쪽으로
치우쳐 설계됐다. 단, 광고 네트워크의 "채움 실패(no fill)"는 로컬 파일 I/O의 일시적
잠금과 근본적으로 다른 문제라는 점(§3)이 이 시스템 고유의 설계 포인트다 — 그 차이를
무시하고 세이브 시스템의 재시도 프로파일을 그대로 가져오면 안 된다.

### 1. 부활(Revive) 광고 사전로드 트리거 — `neglectMeter` 임계값

**규칙**: `revive` 타입은 항상 사전로드해두지 않는다(대부분의 AtRisk 에피소드는
`NEGLECT_DECAY=25/hr`로 금방 자가치유되므로 상시 로드는 네트워크·배터리 낭비) —
그렇다고 `neglectMeter=100`(Wither 순간)까지 기다리지도 않는다(Core Rule 2 정면
위반). 대신 `potato-stat-system.md`가 이미 노출하는 `neglectMeter`가 "일시적 방문이
아니라 진짜 위기로 굳어지고 있다"고 판단할 만한 지점을 넘는 순간 사전로드를
발행한다.

`TriggerRevivePreload(N, S) = (N ≥ θ_REVIVE) ∧ (S ∈ {Idle, LoadFailed})`

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 방치 지표 | `N` | float | 0–100 | `potato-stat-system.md` Formulas §4가 노출하는 `neglectMeter`, 이 시스템은 읽기만 함 |
| 현재 상태 | `S` | enum | {Idle, Preloading, Ready, Showing, Cooldown, LoadFailed} | 이 GDD의 States and Transitions 표에서 그대로 가져옴 |
| 사전로드 임계값 | `θ_REVIVE`(`REVIVE_PRELOAD_THRESHOLD`) | int (const) | **40** | 이 값을 넘는 순간 `RequestAd("revive")` 자동 발행 |
| 결과 | `TriggerRevivePreload` | bool | {0,1} | true면 Idle 또는 LoadFailed → Preloading 전이(§5 슬롯 중재 대상) |

**Output Range:** 불리언 게이트. `θ_REVIVE=40`은 `potato-stat-system.md` §4의
닫힌 형식 `N(τ) = NEGLECT_K · ACTIVE_RATE · τ²/2 = 0.625τ²`(τ = AtRisk 진입 후
경과시간)가 세 스탯 모두에 대해 **동일한 형태**라는 사실에 근거한다 — `NEGLECT_K`·
`ACTIVE_RATE`가 스탯 공유 상수이기 때문이며, 이 관계는 `N ≤ 62.5`(수분이 최초로
물리적 극값에 도달하는 τ=10h 시점) 구간에서 스탯 무관하게 성립한다. `θ_REVIVE=40`은
이 구간 안에 있으므로, **어느 스탯이 위기를 이끌든** 트리거 시각은 `τ = √(40/0.625)
= 8.0h`로 동일하다.

**Example:** 습도/폭우안개 최악 시나리오(potato-stat-system.md §6, AtRisk 진입
τ=0 ≡ 절대시각 t=6h, Wither까지 τ=12.65h). `τ=8.0h`(절대 t=14h) 시점에
`N(8.0)=0.625×64=40` → `TriggerRevivePreload=true` → `RequestAd("revive")` 발행.
Wither까지 남은 시간 = `12.65-8.0 = 4.65h`(4시간 39분) — 이는 §3 최악 재시도
소요시간(12.5분)의 **약 22배** 여유. 온도(4.7h)·수분(5.0h) 시나리오는 이보다 더
여유롭다 — 습도가 항상 가장 타이트한 바인딩 케이스.

*Design note*: 이미 `Preloading`/`Ready`/`Showing`/`Cooldown` 상태에서는 이 조건이
재평가돼도 아무 효과가 없다(`S ∈ {Idle, LoadFailed}` 밖이므로) — 별도의 엣지 트리거
로직 없이 매 `neglectMeter` 갱신 틱마다 그냥 평가해도 안전하다(자연히 멱등).
`Ready` 상태에서 광고 인벤토리 자체가 시간이 지나며 만료되는 실제 SDK 동작(대부분
1~4시간)은 이 GDD가 아직 다루지 않는다 — Open Questions로 남길 항목.

---

### 2. 로드 워치독 타임아웃 (Fast-Fail 상한)

**규칙**: 실제 SDK는 보통 자체 타임아웃을 갖지만, 그 콜백(`onReady`/`onFailed`)이
버그·행(hang)으로 영영 발화하지 않는 경우에 대비해 이 시스템 자체가 상한을 하나
더 둔다. 이 타임아웃은 SDK 브랜드에 무관하게 세 타입 모두 동일하게 적용한다
(SDK-agnostic 원칙 유지).

`LoadTimedOut(t) = dispatched ∧ (S = Preloading) ∧ ((t - t_loadStarted) ≥ T_LOAD_TIMEOUT_SEC)`

**정밀화(qa-lead 검토 반영, 2026-07-11)**: `Preloading` 상태는 실제로 두 하위 국면을
포함한다 — (a) `Q(t)`에 편입되어 §5의 로딩 슬롯이나 SDK 초기화를 기다리는 국면과,
(b) 슬롯을 획득해 실제로 SDK에 요청이 전달되고 `t_loadStarted`가 설정된 국면. 이
워치독은 오직 (b)에서만 카운트를 시작한다 — `dispatched=false`(아직 (a) 국면)인
동안에는 아무리 오래 대기해도 `LoadTimedOut`이 발화하지 않는다. 이 가드가 없으면
큐에서 대기 중인 요청이 `t_loadStarted` 미설정(기본값)으로 인해 즉시 오발화할
위험이 있다.

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 현재 시각 | `t` | float (UTC epoch sec) | ≥0 | 벽시계 시각 |
| 로드 시작 시각 | `t_loadStarted` | float (UTC epoch sec) | ≥0 | SDK에 로드 요청을 보낸 시각 |
| 워치독 상한 | `T_LOAD_TIMEOUT_SEC` | float (const, sec) | **15** | 이 시간 안에 SDK 콜백이 없으면 강제로 실패 처리 |
| 디스패치 여부 | `dispatched` | bool | {0,1} | `t_loadStarted`가 실제로 설정됐는가(§5에서 슬롯을 획득해 SDK에 요청을 보낸 순간 true) |
| 결과 | `LoadTimedOut` | bool | {0,1} | true면 `onFailed(reason=timeout)`을 이 시스템이 직접 합성해 발행, `Preloading → LoadFailed` |

**Output Range:** 불리언 게이트. 최악의 "무한 대기" 윈도우가 정확히 15초로 상한
고정된다 — 실제 모바일 광고 SDK의 일반적 자체 타임아웃(대략 10~30초대)보다 짧지
않으면서, 정상적으로 느린 네트워크에서의 성공 로드(수 초~10초대)를 성급하게 죽이지도
않는 절충값. 15초는 §5의 단일 로딩 슬롯이 한 요청 때문에 막혀 있을 수 있는 최대
시간과도 직결되므로, 이 값이 커질수록 다른 두 타입의 대기 상한(§5)도 함께 늘어난다.

**Example:** `revive` 사전로드 요청이 `t=100.0s`에 SDK로 전달됨. 네트워크 스택
행(hang)으로 SDK가 `onReady`도 `onFailed`도 영영 호출하지 않음. 워치독이
`t=115.0s`(100+15)에 발화 → `LoadTimedOut=true` → 이 시스템이 합성 실패를 발행,
`Preloading → LoadFailed` 전이, §3 재시도 백오프 시작.

---

### 3. 로드 실패 재시도 백오프 (광고 네트워크 전용)

**규칙**: `save-persistence-system.md` §3의 파일 I/O 재시도(수백ms~수초 단위)와는
전제 자체가 다르다 — 파일 잠금은 보통 짧은 경합이라 촘촘한 재시도가 통하지만,
광고 네트워크의 "지금 채울 광고가 없음(no fill)"은 그 네트워크의 인벤토리가 실제로
보충돼야 풀리는 상태라 초 단위 재시도로는 해소되지 않는다. 촘촘한 재시도는 (a)
배터리·네트워크만 낭비하고 (b) 대부분의 광고 SDK 이용약관이 금지하는 과도한
재요청 패턴에 해당할 위험이 있다. 그래서 분 단위 지수 백오프를 쓴다.

`delay(n) = min(T_BASE_AD · M_AD^(n-1), T_CAP_AD)`, `n = 1 … N_MAX_AD`

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 재시도 순번 | `n` | int | 1–`N_MAX_AD` | 몇 번째 재시도인지 |
| 기본 지연 | `T_BASE_AD` | float (const, sec) | **30** | 첫 재시도 전 대기 |
| 배수 | `M_AD` | float (const) | **2** | 매 재시도마다 2배 |
| 지연 상한 | `T_CAP_AD` | float (const, sec) | **300**(5분) | 대기 시간 최댓값 |
| 최대 재시도 횟수 | `N_MAX_AD` | int (const) | **5** | 이 시점까지 실패하면 "능동 재시도 에피소드"를 종료 |
| 결과 | `delay(n)` | float (sec) | 0–300 | n번째 재시도 전 대기 시간 |

**시퀀스:** `delay(1..5) = 30, 60, 120, 240, 300`초 → 총 6회 시도(초기 1 + 재시도
5), 최악 누적 대기 = `30+60+120+240+300 = 750초(12분 30초)`.

**Output Range:** 능동 재시도 국면은 최대 12.5분으로 상한이 걸린다. `N_MAX_AD`
소진 후에는 별도의 타이머 메커니즘을 새로 두지 않는다 — States and Transitions
표가 이미 잠근 문구("재시도(Edge Cases) → Preloading 또는 계속 실패 상태 유지")
그대로, `LoadFailed`는 §1의 `TriggerRevivePreload(N,S)` 조건에 계속 포함되므로
(`S ∈ {Idle, LoadFailed}`), 다음 `neglectMeter` 갱신 틱에서 조건이 여전히 참이면
자동으로 새 시도(`n`이 1부터 재시작)가 발행된다 — 위기가 실제로 진행 중인 한
"조용히 포기"하는 경로는 없다.

**Example:** `revive` 로드가 `no fill`로 5회 연속 실패(`n=1..5`). 경과: `t=0`
(초기 실패) → `delay(1)=30s` 대기 → 재시도(`t=30`) 실패 → `delay(2)=60s` →
재시도(`t=90`) 실패 → `delay(3)=120s` → 재시도(`t=210`) 실패 → `delay(4)=240s` →
재시도(`t=450`) 실패 → `delay(5)=300s`(cap) → 재시도(`t=750`) 실패 →
`N_MAX_AD(5)` 소진, `LoadFailed` 유지. §1 예시에서 트리거 시점의 잔여 버퍼가
4.65시간(279분)이었으므로, 이 최악의 12.5분짜리 소진 에피소드를 겪어도 여전히
266분 이상의 여유가 남는다.

*Design note*: 지터(jitter) 미적용 — `save-persistence-system.md` §3과 동일한
이유로, 여러 클라이언트가 같은 자원을 두고 경쟁하는 상황(thundering herd)이 아니라
단일 클라이언트가 하나의 광고 네트워크에 순차 요청하는 구조라 불필요.

---

### 4. Cooldown → 다음 사전로드 재시작 지연

**규칙**: 광고 소비(완료/중도이탈/오류 무관) 직후 `Cooldown`에 진입하면, 짧은
기술적 유예 후 **조건 없이** 같은 타입의 다음 사전로드를 자동 발행한다 — 준비된
광고가 남아돌아도 플레이어에게 아무 비용이 없으므로(Pillar 3), `neglectMeter`가
그새 임계값 아래로 내려갔는지 등은 재확인하지 않는다. `revive`는 유예를 최소화한다
— 감자가 여러 개인 세이브에서 같은 세션 안에 다른 감자가 곧바로 다시 Wither에
진입할 수 있기 때문이다. `gacha`/`booster`는 세션 자체가 자연스럽게 벌어지는 편이라
유예를 조금 더 넉넉히 둬도 체감 차이가 없다.

`NextPreloadTrigger(adType, t_showEnded) = t_showEnded + T_COOLDOWN(adType)`

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 광고 소비 종료 시각 | `t_showEnded` | float (UTC epoch sec) | ≥0 | 완료/중도이탈/오류 콜백이 도착한 시각 |
| 쿨다운 지속시간 | `T_COOLDOWN(adType)` | float (const, sec, 타입별) | 아래 표 | SDK 측 광고 뷰 해제(dismiss teardown) 직후 재요청 시 발생할 수 있는 재진입 이슈를 피하기 위한 최소 기술적 유예 |
| 결과 | `NextPreloadTrigger` | float (UTC epoch sec) | ≥`t_showEnded` | 이 시각에 무조건 `Cooldown → Preloading` 자동 전이 |

| 타입 | `T_COOLDOWN` | 근거 |
|---|---|---|
| `revive` | **2초**(`T_COOLDOWN_REVIVE`) | 세 타입 중 가장 짧게 — 같은 세션 내 재발 가능성이 가장 높음(다른 감자가 곧 Wither 진입할 수 있음) |
| `gacha`, `booster` | **5초**(`T_COOLDOWN_STANDARD`, 공용) | 시간 압박이 없어 SDK 뷰 해제가 완전히 끝날 여유를 조금 더 줌 — 어차피 자연 세션 간격(분~시간 단위)에 비하면 무시할 수준 |

**Output Range:** 항상 `t_showEnded` 이후 2~5초 사이에 확정적으로 발생 — 플레이어
체감상 사실상 즉시(사람이 인지 가능한 세션 간격보다 훨씬 짧음). 실제 로딩 시작
여부(슬롯 획득)는 §5의 우선순위 중재를 따른다.

**Example:** `revive` 광고가 `t=500.0s`에 시청 완료(`OnComplete`) → `Cooldown`
진입 → `t=502.0s`(500+2)에 무조건 `RequestAd("revive")` 재발행, 이 시점
`neglectMeter`가 이미 임계값 아래로 떨어졌어도(다른 감자를 돌봐서 위기가 해소됐어도)
재확인 없이 그대로 발행된다. `gacha`가 `t=700.0s`에 소비됐다면 `t=705.0s`(700+5)에
재발행.

---

### 5. 동시 요청 중재 — 단일 로딩 슬롯 우선순위 큐

**Core Rule 5 해석 확정(사용자 확정, 2026-07-11)**: 잠긴 규칙 "단일 활성 광고
원칙"은 원문상 "동시에 **표시되는** 광고는 하나"만 명시했으나, 이 GDD는 이를
**로딩 중인 사전로드에도 확장 적용**하기로 확정한다 — "표시(Showing)"뿐 아니라
"로딩(Preloading)" 슬롯도 전역적으로 하나만 허용한다(3개 타입 동시 로딩 미허용).
근거: (1) 솔로 개발 스코프에서 SDK 동시 호출 관리 복잡도 회피, (2) 앱 시작
직후처럼 여러 시스템이 동시에 요청해도 순차 처리해도 수 초 내 전부 준비 완료돼
체감 이득이 거의 없음, (3) 많은 SDK 미디에이션 레이어가 내부적으로 어차피 순차
처리함.

`Rank(adType)`: `revive=0`(최고), `gacha=1`, `booster=2`, `shield=3`(최저) — 낮을수록 우선.

`Winner(t) = argmin_{r∈Q(t)} (Rank(r.adType), r.t_Requested)`, 단
`LoadingSlotBusy(t)=false`일 때만 즉시 슬롯 배정

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 대기 요청 집합 | `Q(t)` | set of request | — | 슬롯을 기다리는 미해결 사전로드 요청들 — §1의 `TriggerRevivePreload`, gacha/booster 상위 시스템의 `RequestAd` 호출, **§4의 쿨다운 자동 재발행(3개 타입 전부)** 중 어느 경로로든 생성됨(Core Rule 1의 단일 진입점 원칙상 출처 무관하게 동일하게 `Q(t)`에 편입) |
| 우선순위 | `Rank(adType)` | int (const) | {0,1,2} | 낮을수록 먼저 슬롯을 차지 — 아래 표 |
| 요청 시각 | `t_Requested` | float (UTC epoch sec) | ≥0 | 동순위 타이브레이크용(같은 rank는 없음, FCFS 안전망일 뿐) |
| 슬롯 점유 여부 | `LoadingSlotBusy(t)` | bool | {0,1} | `∃ adType: S(adType)=Preloading`이면 true |
| 선정 결과 | `Winner(t)` | request ∪ {null} | — | 다음에 슬롯을 차지할 요청. `Q(t)=∅`이거나 슬롯이 이미 점유 중이면 `null` |

| Rank | 타입 | 근거 |
|---|---|---|
| 0(최고) | `revive` | Wither 위기와 직결되는 유일한 타입 — 실패가 곧 Pillar 3 정면 위반으로 이어질 수 있음 |
| 1 | `gacha` | 플레이어가 능동적으로 반복 요청하는 참여 루프 — booster보다 세션당 소비 빈도가 높을 것으로 가정(가챠 시스템 GDD 작성 시 재확인 필요, Open Questions) |
| 2 | `booster` | 상대적으로 저빈도·저긴급 — 늦게 준비돼도 다음 자연 재시도로 충분히 커버됨 |
| 3(최저) | `shield` | 예방적·무긴급 능동 설치(온실막 보호) — potato-stat이 정상 체크인 시 ≥18.65h 안전을 보장하므로 위기 revive나 기성 수익 루프와 슬롯 경쟁 시 절대 이기지 않아야 함(`greenhouse-shield-system.md` 확정, 2026-07-18) |

**비선점(non-preemption) 규칙**: 이미 로딩 중인 요청은 그보다 높은 우선순위 요청이
도착해도 **중단되지 않는다**(SDK 호출은 대개 깔끔하게 취소할 수 없으므로). 높은
우선순위 요청은 큐의 맨 앞에 대기하다가, 현재 로딩이 끝나는 즉시(성공/실패/§2
워치독 타임아웃 중 어느 쪽이든) 다음 `Winner(t)`로 즉시 선정된다.

**Output Range:** `Winner(t) ∈ Q(t) ∪ {null}`, 동순위가 존재하지 않으므로(3개
타입이 서로 다른 rank) 항상 결정론적으로 유일한 승자가 정해진다. 패자는 드롭되지
않고 큐에 남아 대기한다(드롭 후 재시도 아님 — 별도 재시도 메커니즘이 필요 없어짐).
한 요청의 최대 점유 시간은 §2의 `T_LOAD_TIMEOUT_SEC=15초`로 상한이 걸리므로, 3개
타입이 동시에 몰려도 가장 낮은 우선순위(`booster`)의 최악 대기 시간은
`2 × 15초 = 30초`를 넘지 않는다(앞선 두 요청이 모두 타임아웃까지 가는 최악의
경우).

**Example (백그라운드 복귀, 세이브에 이미 위기 신호 존재):** 앱이 재개(resume)되는
시점, 세이브에 저장된 `neglectMeter=45`(백그라운드 진입 전 이미 §1 임계값을 넘긴
채였음) → §1 조건이 재개 즉시 참 → `revive` 사전로드 요청 발행(`t=0.0`). 마침
같은 프레임에 가챠·부스터 시스템도 각자의 초기화 루틴에서 `RequestAd("gacha")`,
`RequestAd("booster")`를 호출(`t=0.0`). `Q(0)={revive(rank0), gacha(rank1),
booster(rank2)}`, `LoadingSlotBusy=false` → `Winner=revive` → 슬롯 점유, 로딩
시작. `revive`가 `t=2.3s`에 성공(`Ready` 전이) → 슬롯 해제 → 재평가 →
`Winner=gacha`(남은 것 중 최저 rank) → `t=2.3s`부터 로딩 시작, `t=4.1s` 성공 →
슬롯 해제 → `Winner=booster` → `t=4.1s`부터 로딩 시작. 세 타입 모두 앱 재개 후
수 초 내 순차적으로 `Ready` 도달, 어느 시점에도 두 개가 동시에 로딩 상태가 되지
않는다.

**Example (비선점 확인):** `booster`가 `t=10.0s`에 이미 슬롯을 점유해 로딩 중.
`t=11.0s`에 `neglectMeter`가 §1 임계값을 넘어 `revive` 요청이 새로 발생 →
`Q(11.0)={revive(rank0, t=11.0)}`, `LoadingSlotBusy=true` → `booster`의 로딩은
중단되지 않고 계속됨. `booster`가 `t=13.5s`에 성공(또는 §2 워치독으로 `t=25.0s`에
타임아웃) → 슬롯 해제 즉시 `revive`가 `Winner`로 선정되어 로딩 시작 — `revive`의
최대 추가 대기는 `T_LOAD_TIMEOUT_SEC=15초`로 상한.

---

**범위 참고**: `gacha`/`booster`/`shield` 자체가 *언제* `RequestAd()`를 호출할지(그들의
트리거 조건)는 이 GDD의 범위 밖이다 — 각 상위 시스템(가챠/뽑기 시스템, 성장 부스터
시스템, 온실막 보호)의 GDD 소관이며, Interactions with Other Systems 표에 명시돼 있다.
이 Formulas 섹션이 소유하는 것은 (1) `revive`에 한해 이 시스템이 직접 발행하는 유일한
트리거 조건(§1)과, (2) SDK 플러밍(§2·§3·§5, 그리고 §4는 아래 `shield` 예외를 제외한
타입들)뿐이다.

**`shield` 온디맨드 예외(신규, 2026-07-18)**: `shield`(온실막 보호,
`greenhouse-shield-system.md`)는 다른 세 타입과 달리 **순수 온디맨드**다 — Core Rule 2의
상시 사전로드 대상이 아니고(§1 같은 자동 사전로드 트리거를 두지 않음), §4의 소비 후 자동
재사전로드 루프에서도 제외된다. 플레이어가 설치를 탭하는 순간에만 `RequestAd("shield")`가
발행되며, `RANK_SHIELD=3`(최저)으로 §5 슬롯 중재에 참여한다. SDK 뷰 해제 재진입 안전용
쿨다운은 신규 상수 없이 `T_COOLDOWN_STANDARD=5`를 재사용한다.

**상수 요약**(레지스트리 등록 후보): `REVIVE_PRELOAD_THRESHOLD=40`;
`T_LOAD_TIMEOUT_SEC=15`; `T_BASE_AD=30`, `M_AD=2`, `T_CAP_AD=300`, `N_MAX_AD=5`;
`T_COOLDOWN_REVIVE=2`, `T_COOLDOWN_STANDARD=5`; `RANK_REVIVE=0`, `RANK_GACHA=1`,
`RANK_BOOSTER=2`, `RANK_SHIELD=3`.

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드.)*

- **If `Ready` 상태의 광고가 일정 시간 표시되지 않고 방치된다면(SDK 인벤토리 자연 만료,
  보통 1~4시간)**: 만료 감시 타이머를 이 시스템이 별도로 두지 않는다 — 대신
  `ShowAd()` 호출 시 SDK가 만료를 알려주면(그런 콜백이 있다면) 그 즉시 `LoadFailed`로
  전이하고 §3 재시도를 시작한다. SDK가 만료를 알려주지 않는 경우, `ShowAd()` 자체의
  실패로 감지된다.

- **If `revive` 광고가 `Ready` 상태인데 위기가 자연 해소되어(예: 다른 감자를 돌봐
  `neglectMeter`가 임계값 아래로 내려감) 결국 표시되지 않는다면**: 폐기하지 않고
  `Ready` 상태를 그대로 유지한다 — Pillar 3(준비된 광고가 남아도는 것은 비용이 없음,
  §4와 동일 철학). 다음 위기 때 즉시 재사용된다.

- **If 앱이 `Showing` 도중 백그라운드로 전환된다면(전화 수신 등)**: SDK의 자체 처리에
  위임한다 — 대부분의 광고 SDK는 이 경우를 "중도이탈(dismissed)"로 콜백한다. 이
  시스템은 별도 타임아웃을 걸지 않고, SDK 콜백을 그대로 신뢰한다.

- **If SDK가 아직 초기화되지 않은 상태(콜드 스타트 직후)에서 `RequestAd()`가
  호출된다면**: 초기화 완료까지 요청을 큐에 대기시킨다(§5의 `Q(t)`에 편입) — 실패로
  처리하지 않는다. 이는 앱 시작 직후 세 타입이 동시에 요청하는 §5 예시와 자연스럽게
  합쳐진다.

- **If 기기가 오프라인(비행기 모드 등)이라면**: §2의 15초 워치독까지 기다리지 않고,
  네트워크 가용성을 먼저 확인해 즉시 실패 처리한다(빠른 실패 원칙의 직접 적용) —
  네트워크가 복구되면 §3 재시도가 정상 재개된다.

- **If `LoadFailed` 상태에서 `delay(n)` 백오프 대기 중, 상위 시스템이 `RequestAd(adType)`를
  외부에서 호출한다면(예: 부활 광고 시스템의 플레이어 CTA 탭)**: 예약된 백오프 재시도를
  기다리지 않고 즉시 새 로드 시도를 시작하며, 재시도 순번 `n`을 **1로 리셋**한다(새 재시도
  에피소드로 취급). 활동 중인 플레이어의 명시적 요청이 `N_MAX_AD(5)` 소진으로 락아웃되지
  않도록 하기 위함 — "위기가 진행 중인 한 조용히 포기하는 경로는 없다"는 §3 철학과 일치.
  이중 로드는 발생하지 않는다: 호출자 측 단일 진행 가드(예: 부활 광고 시스템 Core Rule 6)가
  완료/타임아웃까지 재호출을 막으므로 같은 adType의 `S=LoadFailed` 가드가 동시에 두 번
  평가되지 않는다(`revive-ad-system.md` 확정, 2026-07-18).

## Dependencies

**이 시스템이 의존하는 것**: 없음 — Foundation, 시스템 인덱스와 일치. 단 Formulas §1이
`potato-stat-system.md`의 `neglectMeter`를 **읽기 전용**으로 참조하는 소프트 계약이
존재한다(`save-persistence-system.md`와 동일 패턴 — 인덱스 그래프에는 화살표로 표현되지
않지만 두 GDD가 서로를 인용해야 정합성이 유지됨).

**이 시스템에 의존하는 것**:

| 시스템 | 의존 강도 | 인터페이스 |
|---|---|---|
| 부활 광고 시스템 | Hard | `RequestAd("revive")`, `ShowAd("revive")` |
| 가챠/뽑기 시스템 | Hard | `RequestAd("gacha")`, `ShowAd("gacha")` |
| 성장 부스터 시스템 | Hard | `RequestAd("booster")`, `ShowAd("booster")` |

## Tuning Knobs

| 노브 | 현재값 | 권장 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `REVIVE_PRELOAD_THRESHOLD` | 40 | 20~62.5(수분 극값 도달 전) | 위기 초반부터 불필요하게 자주 로드 시도(네트워크·배터리 낭비) | Wither까지 남은 여유 시간이 줄어 §3 재시도 소진과 위기 도달이 근접해질 위험 |
| `T_LOAD_TIMEOUT_SEC` | 15 | 10~30 | 정상적으로 느린 네트워크의 성공 로드를 성급하게 실패 처리 | §5 슬롯 점유 최대 시간 증가, 낮은 우선순위 타입 대기 증가 |
| `T_BASE_AD`/`M_AD`/`T_CAP_AD`/`N_MAX_AD` | 30/2/300/5 | 총 재시도 시간 5~20분 유지 | 광고 SDK 이용약관상 과도한 재요청 패턴 위험 | 능동 재시도 국면이 길어져 §1의 여유 버퍼를 잠식 |
| `T_COOLDOWN_REVIVE` | 2 | 1~5 | SDK 뷰 해제 미완료 상태에서 재요청 시 재진입 오류 위험 | 연속 위기 상황에서 다음 부활 광고 준비가 늦어짐 |
| `T_COOLDOWN_STANDARD` | 5 | 3~10 | 상동(재진입 오류 위험) | 체감 차이 없음(자연 세션 간격이 훨씬 김) — 안전하게 높여도 무방 |
| `Rank(adType)` 순서 | revive>gacha>booster | 변경 시 §5 예시 재검증 필요 | (해당 없음 — 순서형 노브) | 가챠/부스터 GDD 작성 후 실제 소비 빈도가 다르게 확인되면 재조정 |

## Visual/Audio Requirements

이 시스템은 원칙적으로 무음·무표시다 — SDK가 실제 광고 뷰(외부 크리에이티브)를 자체
렌더링하며, 이 시스템은 그 앞뒤 상태 전환(로딩·준비·표시·소비)만 관리한다. 유일한
예외는 상위 시스템(부활/가챠/부스터)이 "지금 로딩 중"이나 "실패" 상태를 자체 UI로
표현하고 싶을 경우인데, 그 시각 표현 자체는 각 상위 시스템 GDD의 몫이다.

## UI Requirements

이 시스템은 직접적인 UI를 갖지 않는다. 상태(Idle/Preloading/Ready/Showing/Cooldown/
LoadFailed)를 조회하는 API만 노출하며, 그 값을 화면에 어떻게 반영할지는 상위 시스템
(부활 광고·가챠·성장 부스터)의 GDD가 각자 정의한다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션.
> 상담 중 발견된 근본 이슈(`Preloading`이 "큐 대기"와 "SDK 실제 호출 중" 두 국면을
> 암묵적으로 겹쳐 씀)는 Formulas §2에 `dispatched` 가드를 추가하고 §5의 `Q(t)`
> 출처 목록에 §4를 보강해 해소했다.)*

아래 기준은 Detailed Design·Formulas·Edge Cases에서 정의된 필드·상수명을 그대로
사용한다. 이 시스템의 "상태"(Idle/Preloading/Ready/Showing/Cooldown/LoadFailed)는
States and Transitions 표에 정의된 대로 adType(`revive`/`gacha`/`booster`)별로
각각 독립적으로 관리된다 — 세 타입은 각자 자신의 상태를 갖는다.

### 상태 전이

- **AC-ST-01 (초기 상태)** **GIVEN** adType이 초기화됨, 아직 어떤 트리거도 없음 **WHEN** 상태 조회 **THEN** `S(adType) = Idle`.
- **AC-ST-02 (Idle → Preloading, revive 자동 트리거)** **GIVEN** `S(revive)=Idle`, `neglectMeter ≥ θ_REVIVE(40)` **WHEN** `TriggerRevivePreload` 평가 **THEN** `TriggerRevivePreload=true`, `RequestAd("revive")`가 발행되어 `Q(t)`에 편입, `S(revive)=Preloading`.
- **AC-ST-03 (Idle → Preloading, gacha/booster 수동 요청)** **GIVEN** `S(gacha)=Idle`(또는 `booster`) **WHEN** 상위 시스템이 `RequestAd(adType)` 호출 **THEN** 해당 adType이 `Q(t)`에 편입, `S(adType)=Preloading`.
- **AC-ST-04 (Preloading → Ready, SDK 성공)** **GIVEN** `S(adType)=Preloading`, `dispatched=true` **WHEN** SDK가 `onReady` 콜백 **THEN** `S(adType)=Ready`.
- **AC-ST-05 (Preloading → LoadFailed, SDK 실패)** **GIVEN** `S(adType)=Preloading`, `dispatched=true` **WHEN** SDK가 `onFailed` 콜백(no-fill 등) **THEN** `S(adType)=LoadFailed`, §3 재시도 시퀀스(`n=1`) 시작.
- **AC-ST-06 (Preloading → LoadFailed, 워치독 타임아웃)** **GIVEN** `S(adType)=Preloading`, `dispatched=true`, `t - t_loadStarted ≥ T_LOAD_TIMEOUT_SEC(15)`, SDK 콜백 없음 **WHEN** 워치독 평가 **THEN** 이 시스템이 합성 `onFailed(reason=timeout)`을 발행, `S(adType)=LoadFailed`.
- **AC-ST-07 (Ready → Showing)** **GIVEN** `S(adType)=Ready` **WHEN** `ShowAd(adType)` 호출 **THEN** `S(adType)=Showing`.
- **AC-ST-08 (Showing → Cooldown, 결과 무관)** **GIVEN** `S(adType)=Showing` **WHEN** 완료(`OnComplete`)/중도이탈(`OnDismissed`)/오류 중 어느 콜백이든 도착 **THEN** `S(adType)=Cooldown`, 세 결과 모두 동일하게 처리됨(결과에 따라 분기하지 않음).
- **AC-ST-09 (Cooldown → Preloading, 루프 완성)** **GIVEN** `S(adType)=Cooldown`, `t_showEnded` 기록됨 **WHEN** `t ≥ NextPreloadTrigger(adType, t_showEnded)` **THEN** `RequestAd(adType)`가 자동 재발행되어 `Q(t)`에 편입, `S(adType)=Preloading`(§5 중재 대상, 즉시 SDK 디스패치를 보장하지 않음).
- **AC-ST-10 (LoadFailed → Preloading, 재시도 분기)** **GIVEN** `S(adType)=LoadFailed`, 현재 재시도 순번 `n ≤ N_MAX_AD(5)` **WHEN** `delay(n)` 경과 **THEN** `RequestAd(adType)` 재발행, `Q(t)` 재편입, `S(adType)=Preloading`.
- **AC-ST-11 (LoadFailed 고착, 재시도 소진 분기)** **GIVEN** `S(adType)=LoadFailed`, `n=N_MAX_AD(5)`의 마지막 재시도도 실패 **WHEN** 소진 이후 시간 경과 **THEN** 이 시스템 내부에는 이 adType을 위한 별도 타이머가 없으므로 `S(adType)=LoadFailed`로 무기한 고착 — `revive`는 §1의 다음 `neglectMeter` 틱이 `S∈{Idle,LoadFailed}` 가드를 통과해 재개하고, `gacha`/`booster`는 상위 시스템의 다음 `RequestAd()` 호출이 있어야만 재개된다(이 GDD 범위 밖).
- **AC-ST-12 (전역 단일 로딩 슬롯 — 두 adType이 동시에 SDK를 실제 점유할 수 없음)** **GIVEN** `S(A)=Preloading`이고 `dispatched=true`(SDK 실제 디스패치 중) **WHEN** 다른 adType B가 `Winner(t)`로 선정을 시도 **THEN** `LoadingSlotBusy(t)=true`이므로 B는 슬롯을 받지 못하고 `Q(t)`에 대기만 함 — 어느 시점에도 두 adType이 동시에 실제 SDK 호출을 갖지 않는다.
- **AC-ST-13 (Showing과 Preloading은 별개의 슬롯)** **GIVEN** `S(A)=Showing` **WHEN** 동시에 다른 adType B가 `RequestAd(B)` 호출, `LoadingSlotBusy=false`(다른 어떤 adType도 현재 Preloading 아님) **THEN** B는 즉시 슬롯을 받아 `S(B)=Preloading`으로 전이할 수 있다 — Core Rule 5의 "단일 활성" 확장은 Showing과 Preloading 각각에 독립적으로 적용되지, 둘을 하나로 묶은 전역 배타 슬롯이 아니다.

### 공식 검증

**§1 부활 사전로드 트리거 (`TriggerRevivePreload`)**
- **AC-F-01** **GIVEN** `N=39.9`, `S=Idle` **WHEN** 평가 **THEN** `TriggerRevivePreload=false`.
- **AC-F-02 (경계값, 포함)** **GIVEN** `N=40.0`(정확히 `θ_REVIVE`), `S=Idle` **WHEN** 평가 **THEN** `TriggerRevivePreload=true`(`≥` 이므로 경계 포함).
- **AC-F-03 (가드 — 활성 상태 중 재평가 무효)** **GIVEN** `N≥40`, `S∈{Preloading, Ready, Showing, Cooldown}` **WHEN** `neglectMeter` 갱신 틱마다 반복 평가 **THEN** 매번 `TriggerRevivePreload=false`(가드 실패) — 이미 진행 중인 요청과 별개로 중복 `RequestAd("revive")`가 발행되지 않는다. `S=Preloading`은 `Q(t)` 편입 즉시(디스패치 여부 무관) 설정되므로, 이미 대기열에 있는 revive 요청이 있는 상태에서 `neglectMeter`가 재차 임계값을 넘어도 두 번째 요청이 생성되지 않는다.
- **AC-F-04 (LoadFailed에서 재개)** **GIVEN** `S=LoadFailed`(재시도 소진), `N≥40` **WHEN** 다음 `neglectMeter` 갱신 틱 **THEN** `TriggerRevivePreload=true`, 새 재시도 에피소드(`n`이 1부터 재시작) 발행.
- **AC-F-05 (τ=8.0h 시각 재현)** **GIVEN** 습도 위기(폭우/안개) AtRisk 진입(τ=0), `neglectMeter` 공식 `N(τ)=0.625τ²` 적용 **WHEN** `τ=8.0h` **THEN** `N=0.625×64=40`, `TriggerRevivePreload=true`.

**§2 로드 워치독 (`LoadTimedOut`)**
- **AC-F-06** **GIVEN** `S=Preloading`, `dispatched=true`, `t_loadStarted=100.0`, SDK 콜백 없음 **WHEN** `t=115.0`(경과 15s) **THEN** `LoadTimedOut=true`, 합성 `onFailed(reason=timeout)` 발행.
- **AC-F-07 (경계값, 포함)** **GIVEN** 상동 **WHEN** `t - t_loadStarted`가 정확히 `14.999`s **THEN** `LoadTimedOut=false`; `15.000`s **THEN** `LoadTimedOut=true`.
- **AC-F-08 (선-도착 콜백 — 중복 발화 금지)** **GIVEN** `t_loadStarted=100.0` **WHEN** SDK의 실제 `onReady`/`onFailed`가 `t=110.0`(15s 이전)에 도착 **THEN** 정상 콜백만 처리되고, 이후 `t=115.0` 시점에 합성 타임아웃 실패가 중복 발화하지 않는다.
- **AC-F-09 (큐 대기 중에는 워치독 미적용)** **GIVEN** `S=Preloading`이지만 `dispatched=false`(다른 adType이 슬롯 점유 중이라 아직 SDK에 디스패치되지 않음) **WHEN** 큐 대기 시간이 15초를 초과 **THEN** `LoadTimedOut=false`가 유지된다 — 워치독은 `dispatched=true`가 된 이후에만 카운트를 시작한다(정밀화 반영).

**§3 재시도 백오프 (`delay(n)`)**
- **AC-F-10 (시퀀스 값)** **GIVEN** `T_BASE_AD=30, M_AD=2, T_CAP_AD=300` **WHEN** `n=1..5` 계산 **THEN** `delay = 30, 60, 120, 240, 300`초.
- **AC-F-11 (총 시도 횟수·최악 대기)** **GIVEN** 5회 연속 실패(`n=1..5`) **WHEN** 누적 **THEN** 총 6회 시도(초기+5재시도), 누적 대기 `750`초(12분 30초).
- **AC-F-12 (지터 없음 — 결정론적)** **GIVEN** 동일한 실패 시퀀스를 반복 실행 **WHEN** `delay(n)` 비교 **THEN** 매 실행마다 정확히 동일한 값(랜덤 지터 없음) — 프로젝트 테스트 표준의 결정론 요구사항과 일치.
- **AC-F-13 (백오프 대기 중 슬롯 해제 — §5와 합성)** **GIVEN** adType A가 `delay(n)` 대기 중(`S(A)=LoadFailed`) **WHEN** 다른 adType B가 이 시점에 `RequestAd(B)` 호출, `LoadingSlotBusy=false` **THEN** B는 A의 백오프 대기와 무관하게 즉시 슬롯을 받을 수 있다(`LoadingSlotBusy`는 `Preloading`+`dispatched=true` 상태만 카운트, `LoadFailed`는 슬롯을 점유하지 않음).

**§4 쿨다운 재시작 (`NextPreloadTrigger`)**
- **AC-F-14 (revive 쿨다운 값)** **GIVEN** `revive`가 `t_showEnded=500.0`에 소비 완료 **WHEN** `NextPreloadTrigger` 계산 **THEN** `= 500.0 + T_COOLDOWN_REVIVE(2) = 502.0`.
- **AC-F-15 (gacha/booster 쿨다운 값)** **GIVEN** `gacha`가 `t_showEnded=700.0`에 소비 완료 **WHEN** 계산 **THEN** `= 700.0 + T_COOLDOWN_STANDARD(5) = 705.0`.
- **AC-F-16 (무조건 재발행 — neglectMeter 재확인 없음)** **GIVEN** `revive` 쿨다운 종료 시점에 `neglectMeter`가 이미 `θ_REVIVE(40)` 미만으로 떨어짐 **WHEN** `NextPreloadTrigger` 도달 **THEN** 그럼에도 `RequestAd("revive")`가 무조건 재발행된다(§1의 조건 재확인 없음).
- **AC-F-17 (§5 재중재 — 단독 대기여도 arbitration을 거침)** **GIVEN** 쿨다운 종료 시점 `Q(t)`가 비어 있음, `LoadingSlotBusy=false` **WHEN** `NextPreloadTrigger`가 `RequestAd(adType)` 재발행 **THEN** 이 요청도 `Q(t)`에 편입되어 `Winner(t)` 계산을 거친다(단독이라 자명하게 즉시 승자로 선정) — 단순 상태 대입이 아니라 항상 arbitration 함수를 통과함을 확인.
- **AC-F-18 (§5 재중재 — 슬롯 점유 중이면 비선점)** **GIVEN** 쿨다운 종료 시점 다른 adType이 이미 `Preloading`+`dispatched=true`(슬롯 점유 중) **WHEN** `NextPreloadTrigger`가 `RequestAd(adType)` 재발행 **THEN** 이 재발행 요청도 예외 없이 `Q(t)`에서 대기하며, 진행 중인 로딩을 선점하지 않는다(자동 트리거라고 특별 취급되지 않음).

**§5 단일 로딩 슬롯 우선순위 (`Winner`)**
- **AC-F-19 (Rank 순서)** **GIVEN** `Rank(revive)=0, Rank(gacha)=1, Rank(booster)=2` **WHEN** 세 타입이 동시에 `Q(t)`에 존재 **THEN** `Winner(t)=revive`(최저 rank).
- **AC-F-20 (슬롯 점유 중이면 승자 없음)** **GIVEN** `LoadingSlotBusy(t)=true` **WHEN** `Q(t)`에 임의 요청 존재 **THEN** `Winner(t)=null`.
- **AC-F-21 (`Q(t)` 공집합)** **GIVEN** `Q(t)=∅` **WHEN** 평가 **THEN** `Winner(t)=null`.
- **AC-F-22 (비선점)** **GIVEN** `booster`가 `t=10.0`에 이미 슬롯 점유·로딩 중(`dispatched=true`) **WHEN** `t=11.0`에 `revive` 요청이 `Q(t)`에 신규 편입 **THEN** `booster`의 진행 중인 로딩은 중단되지 않고 계속됨.
- **AC-F-23 (비선점 이후 즉시 재중재)** **GIVEN** AC-F-22 상황 지속 **WHEN** `booster`가 성공 또는 §2 워치독으로 슬롯 해제 **THEN** 그 즉시 `Winner(t)=revive`로 재선정되어 로딩 시작(`dispatched=true`로 전환).
- **AC-F-24 (`booster` 최악 대기 상한)** **GIVEN** `revive`, `gacha`, `booster`가 동시에 `Q(t)`에 존재, 앞선 두 요청이 각각 §2 워치독(15초)까지 소진 **WHEN** `booster`의 로딩 시작 시점까지 경과시간 측정 **THEN** `booster`의 실제 SDK 디스패치 시작까지 최대 `2×15=30`초를 넘지 않는다.
- **AC-F-25 (동률 타이브레이크 — 도달 불가 안전망)** **GIVEN** 현재 시스템에 정확히 3개 adType만 존재하며 각기 다른 Rank를 가짐 **WHEN** `Q(t)` 임의 구성 확인 **THEN** 동일 Rank를 가진 두 요청이 동시에 `Q(t)`에 존재하는 경우가 구조적으로 발생하지 않는다 — `t_Requested` 타이브레이크는 현재 타입 셋으로는 도달 불가능한 안전망이며, 신규 adType이 추가될 경우에만 검증이 필요해진다.

### 엣지 케이스

- **AC-EC-01 (인벤토리 만료 — SDK가 신호를 주는 경우)** **GIVEN** `S=Ready`, SDK 인벤토리가 시간 경과로 자연 만료 **WHEN** `ShowAd()` 호출 시 SDK가 만료 콜백 발행 **THEN** 그 즉시 `S=LoadFailed` 전이, §3 재시도 시퀀스 시작.
- **AC-EC-02 (인벤토리 만료 — SDK가 신호를 주지 않는 경우)** **GIVEN** 상동, SDK가 전용 만료 콜백을 제공하지 않음 **WHEN** `ShowAd()` 호출이 일반 실패로 반환 **THEN** 이 실패가 §3 재시도 트리거로 동일하게 처리된다(만료 전용 별도 경로 없음).
- **AC-EC-03 (위기 자연 해소 — Ready 유지)** **GIVEN** `S(revive)=Ready`, 위기가 다른 감자 케어로 자연 해소되어 `neglectMeter < θ_REVIVE` **WHEN** 임의 시간 경과, `ShowAd()` 미호출 **THEN** `S(revive)`는 `Ready`로 유지되며 폐기되지 않고, 다음 위기 시 즉시 재사용 가능.
- **AC-EC-04 (백그라운드 전환 중 Showing)** **GIVEN** `S=Showing` **WHEN** 앱이 백그라운드로 전환(전화 수신 등) **THEN** 이 시스템은 별도 타임아웃을 걸지 않고 SDK의 자체 콜백(대개 `dismissed`)만을 기다려 `Cooldown`으로 전이한다.
- **AC-EC-05 (SDK 미초기화 — 큐잉, 실패 아님)** **GIVEN** SDK가 아직 초기화되지 않음(콜드 스타트 직후) **WHEN** `RequestAd(adType)` 호출 **THEN** 즉시 실패 콜백이 발행되지 않고, 요청이 `Q(t)`에 편입되어(`S=Preloading`, `dispatched=false`) 초기화 완료를 대기한다 — `onFailed`가 합성되지 않으며, §2 워치독도 발화하지 않는다(AC-F-09와 동일 가드 적용).
- **AC-EC-06 (오프라인 — 워치독 우회 즉시 실패)** **GIVEN** 기기가 오프라인(비행기 모드 등) **WHEN** `RequestAd(adType)` 호출 **THEN** 네트워크 가용성 확인만으로 즉시 `onFailed`가 발행되어 `S=LoadFailed` 전이 — `T_LOAD_TIMEOUT_SEC(15초)` 만료를 기다리지 않는다.
- **AC-EC-07 (오프라인 → 온라인 복구)** **GIVEN** AC-EC-06으로 `LoadFailed` 진입, §3 재시도 대기 중 **WHEN** 네트워크가 복구된 상태에서 다음 `delay(n)` 만료 **THEN** 정상적인 §3 재시도가 재개되며(오프라인 이력이 별도로 재시도 횟수를 소모하지 않음), 성공 시 통상 경로대로 `Ready` 도달.

### 이 GDD 범위 밖으로 명시적으로 남긴 것

- **gacha/booster의 자체 사전로드 트리거 조건**은 각 상위 시스템 GDD 소관 — 이 GDD가
  검증하는 것은 그 시스템들이 `RequestAd()`를 호출한 **이후**의 동작뿐이다.
- **실제 SDK 브랜드별 동작**(AdMob vs. Unity LevelPlay 등)은 Open Questions에 남은
  ADR 대상 — 이 GDD의 AC는 SDK-agnostic 계약(콜백 시그니처)만 검증한다.
- **인벤토리 만료를 SDK가 콜백으로 안 주는 경우의 폴링 대안**은 Open Questions
  미확정 항목 — AC-EC-02는 "일반 실패로 감지됨"까지만 검증하고, 폴링 메커니즘
  자체는 검증 대상이 아니다.
- **로딩/실패 상태의 화면 표현**은 상위 시스템(부활/가챠/부스터) GDD 소관 — 이
  시스템은 상태 조회 API만 노출한다.

## Open Questions

| 질문 | 담당 | 목표 시점 |
|---|---|---|
| `gacha > booster` 우선순위(§5 Rank)가 실제 소비 빈도와 맞는지 | systems-designer | 가챠/뽑기·성장 부스터 시스템 GDD 작성 시 재확인 |
| 실제 SDK 브랜드 선택(AdMob vs. Unity LevelPlay 등) — 이 GDD는 SDK-agnostic 유지 | technical-director | 아키텍처 단계(ADR) |
| 광고 인벤토리 만료 감지를 SDK가 콜백으로 안 주는 경우의 대안(폴링 등 필요 여부) | engine-programmer | 실제 SDK 선정 후 |
| gacha/booster 각각의 사전로드 트리거 조건(이 GDD 범위 밖) | game-designer | 각 시스템 GDD 작성 시 |
