# 시듦/위기 & 회복 (Wither Crisis & Recovery)

> **Status**: Designed (pending review)
> **Author**: user + systems-designer + economy-designer + art-director + qa-lead
> **Last Updated**: 2026-07-18
> **Implements Pillar**: Pillar 3 (벌주지 않는 힐링), Pillar 1 (손끝의 위안)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode

## Overview

시듦/위기 & 회복은 감자 상태 관리가 `neglectMeter=100`으로 선언하는 시듦(Wither)
사건을 감지해, 그 순간부터 감자가 실제로 되돌려지는 순간까지의 플레이어 경험 전체를
소유하는 게임플레이 레이어다. 이 시스템은 potato-stat이 이미 확정한 위기 판정 로직
(무엇이 시듦인지)이나 회복 함수(`ResetToIdeal()`+`ActivateShield(8h)`, 무엇이
회복인지)를 재발명하지 않는다 — 대신 "지금 위기다"라는 사실과 "지금 회복하라"는
명령 사이의 다리를 놓는다. 이 다리를 어떤 구체적 행동(광고 시청, 혹은 향후 추가될
다른 경로)이 건너는지는 이 시스템이 모르며, 오직 부활 광고 시스템(별도 Economy
시스템, 아직 미설계)이 그 구체적 행동을 완료했을 때 호출하는 "회복 트리거" 인터페이스
만 노출한다. 이 분리 덕분에 게임플레이 규칙(언제 위기이고 회복이 무엇을 의미하는가)이
특정 수익화 메커니즘(광고 SDK)에 결합되지 않는다.

플레이어에게는 이 시스템이 "위기가 닥쳤을 때 무엇을 보고, 무엇을 누를 수 있는가"로
나타난다 — 감자 자체의 시듦 포즈는 이미 표정/애니메이션 시스템이 그리지만, 그 옆이나
위에 뜨는 되돌리기 CTA와 그 톤은 이 시스템의 몫이다. 이 시스템이 없다면 potato-stat의
`neglectMeter=100`이라는 사실은 감자를 영원히 시든 채로 방치하는 결과만 낳는다 —
위기에서 벗어날 구체적 경로를 여는 것이 이 시스템의 존재 이유다.

## Player Fantasy

> *(`creative-director` 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

플레이어가 이 시스템에서 느껴야 할 감정은 potato-stat의 Wither 상태 자체(안쓰러움·
짠함)와는 다른, 위기 이후의 좁은 순간에 한정된 감정이다 — "괜찮아, 되돌릴 방법이
있어"라는 안도의 시작점. art-bible이 이미 Wither을 "공포가 아니라 안쓰러움"으로,
Recovery를 "조용한 경이·다정한 안도"로 잠가뒀으므로, 이 시스템의 판타지는 그 둘
사이의 다리 그 자체다 — 위기를 발견한 플레이어가 "이제 뭘 해야 하지?"라는 막막함
대신 "아, 이렇게 하면 되는구나"라는 명확한 다음 행동을 즉시 찾는 순간.

Pillar 3(벌주지 않는 힐링)의 Design Test가 이미 "'방치하면 감자를 영구히 잃음' vs
되돌릴 수 있는 쪽" 충돌에서 후자를 택하라고 명시했으므로, 이 시스템의 CTA는 절대
죄책감을 유발하는 톤(예: "너 때문에 감자가...")을 쓰지 않는다 — 대신 수확 CTA(이미
확정된 톤: "언제든 편하게")와 같은 계열의, 압박 없는 초대의 언어를 쓴다. 다만 수확과
달리 이 CTA는 좋은 소식이 아니라 다정한 구조 요청이므로 톤의 미묘한 차이가 있다:
수확이 "축하"라면 이 CTA는 "괜찮아, 지금 도와줄 수 있어"에 가깝다.

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 lean에서도 상담 필수. 이 시스템은
> systems-index 고위험 시스템 표에 없어 기본 lean 규칙을 그대로 적용. 단, 무료 회복
> 경로 도입이라는 핵심 포크는 사용자가 직접 결정.)*

### Core Rules

1. **감지**: `감자 상태 관리`의 `OnWitherEntered` 이벤트를 구독해 위기를 감지한다. 단,
   콜드 스타트/포그라운드 복귀 시에는 `오프라인 진행 계산`의 `OnOfflineProgressApplied`
   신호를 먼저 기다린 뒤에만 위기 판정을 평가한다 — 오프라인 배치가 끝나기 전에
   판정하면 계산 중인 stale 상태를 위기로 오판할 수 있다.

2. **회복 경로 이원화(사용자 확정)**: 이 시스템은 정확히 하나의 내부 함수
   `TriggerRecovery()`를 소유하며, 이 함수만이 potato-stat의 `ResetToIdeal()`+
   `ActivateShield(8h)`를 호출할 권한을 갖는다. 두 개의 독립 경로 중 어느 쪽이든 이
   함수를 호출할 수 있다:
   - **(a) 광고 경로**: 부활 광고 시스템(미설계)이 광고 시청 완료를 보고하며 호출.
   - **(b) 무료 체류 경로**: 이 시스템이 자체 소유하는 누적 포그라운드 체류 시간
     (`witherForegroundSeconds`)이 임계값에 도달하면 자동 호출.

3. **무료 체류 카운터는 라이브 전용, 오프라인 갭 중 절대 진행 없음**:
   `witherForegroundSeconds`는 `isWithered=true`이고 앱이 실제로 포그라운드에 떠
   있는 동안에만 매 틱 증가한다 — 백그라운드 전환 시 즉시 정지(리셋 아님, 값 보존),
   앱이 닫혀 있던 시간은 이 카운터에 전혀 반영되지 않는다. 이 설계는
   `offline-progress-calculation.md`·`lifecycle-progression-system.md`·
   `potato-stat-system.md` 세 문서가 이미 확정한 "오프라인 갭 중 Wither는 최대 1회만
   진입하고 갭 끝까지 유지된다"는 구조적 가정을 전혀 건드리지 않는다 — 오프라인 갭
   동안 포그라운드 시간은 정의상 0이므로, 세 문서의 기존 배치 계산·세그먼트 분할
   로직이 재작업 없이 그대로 유효하다.

4. **동시 완료 경쟁 방지**: 광고 경로와 무료 체류 경로가 거의 동시에 완료 조건을
   충족해도 `TriggerRecovery()`는 원자적으로 정확히 1회만 실행된다 — 먼저 도달한
   경로가 승리하고, 나중 경로는 이미 `isWithered=false`가 된 상태를 감지해 자신의
   완료 콜백을 조용히 무시한다(이중 회복·이중 온실막 연장 없음).

