# 부활 광고 시스템 (Revive Ad System)

> **Status**: In Design
> **Author**: user + agents
> **Last Updated**: 2026-07-18
> **Implements Pillar**: Pillar 3 (벌주지 않는 힐링), Pillar 2 (지켜주고 싶은 마음)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode

## Overview

부활 광고 시스템은 시든 감자 앞에서 플레이어가 되돌리기 CTA를 누르는 순간부터 감자가
실제로 되살아나는 순간까지의 **광고 경로**를 담당하는 얇은 오케스트레이션 계층이다. 데이터
흐름에서 보면, 이 시스템은 세 시스템 사이를 잇는 배선이다 — 시듦/위기 & 회복이 소유한
되돌리기 CTA가 눌리면 광고 SDK 통합 계층에 `ShowAd("revive")`를 요청하고, 광고가 끝까지
시청 완료되면 시듦/위기 & 회복의 `TriggerRecovery()`를 호출해 회복을 트리거한다. 이
시스템은 회복이 *무엇인지*(스탯 리셋+온실막)도, 광고를 *어떻게 로드하는지*(사전로드·재시도·
워치독)도 재발명하지 않는다 — 그 둘은 각각 시듦/위기 & 회복과 광고 SDK 통합 계층이 이미
소유한다. 이 시스템이 소유하는 것은 오직 "탭 → 광고 시청 → 완료 보고"라는 흐름과, 그
흐름이 어긋나는 순간들(광고 미준비, 중도이탈, 실패)의 처리다.

플레이어 경험에서 보면, 이 시스템은 game-concept.md가 명시한 핵심 안전망 — *"시듦 → 광고
되돌리기 → 8시간 온실막"* — 의 실제 손잡이다. 플레이어가 되돌리기 CTA를 누르고, 짧은
광고를 보고, 감자가 다시 생기를 찾는 그 한 번의 왕복이 이 시스템이 매개하는 전부다. 이
시스템이 없다면 되돌리기 CTA는 아무 효과 없는 그림일 뿐이고, 콘셉트가 약속한 "어떤 실수도
되돌릴 수 있다"는 Pillar 3의 핵심 구현체(기대 수익의 50%를 담당하는 부활 광고)가 성립하지
않는다.

## Player Fantasy

> *(creative-director 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

이 시스템이 주는 감정은 **"내가 되살릴 수 있어"라는 능동적 구원감**이다 — 시든 감자를
앞에 두고 막막해하는 대신, 되돌리기 CTA를 누르고 짧은 광고 하나를 보는 것만으로 감자가
다시 생기를 찾는다. 이 순간의 핵심은 대가의 가벼움이다: 돈이 아니라 잠깐의 주의만으로,
어떤 영구적 상실도 없이 실수를 되돌린다. 플레이어는 "큰일 났다"가 아니라 "아, 이렇게 하면
돼"라는 안도와 함께, 자기 손으로 감자를 구해냈다는 조용한 보람을 얻는다.

이 판타지는 시듦/위기 & 회복이 이미 잠근 톤(위기 이후의 다정한 구조 요청, "괜찮아, 지금
도와줄 수 있어")을 그대로 이어받되, 추상적 다리가 아니라 **실제 손잡이를 당기는 촉감**으로
구체화한다. Pillar 2(지켜주고 싶은 마음)가 이 행위의 연료다 — 익명의 자원이라면 광고를 볼
이유가 없지만, 정든 내 감자이기에 그 짧은 광고가 "귀찮은 관문"이 아니라 "지켜주는 선택"으로
읽힌다. 그리고 Pillar 3(벌주지 않는 힐링)의 안티필러가 가드레일이다: 이 시스템은 "지금 광고
안 보면 감자가 죽는다"는 공포로 클릭을 강요하지 않는다 — 무료 체류 경로(30분, wither-recovery
소유)가 항상 대안으로 존재하므로, 광고는 "더 빠른 구원"일 뿐 "유일한 탈출구"가 아니다.

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 상담 필수. 핵심 포크(미준비 처리)는
> 사용자 확정.)*

### Core Rules

1. **되돌리기 CTA 탭 수신**: 이 시스템은 시듦/위기 & 회복이 소유한 되돌리기 CTA의 탭
   이벤트를 구독한다(CTA 시각·배치·카피는 wither-recovery UI 소유). 탭은 `isWithered=true`일
   때만 유효 — CTA가 그 조건에서만 노출되지만, 핸들러에서도 `isWithered`를 재확인해 방어한다.

2. **광고 준비 분기(대기-후-재생)**: 탭 시 ad-sdk의 revive 상태를 조회한다.
   - **Ready** → 즉시 `ShowAd("revive")`.
   - **미준비**(Idle/Preloading/LoadFailed/Cooldown) → 탭 수락, "준비 중" 대기 진입,
     `onReady` 구독해 Ready 즉시 자동 `ShowAd`. 로딩을 새로 시작해야 하는 경우에만
     `RequestAd("revive")`를 발행한다:
     - **Idle/LoadFailed** → `RequestAd("revive")` 발행(LoadFailed면 예약된 백오프 재시도를
       기다리지 않고 즉시 새 시도 — ad-sdk가 재시도 순번 `n`을 1로 리셋, 교차 수정으로 확정).
     - **Preloading/Cooldown** → 이미 로딩 중이거나 ad-sdk가 자동 재로드를 예약했으므로
       `RequestAd`를 호출하지 않고 `onReady`만 구독(중복 로드 방지).
   - 대기는 `REVIVE_CTA_WAIT_TIMEOUT_SEC`(신규 상수, Formulas)로 상한. 초과 시 대기 종료·
     다정한 안내(CTA 유지, 무료 경로가 대안). ad-sdk 최악 재시도(750s)를 CTA 상호작용에
     노출하지 않기 위함.
   - **구독 정리(stale callback 방지)**: `WaitingForAd`를 이탈하는 모든 경로(`onReady`
     도착으로 인한 정상 진행 제외 — 즉 타임아웃·무료 경로 선점·백그라운드 리셋)에서
     `onReady` 구독을 **명시적으로 해제**한다. 재탭은 항상 새 구독을 새로 발급하며, 이전
     구독의 잔존 콜백이 나중에 발화해도 아무 효과가 없다(이중 구독·탭 없는 `ShowAd` 방지).

