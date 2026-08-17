# 온실막 보호 (Greenhouse Shield)

> **Status**: In Design
> **Author**: user + agents
> **Last Updated**: 2026-07-18
> **Implements Pillar**: Pillar 3 (벌주지 않는 힐링), Pillar 4 (짧게 자주 부담없이)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode

## Overview

온실막 보호는 플레이어가 감자를 안심하고 두고 나갈 수 있도록, 지정한 시간 동안 방치의
위험을 완전히 차단하는 보호막을 **위기가 오기 전에 미리** 설치하는 시스템이다. 데이터
계층에서 보면, 이 시스템은 감자 상태 관리(Foundation)가 소유한 `shieldActiveUntil`
타임스탬프를 미래로 밀어 `isShielded=true`를 만드는 **유일한 능동적(플레이어 개시)
경로**다 — 이 값이 미래인 동안 방치 지표(`neglectMeter`) 누적이 0으로 멈추므로
(potato-stat Formula §4), 오프라인 갭이 아무리 길어도 그 보호 창(window) 안에서는
감자가 시들 수 없다. 플레이어 경험에서 보면, 이 시스템은 game-concept.md가 핵심
리텐션 훅으로 명명한 *"취침·업무 전 8시간 온실막을 미리 설치하고 안심하고 나가기"*라는
능동적 안전 최적화 행동 그 자체다 — 위기가 닥친 뒤 되돌리는 것(시듦/위기 & 회복 소관)이
아니라, 위기가 오기 전에 스스로 지켜두는 예방적 돌봄이다.

이 시스템이 소유하는 것과 소유하지 않는 것의 경계는 분명하다. **회복 경로가 부여하는
온실막**(시듦 → 광고/무료 회복 → 8시간 보호)은 이미 시듦/위기 & 회복 시스템과 감자
상태 관리가 소유·완결했다 — 이 시스템은 그 경로를 재구현하지 않는다. 대신 이 시스템은
감자가 **아직 시들지 않은 평상시에**, 플레이어가 광고 시청이라는 능동적 대가를 치르고
같은 8시간 보호를 미리 획득하는 **두 번째 경로**를 신설한다. 이 시스템이 없다면 온실막은
오직 감자를 한 번 시들게 만든 뒤에야 얻을 수 있고, 콘셉트가 약속한 "미리 지켜두고 부담
없이 나가는" 예방적 안심은 성립하지 않는다.

## Player Fantasy