5. **리셋 규칙**: `witherForegroundSeconds`는 오직 실제 `TriggerRecovery()` 실행
   시에만 0으로 리셋된다 — 그 전까지는 백그라운드 전환·앱 재시작을 거쳐도 누적값이
   그대로 보존된다(Pillar 3: 짧은 세션을 여러 번 나눠 방문해도 그동안의 "돌봄"이
   사라지지 않음).

6. **CTA 우선순위**: 화면의 유일한 히어로 액션(art-bible UI/HUD §4)은 광고 시청
   CTA다 — 무료 체류 경로는 별도 버튼이 아니라 조용한 진행 표시(정확한 카피는 UI
   Requirements에서 확정)로만 존재한다. 이는 수익 모델(부활 광고=기대 수익 50%)을
   해치지 않으면서도 Pillar 3의 "항상 되돌릴 수 있다"는 약속을 정직하게 지킨다 —
   숨기지 않되 경쟁시키지 않는다.

### States and Transitions

| 상태 | 진입 조건 | 이탈 조건 |
|---|---|---|
| **Dormant(평상시)** | 기본값 — `isWithered=false` | `OnWitherEntered` 수신 → CrisisActive |
| **CrisisActive(위기 진행 중)** | `OnWitherEntered` 수신, `witherForegroundSeconds` 카운트 시작(포그라운드 한정) | `TriggerRecovery()` 실행(광고 또는 무료 경로) → Dormant |

### Interactions with Other Systems

| 시스템 | 관계 | 인터페이스 |
|---|---|---|
| 감자 상태 관리 | 구독(호출자) | `OnWitherEntered` 구독, `ResetToIdeal()`+`ActivateShield(8h)` 호출(이 시스템만의 배타적 권한) |
| 오프라인 진행 계산 | 구독 | `OnOfflineProgressApplied` 신호 이후에만 위기 판정 평가(Core Rule 1) |
| 날씨 시스템 | 조회(Soft, 신규 확인) | `GetCurrentWeather()` — CTA 카피 플레이버("이 날씨 때문에 지쳤나봐요" 류)에만 사용, 구조적 판정에는 관여 안 함. systems-index가 이미 이 의존을 명시했으나 Hard 인터페이스는 아니었음을 이 GDD가 확정 |
| 부활 광고 시스템 | 이 시스템에 의존(호출자) | 되돌리기 CTA 탭 구독 + 광고 완료(`onComplete`) 시 `TriggerRecovery()` 호출(계약 확정, `revive-ad-system.md` 2026-07-18) |

## Formulas

> *(`systems-designer`+`economy-designer` 상담 완료 — lean 모드에서도 Formulas는 상담
> 필수 섹션. 무료 회복 임계값을 두고 두 에이전트의 권장값이 갈렸으나(1800s vs 300s),
> 사용자가 systems-designer의 1800s(광고 재시도 최악 소진 시간보다 확실히 크게 잡아
> 수익 보호에 더 보수적)를 확정.)*

### 1. 무료 체류 카운터 갱신 (`witherForegroundSeconds`)

Core Rule 3이 이미 확정한 대로, 이 카운터는 라이브 틱에서만 갱신되며 오프라인 배치
닫힌 형식이 필요 없다 — 정의상 앱이 포그라운드에 없으면 절대 진행되지 않으므로,
`offline-progress-calculation.md`류의 세그먼트 분할·재구성 로직이 이 값에는
아예 적용될 여지가 없다(오프라인 갭 중 이 값은 항상 정확히 정지 상태로 재개된다).

The `witherForegroundSeconds` formula is defined as:

`F' = F + Δt_live · 1[isWithered ∧ isForeground]`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 갱신 전 누적 체류초 | F | float (sec) | ≥0 | 이전 틱의 `witherForegroundSeconds` |
| 갱신 후 누적 체류초 | F' | float (sec) | ≥0 | 이번 틱 반영 후 `witherForegroundSeconds` |
| 라이브 틱 간 실경과시간 | Δt_live | float (sec) | ≥0 | 반드시 앱이 실제로 살아있는 동안의 벽시계 경과분만(오프라인 갭은 이 값에 절대 기여하지 않음) |
| 위기 진행 여부 | isWithered | bool | {0,1} | `neglectMeter=100`이며 아직 `TriggerRecovery()` 미실행 |
| 포그라운드 여부 | isForeground | bool | {0,1} | OS 생명주기 신호. false가 되는 즉시 F는 그 값에서 정지(리셋 아님, Core Rule 3) |
| 게이트 지시함수 | 1[...] | {0,1} | {0,1} | 대괄호 조건이 참이면 1, 거짓이면 0 — 둘 중 하나라도 거짓이면 F는 불변 |

**Output Range:** 단일 위기 에피소드 내에서 단조 비감소(감소 없음, 리셋만 있음) —
`TriggerRecovery()` 실행 시에만(Core Rule 5) 정확히 0으로 리셋. 이론상 상한 없음이나,
Formula 2의 게이트를 넘는 순간 에피소드 자체가 종료되므로 실질적으로
`WITHER_FREE_RECOVERY_THRESHOLD_SEC` 근방에서 관측이 끝난다.

**Example:** 감자가 `t=09:00`에 시듦 진입. 세션 1(`t=09:00~09:00:55`, 55초 포그라운드
+ 시듦 지속) → `F: 0→55`. 이후 3시간 백그라운드(오프라인 아님, 앱이 그냥 백그라운드 —
어느 쪽이든 F는 55에서 정지). 세션 2(80초 포그라운드+시듦 지속) →
`F: 55→135`. 이 두 세션 사이 실제 경과한 3시간은 `Δt_live`에 전혀 기여하지 않는다.

---

### 2. 무료 회복 트리거 게이트 (`TriggerFreeRecovery`)

The `TriggerFreeRecovery` formula is defined as:

`TriggerFreeRecovery(F) = (F ≥ WITHER_FREE_RECOVERY_THRESHOLD_SEC)`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 누적 체류초 | F | float (sec) | ≥0 | Formula 1의 결과값(`witherForegroundSeconds`) |
| 무료 회복 임계값 | WITHER_FREE_RECOVERY_THRESHOLD_SEC | float (const, sec) | **1800**(30분) | 이 값 도달 시 (b) 무료 체류 경로가 `TriggerRecovery()`를 자동 호출 |
| 결과 | TriggerFreeRecovery | bool | {0,1} | true면 즉시 `TriggerRecovery()` 호출 시도 — Core Rule 4의 원자적 경쟁 방지(광고 경로가 먼저 도달했다면 이 호출은 조용히 무시됨) 대상 |