3. **완료 → 회복 트리거(원자적 1회)**: `onComplete`이면 `TriggerRecovery()`를 정확히 1회
   호출. `TriggerRecovery()`는 무료 경로가 이미 호출했을 수 있으므로 wither-recovery Core
   Rule 4의 원자적 방지에 의해 `isWithered=false`면 조용히 no-op — 이 시스템은 그 결과를
   신뢰하고 이중 방어를 두지 않는다.

4. **중도이탈·실패 → 회복 없음**: `onDismissed`/`onFailed`이면 `TriggerRecovery()` 미호출.
   감자는 시듦 유지, CTA도 유지, 다정한 안내 표시. 시듦 중 방치 지표는 100에서 동결
   (potato-stat)이라 광고 실패로 시간이 흘러도 악화 없음 — Pillar 3.

5. **보상 무결성(MVP: 클라이언트측)**: `onComplete`를 신뢰해 클라이언트에서 `TriggerRecovery()`
   호출. 서버측 리워드 검증은 MVP 범위 밖·문서화된 리스크로 수용 — 부활 보상은 판매 가능
   재화가 아니라 "자기 감자 되돌리기"라 위조 유인이 낮고(경쟁·거래 없음) 솔로·첫 게임에 서버
   인프라는 과도. 재검토는 Open Questions.

6. **단일 진행 보장**: 되돌리기 플로우는 한 번에 하나 — 대기/표시 중 CTA 재탭은 조용히
   무시(디바운스). ad-sdk 단일 활성 광고 원칙과 정합.

### States and Transitions

자체 영속 상태 없음(광고 상태=ad-sdk, 시듦 상태=potato-stat 소유). 되돌리기 플로우의
전이적 상태만 존재한다.

| 상태 | 의미 | 전이 |
|---|---|---|
| **Idle** | 되돌리기 플로우 비활성(평상시 또는 위기지만 CTA 미탭) | CTA 탭 → (Ready면) Showing / (미준비면) WaitingForAd |
| **WaitingForAd** | 탭 수락 후 광고 준비 대기("준비 중") | onReady → Showing / 타임아웃 → Idle(안내 표시) / 대기 중 `isWithered=false`(무료 경로 선점) → Idle |
| **Showing** | `ShowAd("revive")` 진행 중 | onComplete → TriggerRecovery 후 Idle / onDismissed·onFailed → Idle(안내 표시) |

### Interactions with Other Systems

| 시스템 | 관계 | 인터페이스 |
|---|---|---|
| 시듦/위기 & 회복 | 구독(CTA 탭)+호출 | 되돌리기 CTA 탭 구독, 완료 시 `TriggerRecovery()` 호출(wither-recovery Core Rule 2의 "부활 광고 시스템이 완료 보고" 계약 이행). `isWithered` 조회로 탭 방어 |
| 광고 SDK 통합 계층 | 의존(호출자) | `RequestAd("revive")`/`ShowAd("revive")`, revive 상태 조회, `onReady`/`onComplete`/`onDismissed`/`onFailed` 구독. `RANK_REVIVE=0` 최고 우선순위, 사전로드·재시도·워치독은 ad-sdk 소유 |
| 감자 상태 관리 | 조회(간접) | `isWithered` 조회로 탭 유효성 방어. 회복 자체(ResetToIdeal+ActivateShield)는 `TriggerRecovery` 경유라 직접 호출 안 함 |

## Formulas

> *(systems-designer 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션. 이 얇은 계층이
> 실제로 소유하는 공식은 단 하나(`CTAWaitTimedOut`)이며, 나머지 로직은 숫자 없는 불리언
> 상태 가드다.)*

### 1. CTA 대기 타임아웃 (`CTAWaitTimedOut`) — 이 시스템 소유

The `CTAWaitTimedOut` formula is defined as:

`CTAWaitTimedOut(t) = waiting ∧ (t − t_waitStarted ≥ REVIVE_CTA_WAIT_TIMEOUT_SEC)`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 현재 시각 | `t` | float (UTC sec) | ≥0 | 벽시계 시각 |
| 대기 시작 시각 | `t_waitStarted` | float (UTC sec) | ≥0 | Waiting 진입(CTA 탭) 시각 |
| 대기 여부 | `waiting` | bool | {0,1} | "준비 중" 상태(탭~onReady/ShowAd 또는 타임아웃) |
| CTA 대기 상한 | `REVIVE_CTA_WAIT_TIMEOUT_SEC` | float (const, sec) | **35**(구조적 하한 32) | 이 시스템의 UI 인내 예산 |
| 결과 | `CTAWaitTimedOut` | bool | {0,1} | true → 대기 종료, 다정한 안내, CTA 유지(무료 30분 경로가 대안) |

**Output Range:** 불리언 게이트, 경과시간 기준 결정론적. 일반 케이스(탭 시 이미 Ready —
revive는 `neglectMeter≥40`에서 사전로드되어 Wither 수 시간 전부터 준비됨)에선 절대 발화하지
않는다(Waiting 미진입). 꼬리 위험 경로(느린 디스패치·슬롯 경합·Cooldown 재탭)에서만 발화.