> *(creative-director 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

이 시스템의 판타지는 **"떠나기 전에 이불을 덮어주는 마음"** 이다 — 위기에 쫓겨서가
아니라, 스스로 원해서 미리 지켜두는 능동적 돌봄. 플레이어는 잠들기 전이나 일하러 나가기
전 감자에게 온실막을 씌워주는 작은 의식을 치르고 "이제 마음 놓고 가도 돼"라는 예방적
평온을 얻는다. 이 감정은 시듦/위기 & 회복의 판타지("괜찮아, 되돌릴 수 있어" — 위기
*이후*의 안도)와 명확히 구별된다: 저쪽이 넘어진 뒤 일으켜 세우는 구조라면, 이쪽은
넘어지지 않게 미리 손을 잡아두는 예방이다. Pillar 2(지켜주고 싶은 마음)가 반응이 아니라
앞선 배려의 형태로 나타나는 순간이다.

두 번째 층위는 **"내가 없어도 괜찮다"는 조용한 신뢰**다 — 설치하고 나간 뒤 앱을 다시
열었을 때, 감자가 정확히 약속대로 무사한 것을 발견하는 안도(오프라인 진행 계산이 그
정확성을 보증). 이 판타지는 Pillar 4("*…안심하고 나갈 수 있다. 게임이 플레이어의 시간을
인질로 잡지 않는다*")를 직접 구현하며, game-concept.md Key Dynamics가 명명한 "오프라인
안전 최적화 행동"이 실제 정서 보상을 갖게 만든다.

**설계 가드레일(중요)**: 능동 설치는 광고로 게이팅되므로, 판타지가 "광고로 불안을
없앤다"로 읽히면 Pillar 3의 안티필러(공포 마케팅 과금)를 정면 위반한다. 이를 막는 구조적
근거는 potato-stat §6이 이미 확정했다 — 하루 2번 체크인이면 온실막 없이도 항상 안전
(시듦까지 최소 18.65h)하므로, 온실막은 "안 하면 죽는다"의 공포 해소가 아니라 8시간+
자리를 비우는 상황을 위한 **편의·돌봄 의식**이다. 게임은 불안을 먼저 만들고 그 해소를
팔지 않는다 — 온실막은 이미 안전한 플레이어가 "더 오래, 더 마음 편히" 있기 위한
선택지다.

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 상담 필수. 4개 핵심 설계 포크
> (광고 연동/중첩 규칙/시듦 게이팅/남용 방지)는 사용자가 직접 결정.)*

### Core Rules

1. **능동 설치 트리거**: 플레이어가 온실막 설치 어포던스(UI Requirements 소관)를
   조작하면, 이 시스템은 광고 SDK 통합 계층에 `RequestAd("shield")`/`ShowAd("shield")`를
   요청한다. 광고 시청이 **완료**로 콜백되는 순간에만 온실막이 부여된다(중도이탈·실패
   시 부여 없음, 상태 불변).

2. **온실막 부여 = `shieldActiveUntil` SET**: 광고 완료 시(단, Core Rule 7의 부여 시점 시듦
   재확인을 통과한 경우에만) 이 시스템은 감자 상태 관리의
   `ActivateShield(SHIELD_DURATION)`을 호출한다 — 이는 `shieldActiveUntil = now +
   SHIELD_DURATION(8h)`로 **덮어쓴다(SET)**, 절대 기존 잔여에 누적하지 않는다. 회복
   경로(`TriggerRecovery`)와 정확히 같은 SET 의미론이라, 두 경로 중 어느 쪽이 마지막이든
   결과는 항상 "그 시각부터 8시간"이다. **`ResetToIdeal()`은 호출하지 않는다** — 능동
   설치는 감자를 "되돌리는" 게 아니라 "지켜두는" 것이므로 스탯을 건드리지 않는다.

3. **시듦 게이팅**: 능동 설치는 `isWithered == false`일 때만 가능하다. `isWithered ==
   true`(neglectMeter=100)이면 설치 어포던스를 비활성화/숨긴다 — 이미 시든 감자에 온실막을
   씌우면 방치 지표를 100에 고정할 뿐 시듦을 풀지 못해 무의미하고, 시듦 상태의 유일한
   회복 액션은 시듦/위기 & 회복의 되돌리기 CTA다. 단 **AtRisk(스탯 이탈·neglect<100)에서는
   설치가 유효·허용**된다 — 온실막이 그 시점 누적을 동결시켜 "위험하지만 지금 나가야
   하는" 상황을 안전하게 만든다.

4. **중첩 없음(SET 귀결)**: 온실막이 이미 활성(`isShielded=true`)인 동안 재설치해도
   스택되지 않고 그 시각 기준 8시간으로 갱신될 뿐이다. 잔여가 많을 때(7h) 재설치는 이득이
   거의 없고(7h→8h) 잔여가 적을 때(1h)만 유의미한 top-up이 된다 — 이 자연 억제 덕에
   별도 쿨다운·횟수 제한이 없다.

5. **광고 미가용 시 실패 처리**: SDK가 실패(no-fill/오프라인/타임아웃)를 콜백하면 온실막은
   부여되지 않고 상태가 불변이며, 다정한 실패 안내(카피는 UI Requirements)를 제시한다.
   능동 설치는 위기가 아니므로(감자는 여전히 안전) 광고 실패가 Pillar 3 위반이 되지
   않는다 — revive처럼 사전로드하지 않고 온디맨드로 요청하는 근거.

6. **오프라인 배치 후에만 게이팅 확정**: 콜드 스타트/포그라운드 복귀 직후에는
   `OnOfflineProgressApplied`(오프라인 진행 계산) 신호 이후에만 게이팅 상태(`isWithered`/
   `isShielded`)를 확정한다 — 배치 전 stale 상태로 어포던스를 잘못 활성/비활성화하지 않기
   위함(wither-recovery Core Rule 1과 동일 논리).

7. **부여 시점 시듦 재확인(레이스 방어)**: 광고 완료 콜백이 `ActivateShield`을 호출하기
   직전에 `isWithered`를 **재확인**한다 — 광고 시청 중(Requesting) 감자가 시들어
   (`neglectMeter=100`) `isWithered=true`가 됐다면 온실막을 부여하지 않고 조용히
   no-op으로 처리한다(광고 실패와 동일한 무부여 패턴, 상태는 Requesting→Blocked). 이
   방어가 없으면 `isShielded=true`와 `isWithered=true`가 동시에 성립하는 부정합 상태
   (온실막이 무의미하게 소비됨)가 발생할 수 있다 — Core Rule 2·3의 게이팅이 탭 시점에만
   걸려 있고 부여는 광고 완료 시점에 일어나는 시차 때문이다. 이 경우 플레이어는 시듦
   화면의 되돌리기 CTA(시듦/위기 & 회복 소관)로 안내된다. (발생 창은 극히 좁다 — 탭 시점
   `neglectMeter`가 광고 지속시간 내 100에 도달할 만큼 임계에 근접해 있어야 하므로.)

### States and Transitions

이 시스템은 **자체 영속 상태를 소유하지 않는다**(`shieldActiveUntil`은 potato-stat 소유,
save-persistence에 이미 등록됨 — 이 시스템은 신규 세이브 필드를 추가하지 않는다). 설치
어포던스의 가용성은 potato-stat 상태에서 파생되고, 광고 플로우만 전이적으로 존재한다.

| 상태 | 의미 | 파생/전이 |
|---|---|---|
| **Available** | 설치 가능 — 어포던스 활성 | `isWithered=false`에서 파생(Healthy/AtRisk 무관, `isShielded=true`여도 top-up 가능) |
| **Blocked** | 설치 불가 — 어포던스 **완전히 숨김**(비활성-표시 아님, UI Requirements 확정) | `isWithered=true`에서 파생 |
| **Requesting** | 광고 요청·시청 진행 중(전이적) | Available→Requesting(설치 조작); 완료 시 Core Rule 7 재확인 후 — 시듦 아니면 `ActivateShield(8h)` 후 Available 복귀, 시듦이면 미부여 후 Blocked; 실패/이탈→Available 복귀(불변) |

### Interactions with Other Systems

| 시스템 | 관계 | 인터페이스 |
|---|---|---|
| 감자 상태 관리 | 호출(쓰기)+조회 | `ActivateShield(SHIELD_DURATION)` 호출(shieldActiveUntil SET) — 회복 경로와 **공유하는 공용 역량**. `isWithered`/`isShielded` 조회로 게이팅·top-up 판정. `ResetToIdeal()`은 호출 안 함 |
| 광고 SDK 통합 계층 | 의존(호출자) | 신규 `RequestAd("shield")`/`ShowAd("shield")`, 온디맨드(사전로드 없음), `RANK_SHIELD` 최저 우선순위 |
| 시듦/위기 & 회복 | 조회(Soft) | `isWithered`(potato-stat 경유)로 게이팅 — 시듦 중엔 설치가 물러나고 회복 CTA가 유일. wither-recovery Open Question("온실막 보호와의 인터페이스") 확정: 직접 호출 없이 공유 상태만 읽음 |
| 오프라인 진행 계산 | 구독 | `OnOfflineProgressApplied` 이후에만 게이팅 확정(Core Rule 6). offline-calc가 `shieldActiveUntil`을 분할점으로 읽으므로 이 시스템이 쓴 값이 다음 갭 계산에 반영됨(간접) |

## Formulas

> *(systems-designer 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션. 이 시스템은
> 얇은 오케스트레이션 계층이라 자체 소유 공식이 적다 — 대부분의 수학은 potato-stat·
> ad-sdk·save-persistence가 이미 소유하며 여기서는 참조만 한다.)*

### 1. 설치 가용성 판정 (`CanInstall`) — 이 시스템 소유

The `CanInstall` formula is defined as:

`CanInstall(isWithered) = ¬isWithered`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 시듦 여부 | `isWithered` | bool | {0,1} | potato-stat 소유 플래그, `neglectMeter==100`일 때만 true |
| 설치 가능 여부 | `CanInstall` | bool | {0,1} | 이번 프레임 설치 어포던스 활성/탭 가능 여부 |

**Output Range:** 불리언, 클램프 없음(전함수 — `isWithered` 두 값 모두에 정의됨).

**의도된 제외**: `isShielded`는 입력이 아니다 — Core Rule 4대로 `isWithered=false`인 한
이미 보호 중(방금 부여 직후 포함)이어도 top-up 설치가 허용된다. 향후 구현자가 "친절하게"
`isShielded` 가드를 추가해 top-up UX를 깨는 것을 막기 위해 명시적으로 표에 기록한다.

**Example:** `isWithered=false`(Healthy, 또는 AtRisk 예: 수분=15) → `CanInstall=true`.
`isWithered=true`(방금 시듦) → `CanInstall=false`, 어포던스 숨김, wither-recovery CTA가
유일 경로가 됨.

---

### 2. 온실막 부여 (SET) — 참조 전용 (정식 소유자: potato-stat)

이 시스템은 광고 완료 시 `ActivateShield(SHIELD_DURATION)`을 호출한다. 이 mutator의
정식 정의:

`shieldActiveUntil' = t_call + SHIELD_DURATION` (항상 SET, 절대 누적 아님)

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 부여 시각 | `t_call` | float, UTC | — | `ActivateShield`이 실행되는 순간(=광고 완료 콜백 시각) |
| 보호 지속 | `SHIELD_DURATION` | float (const), h | 8 (레지스트리) | 이 시스템은 항상 이 값을 전달 |
| 새 만료시각 | `shieldActiveUntil'` | float, UTC | — | 항상 덮어쓰기(SET) |

**참조 계약**: `ActivateShield`은 potato-stat 소유 필드 `shieldActiveUntil`을 쓴다 — 두
호출자를 갖는다(회복 경로는 `ResetToIdeal()`과 번들, 이 시스템은 **단독 호출**).
`isShielded = now < shieldActiveUntil` 및 방치 억제(`dN/dt`의 `(1-isShielded)` 계수)는
potato-stat Formulas §4(레지스트리 `NEGLECT_ACCUMULATION`) 소유이므로 여기서 재정의하지
않는다.

> ⚠️ **systems-designer 발견**: potato-stat은 현재 `ActivateShield(duration)`을 독립 공식
> 항목으로 두지 않고 회복 프로세스 산문에 `ResetToIdeal()`과 묶어서만 서술한다(line 51).
> 이 시스템이 **최초로 단독 호출**하는 호출자이므로, potato-stat Formulas에 독립 항목
> 추가를 권장한다(Section F 교차 수정 후보 — 아래 Dependencies 참고).

---

### 3. Top-up 한계이득 (`ΔP`) — 검증 전용 공식(런타임 미호출)

Core Rule 4의 "잔여가 많을 때 재설치는 무의미"라는 수치 주장을 뒷받침하는 검증 공식이다
(런타임에 매 프레임 계산되지 않음 — potato-stat §6 "시듦까지의 시간 정합성 검증"과 같은
검증-전용 범주).

`R_remaining(t) = max(0, shieldActiveUntil_prev - t)`
`ΔP(t) = clamp(SHIELD_DURATION - R_remaining(t), 0, SHIELD_DURATION)`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 부여 시각 | `t` | float, UTC h | — | 이 설치/top-up 완료 순간(광고 완료 콜백 시각) |
| 직전 만료시각 | `shieldActiveUntil_prev` | float, UTC h | — | 이 부여 직전 `shieldActiveUntil` 값 |
| 보호 지속 | `SHIELD_DURATION` | float (const), h | 8 | 재사용(재정의 아님) |
| 잔여 보호 | `R_remaining(t)` | float, h | [0, 8] | 아무것도 안 해도 유지될 보호시간 |
| 한계이득 | `ΔP(t)` | float, h | [0, 8] | 지금 설치해서 실제로 새로 얻는 보호시간 |

**Output Range:** [0, 8]시간 폐구간. 바깥 `clamp`은 SET 불변식상(부여 직후 `shieldActiveUntil`은
"지금+8h"를 넘을 수 없음) 이론적으로 잉여지만, 손상/조작된 `shieldActiveUntil`이 이
공식에 도달할 경우의 방어로 유지한다.

**Example:** 12:00에 설치 → `shieldActiveUntil=20:00`. 19:00 재탭(`R_remaining=1h`) →
`ΔP = 8-1 = 7h`(유의미한 top-up). 같은 설치에서 13:00 재탭(`R_remaining=7h`) →
`ΔP = 8-7 = 1h`(거의 무의미 — 이 값이 "쿨다운 불필요"의 수치 근거).

---

### 4. 광고 우선순위 상수 (`RANK_SHIELD`) — 요구사항만, 소유자: ad-sdk

`Rank(adType)` 표와 상수는 ad-sdk-integration-layer 소유(현재 `RANK_REVIVE=0`/
`RANK_GACHA=1`/`RANK_BOOSTER=2`). 이 GDD는 값을 정의하지 않고 **요구사항**만 명시한다:

`RANK_SHIELD = 3` (최저, `RANK_BOOSTER=2`보다 큼)

근거: `Winner(t) = argmin_{r∈Q(t)} Rank(r.adType)`이므로, 예방적·무긴급 액션(potato-stat이
정상 체크인 시 ≥18.65h 안전을 보장)이 실제 위기 revive나 이미 확립된 수익 루프
(gacha/booster)와 슬롯 경쟁 시 절대 이기지 않아야 한다 — 최저값 3만이 이 속성과 일치한다.
ad-sdk에 이 4번째 타입을 추가하는 것은 그 문서의 아키텍처 수정 사항이다(Section F 참고).

---

**상수 요약**: 이 세션의 자체 공식(`CanInstall`, `ΔP`)은 이 GDD 안에서만 소비되는 내부
공식이라 레지스트리 신규 등록이 없다. `RANK_SHIELD=3`은 ad-sdk 소유 상수로 그 문서에
등록된다(Section F). 재사용 상수: `SHIELD_DURATION=8`(레지스트리, potato-stat 경유 소비).

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드. systems-designer의 Formulas 상담 중 나온 엣지
> 분석(시계 조작·슬롯 경합) 반영.)*