**Output Range:** 불리언 게이트, 결정론적·단조(한 번 true가 되면 그 에피소드 안에서
계속 true — 단, `TriggerRecovery()` 실행과 동시에 F가 0으로 리셋되고 `isWithered`도
false가 되므로 재평가 자체가 무의미해진다). 임계값(1800초)은 재시도 백오프 최악
소진 시간(750초, `ad-sdk-integration-layer.md` §3)보다 크게 설계해, 광고가 완전히
불가능한 최악의 경우에도 무료 경로가 광고 재시도 소진 로직과 구조적으로 경합하지
않는다.

**Example:** 위 §1 예시 이어서 — 이후 18회의 짧은 방문(평균 약 97초, Pillar 4의
30초~2분 범위 내)을 거쳐 `F=1750`까지 누적. 다음 방문에서 51초 포그라운드+시듦
지속 → `F'=1801`. `TriggerFreeRecovery(1801) = (1801 ≥ 1800) = true` →
`TriggerRecovery()` 호출 시도(이 시점까지 광고 경로가 먼저 회복시키지 않았다면
그대로 실행) → 스탯 리셋 + 8시간 온실막 + `witherForegroundSeconds` 0으로 리셋
(Core Rule 5). 총 소요는 단일 세션으로는 불가능하고 최소 15회 이상의 개별 방문을
요구한다(2분짜리 세션을 15회 연속해도 1800초에 정확히 도달 — 구조적 하한).

---

**상수 요약**(레지스트리 등록 후보): `WITHER_FREE_RECOVERY_THRESHOLD_SEC=1800`(30분).

**설계 근거 메모(economy-designer, 값 자체는 소수의견으로 채택 안 됨)**: 300초(5분,
조정범위 240~480초)를 대안으로 제시 — "폰을 켜놓고 기다리는" 단일 시도 악용은 값의
크기와 무관하게 원천 차단 불가능하므로, 핵심은 절대 크기가 아니라 "광고보다 명백히
번거로운가"라는 논리. 사용자가 수익 보호를 우선해 1800초를 최종 채택했으나, 이
반대 논거와 아래 4가지 우려는 Open Questions로 보존한다: (1) 세션 길이 가정
(30초~2분)이 측정 데이터가 아니라 설계 가정, (2) 단일 연속 대기 악용을 막는 하드
캡이 없음(설계상 의도적 — Pillar 4 위반 방지), (3) UI 진행 표시가 리터럴 카운트다운
이면 안 됨(광고 CTA와 정보로 경쟁하게 됨 — 흐릿한 채움/글로우 권장), (4) 다중 감자
(감자 마을 이후) 등장 시 병렬 무료 회복 남용 가능성.

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드.)*

- **If 앱이 CrisisActive 중 완전히 종료되고 오래 오프라인 갭이 지속된다면**: `F`는
  그 갭 동안 전혀 증가하지 않고 직전 값에서 정지된다(Core Rule 3). 앱 재개 시
  `OnOfflineProgressApplied` 후 위기 판정을 재평가하며(Core Rule 1), 여전히
  `isWithered=true`면 CrisisActive를 그대로 재개(`F`값 그대로 유지).

- **If 새 위기 에피소드가 시작된다면(이전에 회복한 적 있음)**: `F`는 직전 에피소드의
  값을 이어받지 않고 0부터 다시 시작한다 — `TriggerRecovery()` 실행 시점(Core
  Rule 5)이 이미 0 리셋을 보장하므로 별도 처리 불필요.

- **If 신규 세이브(최초 실행) 또는 오프라인 갭 중 처음으로 시듦에 진입했다면**: `F`는
  그 순간(오프라인 갭을 마치고 `OnOfflineProgressApplied`가 발행된 직후) 정확히
  0으로 초기화된다 — 오프라인 갭 자체의 길이와 무관하게 무료 경로는 항상 순수
  라이브 시간만으로 시작한다.

- **If 기기 시계가 조작된다면**: `F`는 절대 시각의 차이(`now - lastTimestamp`)로
  계산되지 않고 매 프레임 실제 경과시간(`Time.deltaTime`류)만 누적하므로, 시계를
  뒤로 돌려도 누적값이 증가하거나 감소하는 악용이 구조적으로 불가능하다 —
  potato-stat/lifecycle이 `lastUpdatedUtc` 기반 방어 클램프를 따로 두는 것과 달리
  이 카운터는 설계상 그 방어가 필요 없다.

- **If 광고 경로와 무료 경로가 동일 프레임에 완료 조건을 동시에 충족한다면**: Core
  Rule 4의 원자적 경쟁 방지 규칙이 그대로 적용된다 — 구현 순서상 먼저 평가되는
  쪽이 승리하고 나머지는 `isWithered=false`를 감지해 무시된다.

## Dependencies

### Upstream (이 시스템이 의존)

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 감자 상태 관리 | Hard | `OnWitherEntered` 구독, `ResetToIdeal()`+`ActivateShield(8h)` 호출(양방향 확인: potato-stat States 표가 이미 이 GDD를 `TriggerRecovery()` 호출자로 명시, 2026-07-18 갱신) |
| 오프라인 진행 계산 | Hard | `OnOfflineProgressApplied` 신호 구독(Core Rule 1) — 이 신호 이전에는 위기 판정을 평가하지 않음 |
| 날씨 시스템 | Soft | `GetCurrentWeather()` — CTA 카피 플레이버에만 사용 |

**세이브/영속성 등록 스키마(신규)**: `witherForegroundSeconds`(F)는 앱 재시작을 거쳐도
보존돼야 하므로(Core Rule 5) 반드시 등록이 필요하다 —
`Register("wither_crisis_recovery", {witherForegroundSeconds})`. UTC 타임스탬프가
아닌 순수 누적 카운터이므로 `RegisterUtcTimestamp`가 아니라 일반 `Register` 패턴을
쓴다(lifecycle의 `growthHoursAccumulated`와 동일 패턴).

### Downstream (이 시스템에 의존, 전부 미설계 — 잠정 계약)

| 시스템 | 강도 | 인터페이스(잠정) |
|---|---|---|
| 부활 광고 시스템 | Hard | 되돌리기 CTA 탭 구독 + 광고 완료(`onComplete`) 시 `TriggerRecovery()` 호출 — 이 인터페이스 없이는 그 시스템이 아무 효과도 낼 수 없음(계약 확정, `revive-ad-system.md` 2026-07-18) |
| 온실막 보호 | Sibling(상호 보완, 직접 의존 아님) | 온실막 보호는 potato-stat의 `isWithered`를 **독립적으로** 읽어 설치를 게이팅한다(이미 시든 감자엔 사전 설치형 온실막이 무의미) — 이 시스템을 직접 호출하지 않는다. 둘은 공유 상태(`isWithered`)로 조정될 뿐 런타임 의존이 아님(`greenhouse-shield-system.md` 확정, 2026-07-18) |
| 알림 시스템 | Soft | 위기 진입 알림 트리거용 신호 소비 추정 — 없어도 알림만 누락될 뿐 코어 회복 루프는 유지 |
| 홈 화면 위젯 시스템 | Soft | 무료 회복 진행률(F/임계값) 축소 표시 추정 — 없어도 위젯은 표정/애니메이션의 Wither 실루엣만으로 이미 기능 |