**하한 도출(32s)**: `REVIVE_CTA_WAIT_TIMEOUT_SEC ≥ T_COOLDOWN_REVIVE(2) + 2·T_LOAD_TIMEOUT_SEC(15)
= 32`. 세 항: Cooldown 재탭 시 ad-sdk가 큐 재진입 전 대기하는 2s(§4) + 단일 슬롯을 점유한
타 광고(gacha/booster) 디스패치가 워치독까지 비우는 15s(§5 비선점) + revive 자신의 디스패치
워치독 15s(§2). **35**는 여기에 3s 여유를 둔 값.

**Example(바인딩 최악):** `t=500.0`에 CTA 탭, 마침 직전 광고의 `OnDismissed`가 발화(ad-sdk
Cooldown 진입). 시스템은 "미준비" 감지, `t_waitStarted=500.0`, `onReady` 구독(Cooldown이라
RequestAd 불필요 — ad-sdk §4가 무조건 발행). `t=502.0` ad-sdk 자동 RequestAd. gacha가 슬롯
점유 중이면 revive는 Rank 0이어도 대기, `t=510.0` 슬롯 해제 후 revive 디스패치, 워치독
`t=525.0`(이 예시는 탭 대비 25s 경과 — 예시적 중간값). 구조적 **최댓값 32s**는 타 광고가
탭 직후(`t=502`) 막 디스패치를 시작하는 최악 타이밍에서 도달하며(그 광고 워치독 `t=517` +
revive 자체 워치독 `t=532`), `35`가 그 32s에도 3s 여유로 커버한다.

**의존성 메모**: `REVIVE_CTA_WAIT_TIMEOUT_SEC`는 독립 상수가 아니라 **파생**값 — ad-sdk의
`T_LOAD_TIMEOUT_SEC`/`T_COOLDOWN_REVIVE`가 바뀌면 반드시 재도출해야 한다
(`≥ T_COOLDOWN_REVIVE + 2·T_LOAD_TIMEOUT_SEC`). 이 교차 파일 안전 마진 관계는 Tuning
Knobs에 명시한다.

---

**참조 상수/함수(재정의하지 않음)**: `T_LOAD_TIMEOUT_SEC=15`·`T_COOLDOWN_REVIVE=2`·
`RANK_REVIVE=0`·재시도 750s 체인·`Q(t)`/`Winner(t)` 중재(전부 ad-sdk 소유), `TriggerRecovery()`
및 그 원자적 단일 실행 가드(wither-recovery 소유). 무료 경로(`WITHER_FREE_RECOVERY_THRESHOLD_SEC=1800`,
wither-recovery)는 완전 독립 병렬 경로로 이 시스템과 공식 겹침이 없으며, 두 회복 경로의
경쟁은 wither-recovery의 기존 원자적 가드가 처리한다.

**상태 가드(공식 아님, 숫자 없음)**: `AcceptTap(state) = (state ∉ {WaitingForAd, Showing})` —
Core Rule 6의 단일 진행 보장(재탭 디바운스)은 튜닝 가능한 시간 창이 아니라 상태 술어다.

**상수 요약**(레지스트리 후보): `REVIVE_CTA_WAIT_TIMEOUT_SEC=35`(구조적 하한 32, 안전 범위
32~45s).

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드. systems-designer의 Formulas 상담 중 나온 엣지
> (Cooldown 재탭·타임아웃 후 재탭·무료경로 선점) 반영.)*

- **If 탭 시 광고가 이미 Ready라면**: 대기 없이 즉시 `ShowAd("revive")` — 사전로드 덕에
  이것이 일반 케이스. `CTAWaitTimedOut` 미평가(Waiting 미진입).

- **If 대기(`WaitingForAd`)가 `REVIVE_CTA_WAIT_TIMEOUT_SEC`(35s)를 초과하면**: 대기 종료,
  `TriggerRecovery` 없이 다정한 안내. CTA 유지, 무료 경로(30분)가 대안이라 플레이어는 갇히지
  않음. 감자 시듦 유지(방치 지표 100 동결).

- **If 광고 완료(`onComplete`) 시점에 무료 경로가 이미 회복을 트리거했다면**: 이 시스템은
  그대로 `TriggerRecovery()` 호출하지만 wither-recovery Core Rule 4 원자적 가드가
  `isWithered=false`를 감지해 조용히 no-op(이중 회복·이중 온실막 없음). 별도 사전 확인 없이
  그 가드를 신뢰.

- **If 중도이탈(`onDismissed`)/실패(`onFailed`)라면**: `TriggerRecovery` 미호출, 감자 시듦
  유지, CTA 유지, 다정한 안내. 시듦 중 방치 지표 동결이라 재시도까지 시간 흘러도 악화 없음.

- **If 대기/표시 중 CTA 재탭한다면**: `AcceptTap(state)=false`(state∈{WaitingForAd,Showing})
  이라 조용히 무시(디바운스). 두 번째 `RequestAd`/`ShowAd` 없음.

- **If 대기 타임아웃 후 다시 탭한다면**: 타임아웃이 이미 Idle로 빠져나왔으므로 정당한 새
  탭 — `t_waitStarted` 새로 시작(새 35s 예산). 단 ad-sdk 상태가 이미 Preloading/Cooldown이면
  `RequestAd` 재발행 안 함(Core Rule 2 "새로 시작해야 하는 경우에만"이 커버, 중복 로드 방지).

- **If 대기(`WaitingForAd`) 중 무료 경로가 회복을 선점해 `isWithered=false`가 되면**: 되살릴
  감자가 더 이상 시들지 않았으므로 플로우를 즉시 취소하고 Idle 복귀(광고 미표시). 로드된
  광고는 Ready로 남아 다음 위기에 재사용(ad-sdk). 참고: `Showing` 중엔 앱이 백그라운드라
  무료 경로 카운터(포그라운드 전용)가 안 움직여 이 선점은 `WaitingForAd`에서만 발생.