- **If 광고가 실패한다면(no-fill/오프라인/워치독 타임아웃)**: 온실막은 부여되지 않고
  `shieldActiveUntil` 포함 어떤 상태도 변하지 않는다. 다정한 실패 안내(UI Requirements)를
  제시하고 어포던스는 Available로 남는다. 능동 설치는 위기가 아니므로(감자는 여전히 안전)
  이 실패가 Pillar 3 위반이 되지 않는다.

- **If 플레이어가 광고를 끝까지 안 보고 중도이탈한다면**: 완료 콜백만이 부여를
  트리거하므로(Core Rule 1) 온실막 부여 없음, 상태 불변. Requesting→Available 복귀.

- **If 이미 온실막이 활성인 상태에서 재설치한다면(top-up)**: SET 의미론대로
  `shieldActiveUntil = now + 8h`로 갱신될 뿐 스택되지 않는다. `ΔP`(Formula 3)만큼만 실질
  이득 — 잔여가 많으면 이득이 0에 수렴하므로 버그가 아니라 의도된 자연 억제다.

- **If AtRisk(스탯 이탈, neglect<100)에서 설치한다면**: 설치가 허용되고, 그 시점부터
  방치 지표 누적이 동결된다(potato-stat `dN/dt`의 `(1-isShielded)=0`). 단 스탯 자체는
  보호 중에도 계속 드리프트하므로(potato-stat 확정), 온실막 만료 시점 overshoot이 설치
  시점보다 클 수 있고 누적이 그 지점부터 재개된다 — "온실막은 누적만 막고 드리프트는 안
  막는다"는 potato-stat 계약의 정상 귀결이다.

- **If 오프라인 갭 도중 감자가 시들어 앱 재개 시 `isWithered=true`라면**:
  `OnOfflineProgressApplied` 이후 게이팅이 확정되므로(Core Rule 6) 설치 어포던스는
  Blocked가 되고, 화면엔 wither-recovery의 되돌리기 CTA만 남는다. 시든 감자엔 온실막을
  설치할 수 없다.

- **If 기기 시계가 조작된다면**: 부여 write(`shieldActiveUntil = now + 8h`)는 라이브 `now`를
  쓰므로 게임 내 다른 라이브 타임스탬프와 동일한 신뢰 수준이며 이 시스템 고유의 방어가
  필요 없다. read(`isShielded`)는 potato-stat의 `RegisterUtcTimestamp("potato_shieldActiveUntil")`
  + save-persistence `IsTimestampValid`(`ε_FUTURE=300s`) 게이트를 그대로 상속한다 — 시계를
  앞으로 돌려 만료를 미래로 밀거나, 뒤로 돌려 만료된 온실막을 되살리려는 시도는 둘 다
  로드 시점에 `IsTimestampValid=false`로 잡혀 `t_deviceNow`로 대체된다. 라이브 세션 중
  저장/로드 없이 시계를 되돌리는 잔여 위험은 이 시스템이 새로 만든 것이 아니라 게임 전체
  라이브 타임스탬프가 공유하는 기존 위험이며 save-persistence 소관이다.