**Hard vs Soft 판정 기준**: 다른 GDD들과 동일 — 이 시스템이 응답하지 않을 때 대상
시스템이 아예 작동을 멈추면 Hard, 연출/편의만 사라지고 핵심 루프는 유지되면 Soft.

## Tuning Knobs

| 노브 | 현재값 | 권장 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `WITHER_FREE_RECOVERY_THRESHOLD_SEC` | 1800초(30분) | 900~3600초(15분~1시간) | 단일 세션(최대 2분) 근처까지 낮아지면 "가만히 기다리기"가 광고 시청보다 심리적으로 더 쉬워져 부활 광고(기대 수익 50%) 잠식 위험(economy-designer 우려) | 재시도 소진 시간(750초)에 지나치게 근접하거나 그보다 짧아지면 광고가 완전히 불가능한 최악의 경우 무료 경로마저 사실상 도달 불가능해져 Pillar 3의 "항상 되돌릴 수 있다" 약속이 기술적 허울로 전락 |

**노브 간 상호작용**: 이 값은 `ad-sdk-integration-layer.md`의 `T_BASE_AD`/`M_AD`/
`T_CAP_AD`/`N_MAX_AD`(재시도 백오프 최악 소진 750초)와 독립적이지만 안전하게
비교돼야 한다 — 이 값이 750초보다 항상 커야 "광고 완전 불가 시나리오에서도 무료
경로가 유의미한 시간 내에 도달 가능"이라는 안전 속성이 유지된다. 재시도 로직이
변경되면 이 값도 재검증이 필요하다.

## Visual/Audio Requirements

> 이 섹션은 새로운 무드·색·형태를 정의하지 않는다 — art-bible이 이미 잠근 원칙을 이
> 시스템이 소유한 두 화면 요소(부활 광고 CTA, 무료 체류 경로의 조용한 진행 표시)에
> 그대로 적용하고, art-bible이 아직 다루지 않은 좁은 틈(진행 표시 자체의 시각 형태)만
> 기존 원칙에서 도출해 채운다(art-director 상담, 2026-07-18).

### 1. 무료 체류 경로 진행 표시 — 흐릿한 채움 확인, 신규 UI 언어는 만들지 않는다

art-bible UI/HUD §6(위기 알림 금지 원칙)은 이 질문에 절반만 답한다. "배지·점멸·
카운트다운·경고 문구 금지"라는 금지 목록은 그대로 상속되어, economy-designer가
우려한 "리터럴 카운트다운 금지" 요구를 별도 재확정 없이 이미 충족한다. 그러나 §6은
OS 위젯/알림이 감자의 시듦 상태 *자체*를 어떻게 비추는가를 다룬 것이지, 이 GDD가
신설하는 "무료 회복까지 얼마나 쌓였는가"라는 새로운 2차 신호에는 원래 선례가 없다 —
art-bible이 잠긴 시점에 이 메커닉 자체가 존재하지 않았기 때문이다. 따라서 완전히
새 시각 언어를 발명하는 대신, 이미 잠긴 장치들을 조합해 좁게 답한다.

1. **채움 메커니즘 자체는 이미 선례가 있다.** UI/HUD §4 표의 "롱프레스(난로 등)
   진행 피드백" 행이 이미 "번짐/글로우가 서서히 완성되고, 완성되는 순간 자체가 유일한
   신호이며 숫자 카운트다운은 절대 금지"라는 문법을 잠가뒀다. economy-designer의
   "흐릿한 채움/글로우" 제안은 발명이 아니라 이 기존 문법을 다른 트리거(누적 체류초)에
   재적용하는 것이므로 **확인(confirm)**한다.
2. **색은 Amber Ochre를 쓰지 않는다.** Color System §5의 Design test가 이미 "화면당
   히어로 액션은 Amber Ochre 하나뿐"이라고 못박았다 — 진행 표시가 버튼이 아니더라도
   같은 색을 쓰면 육안으로 두 번째 강조 요소처럼 읽혀 Core Rule 6의 "경쟁시키지
   않는다"는 요구를 색 채널에서 어긴다. 대신 위기/시듦 상태 자체가 이미 쓰는 팔레트
   (Lavender Ash Base/Dark, Potato Blush Dark)에서 채도를 아주 소폭 되찾는 정도로
   그친다 — Recovery 전용 골든 블룸(중심→바깥 번짐)의 채도·속도에는 명백히 못 미치게
   캡을 둔다. 이는 Character Design Direction의 "의미 예약(semantic reservation)"
   원칙과 같은 논리다 — 진짜 회복 순간의 특별함을 미리 써버리면 안 된다.
3. **모션은 연속 애니메이션이 아니라 세션 경계에서만 갱신되는 계단식이어야 한다.**
   `witherForegroundSeconds`는 라이브 틱에서만 오르고 백그라운드 전환 시 즉시
   멈춘다(Core Rule 3) — 화면이 그 값을 실시간으로 부드럽게 따라 그리며 "지켜보는
   동안 눈에 보이게 자란다"면, 숫자만 없을 뿐 사실상 "시간이 흐르는 것을 지켜보게
   만드는" 카운트다운을 형태만 바꿔 재도입하는 것이다. Section 1 원칙 3("정적인
   포근함")도 대기 중 모션을 실제 상태 변화에만 허용하므로, 권장 구현은 **각 세션이
   끝나고 다음에 화면을 열었을 때만** 그 사이 쌓인 만큼 채움이 한 단계 조용히
   짙어지는 방식(크로스페이드로 정착 — 정확한 ms는 UI Requirements에서 확정)이다.
   화면을 계속 들여다보고 있어도 채움이 눈앞에서 실시간으로 차오르지 않는다.
4. **별도 위젯보다 기존 장면 요소에 통합하는 쪽을 권장한다.** §6이 이미 "새 UI를
   만들지 않는다"는 답을 OS 위젯 층위에서 증명했다 — 같은 절제 원칙을 이 진행
   표시에도 적용해, 독립된 프로그레스 바보다 위기 장면 자체(감자의 톤 또는 배경
   워시)에 얹는 방향을 권장한다. 정확한 배치·크기는 UI Requirements에서 확정한다.