- **If 대기 중 앱이 백그라운드로 전환되면**: `WaitingForAd`는 전이적 비영속이라 되돌리기
  플로우가 Idle로 리셋(잃어도 안전 — CTA 유지). 복귀 시 광고가 Ready면 재탭으로 즉시 표시.
  광고 로드는 ad-sdk 소유라 이 리셋에 영향 없음.

- **If 기기 시계가 조작되면**: `CTAWaitTimedOut`의 경과시간은 대기 중(포그라운드) 실제
  경과시간 누적으로 측정(게임 내 다른 라이브 타이머와 동일)이라 시계 되돌려 대기 무한 연장/
  조기 종료 악용이 무의미. 영속 타임스탬프를 저장 안 해 로드 시점 방어(`IsTimestampValid`)도
  불필요.

## Dependencies

### Upstream (이 시스템이 의존)

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 시듦/위기 & 회복 | Hard | 되돌리기 CTA 탭 이벤트 구독, 완료 시 `TriggerRecovery()` 호출. `isWithered` 조회로 탭 방어. 양방향 확인: wither-recovery Downstream 표가 이 시스템을 명시(잠정→확정, 교차 수정 A, 2026-07-18) |
| 광고 SDK 통합 계층 | Hard | `RequestAd("revive")`/`ShowAd("revive")`, revive 상태 조회, `onReady`/`onComplete`/`onDismissed`/`onFailed` 구독. `RANK_REVIVE=0` 최고 우선순위. 양방향 확인: ad-sdk Interactions 표가 이미 이 시스템을 명시 |
| 감자 상태 관리 | Soft | `isWithered`(potato-stat 소유) 조회로 탭 유효성 방어. 없어도 CTA 게이팅(wither-recovery)이 대부분 방어하나 핸들러 재확인용 |

### Downstream (이 시스템에 의존)

없음. 어떤 시스템도 부활 광고 시스템을 호출하지 않는다.

### 세이브/영속성

신규 세이브 필드 없음 — 이 시스템은 전이적 플로우 상태(Idle/WaitingForAd/Showing)만 가지며
영속 데이터를 소유하지 않는다.

**Hard vs Soft 판정 기준**: 다른 GDD들과 동일 — 이 시스템이 응답하지 않을 때 대상 시스템이
아예 작동을 멈추면 Hard, 연출/편의만 사라지고 핵심 루프는 유지되면 Soft.

## Tuning Knobs

| 노브 | 현재값 | 권장 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `REVIVE_CTA_WAIT_TIMEOUT_SEC` | 35초 | 32~45초 | 구조적 하한(32) 미만이면 Cooldown+경합 꼬리를 성공 직전에 잘라 "준비 중… 실패" 오탐 — 튜닝 선호가 아니라 **정합성 하한** | ~45초 초과는 이득 없음(네트워크 I/O 완료 후 성공 확률이 오르지 않음) + ad-sdk 재시도 백오프(30s+/시도) 영역과 구별이 흐려져 "화면 밖 백오프" 설계 의도 훼손 |

**노브 간 상호작용(파생 관계)**: `REVIVE_CTA_WAIT_TIMEOUT_SEC`는 독립 노브가 아니라 ad-sdk의
`T_COOLDOWN_REVIVE`·`T_LOAD_TIMEOUT_SEC`에서 도출된 값이다(`≥ T_COOLDOWN_REVIVE +
2·T_LOAD_TIMEOUT_SEC`). ad-sdk에서 이 두 값을 변경하면 이 노브를 반드시 재도출·재검증해야
한다 — wither-recovery의 `WITHER_FREE_RECOVERY_THRESHOLD_SEC`이 ad-sdk의 750s 재시도 소진과
안전하게 비교돼야 하는 것과 동일한 교차 파일 안전 마진 패턴이다.

**참조만 하는 값(소유 문서 수정 시 변경)**: `T_LOAD_TIMEOUT_SEC`·`T_COOLDOWN_REVIVE`·
`REVIVE_PRELOAD_THRESHOLD`·`RANK_REVIVE`·재시도 백오프 계열(전부 ad-sdk) ·
`WITHER_FREE_RECOVERY_THRESHOLD_SEC`(wither-recovery) · `SHIELD_DURATION`(회복 시 부여,
potato-stat 경유).

## Visual/Audio Requirements

> 이 섹션은 새 시각 언어를 정의하지 않는다 — 되돌리기 CTA의 시각·배치·카피는 wither-recovery
> UI Requirements가 소유하고, 광고 로딩 UI 트리트먼트는 greenhouse-shield-system.md가 이미
> 재사용 컨벤션으로 정의했다. 이 시스템은 그 컨벤션의 실제 정본(부활 광고) 사례로서 참조·
> 적용한다.

**1. 되돌리기 CTA — wither-recovery 소유(재정의 안 함)**: 히어로 버튼 스펙(Amber Ochre Base,
Tier 2 둥근 기하, 하단 중앙 엄지존, 대기 중 펄스/글로우 금지, 프레스 시 스퀴시+번짐)은
wither-recovery Visual/Audio §2가 이미 잠갔다. 이 시스템은 그 버튼의 탭을 소비할 뿐 시각을
소유하지 않는다.

**2. 로딩("준비 중")·실패 피드백 — greenhouse-shield 컨벤션 상속**: 탭 후 광고가 미준비라
대기하는 동안(`WaitingForAd`)의 피드백은 greenhouse-shield-system.md가 정의한 광고 로딩
트리트먼트를 그대로 따른다 — 탭 즉시 1회성 프레스 확인(스퀴시+번짐), 이후 정적 탈채도 "대기"
룩(스피너 금지 — 최대 35s 동안 반복 루프 모션은 Section 1 원칙 3 위반), 플레이어가 시선을
떼도 무방. 타임아웃/실패 시 조용한 페이드 안내(빨강·경고 아이콘·모달 없음), 톤은
wither-recovery의 "실패를 명명하지 않고 경보하지 않는다" 상속. 일반 케이스(사전로드로 이미
Ready)에선 이 대기 피드백 자체가 나타나지 않는다.