- **If 플레이어가 설치를 탭한 순간 광고 로딩 슬롯이 다른 타입에 점유돼 있다면**: `shield`는
  `RANK_SHIELD=3`(최저)이므로 진행 중인 로드를 선점하지 않고 ad-sdk §5의 큐에서 대기한다.
  최악 대기는 앞선 로드가 워치독까지 소진될 때로 대략 `≤ 2×T_LOAD_TIMEOUT_SEC(≈30초)`
  상한 안에 있다(정확한 상한은 ad-sdk에 shield 타입을 추가할 때 그 문서 §5 큐 대기 공식으로
  재검증). revive는 Ready 도달 후엔 슬롯을 점유하지 않으므로 영구 기아는 없다. 이 경합은
  "나가기 직전 설치"라는 코어 유즈케이스와 겹칠 수 있으므로 UI가 짧은 로딩 상태를 다정하게
  표현해야 한다(UI Requirements).

- **If 신규 세이브(최초 실행)라면**: `shieldActiveUntil`=만료값(epoch), `isShielded=false`,
  `isWithered=false` → `CanInstall=true`. 최초 실행부터 설치가 가능하다(실제 노출 시점의
  온보딩 게이팅은 온보딩 시스템 소관).

## Dependencies

### Upstream (이 시스템이 의존)

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 감자 상태 관리 | Hard | `ActivateShield(SHIELD_DURATION)` 호출(shieldActiveUntil SET, 회복 경로와 **공용 역량**), `isWithered`/`isShielded` 조회(게이팅·top-up 판정). 양방향 확인: potato-stat이 이 시스템을 `ActivateShield` 2번째 호출자로 명시함(교차 수정 B, 2026-07-18) |
| 광고 SDK 통합 계층 | Hard | 신규 `RequestAd("shield")`/`ShowAd("shield")`, 온디맨드(사전로드 없음), `RANK_SHIELD=3` 최저 우선순위. 양방향 확인: ad-sdk가 shield를 4번째 타입으로 등록함(교차 수정 C, 2026-07-18) |
| 오프라인 진행 계산 | Soft | `OnOfflineProgressApplied` 구독 — 게이팅 상태 확정 타이밍 보장(Core Rule 6). 없으면 콜드 스타트 직후 게이팅이 잠깐 stale할 뿐 코어 설치는 작동 |

### Sibling (상호 보완 — 직접 의존 아님)

| 시스템 | 관계 | 근거 |
|---|---|---|
| 시듦/위기 & 회복 | 공유 상태 조정 | 둘 다 potato-stat의 `isWithered`를 독립적으로 읽는다 — 이 시스템은 `isWithered=true`면 물러나고(게이팅), wither-recovery가 회복 CTA를 소유. 어느 쪽도 상대를 런타임에 직접 호출하지 않음. "없으면 내가 작동 멈추나?" 기준상 Hard 아님(consistency-failures 2026-07-16 방향 역전 패턴 재확인) |

### Downstream (이 시스템에 의존)

없음. 어떤 시스템도 온실막 보호를 호출하지 않는다. 홈 화면 위젯 등이 온실막 잔여시간을
표시하더라도 potato-stat의 `shieldActiveUntil`을 직접 읽지 이 시스템을 경유하지 않는다.

### 세이브/영속성

이 시스템은 신규 세이브 필드를 추가하지 않는다 — `shieldActiveUntil`은 potato-stat이 이미
`RegisterUtcTimestamp("potato_shieldActiveUntil")`로 등록·소유하며, save-persistence §5의
`IsTimestampValid` 게이트를 통과한다.

**Hard vs Soft 판정 기준**: 다른 GDD들과 동일 — 이 시스템이 응답하지 않을 때 대상 시스템이
아예 작동을 멈추면 Hard, 연출/편의만 사라지고 핵심 루프는 유지되면 Soft.

## Tuning Knobs

이 시스템은 **자체 소유 튜닝 노브가 없다** — 온실막의 지속시간·우선순위·재진입 쿨다운이
모두 상위 시스템 소유 상수를 참조·소비하는 얇은 오케스트레이션 계층이다. 참조만 하는 값
(변경 시 소유 문서를 수정할 것):

| 참조 노브 | 값 | 소유 | 이 시스템에서의 의미 |
|---|---|---|---|
| `SHIELD_DURATION` | 8h | game-concept(레지스트리) | 능동 설치가 부여하는 보호 길이. 회복 경로와 공유 — 현재 두 경로가 동일 값 |
| `RANK_SHIELD` | 3(최저) | ad-sdk | 슬롯 중재 우선순위. 위기 revive·기성 수익 루프에 항상 양보 |
| `T_COOLDOWN_STANDARD` | 5초 | ad-sdk | shield SDK 뷰 해제 재진입 쿨다운(재사용, 신규 상수 없음) |

**잠재적 미래 노브(현재 미도입)**: 능동 설치 전용 지속시간(`PROACTIVE_SHIELD_DURATION`)을
회복 경로의 8h와 분리할지는 현재 통일(둘 다 `SHIELD_DURATION`)로 둔다. 분리하면 "능동
설치는 조금 짧게(예: 6h) 두어 회복 보상의 상대적 가치를 지킨다" 같은 튜닝이 가능하나,
현재는 단순성·일관성을 위해 통일하고 필요 시 Open Questions에서 재검토한다.

## Visual/Audio Requirements

> 이 섹션은 새로운 무드·색·형태를 정의하지 않는다 — art-bible이 이미 잠근 원칙을 이
> 시스템의 유일한 신규 시각 표면(설치 어포던스)에 적용하고, 온실막의 감자 위 시각 표현은
> potato-stat/expression-animation Visual/Audio §4가 이미 소유한다(art-director 상담,
> 2026-07-18).

### 1. 설치 어포던스 — Tier 2 중립 유틸리티, 히어로 아님

- **프로미넌스**: Amber Ochre 히어로 트리트먼트를 쓰지 않는다. art-bible Color System §3은
  Amber Ochre를 "돌봄의 결과로 도착하는 온기"(회복 블룸·수확 글로우·난로 터치)에 의미
  예약했으므로, 선제적 광고 요청 어포던스에 쓰면 그 색의 정의 자체를 오용한다. 대신 Shape
  Language §3의 Tier 2 문법(단순 기하, 예각 없는 둥근 형태)을 따르는 중립 UI 아이콘으로
  그린다 — 이는 백엔드의 `RANK_SHIELD=3`(4개 광고 타입 중 최저 우선순위)과도 시각 위계가
  일치한다.
- **색·상태(기존 아이콘 규칙 재사용, 신규 언어 아님)**: art-bible UI/HUD §3의 기존 규칙
  "기본=아웃라인(Ink Umber) / 활성=solid fill"을 그대로 적용한다. Available(미보호)=Ink
  Umber 아웃라인, Shielded(보호 중)=Ink Umber **solid fill**(평평 — Amber Ochre도 Butter
  Yellow 틴트도 쓰지 않음, 온기는 감자 오버레이가 전담, 사용자 확정). 색이 아니라 형태(fill
  유무)로 상태를 구분하므로 색맹 안전.
- **대기 중 모션 없음**: art-bible UI/HUD §4의 "대기 중 펄스/글로우 금지"를 그대로 상속 —
  이 어포던스는 대기 중 절대 펄스·글로우·점멸하지 않는다. 광고 버튼이 시선을 반복적으로
  끌면 공포 마케팅 문법이 되기 때문(Pillar 3 안티필러).