- **Design test**: 진행 표시가 (a) Amber Ochre를 쓰거나, (b) 숫자/퍼센트를 노출하거나,
  (c) 앱을 계속 보고 있는 동안 실시간으로 눈에 띄게 차오른다면 → 반려. (c)는 특히
  "카운트다운을 프로그레스 바로 위장한 것"에 불과하므로 세션 경계 계단식으로
  재설계한다.
- **Pillar 연결**: Pillar 3 (벌주지 않는 힐링 — 침묵도 안전 신호), Pillar 1 (핵심
  CTA가 항상 명확히 우선함)

### 2. 부활 광고 CTA — 동일한 히어로 버튼 언어를 그대로 재사용, 톤은 배경이 대신 말한다

시각적으로는 새 버튼 언어가 필요 없다. UI/HUD §4·§8이 이미 확정한 히어로 버튼
스펙(Amber Ochre Base 단색, Tier 2 둥근 기하, 더 큰 라운드니스+여백, 하단 중앙 엄지
도달 영역, 대기 중 펄스/글로우 금지, 프레스 시 스퀴시+번짐 반응)을 그대로 쓴다.
Player Fantasy가 이미 명시한 "축하가 아니라 다정한 구조 요청"이라는 톤 차이는
**버튼 자체의 형태·색을 바꿔서** 표현하지 않는다 — art-bible Mood & Atmosphere가
이미 10개 상태 전체에 걸쳐 "같은 UI 어휘, 다른 장면 무드"로 통일감을 지켜왔으므로
(Cross-State Quick Reference), 이 CTA도 같은 원리를 따른다. 톤의 차이는 버튼이
놓이는 **장면**이 이미 대신 말하고 있다 — 위기/시듦 화면은 이 문서 전체에서 가장
탈채도된 배경(Lavender Ash 계열)인 반면 수확 화면은 노을·황금시간대다. 같은 앰버
버튼이라도 "탈색된 조용한 장면 위에 놓인 유일한 따뜻한 색"으로 읽히는 것과 "이미
따뜻한 장면의 절정"으로 읽히는 것은 배경만으로 이미 다른 감정을 전달한다.

부수적으로 §7("앰버 오커 히어로 버튼의 카멜레온화")의 완화 규칙은 이 화면에는
적용되지 않는다 — §7이 우려한 3개 화면(폭염·수확·회복)은 배경 자체가 이미 Amber
Ochre 계열이라 버튼이 묻힐 위험이 있었지만, 위기/시듦 배경은 그 반대(탈채도·저채도)
이므로 이 CTA는 오히려 화면에서 가장 채도 높은 단일 지점으로 저절로 도드라진다 —
추가 보정 없이도 3중 인코딩(형태+여백+색)이 자연히 강하게 작동한다.

- **Design test**: 이 CTA가 Harvest CTA와 형태·색이 다르게 그려져 있다면(예: 더
  화려한 장식, 다른 강조색) → 반려 — 톤 차이는 배경 무드가 전담하며 버튼 자체는
  재사용한다. Recovery 전용 골든 블룸이나 Harvest 전용 표면 하이라이트가 버튼
  크롬에 차용되어 있다면 → 반려(의미 예약 위반).
- **Pillar 연결**: Pillar 1 (핵심 액션은 언제나 명확히 보임), Pillar 3 (죄책감 없는
  초대의 톤이 시각적으로도 강요처럼 보이지 않음)

### 3. 오디오 (범위 메모)

이 시스템은 사운드를 직접 재생하지 않는다. 이 시스템이 오디오가 구독 가능한 훅으로
노출하는 지점은 정확히 둘이며 둘 다 신규가 아니다 — CrisisActive 진입
(`OnWitherEntered`)과 `TriggerRecovery()` 실행(`OnRecovered`), 둘 다
`potato-stat-system.md` §5가 이미 노출한 훅이다. Core Rule 4(원자적 단일 실행)에
따라 광고 경로든 무료 체류 경로든 결과는 동일한 단일 `OnRecovered` 이벤트이므로, 이
GDD는 경로를 구분할 신규 오디오 훅을 추가로 열지 않는다 — 두 경로의 완료음을 톤
대비 있게 구별할지(예: 광고 경로=더 또렷한 확인음, 무료 경로=더 은은한 확인음)는 이
GDD가 결정하지 않고 열린 질문으로 남긴다.

CTA 버튼의 등장 자체(위기 진입 시 화면에 나타나는 순간)에는 §4의 "대기 중 시선을
끄는 모션 금지" 원칙과 동일 논리로 별도 등장 스팅어를 권장하지 않는다 — 조용히
화면에 존재하는 것 자체가 이 시스템의 톤이다. 실제 SFX 큐 설계(부활 성공 사운드,
광고 시청 완료음 등)는 별도 오디오 GDD(ASMR 적응형 오디오 시스템, 아직 미설계)의
몫이다.

## UI Requirements

이 시스템은 신규 풀스크린 모달을 만들지 않는다 — art-bible UI/HUD 원칙(화면 오버레이,
디에제틱 액자 아님)을 그대로 따라 메인 게임 화면 위에 오버레이로 뜨는 CTA+진행표시일
뿐이다.

**구성 요소**:
1. **감자 자체**: 표정/애니메이션 시스템이 그리는 Wither-sagging 포즈 — 이 시스템은
   소유하지 않고 그대로 배경에 둔다.
2. **부활 광고 CTA**: 화면의 유일한 히어로 액션(Core Rule 6), 하단 중앙 엄지 도달
   영역에 배치, 수확 CTA와 동일한 버튼 스펙(Visual/Audio Requirements §2). 카피는
   죄책감을 유발하는 톤을 절대 쓰지 않는다(Player Fantasy에서 이미 확정) — "언제든
   편하게" 계열의 다정한 초대 언어를 쓰되, 수확 CTA의 "축하" 톤과는 구별되는 "구조
   요청"의 뉘앙스를 카피에 담는다(정확한 문구는 `writer`/`ux-designer` 후속 과제).
3. **무료 체류 진행 표시**: 독립 위젯이 아니라 위기 장면 요소(감자의 톤 또는 배경
   워시)에 통합된 조용한 채움 신호(Visual/Audio Requirements §1) — 세션 경계에서만
   갱신, 실시간 카운트다운 아님, 숫자/퍼센트 노출 없음.

**터치 케어 제스처와의 관계**: 이 화면이 떠 있는 동안에도 창문/난로/물뿌리개
제스처는 여전히 화면에 존재하지만 potato-stat의 `IsWithering()`에 의해 비활성화된
상태다(`touch-care-interaction.md` 확정) — 이 시스템은 그 제스처들을 숨기거나
제거하지 않는다, 단지 조작 무효 상태로 둘 뿐이다(정확한 게이팅 UX는 그 시스템 소관).