**3. 회복 연출 — potato-stat/expression 소유**: 광고 완료→`TriggerRecovery()`→`OnRecovered`의
감자 회복 연출(중심→바깥 채도 번짐, Blush 블룸)은 potato-stat Visual/Audio §4가 소유한다. 이
시스템은 트리거만 매개한다.

**4. 오디오(범위 메모)**: 사운드 직접 재생 안 함. 부활 성공음/광고 완료음은 potato-stat의
`OnRecovered` 훅을 통해 ASMR 적응형 오디오 GDD가 설계한다. 광고 경로 vs 무료 경로 완료음
구별은 wither-recovery의 기존 열린 질문(같은 범주)에 이미 이월돼 있다.

## UI Requirements

> 신규 풀스크린 모달 없음. 되돌리기 CTA는 wither-recovery UI 소유이므로, 이 시스템의 UI
> 요구사항은 그 CTA가 이 시스템의 플로우 상태(Idle/WaitingForAd/Showing)를 어떻게 반영하는지의
> 계약뿐이다.

**CTA 상태 반영(greenhouse-shield 컨벤션 상속)**:

| 플로우 상태 | CTA가 보이는 것 |
|---|---|
| Idle(위기 중, 미탭) | wither-recovery의 기본 히어로 버튼(탭 대기) |
| WaitingForAd | 1회성 프레스 확인 후 정적 "준비 중" 룩(스피너 없음), 재탭 조용히 디바운스 |
| Showing | SDK 광고 뷰가 화면 인수(이 GDD·wither-recovery 범위 밖) |
| 타임아웃/실패 복귀 | CTA가 탭 이전 상태로 복귀 + 저강조 페이드 안내(빨강·경고·모달 없음), 즉시 재탭 가능 |

**카피(이월)**: "준비 중" 상태·타임아웃·실패의 정확한 문구는 wither-recovery가 이미 이월한
"되돌리기 CTA 카피"(writer/ux-designer 후속)와 함께 Pre-Production `/ux-design`에서 확정한다.
톤 원칙만 잠금: 죄책감·공포 없이("지금 광고 안 보면 죽는다" 금지), 무료 경로가 대안임을
은근히 상기시킬 수 있음.

**접근성**: CTA 히트타깃 ≥88dp는 wither-recovery UI가 이미 보장한다. 이 시스템은 추가
상호작용 표면을 만들지 않는다.

**교차 플래그(producer/game-designer)**: greenhouse-shield가 남긴 "광고 로딩 UI 트리트먼트
재사용 컨벤션화" 후속 과제의 실제 정본 사례가 이 시스템이다 — 가챠/부스터 로딩도 이 패턴을
상속하도록 컨벤션 문서화를 권장한다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션. 상담 중
> 발견된 설계 공백(`onReady` 구독 해제 계약 누락, stale callback 위험)은 Core Rule 2에
> 구독 정리 규칙을 추가하고 AC-EC-04/05로 해소.)*

이 시스템은 자체 영속 상태가 없으므로 아래 "플로우 상태"는 전이적 값(Idle/WaitingForAd/
Showing)이다. 두 시스템이 각각 "Showing"이라는 상태를 가지므로, **이 시스템의 플로우
상태**는 `state`로, **ad-sdk의 revive adType 상태**는 `S(revive)`(Idle/Preloading/Ready/
Showing/Cooldown/LoadFailed)로 표기해 구분한다. `TriggerRecovery`/`RequestAd`/`ShowAd`는
타 시스템 소유이므로 이 시스템의 기준은 그 함수들을 **어떻게 호출/구독하는지**까지만
검증한다.

### 상태 전이