- **Design test**: 이 어포던스가 (a) Amber Ochre를 쓰거나, (b) 대기 중 움직이거나,
  (c) 상시 히어로 크기/헤일로를 가지면 → 반려. Amber Ochre 히어로 + 하단 중앙 엄지존은
  오직 대안 없는 단일 액션(오늘은 Wither 되돌리기 CTA뿐)에만 예약된다.
- **Pillar 연결**: Pillar 3(공포 마케팅 없는 조용한 초대), Pillar 4(요구하지 않는 존재감).

### 2. "보호 중" 피드백 — 이진 신호, 카운트다운 없음

두 종류의 읽힘을 구분한다: 정서적 "안전하다"와 기능적 "지금 켜져 있나".

- **정서적 읽힘 = 이미 해결됨(중복 금지)**: 감자 위 반투명 온실막 오버레이(potato-stat/
  expression 소유)가 Tier 1 히어로 위치에서 상시 보이므로 Section 1 원칙 3("정적인
  포근함=안전 신호")을 이미 충족한다. 어포던스에 별도 "얼마 남음" 지표를 만들면 카운트다운을
  프로그레스 바로 위장한 것이 되어(wither-recovery Design test가 금지) 반려.
- **기능적 읽힘 = 아이콘 fill 이진뿐**: Available=아웃라인 / Shielded=solid fill. 신규
  색조·그라데이션·감쇠 애니메이션 없음. `ΔP`(Formula 3)상 잔여가 1h 이상이면 재설치 이득이
  미미하므로, 게임은 플레이어가 잔여 시간을 알 구조적 필요가 없다 — "켜짐/꺼짐"이 정직하고
  충분한 신호다.
- 계단식 신선도 큐는 검토했으나 "나가서 잊는" 판타지·Pillar 4 스코프상 미도입(사용자 확정).

### 3. 오디오 (범위 메모)

이 시스템은 사운드를 직접 재생하지 않는다. 온실막 부여는 potato-stat이 `shieldActiveUntil`
SET 시 발화하는 기존 훅(회복 경로와 공용 `ActivateShield` 기반)을 재사용하며, 이 시스템은
신규 오디오 훅을 열지 않는다. 버튼 프레스 피드백(스퀴시+번짐)은 art-bible UI/HUD §4의 일반
버튼 규칙이 이미 커버한다. 능동 설치 완료음을 위기 회복 완료음과 구별할지는 wither-recovery가
이미 남긴 "광고 vs 무료 경로 완료음 구별" 열린 질문과 같은 범주로, ASMR 적응형 오디오 GDD
소관으로 이월한다.

## UI Requirements

> 이 시스템은 신규 풀스크린 모달을 만들지 않는다 — art-bible UI/HUD 원칙(화면 오버레이)을
> 따라 메인 게임 화면 위에 오버레이로 뜨는 설치 어포던스일 뿐이다(ux-designer 상담,
> 2026-07-18).

**배치 & 프로미넌스**: 하단 **모서리**(엄지 도달 아크 내), 하단 **중앙 아님** — art-bible
UI/HUD §8이 하단 중앙 엄지존을 "문서상 단일 최중요 CTA"(Wither 되돌리기)에 예약했으므로,
중앙을 점유하면 위기 CTA의 "화면당 하나" 신호값을 훼손한다. 모서리 배치는 도달성과
저강조(secondary)를 동시에 해결한다(Hoober 엄지존 연구: 세로 상단 모서리=최악 도달,
하단 모서리=편안·자연히 보조로 읽힘). 3개 터치 케어 제스처(창문/난로/물뿌리개) 및 위기
CTA 슬롯과 공간적으로 분리해 "4번째 잡일"로 오인되지 않게 한다. 정확 좌표·좌우 모서리는
감자 네거티브 스페이스 헤일로와 대조해 Pre-Production `/ux-design`에서 확정(art-director).

**상태 → UI 매핑**:

| 상태 | 플레이어가 보는 것 | 할 수 있는 것 |
|---|---|---|
| Available(미보호) | Tier 2 아이콘, Ink Umber 아웃라인, 정적 | 탭 → Requesting |
| Available(보호 중, top-up) | 같은 위치 아이콘이 Ink Umber **solid fill** + 감자 위 반투명 오버레이(potato-stat/expression 소유)의 이중 채널 | 재탭 → top-up(SET 갱신, now+8h) |
| Blocked(`isWithered`) | **완전히 숨김**(비활성-표시 아님) — 위기 화면 단일 포커스(되돌리기 CTA) 보존, 시든 감자에 설치는 논리적 비약(사용자 확정) | 없음(타깃 부재) |
| Requesting(광고 로딩/표시) | 탭 즉시 1회성 프레스 확인(스퀴시+번짐) 후 정적 탈채도 "대기" 룩 — 스피너 없음(최대 ~30s 로딩 동안 반복 루프 모션은 Section 1 원칙 3 위반) | 반복 탭은 조용히 디바운스 |
| 실패 후 복귀 | 아이콘이 탭 직전 상태로 정확히 복귀 + 저강조 마이크로 카피가 아이콘 근처에 페이드 인/아웃(§4 모달 페이드+정착 재사용). 빨강·경고 아이콘·모달 없음 | 즉시 재탭 가능 |

**로딩 & 실패 UX**: shield는 온디맨드(사전로드 없음)라 로딩 지연이 예외가 아니라 상시
케이스다. 탭 시 1회성 프레스 확인 → 정적 "대기" 룩(반투명 아웃라인, 스피너 금지, 플레이어가
시선을 떼도 무방) → 성공 시 SDK 재생 UI가 인수(이 GDD 범위 밖) → 복귀 시 solid fill. 실패/
이탈 시 조용한 페이드 카피, 톤은 wither-recovery의 "실패를 명명하지 않고 경보하지 않는다"를
상속한다.

**"보호 중" 커뮤니케이션**: 이진(아이콘 fill + 감자 오버레이)만, 리터럴 카운트다운/숫자/
퍼센트 없음(art-bible §4 + wither-recovery 금지 상속). 나가서 잊는 판타지를 위해 "얼마
남음" 큐는 두지 않는다(사용자 확정).

**접근성**: 히트타깃 ≥88×88dp(art-bible §5, 신규 티어 없음 — 오탭으로 광고가 뜨는 비용이
아무것도 안 하는 오탭보다 나쁘므로 관대한 타깃 유지), 터치 전용(호버 금지), 아웃라인↔fill은
색 아닌 형태 구분. "간단 터치 모드"(art-bible 열린 과제)와 무관 — 이미 단일 탭이라 대체 입력
매핑 불필요(오히려 그 모드가 지향하는 타깃 인터랙션 형태의 예시).

**지금 잠그는 것 vs Pre-Production 이월**:
- **잠금(이 GDD)**: 배치 존(하단 모서리·엄지 도달·분리), 5상태 매핑, 이진 노-카운트다운,
  로딩/실패 원칙(1회성 확인·정적 대기·조용한 페이드·스피너/경보 금지), 히트타깃 상속.
- **이월(`/ux-design` + art-bible 후속)**: 실제 아이콘 아트, 정확 좌표/모서리(감자 헤일로
  대조), 설치/top-up/실패 카피(writer+ux), Amber Ochre 아닌 정확 색 지정.
- **교차 시스템 플래그(producer/game-designer)**: 이 GDD가 프로젝트 최초의 "광고 로딩 UI
  트리트먼트" 정의다(ad-sdk는 UI 무소유). Requesting/실패 패턴을 재사용 컨벤션으로 캡처해
  다른 광고 게이팅 CTA(가챠/부스터/revive 로딩)가 상속하도록 권장.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션. 상담 중
> 발견된 레이스 컨디션(광고 시청 중 시듦 → 부여 시점 부정합)은 Core Rule 7과 AC-EC-12로
> 해소.)*