**다운스트림 UI 표면과의 경계**: 홈 화면 위젯의 무료 회복 진행률 축소 표시(선택,
Dependencies §Downstream)는 이 시스템이 노출하는 `witherForegroundSeconds`/
`WITHER_FREE_RECOVERY_THRESHOLD_SEC` 값을 그대로 참조할 뿐, 화면 레이아웃 자체는
홈 화면 위젯 시스템 GDD 소관이다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션.)*

아래 기준은 Detailed Design·Formulas·Edge Cases·Dependencies에서 정의된 필드·상수명을
그대로 사용한다(`witherForegroundSeconds`(F), `isWithered`, `isForeground`,
`WITHER_FREE_RECOVERY_THRESHOLD_SEC`, `TriggerRecovery()`). "상태(State)"는 Dormant/
CrisisActive 두 값뿐이며 `isWithered`로부터 파생된다 — `witherForegroundSeconds`(F)만
이 시스템이 직접 소유·영속하는 필드다. `TriggerRecovery()`는 호출자가 광고 경로든 무료
체류 경로든 동일한 사후 조건(potato-stat 리셋+온실막, F=0 리셋, Dormant 전이)을 가지므로,
아래 기준은 실제 부활 광고 시스템(미설계)의 구현 세부와 무관하게 `TriggerRecovery()`를
직접 호출하는 시나리오로도 검증 가능하도록 작성했다.

### 상태 전이

- **AC-ST-01 (Dormant 초기값)**
  **GIVEN** 신규 세이브(최초 실행) 또는 직전 에피소드가 이미 회복된 상태
  **WHEN** 시스템을 초기화/조회한다
  **THEN** 상태 = Dormant, `witherForegroundSeconds = 0`.

- **AC-ST-02 (Dormant → CrisisActive)**
  **GIVEN** 상태 = Dormant
  **WHEN** `OnWitherEntered` 이벤트를 수신한다(Core Rule 1의 게이트를 이미 통과한 시점 — AC-INT-01·02 참고)
  **THEN** 상태 = CrisisActive로 전이하고, 이후 포그라운드+시듦 조건이 충족되는 매 틱마다 `witherForegroundSeconds` 카운트가 시작된다.

- **AC-ST-03 (CrisisActive → Dormant, 광고 경로)**
  **GIVEN** 상태 = CrisisActive
  **WHEN** 부활 광고 시스템이 광고 시청 완료를 보고하며 `TriggerRecovery()`를 호출한다
  **THEN** potato-stat의 `ResetToIdeal()` + `ActivateShield(8h)`가 호출되고, `witherForegroundSeconds = 0`으로 리셋되며, 상태 = Dormant로 전이한다.

- **AC-ST-04 (CrisisActive → Dormant, 무료 체류 경로)**
  **GIVEN** 상태 = CrisisActive, `F = WITHER_FREE_RECOVERY_THRESHOLD_SEC`(1800) 도달
  **WHEN** `TriggerFreeRecovery(F) = true`가 되어 이 시스템이 자체적으로 `TriggerRecovery()`를 호출한다
  **THEN** 광고 경로와 동일하게 `ResetToIdeal()` + `ActivateShield(8h)`가 호출되고, `F = 0`, 상태 = Dormant로 전이한다.

- **AC-ST-05 (동시 완료 경쟁 — 승자 단독 실행)**
  **GIVEN** 상태 = CrisisActive, 광고 경로와 무료 체류 경로가 거의 동시에 완료 조건을 충족
  **WHEN** 먼저 평가되는 경로가 `TriggerRecovery()`를 실행한다
  **THEN** `ResetToIdeal()` + `ActivateShield(8h)`는 정확히 1회만 실행되고, 상태는 정확히 1회만 Dormant로 전이한다(이중 실행 없음).

- **AC-ST-06 (동시 완료 경쟁 — 패자는 안전한 no-op)**
  **GIVEN** AC-ST-05와 동일 상황, 승자 경로가 이미 `TriggerRecovery()`를 실행해 상태 = Dormant가 된 직후
  **WHEN** 나중에 도달한 경로가 자신의 완료 콜백(광고 시청 완료 보고 또는 임계값 게이트)을 실행 시도한다
  **THEN** 이 시스템은 `isWithered = false`를 감지해 콜백을 조용히 무시하고, `ResetToIdeal()`/`ActivateShield()`는 추가 호출되지 않으며(온실막 이중 연장 없음), `witherForegroundSeconds`도 추가로 변경되지 않는다.

- **AC-ST-07 (CrisisActive 유지 — 오프라인 갭을 거친 재개)**
  **GIVEN** 상태 = CrisisActive, `F = 200`, 앱이 완전히 종료되고 긴 오프라인 갭 발생
  **WHEN** 앱이 재개되고 `OnOfflineProgressApplied` 수신 후 위기 판정을 재평가했을 때 여전히 `isWithered = true`
  **THEN** 상태 = CrisisActive로 그대로 재개되고, `F`는 갭 이전 값(200)에서 그대로 이어서 카운트를 시작한다(0으로 재시작하지 않음).

### 공식 검증

**Formula 1 — `witherForegroundSeconds` 갱신**

- **AC-F-01 (F=0 경계 — 위기 진입 직후 최초 누적)** **GIVEN** `F=0`(방금 CrisisActive 진입), `isWithered=true`, `isForeground=true` **WHEN** `Δt_live=1s` 경과 **THEN** `F'=1`.
- **AC-F-02 (라이브 누적 — 기본 사례)** **GIVEN** `F=0`, `isWithered=true`, `isForeground=true` **WHEN** `Δt_live=55s` 경과 **THEN** `F'=55`.
- **AC-F-03 (백그라운드 전환 — 정지, 리셋 아님)** **GIVEN** `F=55`(직전 라이브 세션 종료 값) **WHEN** `isForeground=false`로 전환된 채 3시간 경과 **THEN** `F`는 변함없이 `55`(백그라운드 경과시간은 `Δt_live`에 전혀 기여하지 않음 — 0으로 리셋되지도 않음).
- **AC-F-04 (포그라운드 복귀 후 이어서 누적)** **GIVEN** AC-F-03 직후 `F=55`, `isWithered=true` 유지 **WHEN** 포그라운드 복귀 후 `Δt_live=80s` 경과 **THEN** `F'=135`.
- **AC-F-05 (게이트 — `isWithered=false`면 F 불변)** **GIVEN** `F=100`, `isForeground=true`이나 `isWithered=false`(위기 아님) **WHEN** 임의 시간 경과 **THEN** `F`는 `100`으로 불변(지시함수 `1[...]=0`).
- **AC-F-06 (게이트 — 두 조건 중 하나만 거짓이어도 불변)** **GIVEN** `F=100`, `isWithered=true`이나 `isForeground=false` **WHEN** 임의 시간 경과 **THEN** `F`는 불변(OR 조건 중 하나만 거짓이어도 정지 — Core Rule 3).