- **AC-ST-01 (Idle 초기값)** **GIVEN** `isWithered=true`(CTA 노출), 되돌리기 플로우 미탭 **WHEN** `state` 조회 **THEN** `state=Idle`.
- **AC-ST-02 (Idle → Showing, 일반 케이스 즉시 재생)** **GIVEN** `state=Idle`, `isWithered=true`, `S(revive)=Ready` **WHEN** CTA 탭 **THEN** `ShowAd("revive")` 즉시 호출, `state=Showing`. `RequestAd` 미호출, `onReady` 구독 없음, `CTAWaitTimedOut` 평가 대상 아님.
- **AC-ST-03 (Idle → WaitingForAd, `S(revive)=Idle`)** **GIVEN** `state=Idle`, `isWithered=true`, `S(revive)=Idle` **WHEN** CTA 탭 **THEN** 탭 수락, `state=WaitingForAd`, `t_waitStarted=현재시각`, `RequestAd("revive")` 발행, `onReady` 구독.
- **AC-ST-04 (Idle → WaitingForAd, `S(revive)=LoadFailed`)** **GIVEN** `state=Idle`, `isWithered=true`, `S(revive)=LoadFailed` **WHEN** CTA 탭 **THEN** `state=WaitingForAd`, `RequestAd("revive")` 발행(ad-sdk가 재시도 순번 `n`을 1로 리셋, 예약 백오프 무시), `onReady` 구독.
- **AC-ST-05 (Idle → WaitingForAd, `S(revive)=Preloading` — RequestAd 미발행)** **GIVEN** `state=Idle`, `isWithered=true`, `S(revive)=Preloading` **WHEN** CTA 탭 **THEN** `state=WaitingForAd`, `onReady`만 구독, `RequestAd("revive")` 미호출.
- **AC-ST-06 (Idle → WaitingForAd, `S(revive)=Cooldown` — RequestAd 미발행)** **GIVEN** `state=Idle`, `isWithered=true`, `S(revive)=Cooldown` **WHEN** CTA 탭 **THEN** `state=WaitingForAd`, `onReady`만 구독, `RequestAd("revive")` 미호출(ad-sdk `NextPreloadTrigger`가 자동 재로드 예약).
- **AC-ST-07 (WaitingForAd → Showing, onReady 도착)** **GIVEN** `state=WaitingForAd`(임의 경로로 진입) **WHEN** 구독한 `onReady` 도착 **THEN** 즉시 `ShowAd("revive")`, `state=Showing`.
- **AC-ST-08 (Showing → Idle, onComplete)** **GIVEN** `state=Showing` **WHEN** `onComplete` 도착 **THEN** `TriggerRecovery()` 정확히 1회 호출, `state=Idle`.
- **AC-ST-09 (Showing → Idle, onDismissed)** **GIVEN** `state=Showing` **WHEN** `onDismissed` 도착 **THEN** `TriggerRecovery()` 미호출, `state=Idle`, 다정한 안내, `isWithered` 유지.
- **AC-ST-10 (Showing → Idle, onFailed)** **GIVEN** `state=Showing` **WHEN** `onFailed` 도착 **THEN** `TriggerRecovery()` 미호출, `state=Idle`, 다정한 안내, `isWithered` 유지.
- **AC-ST-11 (WaitingForAd → Idle, 타임아웃)** **GIVEN** `state=WaitingForAd`, `t_waitStarted=T0` **WHEN** `현재시각-T0 ≥ REVIVE_CTA_WAIT_TIMEOUT_SEC(35)`이고 `onReady` 미도착 **THEN** `state=Idle`, `TriggerRecovery()` 미호출, CTA 유지, 다정한 안내, `onReady` 구독 해제.
- **AC-ST-12 (재탭 디바운스 — WaitingForAd 중)** **GIVEN** `state=WaitingForAd` **WHEN** CTA 재탭 **THEN** `AcceptTap(WaitingForAd)=false`이라 무시, 두 번째 `RequestAd`/`onReady` 구독 없음, `state` 불변.
- **AC-ST-13 (재탭 디바운스 — Showing 중)** **GIVEN** `state=Showing` **WHEN** CTA 재탭 **THEN** `AcceptTap(Showing)=false`이라 무시, 두 번째 `ShowAd` 없음, `state` 불변.
- **AC-ST-14 (타임아웃 후 재탭 — 새 대기 사이클)** **GIVEN** `state=Idle`(직전 타임아웃 도달), `S(revive)=Preloading`(직전 요청 여전히 로딩 중) **WHEN** CTA 재탭 **THEN** `AcceptTap(Idle)=true`, 수락, `state=WaitingForAd`, `t_waitStarted` 새로 기록(새 35s 예산), `RequestAd` 재발행 안 함(Core Rule 2 "새로 시작해야 하는 경우에만").
- **AC-ST-15 (무료 경로 선점 — WaitingForAd 중 취소)** **GIVEN** `state=WaitingForAd` **WHEN** 무료 체류 경로가 먼저 `TriggerRecovery()` 실행해 `isWithered=false`가 됨(광고 완료 콜백 전) **THEN** 즉시 플로우 취소, `state=Idle`(광고 미표시), `onReady` 구독 해제, 로드된 광고는 폐기 없이 `S(revive)=Ready`로 유지.
- **AC-ST-16 (백그라운드 전환 — WaitingForAd 리셋)** **GIVEN** `state=WaitingForAd` **WHEN** 앱이 백그라운드로 전환 **THEN** 복귀 시점까지 늦어도 `state=Idle`로 리셋(전이적 비영속), `onReady` 구독 해제, CTA 유지, `S(revive)`(ad-sdk 소유)는 이 리셋에 영향받지 않음.

### 공식 검증 — `CTAWaitTimedOut`

- **AC-F-01 (경계 미만)** **GIVEN** `waiting=true`, `t_waitStarted=T0` **WHEN** `t-T0=34.999s` **THEN** `CTAWaitTimedOut=false`.
- **AC-F-02 (경계 정확, 포함)** **GIVEN** 상동 **WHEN** `t-T0=35.000s` **THEN** `CTAWaitTimedOut=true`.
- **AC-F-03 (waiting=false 게이트)** **GIVEN** `waiting=false`(`state∈{Idle,Showing}`) **WHEN** 임의 경과시간(35s+ 포함) **THEN** `CTAWaitTimedOut=false`.
- **AC-F-04 (일반 케이스 — 미평가)** **GIVEN** 탭 시 `S(revive)=Ready`(AC-ST-02 경로) **WHEN** 즉시 `ShowAd` **THEN** `waiting`이 한 번도 true가 안 되므로 `CTAWaitTimedOut` 평가 대상 자체가 아님.
- **AC-F-05 (하한 정합성 — 상수 정적 검증)** **GIVEN** `T_COOLDOWN_REVIVE=2`, `T_LOAD_TIMEOUT_SEC=15`(ad-sdk), `REVIVE_CTA_WAIT_TIMEOUT_SEC=35` **WHEN** `T_COOLDOWN_REVIVE + 2·T_LOAD_TIMEOUT_SEC` 계산 **THEN** `=32`이고 `35 ≥ 32`(여유 3초) — 런타임 아닌 상수 초기화 시점 정적 assert 대상.
- **AC-F-06 (32s 최악 바인딩 경로 — 최댓값 재현)** **GIVEN** `t=0` 탭, `S(revive)=Cooldown` **WHEN** `t=2` 자동 RequestAd 시점에 정확히 타 adType이 막 디스패치 시작(`t_loadStarted=2`)해 revive가 그 워치독(`t=17`)까지 슬롯 못 받고 이후 revive 자체 워치독이 `t=32`까지 소진 **THEN** `t=32`(경과 32.000s)에서 `CTAWaitTimedOut=false`(35>32) — 최댓값에서도 3s 여유 유지 확인.
- **AC-F-07 (본문 예시 재현 — 비최악 25s)** **GIVEN** Formulas §1 Example(`t=500` 탭 Cooldown, `t=502` 자동 RequestAd, `t=510` 슬롯 해제, `t=525` 워치독) **WHEN** 경과시간 계산 **THEN** `525-500=25s`(35s 이내) — 이 값은 예시적 중간값이지 32s 최댓값이 아님(문서 정합성 확인용).
- **AC-F-08 (시계 조작 방어)** **GIVEN** `state=WaitingForAd`, `t_waitStarted` 기록됨(휘발성) **WHEN** 기기 시계 임의 조작 **THEN** `CTAWaitTimedOut` 판정이 라이브 프레임 실측 경과시간 누적만으로 산출(영속 타임스탬프 없음)되어 조기 타임아웃/무한 연장 악용 불가.