아래 기준은 Detailed Design·Formulas·Edge Cases·Dependencies에서 정의된 필드·상수명을
그대로 사용한다(`shieldActiveUntil`, `isWithered`, `isShielded`, `neglectMeter`,
`CanInstall`, `SHIELD_DURATION`, `RANK_SHIELD`). 이 시스템은 자체 영속 상태가 없으므로,
아래 "상태"는 매 조회 시점 `isWithered`/`isShielded`로부터 파생되는 값이다.
`ActivateShield`/`ResetToIdeal`은 potato-stat 소유이므로, 이 시스템의 기준은 그 함수들을
**어떻게 호출하는지**(호출 여부·인자·순서)까지만 검증하고 함수 내부 구현은 potato-stat
자체 AC의 몫이다.

### 상태 전이

- **AC-ST-01 (Available — Healthy)** **GIVEN** `isWithered=false`, 세 스탯 모두 안전 구간 안(Healthy) **WHEN** 설치 어포던스 상태 조회 **THEN** `CanInstall=true`, 상태=Available, 어포던스 활성 표시.
- **AC-ST-02 (Available — AtRisk도 설치 가능)** **GIVEN** `isWithered=false`, 스탯 1개 이상 이탈(AtRisk, 예: 수분=15) **WHEN** 상태 조회 **THEN** `CanInstall=true`, 상태=Available — AtRisk도 Healthy와 동일하게 설치 가능(Core Rule 3).
- **AC-ST-03 (Available → Requesting)** **GIVEN** 상태=Available **WHEN** 어포던스 탭 **THEN** `RequestAd("shield")`/`ShowAd("shield")` 호출, 상태=Requesting, 이 시점 `shieldActiveUntil` 불변(부여는 완료 시점에만).
- **AC-ST-04 (Requesting → Available, 완료 → 부여)** **GIVEN** 상태=Requesting, `isWithered=false` 유지 **WHEN** SDK 완료(`OnComplete`) 콜백 **THEN** `ActivateShield(SHIELD_DURATION)` 정확히 1회 호출, `shieldActiveUntil=t_call+8h`(SET), 상태=Available 복귀, `isShielded=true`.
- **AC-ST-05 (Requesting → Available, 중도이탈 → 부여 없음)** **GIVEN** 상태=Requesting **WHEN** 완료 전 광고 닫음(`OnDismissed`) **THEN** `ActivateShield` 미호출, `shieldActiveUntil` 불변, 상태=Available 복귀(탭 이전과 동일).
- **AC-ST-06 (Requesting → Available, 실패 → 부여 없음)** **GIVEN** 상태=Requesting **WHEN** SDK 실패(no-fill/오프라인/타임아웃) 콜백 **THEN** `ActivateShield` 미호출, `shieldActiveUntil` 불변, 상태=Available 복귀, 다정한 실패 안내 표시.
- **AC-ST-07 (Blocked — 시듦 중)** **GIVEN** `isWithered=true` **WHEN** 상태 조회 **THEN** `CanInstall=false`, 상태=Blocked, 어포던스 완전히 숨김(비활성 표시 아님 — 탭 타깃 자체가 없음).
- **AC-ST-08 (Blocked — UI 우회 시에도 요청 거부)** **GIVEN** `isWithered=true` **WHEN** 설치 핸들러가 UI를 거치지 않고 직접 호출됨(레이스: UI가 아직 Blocked로 갱신 안 된 프레임에 탭 이벤트가 큐잉돼 있던 경우) **THEN** 핸들러가 `CanInstall(isWithered)`를 재확인해 false 반환, `RequestAd` 미발행 — "UI 숨김"이 유일한 방어선이 아님을 검증.
- **AC-ST-09 (Top-up 허용 — `isShielded`는 게이팅 입력 아님)** **GIVEN** `isWithered=false`, `isShielded=true`(잔여 3h) **WHEN** 상태 조회 **THEN** `CanInstall=true`, 상태=Available — 이미 보호 중이어도 재설치 허용.
- **AC-ST-10 (Top-up 실행 — SET, 누적 아님)** **GIVEN** `isShielded=true`, `shieldActiveUntil=t0`(잔여 3h) **WHEN** 재설치 광고가 `t1`에 완료 **THEN** `shieldActiveUntil=t1+8h`(‑`t0+3h+8h`가 아님) — 기존 잔여 완전 무시·덮어씀 확인.
- **AC-ST-11 (Requesting 중 반복 탭 — 디바운스)** **GIVEN** 상태=Requesting(이미 `RequestAd("shield")` 발행) **WHEN** 해소 전 어포던스 재탭 **THEN** 두 번째 `RequestAd("shield")` 미발행(조용히 디바운스).

### 공식 검증

**`CanInstall`**
- **AC-F-01** **GIVEN** `isWithered=true` **WHEN** `CanInstall` 평가 **THEN** `false`.
- **AC-F-02** **GIVEN** `isWithered=false` **WHEN** 평가 **THEN** `true`.
- **AC-F-03 (의도된 제외 — 부여 직후에도 top-up 허용)** **GIVEN** `isWithered=false`, `isShielded=true`(방금 부여, 예: 1초 후) **WHEN** `CanInstall` 평가 **THEN** `true` — `isShielded` 값과 무관하게 동일 결과(구현자가 `isShielded` 가드를 추가하는 회귀를 막는 명시 테스트).
- **AC-F-04 (대조군)** **GIVEN** `isWithered=false`, `isShielded=false` **WHEN** 평가 **THEN** `true` — AC-F-03과 동일 결과, `CanInstall`이 `isWithered`만의 순수함수임을 확인.

**`ActivateShield` 호출 계약(호출 방식만 — 함수 내부는 potato-stat 소관)**
- **AC-F-05 (기본 SET)** **GIVEN** `shieldActiveUntil`=만료값, `t_call=14:00` **WHEN** 완료로 `ActivateShield(8h)` 호출 **THEN** `shieldActiveUntil'=22:00`.
- **AC-F-06 (Top-up SET — 잔여 무시)** **GIVEN** `shieldActiveUntil=20:00`(잔여 있음), `t_call=19:00` **WHEN** `ActivateShield(8h)` 호출 **THEN** `shieldActiveUntil'=03:00`(다음날) — `20:00+8h=04:00`이 아님, 기존 값 완전 무시.
- **AC-F-07 (항상 상수 전달)** **GIVEN** 임의의 설치/top-up 완료 이벤트 **WHEN** `ActivateShield` 호출 **THEN** `duration` 인자는 항상 정확히 `SHIELD_DURATION`(8h).