**Formula 2 — `TriggerFreeRecovery` 임계값 게이트**

- **AC-F-07 (F=0 경계 — 게이트 거짓)** **GIVEN** `F=0` **WHEN** `TriggerFreeRecovery(F)` 평가 **THEN** `false`, `TriggerRecovery()` 호출 시도 없음.
- **AC-F-08 (임계값 직전 — 거짓, 상한 경계)** **GIVEN** `F = WITHER_FREE_RECOVERY_THRESHOLD_SEC - 1`(1799) **WHEN** 평가 **THEN** `false` — 위기 지속, `F`는 다음 틱에도 계속 누적 대상.
- **AC-F-09 (임계값 정확히 도달 — 참, 경계 포함)** **GIVEN** `F = WITHER_FREE_RECOVERY_THRESHOLD_SEC`(1800, 정확히 등호) **WHEN** 평가 **THEN** `true`(`≥` 연산자이므로 경계값 포함) → `TriggerRecovery()` 호출 시도.
- **AC-F-10 (임계값 초과 — 참)** **GIVEN** `F=1801` **WHEN** 평가 **THEN** `true`.
- **AC-F-11 (트리거 직후 재평가 무의미 — F 리셋과 동시 발생)** **GIVEN** `F=1800`에서 `TriggerFreeRecovery(F)=true`로 `TriggerRecovery()`가 실행됨(다른 경로가 선점하지 않았다고 가정) **WHEN** 실행 완료 직후 게이트를 재평가 **THEN** `F=0`(Core Rule 5)이 이미 반영되어 `TriggerFreeRecovery(0)=false` — 동일 프레임 내 재호출·이중 트리거 없음.
- **AC-F-12 (다회 방문 누적 — 회귀 기준선)** **GIVEN** 18회 방문 누적으로 `F=1750`(GDD §2 예시) **WHEN** 다음 방문에서 `Δt_live=51s` 추가 **THEN** `F'=1801`, `TriggerFreeRecovery(1801)=true`.

### 엣지 케이스

- **AC-EC-01 (오프라인 갭 중 CrisisActive 동결 및 재개)** **GIVEN** 상태=CrisisActive, `F=200`, 앱이 완전히 종료된 채 긴 오프라인 갭 지속 **WHEN** 앱 재개 후 `OnOfflineProgressApplied` 신호 수신 뒤 위기 판정을 재평가 **THEN** `F`는 갭 동안 전혀 증가하지 않고 정확히 `200`으로 유지되며, 여전히 `isWithered=true`면 CrisisActive를 `F=200`에서 그대로 재개한다.
- **AC-EC-02 (신규 위기 에피소드는 F=0부터 재시작)** **GIVEN** 직전 에피소드가 `TriggerRecovery()`로 회복 완료(`F=0`으로 리셋된 상태), 이후 새로운 `OnWitherEntered` 발생 **WHEN** 새 CrisisActive 진입 **THEN** `F`는 직전 에피소드 값을 이어받지 않고 정확히 `0`부터 시작한다(Core Rule 5의 리셋이 이미 보장하므로 이 기준은 회귀 확인용).
- **AC-EC-03 (신규 세이브/오프라인 갭 중 최초 시듦 진입 시 F 초기값)** **GIVEN** 신규 세이브 또는 오프라인 갭 도중 처음으로 `neglectMeter=100`(위기 최초 진입) **WHEN** `OnOfflineProgressApplied` 신호 발행 직후 위기 판정을 평가 **THEN** `F`는 오프라인 갭의 실제 길이와 무관하게 정확히 `0`으로 초기화되고, 이후 순수 라이브 시간만으로 누적을 시작한다.
- **AC-EC-04 (기기 시계 조작 방어)** **GIVEN** `F=500` **WHEN** 기기 시계가 과거로 조작된 상태에서 포그라운드+시듦 지속 100초(프레임 단위 경과시간 기준, `Time.deltaTime`류)가 실제로 경과 **THEN** `F=600` — 절대 시각 차이(`now - lastTimestamp`)가 아니라 프레임 경과시간 누적 방식이므로 시계 조작으로 `F`가 비정상적으로 증가·감소하지 않는다.
- **AC-EC-05 (동일 프레임 경쟁 — 원자적 단일 실행)** **GIVEN** 상태=CrisisActive, 광고 경로 완료 콜백과 무료 경로 임계값 도달(`F≥1800`)이 동일 프레임/틱에서 함께 조건을 충족 **WHEN** 두 경로가 모두 `TriggerRecovery()` 호출을 시도 **THEN** `ResetToIdeal()`+`ActivateShield(8h)`는 정확히 1회만 실행되고, 나중에 평가된 경로는 `isWithered=false`를 감지해 조용히 무시한다(no-op, 이중 온실막 연장 없음) — AC-ST-05/06과 동일 속성의 동일-프레임 특수 사례.

### 통합/인터페이스