### 엣지 케이스

- **AC-EC-01 (Cooldown 자동 재로드 의존 — 유일한 안전망은 타임아웃)** **GIVEN** `S(revive)=Cooldown`에서 탭해 `state=WaitingForAd`(RequestAd 미발행) **WHEN** ad-sdk `NextPreloadTrigger` 자동 전이가 지연/미발생 **THEN** 이 시스템은 대체 `RequestAd`를 스스로 발행하지 않고 오직 `CTAWaitTimedOut`(35s)만이 대기를 종료 — 자동 로드 재개는 전적으로 ad-sdk 책임.
- **AC-EC-02 (탭 유효성 방어 — isWithered=false에서 도착한 탭)** **GIVEN** CTA 탭 이벤트가 어떤 경로로든 도착했으나 조회 시점 `isWithered=false` **WHEN** 핸들러 실행 **THEN** 탭 무효 처리, `RequestAd`/`ShowAd`/`TriggerRecovery()` 중 어느 것도 미호출.
- **AC-EC-03 (보상 무결성 — 조건 없는 신뢰)** **GIVEN** `onComplete` 도착 **WHEN** `TriggerRecovery()` 호출 **THEN** 서버측 검증/재확인 호출 없이 클라이언트 콜백만으로 즉시 트리거(MVP 확정 — 위조 시나리오 자체는 검증 대상 아님).
- **AC-EC-04 (구독 정리 — 타임아웃 후 잔존 콜백 무해)** **GIVEN** 첫 탭이 타임아웃(AC-ST-11)으로 종료돼 `state=Idle`, 첫 `onReady` 구독 해제됨 **WHEN** 원래 광고 요청이 나중에 준비완료돼 `onReady` 발화 시도 **THEN** 해제된 구독이라 아무 효과 없음(`ShowAd` 미호출, `state` 불변) — 탭 없는 `ShowAd` 방지.
- **AC-EC-05 (구독 정리 — 재탭 시 이중 구독 없음)** **GIVEN** AC-ST-14 상황(타임아웃 후 재탭) **WHEN** 두 번째 `WaitingForAd` 사이클에서 `onReady` 도착 **THEN** 정확히 1회만 `ShowAd` 호출(이중 호출 없음).
- **AC-EC-06 (Showing 중 무료 경로 선점 — 가정이 틀려도 안전)** **GIVEN** `state=Showing`(전제를 깨고 `isForeground`가 여전히 true로 관측됨) **WHEN** 무료 경로가 `TriggerRecovery()`를 먼저 실행해 `isWithered=false`가 된 직후 `onComplete` 도착 **THEN** 이 시스템은 그대로 `TriggerRecovery()` 호출(AC-INT-03 무조건 호출)하고 wither-recovery 원자적 가드가 no-op해 이중 회복 없음 — `Showing` 중 이 시나리오가 구조적으로 불가능하다는 전제와 무관하게 안전.

### 통합/인터페이스

- **AC-INT-01 (CTA 탭 구독 계약)** **GIVEN** wither-recovery가 되돌리기 CTA 탭 이벤트 발행 **WHEN** 이 시스템이 구독 중 **THEN** 탭 발생 시 이 시스템 핸들러 호출(CTA 시각·배치·카피는 검증 대상 아님 — wither-recovery UI 소유).
- **AC-INT-02 (isWithered 방어적 재확인 — 정상 통과)** **GIVEN** 탭 도착 시 `isWithered=true` **WHEN** 핸들러 실행 **THEN** `AcceptTap(state)` 평가로 정상 진행.
- **AC-INT-03 (onComplete → TriggerRecovery, 조건 없는 호출)** **GIVEN** `state=Showing` **WHEN** `onComplete` 도착 **THEN** `TriggerRecovery()`가 사전 `isWithered` 재확인 없이 무조건 정확히 1회 호출(이중 방어 코드 없음 — wither-recovery 원자적 가드 신뢰).
- **AC-INT-04 (onDismissed/onFailed → TriggerRecovery 미호출 계약)** **GIVEN** `state=Showing` **WHEN** `onDismissed` 또는 `onFailed` 도착 **THEN** 두 콜백 모두 100% `TriggerRecovery()` 미호출 — 회귀 방지 핵심.
- **AC-INT-05 (TriggerRecovery no-op 실증 — 진짜 통합)** **GIVEN** 무료 경로가 이미 회복을 트리거해 `isWithered=false`인 상태에서 `onComplete` 도착 **WHEN** 이 시스템이 그대로 `TriggerRecovery()` 호출 **THEN** (실제 wither-recovery 구현과 결합 시) 이중 `ResetToIdeal()`/`ActivateShield()`가 발생하지 않으며, 이 시스템 쪽엔 `isWithered` 사전 확인/결과 분기 코드가 존재하지 않음(정적 감사 겸용).
- **AC-INT-06 (RequestAd 라우팅 — Idle/LoadFailed에서만 발행)** **GIVEN** `S(revive) ∈ {Idle, LoadFailed}` **WHEN** `WaitingForAd` 진입 **THEN** `RequestAd("revive")` 정확히 1회 발행.
- **AC-INT-07 (RequestAd 라우팅 — Preloading/Cooldown에서는 미발행)** **GIVEN** `S(revive) ∈ {Preloading, Cooldown}` **WHEN** `WaitingForAd` 진입 **THEN** `RequestAd("revive")` 미호출, `onReady`만 구독.
- **AC-INT-08 (신규 세이브 필드 없음 — 비영속 확인)** **GIVEN** `state`가 임의값(WaitingForAd 포함) **WHEN** 앱 재시작(세이브/로드, ad-sdk·wither-recovery 데이터 정상 복원) **THEN** 이 시스템 `state`는 항상 `Idle`로 초기화 — 세이브 스키마에 revive-ad-system 네임스페이스 필드 미등록(정적 감사 겸용).
- **AC-INT-09 (단일 진행 — SDK 이중 호출 방지)** **GIVEN** `state ∈ {WaitingForAd, Showing}` **WHEN** CTA가 연타 등으로 탭 이벤트를 여러 번 발행 **THEN** `RequestAd`/`ShowAd`는 최초 1회 조합만 호출되고 이후 탭은 전부 무시 — Core Rule 6 + ad-sdk "단일 활성 광고" 조합 성립 확인.