**`ΔP`(검증 전용, 순수함수로 테스트)**
- **AC-F-08 (늦은 top-up — 유의미)** **GIVEN** `shieldActiveUntil_prev=20:00`, `t=19:00` **WHEN** `ΔP` 계산 **THEN** `R_remaining=1h`, `ΔP=clamp(8-1,0,8)=7h`.
- **AC-F-09 (이른 top-up — 미미)** **GIVEN** `shieldActiveUntil_prev=20:00`, `t=13:00` **WHEN** 계산 **THEN** `R_remaining=7h`, `ΔP=1h`.
- **AC-F-10 (만료 후 — 전체 이득)** **GIVEN** `shieldActiveUntil_prev < t` **WHEN** 계산 **THEN** `R_remaining=0`, `ΔP=8h`.
- **AC-F-11 (바깥 clamp 방어)** **GIVEN** 손상/조작된 `shieldActiveUntil_prev=t+10h`(이론 최댓값 t+8h 초과) **WHEN** 계산 **THEN** `R_remaining=10h`, `ΔP=clamp(-2,0,8)=0`.

**`RANK_SHIELD`(요구사항 확인 — arbitration 자체는 ad-sdk 소관)**
- **AC-F-12 (최저 순위)** **GIVEN** `RANK_REVIVE=0,GACHA=1,BOOSTER=2,SHIELD=3` **WHEN** 비교 **THEN** `RANK_SHIELD`가 다른 셋보다 항상 큼(최저 우선순위).
- **AC-F-13 (경쟁 시 shield 패배)** **GIVEN** `Q(t)`에 `shield`와 `{revive,gacha,booster}` 중 하나 이상 동시 존재 **WHEN** `Winner(t)` 계산(ad-sdk 소유) **THEN** `Winner(t)`는 절대 `shield` 아님.
- **AC-F-14 (경쟁자 없을 때 즉시 승자)** **GIVEN** `Q(t)={shield}`, `LoadingSlotBusy=false` **WHEN** `Winner(t)` 계산 **THEN** `Winner(t)=shield`(최저 순위여도 경쟁 없으면 즉시 처리).

### 엣지 케이스

- **AC-EC-01 (실패 — 상태 불변)** **GIVEN** 상태=Requesting **WHEN** `onFailed` **THEN** `shieldActiveUntil` 불변, 상태=Available 복귀, 다정한 실패 카피, 다른 상태 변경 없음.
- **AC-EC-02 (중도이탈 — 상태 불변)** **GIVEN** 상태=Requesting **WHEN** `OnDismissed` **THEN** `ActivateShield` 미호출, `shieldActiveUntil` 불변, Available 복귀.
- **AC-EC-03 (잔여 적을 때 top-up — 유의미)** **GIVEN** `isShielded=true`, 잔여 1h **WHEN** 재설치 완료 **THEN** `shieldActiveUntil=now+8h`(`ΔP=7h`), 정상 허용.
- **AC-EC-04 (잔여 많을 때 top-up — 미미해도 허용)** **GIVEN** `isShielded=true`, 잔여 7h **WHEN** 재설치 완료 **THEN** `shieldActiveUntil=now+8h`(`ΔP=1h`), 이득이 작다는 이유로 차단/경고 안 함.
- **AC-EC-05 (AtRisk 설치 — 누적만 동결, 스탯 안 건드림)** **GIVEN** `isWithered=false`, AtRisk(수분=15, `overshootMoisture=10`), `neglectMeter` 누적 중 **WHEN** 설치 완료(`ActivateShield`) **THEN** `dN/dt`가 즉시 0, `neglectMeter` 누적 정지, 그러나 스탯은 전혀 안 변함(수분=15 그대로, `ResetToIdeal()` 미호출).
- **AC-EC-06 (드리프트가 보호 창 초과)** **GIVEN** AC-EC-05 상황, 설치 시점 `overshoot=10` **WHEN** 8h 후 만료, 그동안 스탯 계속 드리프트 **THEN** 만료 시 `overshoot`가 설치 시점보다 클 수 있고, 만료 즉시 그 값 기준으로 `neglectMeter` 누적 재개 — 버그 아님, 계약된 정상 귀결.
- **AC-EC-07 (오프라인 갭 중 시듦 → 복귀 시 Blocked)** **GIVEN** `isWithered=false`로 종료, 갭 중 `neglectMeter`가 100 도달 **WHEN** 재개 후 `OnOfflineProgressApplied` 뒤 게이팅 재평가 **THEN** `isWithered=true` 확정, 상태=Blocked, 어포던스 숨김, wither-recovery 되돌리기 CTA만 노출.
- **AC-EC-08 (시계 조작 — 신규 방어 없음, 상속 확인)** **GIVEN** 정상 부여된 미래 `shieldActiveUntil` **WHEN** 기기 시계가 저장/로드 경계 넘어 조작 **THEN** `IsTimestampValid`(save-persistence, `ε_FUTURE=300s`)가 `potato_shieldActiveUntil`에도 동일 적용됨을 확인 — 이 시스템 자체는 별도 로직 없음(회귀 확인용, potato-stat/save-persistence 기존 테스트와 중복 가능).
- **AC-EC-09 (슬롯 경합 — 선점 없이 대기)** **GIVEN** 다른 adType(booster)이 `Preloading`+`dispatched=true` **WHEN** 설치 탭으로 `RequestAd("shield")` 발행 **THEN** `shield`가 `Q(t)`에 `Rank=3`으로 대기, 진행 중 booster 로딩 중단 안 됨.
- **AC-EC-10 (슬롯 경합 — 최악 대기 상한)** **GIVEN** shield보다 높은 우선순위 요청 2개가 각각 `T_LOAD_TIMEOUT_SEC(15s)`까지 소진 **WHEN** 탭부터 shield 실제 디스패치까지 측정 **THEN** `≤2×15=30초`.
- **AC-EC-11 (신규 세이브)** **GIVEN** `shieldActiveUntil`=만료값(epoch), `isShielded=false`, `isWithered=false` **WHEN** 상태 조회 **THEN** `CanInstall=true`, 상태=Available(온보딩 노출 타이밍은 범위 밖).
- **AC-EC-12 (광고 중 시듦 — 부여 시점 재확인·미부여, 레이스 방어)** **GIVEN** `isWithered=false`(AtRisk, 임계 근접)에서 설치 탭 → 상태=Requesting, 광고 시청 중 `neglectMeter`가 100 도달해 `isWithered=true`가 됨 **WHEN** 광고가 완료(`OnComplete`)로 콜백 **THEN** Core Rule 7에 따라 `ActivateShield` 호출 직전 `isWithered` 재확인이 true를 감지 → `ActivateShield` **미호출**, `shieldActiveUntil` 불변, 상태=Blocked로 전이(`isShielded=true`와 `isWithered=true`가 동시에 성립하는 부정합 상태가 절대 생기지 않음), 플레이어는 되돌리기 CTA로 안내.

### 통합/인터페이스