- **AC-INT-01 (오프라인 배치 완료 전 위기 판정 금지)** **GIVEN** 콜드 스타트 또는 포그라운드 복귀 직후, 오프라인 진행 계산이 아직 `OnOfflineProgressApplied`를 발행하지 않은 상태(배치 계산 중이라 `neglectMeter`가 stale일 수 있음) **WHEN** 이 시점에 `neglectMeter=100`으로 읽힌다 하더라도 **THEN** 이 시스템은 위기 판정을 평가하지 않으며 `OnWitherEntered` 구독 콜백도 이 시점에는 트리거되지 않는다(Core Rule 1).
- **AC-INT-02 (배치 완료 후 즉시 재평가 — 판정 누락 방지)** **GIVEN** `OnOfflineProgressApplied` 신호가 방금 발행 완료, 이 시점의 `neglectMeter=100`(오프라인 배치가 확정한 최종값) **WHEN** 이 시스템이 신호를 수신한다 **THEN** 같은 프레임에 즉시 위기 판정을 평가해 상태=CrisisActive로 전이한다(콜드 스타트에서도 위기 판정이 누락되지 않음).
- **AC-INT-03 (`ResetToIdeal` 배타적 권한 / `ActivateShield` 공용 역량)** **GIVEN** 임의의 회복 경로(광고 또는 무료 체류)가 완료 조건을 충족 **WHEN** `TriggerRecovery()`가 실행된다 **THEN** potato-stat의 **`ResetToIdeal()`은 오직 이 함수를 통해서만** 호출된다 — 이 시스템 밖의 다른 경로(예: 부활 광고 시스템이 potato-stat을 직접 호출)로 `ResetToIdeal()`이 호출되는 코드 경로가 존재하지 않는지 정적으로 확인한다. 반면 **`ActivateShield(8h)`는 공용 역량**으로, 이 함수(회복 시 `ResetToIdeal()`과 번들)와 온실막 보호 시스템(능동 설치 시 단독 호출, `greenhouse-shield-system.md` 확정, 2026-07-18) 두 호출자를 가진다 — 따라서 검증 대상은 "회복 경로가 `ActivateShield`을 호출할 때 반드시 `ResetToIdeal()`과 원자적으로 함께 실행하는가"이며, `ActivateShield` 자체의 단독 호출(능동 설치)은 금지 대상이 아니다.
- **AC-INT-04 (세이브 영속성 등록 및 재시작 보존)** **GIVEN** `witherForegroundSeconds = F`(0이 아닌 임의값)인 채로 앱을 재시작(세이브 후 로드) **WHEN** 세이브 로드가 완료된다 **THEN** `Register("wither_crisis_recovery", {witherForegroundSeconds})`를 통해 로드된 `F` 값이 재시작 전과 정확히 동일하며, 일반 `Register` 패턴(‑`RegisterUtcTimestamp`가 아님)이므로 `lastUpdatedUtc` 기반 `IsTimestampValid` 클램프가 이 필드에는 적용되지 않는다.
- **AC-INT-05 (날씨 조회는 구조적 판정에 관여하지 않음 — Soft 경계 확인)** **GIVEN** `GetCurrentWeather()`가 임의의 값을 반환(또는 조회 실패/미구현)한 상태 **WHEN** 위기 판정(`OnWitherEntered` 처리)·`F` 누적·`TriggerFreeRecovery` 게이트·`TriggerRecovery()` 실행 흐름을 평가한다 **THEN** 날씨 조회 결과와 무관하게 판정 결과가 동일하다 — 날씨는 CTA 카피 플레이버에만 사용되며 이 시스템의 어떤 구조적 계산에도 입력으로 쓰이지 않는다.

### 이 GDD 범위 밖으로 명시적으로 남긴 것 (테스트 불가 항목)

- **광고 시청 자체의 메커닉**(로딩·재생·시청 완료 판정·재시도 백오프)은 아직 미설계인
  부활 광고 시스템 소관 — 위 AC들은 그 시스템이 `TriggerRecovery()`를 호출하는 시점부터만
  검증하며, 광고 SDK 통합 자체는 별도 GDD 작성 시 검증한다.
- **CTA의 정확한 UI/카피**(문구 톤, 진행 표시의 시각적 형태, "흐릿한 채움/글로우" 등
  economy-designer가 우려로 남긴 구현 디테일)는 아직 작성되지 않은 이 GDD의 UI
  Requirements 섹션 소관 — 위 기준은 데이터/로직 계약만 검증하며 화면 레이아웃은 다루지
  않는다.
- **무료 회복 임계값(`WITHER_FREE_RECOVERY_THRESHOLD_SEC=1800`)의 실제 수익화 영향**은
  설계 시점 AC로 검증할 수 없다 — 부활 광고 잠식 여부는 라이브 분석(애널리틱스) 데이터로만
  판정 가능하며, Tuning Knobs 섹션이 이미 이 불확실성을 Open Questions로 보존하고 있다.
- **터치 케어 인터랙션의 Wither 중 제스처 게이팅 자체**(세 제스처 비활성화 로직)는
  `touch-care-interaction.md` 소관 — 이 GDD는 그 게이팅이 이미 걸려 있다고 가정만 할 뿐,
  게이팅의 정확성은 이 문서의 AC 대상이 아니다.

## Open Questions

| 질문 | 배경 | 담당 | 해소 시점 |
|---|---|---|---|
| ~~온실막 보호와의 정확한 인터페이스(`isWithered` 조회 목적 확인)~~ **해결됨(`greenhouse-shield-system.md`, 2026-07-18)**: 온실막 보호는 potato-stat의 `isWithered`를 독립적으로 읽어 설치 어포던스를 게이팅(시듦 중 설치 불가)할 뿐 이 시스템을 직접 호출하지 않는다 — 상호 보완(Sibling) 관계로 확정 | — | 해결됨 |
| ~~부활 광고 시스템의 `TriggerRecovery()` 호출 정확한 시그니처·타이밍~~ **해결됨(`revive-ad-system.md`, 2026-07-18)**: 되돌리기 CTA 탭 구독 → ad-sdk `ShowAd("revive")`(대기-후-재생, `REVIVE_CTA_WAIT_TIMEOUT_SEC=35`) → `onComplete` 시 `TriggerRecovery()` 정확히 1회 호출. 중도이탈/실패는 미호출 | — | 해결됨 |
| 알림 시스템이 위기 진입을 언제·어떻게 알릴지 | `OnWitherEntered`를 구독할 것으로 추정하나 알림 문구·타이밍 미확정 | ux-designer | 알림 시스템 `/design-system` 진행 시 |
| 홈 화면 위젯이 무료 회복 진행률을 표시할지, 표시한다면 어떤 형태로 | Dependencies에서 Soft 추정 의존으로만 남김 | art-director/ui-programmer | 홈 화면 위젯 시스템 `/design-system` 진행 시 |
| `WITHER_FREE_RECOVERY_THRESHOLD_SEC=1800`의 실제 부활 광고 잠식 여부 | systems-designer(1800s)와 economy-designer(300s, 240~480s 밴드) 권장값이 갈렸고 사용자가 전자를 채택 — 실측 데이터 없이는 검증 불가 | analytics-engineer | 소프트런칭/라이브옵스 단계, Tuning Knobs 안전범위(900~3600s) 내 재조정 가능성 열어둠 |
| ~~광고 경로 vs 무료 체류 경로의 완료 사운드를 구별할지~~ | **해결됨(2026-07-21)** — `asmr-adaptive-audio.md`가 구별 안 함으로 확정: `OnRecovered` 인터페이스 자체가 경로 파라미터를 노출하지 않으므로(순수 구독자 원칙), 두 경로 모두 동일한 단일 완료 SFX로 처리 | — | 해소됨 |
| 무료 체류 진행 표시의 정확한 크로스페이드 길이·화면 내 배치 | Visual/Audio Requirements §1이 "세션 경계 계단식" 원칙만 확정, 정확한 ms·좌표는 미정 | art-director/ui-programmer | 실제 UI 제작 전 |