### 테스트 계층 및 이 GDD 범위 밖 (테스트 불가/타 시스템 소관)

- **스토리 타입 = Integration(BLOCKING)** — 이 시스템은 ad-sdk·wither-recovery를 잇는 오케스트레이션이라 순수 Logic이 아니다. 권장: `IAdSdkService`/`IWitherRecoveryService` 인터페이스 + fake 구현으로 대부분 AC를 EditMode 검증(결정론), **AC-INT-05(no-op 실증)만** 실제 wither-recovery 원자적 가드와 결합한 진짜 Integration 테스트(`tests/integration/revive-ad-system/`)로 검증. AC-ST-15(무료 경로 선점)의 실기기 타이밍은 PlayMode/manual 1회 보강.
- **범위 밖(설계 시점 AC 불가)**: 실제 광고 SDK 동작(브랜드 미정, 실기기·네트워크 필요 — ad-sdk/실 SDK 선정 후) · 수익화·부활 광고 기대수익 50% 실측(analytics-engineer, 소프트런칭) · 정확한 CTA 카피·"준비 중"/타임아웃 문구(writer/ux, Pre-Production `/ux-design`) · 보상 무결성 서버 검증(Open Questions, producer/technical-director) · 전면 광고 뷰의 실제 OS foreground 유지 여부(engine-programmer/unity-specialist, Unity 6.5 실기기 PlayMode — Finding #2 전제 검증).

## Open Questions

| 질문 | 배경 | 담당 | 해소 시점 |
|---|---|---|---|
| 보상 무결성 서버측 검증 도입 여부 | Core Rule 5는 MVP를 클라이언트측(`onComplete` 신뢰)으로 확정 — 부활 보상은 판매 가능 재화가 아니고 경쟁·거래가 없어 위조 유인이 낮으며, 솔로·첫 게임에 서버 인프라는 과도라 문서화된 리스크로 수용. 규모 확대·치팅 실측 시 재검토 | producer/technical-director/security-engineer | 소프트런칭 이후 또는 서버 백엔드 도입 시 |
| WaitingForAd 중 Preloading→LoadFailed(재시도 소진) 능동 감지 개선 | 탭 시점 상태로 라우팅을 1회 결정하므로, 대기 중 인플라이트 시도가 마지막 재시도(`n=N_MAX_AD`)로 소진되면 죽은 `onReady` 구독으로 35s 타임아웃까지 대기 후 재탭으로 자가치유. 발생 창 매우 좁음(P3) — 능동 재감지·즉시 `RequestAd` 개선의 복잡도 대비 이득 판단 필요 | systems-designer | 밸런스/폴리시 단계 |
| 전면 광고 뷰가 실제로 OS 백그라운드/pause를 유발하는지 | Edge Cases의 "`Showing` 중 무료 경로 선점 불가" 전제가 이에 의존. 다수 SDK는 포커스만 잃고 포그라운드를 유지 → Unity 6.5(학습범위 밖) 실기기로 `Application` 라이프사이클 확인 필요. `AC-EC-06`이 전제가 틀려도 안전함을 이미 고정 | engine-programmer/unity-specialist | 실 SDK 선정 후 실기기 PlayMode |
| "준비 중"·타임아웃·실패의 정확한 카피 | UI Requirements가 톤 원칙만 잠금(죄책감·공포 없이, 무료 경로 은근히 상기). 정확 문구는 wither-recovery 되돌리기 CTA 카피와 함께 이월 | writer/ux-designer | Pre-Production `/ux-design` |
| 광고 로딩 UI 트리트먼트 재사용 컨벤션화 | 이 시스템이 greenhouse-shield가 남긴 컨벤션의 실제 정본 사례 — 가챠/부스터/이 시스템이 공유 패턴을 상속하도록 컨벤션 문서화 권장 | producer/game-designer | 후속 광고 게이팅 CTA 설계 시 |
| 실 광고 SDK 브랜드 선정 | 이 시스템은 SDK-agnostic ad-sdk를 통해서만 간접 소비 — 직접 해소 대상 아님, ad-sdk의 기존 열린 질문 상속 확인만 | technical-director | 아키텍처(ADR) 단계 |