- **AC-INT-01 (`ActivateShield` 호출, `ResetToIdeal` 미호출)** **GIVEN** 광고 완료 콜백 핸들러 **WHEN** 코드 경로 추적 **THEN** `ActivateShield(SHIELD_DURATION)`은 호출되지만 `ResetToIdeal()`은 그 경로 어디에서도 호출 안 됨 — 능동 설치는 스탯을 절대 리셋 안 함.
- **AC-INT-02 (`ActivateShield` 단독 호출 부작용 없음 — consumer-side)** **GIVEN** `ActivateShield(duration)`이 이 시스템에 의해 단독(회복 경로 `TriggerRecovery()` 없이) 호출됨 **WHEN** 호출 완료 **THEN** `shieldActiveUntil`만 갱신, `moisture`/`humidity`/`temperature`/`neglectMeter` 전부 불변(스탯 리셋은 호출자 책임). 대응 producer-side 계약은 potato-stat AC-F-22로 별도 등록됨(2026-07-18).
- **AC-INT-03 (오프라인 배치 완료 전 게이팅 확정 금지)** **GIVEN** 콜드 스타트/포그라운드 복귀 직후, 아직 `OnOfflineProgressApplied` 미발행(‑`isWithered` stale 가능) **WHEN** 어포던스 상태 조회 **THEN** Available/Blocked 어느 쪽도 확정 안 함(stale 값으로 활성/비활성화 안 함).
- **AC-INT-04 (배치 완료 후 즉시 확정)** **GIVEN** `OnOfflineProgressApplied` 방금 발행, `isWithered` 최종 확정값 **WHEN** 게이팅 재평가 **THEN** 같은 프레임/틱에 `CanInstall`이 확정값 반영(추가 지연 없음).
- **AC-INT-05 (`RANK_SHIELD` 등록값 스모크)** **GIVEN** ad-sdk가 `RANK_SHIELD=3` 등록 **WHEN** `RequestAd("shield")` 발행 **THEN** 그 요청이 `Rank=3`으로 `Q(t)` 편입(상수값 조용한 변경 회귀 방지, 전체 arbitration은 ad-sdk 소관).
- **AC-INT-06 (신규 세이브 필드 없음)** **GIVEN** 이 시스템 존재하는 상태로 세이브 생성/로드 **WHEN** 스키마 점검 **THEN** 이 시스템이 등록하는 신규 필드 없음 — `shieldActiveUntil`은 여전히 potato-stat `RegisterUtcTimestamp("potato_shieldActiveUntil")`로만 등록.
- **AC-INT-07 (wither-recovery 직접 호출 없음 — Sibling 경계)** **GIVEN** 시듦/위기 & 회복이 별도 존재 **WHEN** `CanInstall` 평가 **THEN** `isWithered`를 potato-stat에서 직접 조회할 뿐 wither-recovery의 어떤 API도 호출 안 함(Sibling 관계 구조적 유지).

### 이 GDD 범위 밖으로 명시적으로 남긴 것 (테스트 불가/타 시스템 소관)

- **실제 광고 SDK 완료/이탈/실패 동작**(네트워크 조건별) — ad-sdk-integration-layer 자체 AC + 실제 SDK 선정 후.
- **`RANK_SHIELD=3`의 실제 수익화 영향**(revive/gacha/booster 수익 보호 여부) — 라이브 분석, analytics-engineer/live-ops.
- **정확한 히트박스 좌표·모서리·아이콘 아트·카피 톤** — Pre-Production `/ux-design`·writer 소관(UI Requirements 이월 목록).
- **설치 의식의 정서 페이오프**(Player Fantasy) — Visual/Feel 증거(스크린샷+리드 승인, ADVISORY).
- **`ActivateShield`/`ResetToIdeal` 내부 구현 정확성** — potato-stat 자체 AC(단독 호출 무부작용은 AC-F-22로 등록됨).
- **전체 `Winner(t)` arbitration 수학**(shield 경계 밖 다중 타입 인터리빙) — ad-sdk 자체 AC. shield 4-way arbitration 심화 테스트는 ad-sdk 교차 후속 권장(Open Questions).
- **온보딩 노출 타이밍**(어포던스 최초 노출 시점) — 온보딩 시스템 GDD.

## Open Questions

| 질문 | 배경 | 담당 | 해소 시점 |
|---|---|---|---|
| 터치 케어가 광고-only 플레이어에게 100% 선택사항이 되는 것 | `isShielded`는 방치 누적만 막고 lifecycle `Tick`은 `isWithered`에만 의존 → 8h마다 광고 재설치로 무터치 육성 가능. **수용됨**(의도된 Pillar-4 배려, 사용자 확정 2026-07-18) — 밸런스/수익 문제 아님, attachment/과금은 여전히 engagement 필요라 자기교정. 향후 콘텐츠-인게이지먼트 관점 재검토 여지만 열어둠(완화 시 potato-stat/expression 범위) | game-designer/creative-director | 소프트런칭 리텐션 데이터 관찰 후 |
| 능동 설치 전용 지속시간(`PROACTIVE_SHIELD_DURATION`) 분리 여부 | 현재 회복 경로와 통일(둘 다 `SHIELD_DURATION`=8h). 분리하면 "능동 설치는 더 짧게(예: 6h) 두어 회복 보상의 상대적 가치를 지킨다" 튜닝 가능하나 현재 단순성 우선(Tuning Knobs) | systems-designer/game-designer | 밸런스 튜닝 단계 |
| ~~능동 설치 완료음을 위기 회복음과 구별할지~~ | **해결됨(2026-07-21)** — `asmr-adaptive-audio.md`가 구별 안 함으로 확정(wither-recovery의 동일 범주 질문과 함께 해소): 공유 훅(`OnRecovered`/`ActivateShield`)이 경로 파라미터를 노출하지 않으므로 단일 완료 SFX로 처리 | — | 해소됨 |
| shield 4-way arbitration 심화 AC (qa-lead 3c) | ad-sdk의 AC-F-19~25가 revive/gacha/booster만 다루고 shield 포함 4-way·shield vs booster(rank 2 vs 3) 시나리오 미검증. 이 GDD는 경계 AC(AC-F-12~14)만 소유 | qa-lead/systems-designer | ad-sdk-integration-layer 리뷰/교차 후속 시 |
| 설치 어포던스 정확 좌표·좌우 모서리·아이콘 아트·카피·정확 색 | UI Requirements가 배치 존·5상태·원칙만 잠금, 픽셀·아트·문구·Amber Ochre 아닌 정확 색은 이월 | art-director/ux-designer/writer | Pre-Production `/ux-design` |
| 광고 로딩 UI 트리트먼트를 재사용 컨벤션화할지 | 이 GDD가 프로젝트 최초 "광고 로딩 UI 트리트먼트" 정의(ad-sdk는 UI 무소유). Requesting/실패 패턴을 가챠/부스터/revive 로딩이 상속하도록 캡처 권장 | producer/game-designer | 후속 광고 게이팅 CTA 설계 시 |
| `RANK_SHIELD=3`의 실제 수익화 영향 | shield 저우선순위가 revive/gacha/booster 수익을 실제로 보호하는지 실측 필요 | analytics-engineer | 소프트런칭/라이브옵스 |
| 실패 직후 즉시 재탭 시 백오프 루프 "고장난 느낌"(ux-designer flag) | 실패 후 재탭이 ad-sdk `LoadFailed`/backoff(30→60→120→240→300s)와 겹치면 즉시 재실패 루프 가능 — 동작은 정상인데 고장처럼 느껴짐 | economy-designer/ux-designer | ad-sdk 리뷰/실패 UX 상세 설계 시 |
