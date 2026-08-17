# ASMR 적응형 오디오 (ASMR Adaptive Audio)

> **Status**: Designed (pending review)
> **Author**: user + systems-designer + sound-designer + qa-lead
> **Last Updated**: 2026-07-21
> **Implements Pillar**: Pillar 1 (손끝의 위안)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode

## Overview

ASMR 적응형 오디오는 게임 전체에 흩어진 이벤트·상태를 구독해 실제 사운드 재생·
믹싱을 소유하는 구독자(subscriber) 계층이다. 이 시스템은 게임 로직을 소유하지
않고 오직 청취만 한다 — 단발성 이벤트(`OnGestureCompleted`/`OnStageAdvanced`/
`OnWitherEntered`/`OnRecovered`)와 연속 상태(터치 중 `progress`, 날씨
`GetCurrentWeather()` enum) 두 종류의 입력을 받아 적절한 사운드로 매핑한다.

플레이어는 이 시스템과 직접 상호작용하지 않는다 — 터치 케어를 칠 때마다 들리는
소리로 지속적으로 경험할 뿐이다(수동적/환경적). 이 시스템이 없다면
game-concept.md가 명시한 "오디오=정체성의 절반"이 성립하지 않고, 터치 케어는
시각 피드백만 남은 무음 조작으로 전락한다(Unique Hook의 반이 사라짐).

## Player Fantasy

> *(`creative-director` 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

플레이어가 이 시스템에서 느껴야 할 감정은 보상(reward)이 아니라 이완(relaxation
response)이다 — 소리는 터치의 두 번째 감각 채널이다(Pillar 1 "손과 귀로 먼저
느낀다"). game-concept.md Unique Hook이 이미 "빗소리·장작 타는 소리 같은 자연
ASMR 반응이 즉각 붙는다"로 명시했고, MDA Sensation(2순위)을 귀에 직접 전달하는
유일한 채널이 이 시스템이다.

터치 케어 GDD가 이미 확정해둔 원칙(공통 금지: "성공 차임/딩" 류 게임 UI 보상음
문법 금지, 피치 상승으로 다급함 표현 금지)을 이 GDD의 핵심 정서적 원칙으로 그대로
계승한다 — **Design test**: 사운드가 "성취/맞췄다"처럼 읽히면 반려하고 "유기적
해소/질감 변화"로 읽히도록 되돌린다.

날씨 앰비언트는 백색소음/자연 배경(빗소리·바람소리 등)으로 항상 배경에 머물러야
하며, 터치 제스처 사운드와 절대 경쟁하지 않는다(터치가 항상 전경 음량 우선,
art-bible의 "터치 반응 항상 우선" 원칙을 청각 도메인으로 확장).

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 lean에서도 상담 필수.)*

### Core Rules

1. **순수 구독자, 트리거 소유 없음**: 이 시스템은 언제 사운드가 재생되어야 하는지
   스스로 판단하지 않는다 — 모든 트리거는 다른 시스템이 발행하는 이벤트/상태를
   구독(subscribe)해서만 받는다. 이 시스템이 소유하는 것은 정확히 "이벤트/상태
   → 사운드" 매핑 하나뿐.
2. **3계층 레이어 구조(볼륨 우선순위 고정)**:
   - **앰비언트 레이어**(최하위): `GetCurrentWeather()` 구동, 항상 배경 continuous
     loop.
   - **제스처 레이어**(중위): 터치 중 `progress` 구동 continuous texture + 완성
     시 one-shot. 앰비언트보다 항상 청감상 우선(art-bible "터치 반응 항상 우선"
     원칙의 청각 확장).
   - **이벤트 레이어**(최상위, 희소): `OnStageAdvanced`/`OnWitherEntered`/
     `OnRecovered` 등 정서적 무게가 큰 단발 이벤트. 발생 시 짧게 하위 두 레이어를
     덕킹(ducking, 일시적 볼륨 감쇠)해 사운드 공간을 확보한다.
3. **이산(one-shot) vs 연속(continuous) 입력 분류**: 이 시스템이 구독하는 모든
   훅은 정확히 두 형태 중 하나다 — (a) 이산 원샷 이벤트(발생 즉시 정확히 1회
   재생) (b) 연속 파라미터(값이 유효한 동안 계속 구동, 값이 사라지면 자연스럽게
   정지/페이드아웃). 어느 훅이 어느 형태인지는 Interactions 표에서 고정한다.
4. **날씨 앰비언트 크로스페이드는 weather-system 재사용**: 앰비언트 레이어의
   날씨별 전환은 새 오디오 전용 타이밍을 만들지 않고 weather-system이 이미
   확정한 `CROSSFADE_DURATION=3.0s`(라이브 전환에만 재생, 오프라인 갭 후엔 즉시
   스냅)을 그대로 따른다 — 시각과 청각이 같은 시계로 전환돼 어긋나지 않는다.
5. **제스처 사운드는 터치 케어의 확정 명세를 그대로 구현**: 창문/난로/물뿌리개의
   진행 중·완성 사운드 성격은 이미 `touch-care-interaction.md` §5가 확정했다 —
   이 GDD는 그 표를 실제 재생 규칙(연속 vs 이산, 루프 여부)으로 번역할 뿐 새로
   발명하지 않는다.
6. **동시 이벤트 우선순위**: 이벤트 레이어 내에서 두 희소 이벤트가 동시에
   발생하면(예: Recovery와 StageAdvanced 동시), 사전 정의된 우선순위(시듦→회복
   계열이 생장 계열보다 우선, 정서적 무게가 더 크므로)로 하나만 재생하고 나머지는
   스킵한다(둘 다 재생하면 청각적으로 뒤섞임) — 정확한 우선순위 표는 Edge Cases
   에서 확정한다. **이벤트 레이어가 이미 `Playing`(재생 중)일 때 도착하는 새
   이벤트는 우선순위와 무관하게 항상 드롭된다**(비선점 — `ad-sdk-integration-layer.md`
   의 단일 로딩 슬롯 비선점 원칙과 동일 계보) — 재생 중인 이벤트를 인터럽트하고
   새 이벤트로 갈아타지 않는다.
7. **음소거/볼륨 설정과의 관계**: 이 시스템은 자체 음소거 UI를 소유하지 않는다 —
   설정/접근성 시스템(Alpha, 미설계)이 마스터/카테고리별 볼륨을 노출하면 이
   시스템의 믹서가 그 값을 구독한다(잠정 인터페이스, Open Questions).

### States and Transitions

세 레이어는 각각 독립된 간소화 상태를 가진다(공유 상태 없음, 서로 다른 오디오
버스).

| 레이어 | 상태 | 설명 |
|---|---|---|
| **앰비언트** | `Playing`(루프)만 존재 | `GetCurrentWeather()`가 항상 유효한 enum을 반환하므로 이 레이어는 항상 활성 — `Idle` 상태가 없다(날씨는 게임 시작 즉시 존재). 날씨 전환 시 Core Rule 4의 크로스페이드로 다음 루프로 갱신 |
| **제스처** | `Silent`/`Playing`(연속)/`OneShot`(완성) | 터치 케어의 `Active` 상태 동안만 `Playing`, `progress≥θ_gesture` 도달 순간 `OneShot` 1회, 접촉 해제(완성 전) 시 `Decaying`을 따라 페이드아웃 후 `Silent` |
| **이벤트** | `Idle`/`Playing`(one-shot+덕킹) | 희소 이벤트 발생 시에만 `Playing`, 재생 완료 즉시 `Idle`(덕킹도 함께 해제) |

### Interactions with Other Systems

| 시스템 | 관계 | 강도 | 인터페이스 |
|---|---|---|---|
| 터치 케어 인터랙션 | 구독자(이 시스템 ← touch-care) | Hard | `OnGestureCompleted(gestureType)`(이산) + 진행 중 `progress`(연속) 구독. `touch-care-interaction.md` §5가 이미 확정한 제스처별 오디오 성격 표를 그대로 구현 |
| 날씨 시스템 | 구독자(이 시스템 ← weather) | Soft | `GetCurrentWeather()`(연속 enum) 구독 — 앰비언트 레이어 선택. 미응답 시 앰비언트만 무음/디폴트로 밋밋해지고 핵심 루프는 유지 |
| 생애주기 진행 | 구독자(이 시스템 ← lifecycle) | Soft | `OnStageAdvanced`(이산) 구독 — 생장 전환 SFX |
| 감자 상태 관리(potato-stat, `OnWitherEntered`/`OnRecovered` 훅 소유) | 구독자(이 시스템 ← potato-stat, 시듦/위기&회복·온실막 보호·부활 광고 3개 시스템이 공유 호출) | Hard | `OnWitherEntered`(이산)/`OnRecovered`(이산) 구독 — 위기 진입/회복 완료 사운드. 세 상위 시스템(wither-crisis-recovery, greenhouse-shield, revive-ad) 각각이 이 훅을 통해서만 간접 연결(직접 의존 아님) |
| 설정/접근성 시스템(Alpha, 잠정) | 피구독자(그 시스템 → 이 시스템) | Soft(MVP 미구현) | 마스터/카테고리별 볼륨값을 이 시스템의 믹서가 구독(잠정 인터페이스, Open Questions로 이월) |

## Formulas

> *(`systems-designer`+`sound-designer` 병렬 상담 완료 — lean 모드에서도 Formulas는
> 상담 필수 섹션. systems-designer가 공식 구조(EMA 팔로우 필터, 게인 엔벨로프
> 통합, 클립 길이 기반 Hold, 레이어별 덕킹 차등)를 제안했고, sound-designer가
> 실제 오디오 수치 범위(dB·ms)를 제안 — 두 제안을 결합.)*

### F1. 타임브 팔로우 (`TimbreFollow`, 창문/난로 연속 텍스처 전용)

`target(progress) = paramFloor · (paramCeil / paramFloor)^progress`
(로그-선형 보간 — 청각이 로그적이므로 리니어 Hz 보간 대신 사용)

`τ' = τ + (target(progress) - τ) · (1 - e^(-Δt/T_TIMBRE))`,
`T_TIMBRE = T_TIMBRE_ATTACK`(target > τ, 상승 중) 또는 `T_TIMBRE_RELEASE`(target < τ, 하강 중)

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 제스처 진행률 | `progress` | float | [0,1] | 터치 케어 인터랙션 소유, 이 시스템은 조회만(터치 케어 §Formulas §2/§4 재사용, 재정의 안 함) |
| 파라미터 하한 | `paramFloor` | float(const, 제스처별) | 창문: 700~1000Hz(LPF 컷오프) / 난로: 0~10%(새추레이션 드라이브) | `progress=0`일 때 목표값 |
| 파라미터 상한 | `paramCeil` | float(const, 제스처별) | 창문: 9,000~14,000Hz / 난로: 40~60% | `progress=1`일 때 목표값 |
| 팔로우 결과(사전/사후) | `τ`, `τ'` | float | `[paramFloor, paramCeil]` | 실제 오디오 파라미터에 적용되는 값 — target을 즉시 따라가지 않고 시상수로 완만히 추종 |
| 상승 시상수 | `T_TIMBRE_ATTACK` | float(const) | 안전범위 50~150ms(미검증, 플레이테스트로 확정) | 진행이 늘어날 때(눅눅함→맑음, 잔불→허밍) 팔로우 속도 |
| 하강 시상수 | `T_TIMBRE_RELEASE` | float(const) | 안전범위 200~400ms(미검증) | `Decaying` 중 팔로우 속도 — 터치 케어 Formula 4의 "얼어붙었다 서서히 풀림" 정서와 결이 맞는 쪽으로(release가 attack보다 김) |

**Output Range:** `[paramFloor, paramCeil]`로 유계 — target 자체가 이 구간 밖으로
나가지 않으므로 EMA 추종값도 오버슈트 없이 이 구간 안에 머문다.
**Example(창문):** `paramFloor=800Hz, paramCeil=12000Hz`. `progress=0.5` →
`target=800·(12000/800)^0.5=800·√15≈3098Hz`. `progress=1.0` → `target=12000Hz`.
`τ`가 `target`을 `T_TIMBRE_ATTACK(100ms)` 시상수로 추종하며 완만히 근접.

**물뿌리개는 이 공식을 쓰지 않는다** — Interactions 표가 이미 확정한 대로 다브는
이산(discrete) 입력이라 progress 스윕이 아니라 라운드로빈+지터로 처리한다(피치
±2~4%/볼륨 ±2dB, **이 지터는 progress나 긴급도에 매핑되지 않는 순수 반복감 방지용
휴먼화**이지 F1의 팀브 팔로우와 다른 범주다 — Core Rule의 "피치 상승으로 다급함
표현 금지" 제약과 혼동하지 않도록 명시).

**하드 중단 시 거동(qa-lead 발견 갭 해소)**: 터치 케어의 하드 중단(Core Rule 5)이
발생하면 `τ`는 보간 없이 즉시 `paramFloor`로 리셋된다 — F1b의 게인이 동시에
`0`으로 리셋되는 것과 원자적으로 함께 일어나며, 마지막 `τ` 값을 남겨두지 않는다.
재개 후 새 접촉이 시작되면 항상 `paramFloor`에서 다시 추종을 시작한다(stale 필터
상태로 시작하지 않음).

---

### F1b. 게인 엔벨로프 (`GestureGain`, 3가지 정지 사유 통일)

`gain(t) = progressUnderlying(t)`, 단 `progressUnderlying`이 정확히 `0`에 도달하는
순간에만 클릭/팝 방지용 `T_GAIN_RELEASE` 짧은 페이드를 적용

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 근원 진행률 | `progressUnderlying` | float | [0,1] | 터치 케어의 `progress`(Active) 또는 `decayed_progress(t)`(Decaying, Formula 4) 또는 즉시 `0`(Hard Interrupt) — 이 시스템은 어느 것이든 값 그대로 받아 게인에 반영, 별도 감쇠 로직을 중복 구현하지 않는다 |
| 안티클릭 페이드 | `T_GAIN_RELEASE` | float(const) | 15~30ms | `progressUnderlying`이 `0`에 도달하는 모든 경우(정상 완성 후 유지 종료·감쇠 만료·하드 중단)에 공통 적용되는 극히 짧은 디지털 클릭 방지용 페이드 — 체감 지연이 아니라 순수 오디오 위생 처리 |

**Output Range:** `[0,1]`, 항상 `progressUnderlying`과 거의 동일(15~30ms 이내
차이만 존재).
**Example:** 하드 중단 발생 — 터치 케어가 `progress`를 즉시 `0`으로 리셋(유예
없음, Core Rule 5). 이 시스템은 그 `0`을 그대로 받되, 실제 오디오 출력은
`T_GAIN_RELEASE=20ms` 동안 선형으로 무음까지 내려가 디지털 팝 없이 조용해진다 —
게임플레이 규칙(즉시 리셋)은 전혀 바뀌지 않고, 오디오 출력만 위생 처리된다.
**Core Rule 3 해소**: 완성 후 유지 종료(다음 제스처까지 무음)·감쇠 만료·하드
중단, 이 세 가지 서로 다른 "왜 멈췄는가"를 이 공식은 구분하지 않는다 — 셋 다
`progressUnderlying=0`이라는 동일한 최종 상태로 수렴하므로 오디오 레이어 입장에선
동일하게 처리하면 충분하다.

---

### F2. 이벤트 덕킹 (`DuckMultiplier`)

```
DuckMultiplier(t) =
  ease_in(t / T_DUCK_ATTACK) · (floor - 1) + 1,             0 ≤ t < T_DUCK_ATTACK
  floor,                                                     T_DUCK_ATTACK ≤ t < T_DUCK_ATTACK + T_HOLD
  ease_out((t - T_DUCK_ATTACK - T_HOLD) / T_DUCK_RELEASE) · (1 - floor) + floor,
                                                              T_DUCK_ATTACK+T_HOLD ≤ t < T_DUCK_ATTACK+T_HOLD+T_DUCK_RELEASE
  1,                                                         t ≥ 전체 길이
```
`floor = 10^(-D/20)`(dB → 선형 배율 변환)

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 이벤트 발생 후 경과 | `t` | float(초) | ≥0 | 이벤트 레이어 트리거 시각 기준 |
| 어택 시간 | `T_DUCK_ATTACK` | float(const) | 150~300ms(하한 120~150ms, 하드 제약) | 덕킹이 시작돼 바닥까지 내려가는 시간 |
| 유지 시간 | `T_HOLD` | float(const, 이벤트별 파생) | `= clipDuration(eventSFX) + tailPadding - T_DUCK_ATTACK` | 원샷 이벤트 SFX가 끝날 때까지 바닥을 유지 — 고정 상수가 아니라 실제 클립 길이에서 파생 |
| 릴리스 시간 | `T_DUCK_RELEASE` | float(const) | 0.8~2.0s(하한 600~800ms, 하드 제약) | 바닥에서 원래 볼륨(1.0)으로 복귀하는 시간 |
| 덕킹 깊이(dB) | `D` | float(const, 이벤트티어×레이어) | 아래 표, 상한 -10dB(하드 제약) | dB 단위 감쇠량(양수로 표기, 배율 변환 시 음수 지수) |
| 바닥 배율 | `floor` | float | `10^(-D/20)` | 덕킹 최저점의 선형 볼륨 배율(0=완전무음, 1=무변화) — 앰비언트 레이어는 이 값이 0이 되는 것을 절대 허용하지 않는다(하드 제약) |

**덕킹 깊이 2차원 표(이벤트 티어 × 대상 레이어):**

| | 앰비언트 레이어(D_AMBIENT) | 제스처 레이어(D_GESTURE) |
|---|---|---|
| **성장 이벤트**(`OnStageAdvanced`) | `D_AMBIENT_GROWTH = 6dB` | `D_GESTURE_GROWTH = 3dB` |
| **위기/회복 이벤트**(`OnWitherEntered`/`OnRecovered`) | `D_AMBIENT_CRISIS = 9dB` | `D_GESTURE_CRISIS = 5dB` |

두 축 모두 의도적으로 차등화된다 — **가로축**(이벤트 티어)은 Core Rule 6의
정서적 무게 우선순위(위기/회복 > 성장)를 덕킹 깊이 차이(최소 2dB)로 믹스에서도
들리게 하고, **세로축**(대상 레이어)은 Core Rule 2의 레이어 우선순위(제스처 >
앰비언트)를 덕킹 중에도 비례 보존한다 — 제스처 레이어는 항상 앰비언트보다 얕게
덕킹돼 청감상 위계가 이벤트 발생 중에도 뒤집히지 않는다.

**Output Range:** `[floor, 1.0]`, `floor > 0` 항상(앰비언트도 완전 무음 금지,
하드 제약).
**Example(위기/회복 이벤트, 앰비언트 레이어):** `D=9dB` → `floor=10^(-9/20)≈0.355`.
`T_DUCK_ATTACK=200ms` 선형 하강 → `t=200ms`에 배율 `0.355` 도달 → 원샷 SFX
클립 길이(예: 1.8s)+tailPadding(0.3s)-어택(0.2s)만큼 `T_HOLD=1.9s` 유지 →
`T_DUCK_RELEASE=1.5s` 동안 exponential ease-out으로 `1.0`까지 복귀.

**하드 중단 시 거동(qa-lead 발견 갭 해소)**: 앱 백그라운드 전환/강제 종료가
발생하면 진행 중이던 모든 덕킹 배율은 자기 타임라인을 끝까지 흘려보내지 않고
**즉시 `1.0`으로 강제 복귀**한다 — 이벤트 레이어 자체의 리셋(States and
Transitions)과 원자적으로 동시에 일어난다. 이 규칙이 없으면 하드 중단 이후에도
한동안 앰비언트가 부당하게 낮은 채로 남을 위험이 있다.

---

### F3. 동시 이벤트 우선순위 (`WinnerEvent`)

`WinnerEvent(eventsFiredThisWindow) = argmin_e RANK_AUDIO_EVENT(e)`, `e ∈ eventsFiredThisWindow`

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 동시 발생 이벤트 집합 | `eventsFiredThisWindow` | set&lt;event&gt; | 이벤트 레이어 트리거 중 짧은 충돌 윈도우 내 발생분 | 정확한 윈도우 폭은 Edge Cases 참고 |
| 위기/회복 랭크 | `RANK_AUDIO_EVENT_CRISIS` | int(const) | `0`(최우선) | `OnWitherEntered`/`OnRecovered` |
| 성장 랭크 | `RANK_AUDIO_EVENT_GROWTH` | int(const) | `1` | `OnStageAdvanced` |
| 결과 | `WinnerEvent` | event | 입력 집합의 원소 중 하나 | 가장 낮은 랭크(최우선) 이벤트만 실제 재생, 나머지는 스킵(큐잉 없음) |

`ad-sdk-integration-layer.md`의 `RANK_REVIVE=0/RANK_GACHA=1/RANK_BOOSTER=2/
RANK_SHIELD=3`(낮을수록 우선) 패턴을 그대로 재사용 — 이 프로젝트 전역에서
"낮은 숫자=높은 우선순위" 관례가 일관된다.

**충돌 윈도우 앵커링(qa-lead 발견 갭 해소)**: `SIMULTANEOUS_WINDOW_MS`는
**롤링 윈도우가 아니라, 이벤트 레이어가 `Idle`에서 첫 후보를 받은 시각에 고정되는
수집 윈도우**다 — 그 시각부터 `[0, SIMULTANEOUS_WINDOW_MS)`(시작 포함·끝 미포함)
사이에 도착한 이벤트를 전부 모아 `WinnerEvent`를 정확히 1회 평가한 뒤 이벤트
레이어가 `Playing`으로 전이한다. 윈도우가 닫힌 뒤에는 Core Rule 6의 비선점
원칙에 따라 새 이벤트가 도착해도(레이어가 아직 `Playing` 중이므로) 무조건
드롭된다 — 3개 이상이 연쇄로 도착해도 무한히 윈도우가 갱신되지 않는다.

**Output Range:** 입력 집합의 원소 중 정확히 하나(입력이 비어있지 않은 한 총함수).
**Example:** 같은 프레임에 `OnStageAdvanced`와 `OnRecovered`가 동시 발생 →
`RANK_AUDIO_EVENT_GROWTH(1) > RANK_AUDIO_EVENT_CRISIS(0)` → `WinnerEvent=OnRecovered`
만 재생, `OnStageAdvanced`의 SFX는 이번 발생분에 한해 스킵된다(다음 번 단독
발생 시엔 정상 재생).

---

**상수 요약(레지스트리 등록 후보)**: `TimbreFollow`·`GestureGain`·
`DuckMultiplier`·`WinnerEvent`(formula 4종), `T_TIMBRE_ATTACK/RELEASE`·
`T_GAIN_RELEASE`·`T_DUCK_ATTACK/RELEASE`·`D_AMBIENT_GROWTH/CRISIS`·
`D_GESTURE_GROWTH/CRISIS`·`RANK_AUDIO_EVENT_CRISIS/GROWTH`(constant, 다수).

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드.)*

- **If 이벤트 레이어의 두 트리거가 `SIMULTANEOUS_WINDOW_MS`(안전범위 100~250ms)
  이내에 발생한다면**: F3(`WinnerEvent`)이 적용되어 랭크가 낮은(우선순위 높은)
  이벤트만 재생하고 나머지는 스킵한다. 이 윈도우 밖에서 발생한 두 이벤트는
  "동시"로 간주하지 않고 각자 독립적으로(자기 자신의 덕킹 포함) 순차 재생된다.
- **If 두 개의 개별 덕킹이 시간상 서로 겹친다면**(윈도우 밖이라 F3이 적용되지
  않는 경우)**: 별도의 덕킹 합성 로직을 두지 않는다 — 각 레이어는 그 순간
  활성화된 모든 덕킹 배율 중 가장 낮은(가장 깊은) 값을 그대로 취한다(`min`).
  같은 오디오 버스이므로 자연히 더 깊은 덕킹이 우선한다.
- **If 감자가 시듦(Wither) 게이팅 중이라면**: 터치 케어 인터랙션이 `Idle→Active`
  전이 자체를 하지 않으므로(`touch-care-interaction.md` AC-EC-05) 이 시스템에
  `OnGestureCompleted`도 `progress`도 아예 발행되지 않는다 — 제스처 레이어는
  자동으로 무음 상태를 유지하며, 이 시스템 쪽에서 별도의 시듦 게이팅 로직을
  구현할 필요가 없다.
- **If 앱이 백그라운드로 전환되거나 강제 종료된다면**: 터치 케어의 하드 중단
  (Core Rule 5)에 따라 제스처 레이어와 이벤트 레이어는 즉시 리셋된다(유예 없음).
  이때 F1의 `τ`는 즉시 `paramFloor`로, F2의 덕킹 배율은 즉시 `1.0`으로 함께
  리셋된다(각각의 Formula 절에서 확정) — 재개 후 stale 필터 상태나 부당하게
  낮은 앰비언트 볼륨이 남지 않는다. 앱이 포그라운드로 복귀하면 앰비언트
  레이어는 weather-system이 이미 확정한 규칙(오프라인 갭을 지나 재개 시
  크로스페이드 없이 즉시 새 날씨로 스냅)을 그대로 따른다 — 이 시스템이 별도의
  복귀 전용 크로스페이드를 만들지 않는다.
- **If 이벤트 레이어가 이미 `Playing`(재생 중)인 도중 새 이벤트가
  도착한다면(`SIMULTANEOUS_WINDOW_MS` 밖)**: 우선순위와 무관하게 항상
  드롭된다(Core Rule 6 비선점 원칙) — 재생 중인 이벤트를 인터럽트하고 새
  이벤트로 갈아타지 않는다. 예: `OnWitherEntered`(최우선)가 재생 중인데 그 직후
  `OnStageAdvanced`가 도착해도, 랭크와 무관하게 후자는 스킵된다.
- **If 신규 세이브(콜드 스타트, 최초 실행)라면**: 앰비언트 레이어는 States and
  Transitions 표가 이미 확정한 대로 `Idle` 상태 없이 즉시 `Playing`으로
  시작한다(`GetCurrentWeather()`가 최초 실행부터 유효한 enum을 반환하므로).
  제스처·이벤트 레이어는 `Silent`/`Idle`에서 시작 — 아직 어떤 터치나 이벤트도
  발생하지 않았기 때문이다.
- **If 물뿌리개 다브가 짧은 간격으로 연속 재생된다면**: 다브 1회 1회는 F1(타임브
  팔로우)이 아니라 독립적인 라운드로빈+지터 재생이므로, 여러 다브의 사운드가
  겹쳐 들리는 것 자체가 자연스러운 "여러 방울" 효과다 — 창문/난로처럼 하나의
  연속 파라미터로 수렴시킬 필요가 없다(Interactions 표의 이산/연속 분류를 그대로
  반영).

## Dependencies

### Upstream (이 시스템이 의존)

| 시스템 | 방향 | 강도 | 인터페이스 |
|---|---|---|---|
| 터치 케어 인터랙션 | 구독(이 시스템 ← touch-care) | Hard | `OnGestureCompleted(gestureType)`(이산) + 진행 중 `progress`(연속) 구독 — 응답 없으면 제스처 레이어 전체가 무의미(핵심 정체성의 절반) |
| 감자 상태 관리(potato-stat) | 구독(이 시스템 ← potato-stat) | Hard | `OnWitherEntered`/`OnRecovered`(둘 다 이산) 구독 — 위기/회복 이벤트 레이어의 유일한 소스 |
| 날씨 시스템 | 구독(이 시스템 ← weather) | Soft | `GetCurrentWeather()`(연속 enum) 구독 — 앰비언트 레이어 선택. 미응답 시 앰비언트만 무음/디폴트로 밋밋해지고 핵심 루프는 유지 |
| 생애주기 진행 | 구독(이 시스템 ← lifecycle) | Soft | `OnStageAdvanced`(이산) 구독 — 생장 전환 SFX. 미응답 시 생장 전이 자체는 정상 작동, 연출만 무음 |

### Downstream (이 시스템에 의존)

| 시스템 | 방향 | 강도 | 인터페이스 |
|---|---|---|---|
| 설정/접근성 시스템(Alpha) | 피구독자(그 시스템 → 이 시스템) | Soft(MVP 미구현) | 마스터/카테고리별 볼륨값을 이 시스템의 믹서가 구독(잠정 인터페이스, Open Questions로 이월) |

**Hard vs Soft 판정 기준**: 다른 GDD와 동일 — 이 시스템이 응답하지 않을 때 대상
시스템이 아예 작동을 멈추면 Hard, 연출/사운드만 밋밋해지고 핵심 루프는 유지되면
Soft. 이 시스템 자체는 4개 Upstream 중 응답이 없어도(예: 미설계 상태에서도)
어떤 게임플레이도 멈추지 않는다 — 그래서 이 시스템은 다른 시스템들에게 항상
Soft 또는 순수 청취자다.

**양방향 일관성 확인 및 교차 수정(2026-07-21)**: `weather-system.md`·
`touch-care-interaction.md`는 이미 자신의 Downstream 표에 이 시스템을 정확히
기록해두고 있었다(추가 수정 불필요). 반면 `potato-stat-system.md`와
`lifecycle-progression-system.md`의 Downstream 표에는 이 시스템이 Visual/Audio
"범위 메모" 섹션에서만 프로즈로 언급되고 정식 Dependencies 표에는 누락돼
있었다 — 이번 세션에서 두 문서 모두 Soft 행을 추가해 정합성을 맞췄다.

## Tuning Knobs

| 노브 | 기본값 | 안전 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `T_TIMBRE_ATTACK` | 100ms(미검증) | 50~150ms | 음색 변화가 progress 원시값을 그대로 노출해 "물리적 질감"이 아니라 디지털 스위칭처럼 들림 | 손끝 반응이 지연되는 것처럼 느껴져 Pillar 1(즉각 반응) 위반 위험 |
| `T_TIMBRE_RELEASE` | 300ms(미검증) | 200~400ms | 감쇠(Decaying) 중 음색 복귀가 너무 빨라 터치 케어 Formula 4의 "서서히 풀림" 정서와 어긋남 | 손을 뗀 지 한참 지나도 음색이 안 돌아와 "그만뒀는데도 안 사라짐"이 어색하게 느껴짐 |
| `T_GAIN_RELEASE` | 20ms | 15~30ms | 디지털 클릭/팝이 들려 오디오 위생 문제 발생 | 하드 중단(즉시 리셋) 규칙과 체감이 어긋나기 시작(들리는 지연이 생김) |
| `T_DUCK_ATTACK` | 200ms | 150~300ms(하한 120~150ms, 하드 제약) | 덕킹이 순간적으로 걸려 그 자체가 펌핑/게이트 아티팩트로 들려 "다이내믹 이벤트"가 튀는 느낌을 줌 | 이벤트 사운드가 시작된 후에도 한참 배경음이 안 낮아져 이벤트가 묻힘 |
| `T_DUCK_RELEASE` | 성장 1.0s / 위기·회복 1.5s | 0.8~2.0s(하한 600~800ms, 하드 제약) | 복귀가 빨라 "튀어 돌아오는" 것처럼 들려 믹싱 기법 자체가 노출됨 | 이벤트가 끝난 지 한참 지나도 배경음이 안 돌아와 "정적인 포근함" 톤이 깨짐 |
| `D_AMBIENT_GROWTH` / `D_GESTURE_GROWTH` | 6dB / 3dB | 상한 -10dB(하드 제약), 최소 2dB 차이 유지(`D_AMBIENT` > `D_GESTURE`) | 덕킹이 안 들려 이벤트 발생 자체가 인지되지 않음 | -10dB 초과 시 "정적인 포근함" 원칙 위반(게임 UI 성취 팡파레 어휘에 근접) |
| `D_AMBIENT_CRISIS` / `D_GESTURE_CRISIS` | 9dB / 5dB | 상한 -10dB(하드 제약), `D_*_GROWTH`보다 최소 2dB 깊게 유지 | Core Rule 6의 정서적 무게 우선순위(위기 > 성장)가 믹스에서 안 들림 | 위와 동일(-10dB 상한 위반 위험) |
| `SIMULTANEOUS_WINDOW_MS` | 150ms(미검증) | 100~250ms | 사실상 거의 항상 "동시 아님"으로 판정돼 F3(WinnerEvent)이 사실상 작동 안 함 | 실제로는 순차적인 두 이벤트도 "충돌"로 오판해 뒤 이벤트가 부당하게 스킵됨 |
| `AMBIENT_BASE_GAIN` / `GESTURE_BASE_GAIN` | 상대값(예: -6dB / 0dB 기준) | `GESTURE_BASE_GAIN > AMBIENT_BASE_GAIN` 항상 유지 | (앰비언트가 상대적으로 너무 크면) 어떤 덕킹도 없는 평상시에도 터치 제스처 사운드가 앰비언트에 묻혀 Core Rule 2·Player Fantasy의 "터치 항상 전경 우선" 위반(qa-lead 발견 갭 해소) | (제스처가 지나치게 크면) 배경 무드가 거의 안 들려 "ASMR 앰비언트가 정체성의 절반" 원칙이 무색해짐 |

**노브 간 상호작용**: `D_AMBIENT_*`와 `D_GESTURE_*`는 항상 "성장 < 위기/회복"
(가로축)과 "제스처 < 앰비언트"(세로축) 두 부등식을 동시에 만족해야 한다 — 하나만
조정하면 F2의 2차원 위계 보존 목적이 깨진다. `T_DUCK_ATTACK`/`T_DUCK_RELEASE`는
공통 하드 제약(120~150ms/600~800ms 하한)을 모든 이벤트 티어가 공유하므로, 하나를
그 하한 밑으로 내리면 F2 전체의 정서적 안전장치가 무력화된다. `AMBIENT_BASE_GAIN`/
`GESTURE_BASE_GAIN`은 덕킹(F2)과 독립적인 **평상시 기본 위계**를 담당한다 — F2는
이벤트 발생이라는 예외 상황에서만 일시적으로 작동하므로, 이벤트가 전혀 없는
대부분의 플레이 시간 동안 "제스처가 앰비언트보다 우선"을 지키는 것은 이 두
기본 게인 상수의 몫이다(Core Rule 2 전체 요구를 F2 하나로 다 감당하지 않음).

## Visual/Audio Requirements

**Visual**: 해당 없음 — 이 시스템은 순수 오디오 시스템이며 어떤 시각 자산도
소유하지 않는다.

### 오디오 에셋 목록

1. **앰비언트 루프 4종(날씨별, weather-system의 `GetCurrentWeather()` enum과
   1:1 대응)**: 맑음(온화한 백색소음/자연 배경), 비·안개(빗소리+미미한 바람),
   한파·폭설(먹먹한 바람, 성긴 텍스처), 폭염(post-MVP, 건조한 질감). MVP는
   앞 3종만 필요(weather-system MVP 스코프와 일치).
2. **제스처 사운드**(`touch-care-interaction.md` §5 성격 명세를 실제 에셋으로
   번역):
   - 창문: 연속 유리 마찰음 루프(F1 `TimbreFollow`가 LPF 컷오프를 구동, 눅눅함→
     맑음) + 완성 시 원샷 "숨을 내쉬는 바람 소리"
   - 난로: 연속 허밍/잔불 루프(F1이 새추레이션 드라이브를 구동, 온기·배음
     짙어짐) + 완성 시 원샷 "잉걸불이 자리잡는 정착 소리"
   - 물뿌리개: 다브 1회당 4~6종 샘플 라운드로빈(피치 지터 ±2~4%/볼륨 지터
     ±2dB, F1 미적용) + 완성 시 원샷 "옅은 스팀이 빠지는 쉬-"
3. **이벤트 SFX**: `OnStageAdvanced`(게임 전체에서 가장 조용한 사운드 — 4일에
   3번뿐, lifecycle-progression이 이미 "가장 조용한 이벤트"로 시각 연출을
   설계해둔 것과 청각적으로 정합), `OnWitherEntered`(걱정스럽지만 경각심을
   주지 않는 톤 — Pillar 3 공포 마케팅 금지 원칙과 정합, 경보음/단조 스팅어
   금지), `OnRecovered`(안도감/해소 톤 — 광고 경로 vs 무료 경로 완료음 구별
   여부는 여러 GDD가 이미 이월해둔 Open Question).

### 믹싱 철학(재확인, Player Fantasy·Core Rules에서 이미 확정)

- **게임 UI 보상음 문법 전면 금지**: "성공 차임/딩" 류는 이 시스템의 어떤
  레이어에도 등장하지 않는다.
- **피치 기반 다급함 표현 금지**: 모든 강도 변화는 음색/질감(필터·새추레이션·
  볼륨)으로만 표현한다 — 물뿌리개의 반복감 방지 피치 지터(F1 각주)는 이
  금지의 예외가 아니라 애초에 다른 범주(무작위 휴먼화)다.
- **앰비언트 완전 무음 금지**: 어떤 덕킹(F2)도 앰비언트를 `-∞`로 내리지 않는다
  — "날씨 앰비언트는 항상 배경에 머물러야 한다"(Player Fantasy)는 원칙의
  직접적 결과.
- **art-bible 에너지 레벨 어휘 그대로 계승**: "다급함(frenetic/urgent)" 등급
  자체가 art-bible에 없으므로, 이 시스템의 어떤 사운드도 그 등급을 새로
  발명하지 않는다.

📌 **Asset Spec** — Visual/Audio requirements가 정의되었습니다. art bible 승인
이후이므로 `/asset-spec system:asmr-adaptive-audio`를 실행해 에셋별 상세 명세·
생성 프롬프트를 뽑을 수 있습니다(오디오 자산은 시각 자산과 다른 브리프 형식이
필요할 수 있음 — audio-director/sound-designer와 확인 권장).

## UI Requirements

이 시스템은 신규 UI를 소유하지 않는다. 볼륨/음소거 컨트롤은 설정/접근성
시스템(Alpha, 미설계)이 소유할 예정이며, 이 시스템은 그 시스템이 노출하는
값을 구독만 한다(Dependencies 확정).

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — 2026-07-21. lean 모드에서도 Acceptance Criteria는 상담
> 필수 섹션. qa-lead가 발견한 완결성 갭 5건(기본 위계 상수 부재·바지 중 도착
> 처리·충돌 윈도우 앵커링·하드 중단 시 τ/덕킹 거동)을 Core Rules/Formulas/
> Edge Cases에 즉시 반영한 뒤, 그 보강분을 포함한 최종 기준.)*

> 이 시스템은 testing-standards.md 기준 **Logic 성격이 강한 시스템**이다 — 레이어
> 상태 전이(AC-ST), 4개 공식(AC-F), 우선순위/엣지케이스(AC-EC)는 순수 함수/
> 상태머신이라 시간(Δt)을 주입받는 결정론적 유닛 테스트로 전량 자동화 가능하며
> `tests/unit/audio/`에 귀속(BLOCKING). Hard 의존 2건(터치 케어·potato-stat)의
> 구독 계약(AC-INT)은 mock 발행자를 이용한 통합 테스트로 자동화 가능하며
> `tests/integration/audio/`에 귀속(BLOCKING) — Soft 의존은 "미응답해도 핵심
> 루프 유지"만 검증. 사운드 카테고리 라우팅(AC-VA)은 데이터 계약이라 자동화
> 가능하나 ADVISORY. 실제 "이완처럼 들리는가"류 Feel 항목은 범위 밖 절에 명시적
> 분리해 수동 QA로 이월한다.

### I. 레이어 상태 및 전이 (Core Rules 1~3, States and Transitions)

- **AC-ST-01 (콜드 스타트 — 앰비언트 즉시 Playing, Idle 없음)** **GIVEN** 신규
  세이브(최초 실행), `GetCurrentWeather()`가 유효한 enum 반환 **WHEN** 게임
  최초 로드 **THEN** 앰비언트 레이어는 `Idle`을 거치지 않고 즉시 `Playing`
  (해당 날씨 루프)으로 시작한다.
- **AC-ST-02 (콜드 스타트 — 제스처/이벤트는 Silent/Idle에서 시작)** **GIVEN**
  신규 세이브, 아직 어떤 터치·이벤트도 없음 **WHEN** 게임 최초 로드 **THEN**
  제스처 레이어=`Silent`, 이벤트 레이어=`Idle`.
- **AC-ST-03 (제스처 Silent → Playing)** **GIVEN** 대응 제스처가 터치 케어에서
  `Idle` **WHEN** 터치 케어가 `Active`로 전이(접촉 시작) **THEN** 제스처
  레이어는 `Silent→Playing`으로 전이하고 이후 매 프레임 `progress`를 조회해
  F1/F1b 입력으로 쓴다.
- **AC-ST-04 (제스처 Playing → OneShot, θ_gesture 도달)** **GIVEN** 제스처
  레이어=`Playing`, `progress<θ_gesture`(=1.0, touch-care 고정값) **WHEN**
  `progress`가 그 프레임에 최초로 `θ_gesture`에 도달(`OnGestureCompleted`
  발행과 동일 프레임) **THEN** 완성 원샷이 정확히 1회 트리거되고, 접촉이
  유지돼도 추가 원샷은 없다.
- **AC-ST-05 (제스처 Playing → Decaying → Silent)** **GIVEN** 제스처
  레이어=`Playing`, 완성 전 **WHEN** 터치 케어가 `Active→Decaying` 전이(접촉
  해제) **THEN** F1b의 `progressUnderlying`이 터치 케어의 `decayed_progress(t)`
  를 그대로 따라 게인이 감쇠하고, 0에 도달(또는 `DECAY_GRACE_WINDOW` 만료)하는
  순간 `Silent`로 전이 — 이 시스템은 별도 감쇠 타이머를 갖지 않는다.
- **AC-ST-06 (이벤트 Idle → Playing, 3개 소스 공통)** **GIVEN** 이벤트
  레이어=`Idle` **WHEN** `OnStageAdvanced`/`OnWitherEntered`/`OnRecovered` 중
  하나가 F3 평가 후 승자로 확정 **THEN** `Playing`으로 전이하고 F2가 즉시
  하위 두 레이어에 적용되기 시작한다.
- **AC-ST-07 (이벤트 Playing → Idle, 재생 완료=덕킹 해제 동시)** **GIVEN**
  이벤트 레이어=`Playing` **WHEN** 원샷 SFX + F2 전체 길이
  (`T_DUCK_ATTACK+T_HOLD+T_DUCK_RELEASE`)가 경과 **THEN** `Idle` 복귀와 덕킹
  배율의 `1.0` 복귀가 동시에 일어난다.
- **AC-ST-08 (하드 중단 — 즉시 리셋, τ·덕킹 포함)** **GIVEN** 제스처
  레이어=`Playing`/`Decaying`, 이벤트 레이어=`Playing`(덕킹 중) 등 임의 조합
  **WHEN** 앱 백그라운드 전환/강제 종료(터치 케어 Core Rule 5) **THEN** 세 가지가
  원자적으로 동시에 리셋된다 — 제스처/이벤트 레이어는 `Silent`/`Idle`로,
  F1의 `τ`는 `paramFloor`로, F2의 덕킹 배율은 `1.0`으로(`T_GAIN_RELEASE`
  안티클릭 페이드만 예외적으로 짧게 적용).
- **AC-ST-09 (이벤트 레이어 바지 중 도착 — 비선점 드롭)** **GIVEN** 이벤트
  레이어=`Playing`(예: `OnWitherEntered` 재생 중) **WHEN** `SIMULTANEOUS_WINDOW_MS`
  밖에서 새 이벤트(예: `OnStageAdvanced`) 도착 **THEN** 랭크와 무관하게 새
  이벤트는 드롭되고 재생 중인 이벤트는 인터럽트되지 않는다.

### II. F1 — 타임브 팔로우 (TimbreFollow)

- **AC-F-01 (경계값 progress=0)** **GIVEN** 창문 `paramFloor=800Hz,
  paramCeil=12000Hz` **WHEN** `progress=0` **THEN** `target=800Hz`.
- **AC-F-02 (경계값 progress=1)** **GIVEN** 상동 **WHEN** `progress=1` **THEN**
  `target=12000Hz`.
- **AC-F-03 (중간값)** **GIVEN** 상동 **WHEN** `progress=0.5` **THEN**
  `target=800·√15≈3098Hz`(±1Hz).
- **AC-F-04 (시상수 분기 — 상승 vs 하강)** **GIVEN** `τ=5000Hz`, (a)
  `target=8000Hz`(target>τ) (b) `target=3000Hz`(target<τ) **WHEN** `τ'` 갱신
  **THEN** (a)는 `T_TIMBRE_ATTACK`, (b)는 `T_TIMBRE_RELEASE` 사용.
- **AC-F-05 (물뿌리개 F1 미적용 확인)** **GIVEN** 활성 제스처=물뿌리개 **WHEN**
  다브 발생 **THEN** 어떤 LPF/새추레이션 파라미터도 `progress`로 구동되지 않고
  라운드로빈+지터만 적용되며, 이 지터는 `progress`/`dab_progress`에 따라
  변하지 않는다.
- **AC-F-06 (하드 중단 시 τ 즉시 리셋)** **GIVEN** `τ`가 임의 중간값(예:
  5000Hz) **WHEN** 하드 중단 발생 **THEN** `τ`는 보간 없이 즉시 `paramFloor`
  로 스냅되고, 재개 후 새 접촉은 항상 `paramFloor`에서 다시 추종을 시작한다.

### III. F1b — 게인 엔벨로프 (GestureGain)

- **AC-F-07 (게인=progressUnderlying 그대로)** **GIVEN** `progress=0.6`(Active)
  **WHEN** 게인 조회 **THEN** `gain=0.6`, 별도 스무딩/감쇠 로직 없음.
- **AC-F-08 (3가지 정지 사유 통일)** **GIVEN** `progressUnderlying=0` 도달
  원인 3종: (a) 완성 후 유지 종료 (b) Decaying 감쇠 만료 (c) 하드 중단 **WHEN**
  각 케이스 게인 출력 관찰 **THEN** 셋 다 동일하게 `T_GAIN_RELEASE`(15~30ms)
  선형 페이드로 수렴 — 원인별 분기 코드 경로 없음.
- **AC-F-09 (T_GAIN_RELEASE 안전범위)** **GIVEN** 설정값 **WHEN** 검증 **THEN**
  15~30ms 범위 내(스모크 체크 대상).

### IV. F2 — 이벤트 덕킹 (DuckMultiplier)

- **AC-F-10 (dB→floor 변환, 4개 조합)** **GIVEN** `D_AMBIENT_GROWTH=6,
  D_GESTURE_GROWTH=3, D_AMBIENT_CRISIS=9, D_GESTURE_CRISIS=5`(dB) **WHEN**
  `floor=10^(-D/20)` **THEN** 각각 ≈0.501/0.708/0.355/0.562(±0.001).
- **AC-F-11 (봉투 3구간)** **GIVEN** 위기/회복, 앰비언트, `D=9dB`, 클립
  길이=1.8s, tailPadding=0.3s, `T_DUCK_ATTACK=200ms` **WHEN** t=0→종료까지
  진행 **THEN** `T_HOLD=1.8+0.3-0.2=1.9s` 파생, t=200ms에 floor≈0.355 도달,
  200~2100ms 유지, 이후 `T_DUCK_RELEASE` 동안 ease-out으로 1.0 복귀.
- **AC-F-12 (T_HOLD는 파생값 — 고정 상수 아님)** **GIVEN** 서로 다른 클립
  길이(1.2s, 2.4s), 동일 tailPadding·어택 **WHEN** 각각 T_HOLD 계산 **THEN**
  서로 다른 값 산출.
- **AC-F-13 (앰비언트 완전 무음 금지 — 하드 제약)** **GIVEN** 덕킹 깊이 상한
  10dB **WHEN** `floor=10^(-10/20)` **THEN** ≈0.316(>0) — 어떤 `D`로도
  `floor=0` 금지.
- **AC-F-14 (세로축 부등식 — 레이어)** **GIVEN** 성장(6/3), 위기·회복(9/5)
  **WHEN** 티어 내부 비교 **THEN** 두 티어 모두 `D_AMBIENT > D_GESTURE`.
- **AC-F-15 (가로축 부등식 — 이벤트 티어)** **GIVEN** 상동 **WHEN** 레이어별
  티어 비교 **THEN** `D_AMBIENT_CRISIS(9)>D_AMBIENT_GROWTH(6)`,
  `D_GESTURE_CRISIS(5)>D_GESTURE_GROWTH(3)`.
- **AC-F-16 (하드 중단 시 모든 덕킹 즉시 1.0 복귀)** **GIVEN** 앰비언트에
  덕킹 배율 0.355 적용 중 **WHEN** 하드 중단 발생 **THEN** 자기 타임라인을
  끝까지 흘려보내지 않고 즉시 `1.0`으로 강제 복귀, 이벤트 레이어 리셋(AC-ST-08)
  과 동시에 일어난다.

### V. F3 — 동시 이벤트 우선순위 (WinnerEvent)

- **AC-F-17 (랭크 순서)** **GIVEN** `RANK_AUDIO_EVENT_CRISIS=0,
  RANK_AUDIO_EVENT_GROWTH=1` **WHEN** 비교 **THEN** CRISIS 항상 최우선.
- **AC-F-18 (동시 충돌)** **GIVEN** `OnStageAdvanced`와 `OnRecovered`가
  `SIMULTANEOUS_WINDOW_MS`(150ms) 이내 동시 발행 **WHEN** WinnerEvent 평가
  **THEN** `WinnerEvent=OnRecovered`만 재생, `OnStageAdvanced`는 이번 발생분만
  스킵.
- **AC-F-19 (스킵은 발생분 한정)** **GIVEN** AC-F-18 직후 **WHEN** 이후
  `OnStageAdvanced`가 단독(윈도우 밖) 재발생 **THEN** 정상 재생 — 영구 억제
  플래그 없음.
- **AC-F-20 (윈도우 앵커링 — 고정, 롤링 아님)** **GIVEN** 이벤트 A가 `t=0`에
  도착해 윈도우가 열림 **WHEN** 이벤트 B가 `t=140ms`(윈도우 내), 이벤트 C가
  `t=280ms`(A 기준 윈도우 밖, `SIMULTANEOUS_WINDOW_MS=150ms`) 도착 **THEN**
  A·B만 함께 평가되어 WinnerEvent 1회 산출, C는 그 시점 이벤트 레이어가
  아직 `Playing`이면 AC-ST-09(비선점 드롭)에 따라 별도로 드롭된다 — 윈도우가
  C 도착에 맞춰 다시 열리지 않는다.
- **AC-F-21 (기본 위계 — 덕킹과 독립, Core Rule 2)** **GIVEN**
  `GESTURE_BASE_GAIN`(예: 0dB)·`AMBIENT_BASE_GAIN`(예: -6dB), 이벤트 레이어
  `Idle`(덕킹 비활성) **WHEN** 두 레이어 동시 재생 중 실효 음량 비교 **THEN**
  `GESTURE_BASE_GAIN > AMBIENT_BASE_GAIN` 항상 성립 — 이벤트가 전혀 없는
  평상시에도 제스처가 앰비언트보다 우선함이 F2 없이도 유지된다.

### VI. Edge Cases

- **AC-EC-01 (윈도우 밖 — 독립 재생)** **GIVEN** 이벤트 A(t=0.0s), B(t=0.3s),
  window=150ms **WHEN** 시간차(300ms)>window **THEN** F3 미적용, 각자 독립
  재생(자기 F2 포함).
- **AC-EC-02 (겹치는 덕킹 — min 합성)** **GIVEN** 앰비언트에 두 덕킹 동시
  활성(0.708, 0.355) **WHEN** 실제 배율 조회 **THEN** `min(0.708,0.355)=0.355`
  적용, 별도 합성 공식 없음.
- **AC-EC-03 (시듦 게이팅 — 자동 무음)** **GIVEN** `IsWithering()=true`,
  touch-care AC-EC-05에 따라 `Idle→Active` 전이 자체가 없음 **WHEN** 플레이어
  터치 **THEN** `OnGestureCompleted`/`progress` 모두 미발행, 제스처 레이어는
  `Silent` 유지.
- **AC-EC-04 (백그라운드/포그라운드 — 하드 중단+날씨 스냅)** **GIVEN** 제스처=
  `Playing`, 이벤트=`Playing`(덕킹 중), 앰비언트=비 루프 **WHEN** 백그라운드
  후 복귀, 그사이 날씨가 한파로 전환 **THEN** 제스처/이벤트/τ/덕킹은
  AC-ST-08·AC-F-06·AC-F-16대로 즉시 리셋, 앰비언트는 weather-system의
  "오프라인 갭 후 즉시 스냅" 규칙대로 크로스페이드 없이 한파 루프로 전환.
- **AC-EC-05 (콜드 스타트)** — AC-ST-01/02와 동일 기준 참조(중복 정의 방지).
- **AC-EC-06 (물뿌리개 겹침 — 자연스러운 중첩)** **GIVEN** 다브 3회, 200ms
  간격, 각 사운드 길이 250ms **WHEN** 재생 스케줄 확인 **THEN** 서로
  중단시키지 않고 자연 중첩(F1 미적용이므로 강제 수렴 없음) — 의도된 동작.
- **AC-EC-07 (바지 중 도착 — 비선점 드롭)** — AC-ST-09와 동일 기준 참조(중복
  정의 방지).

### VII. Cross-System Interactions

- **AC-INT-01 (순수 구독자 — 역호출 없음, Core Rule 1)** **GIVEN** 4개
  Upstream 이벤트/상태 중 임의 수신 **WHEN** 핸들러 실행 **THEN** potato-stat/
  touch-care/weather/lifecycle의 어떤 쓰기 API도 호출하지 않는다(mock 발행자
  + spy로 자동화 가능).
- **AC-INT-02 (터치 케어 Hard 계약)** **GIVEN** touch-care가 Active 중 매
  프레임 `progress` 갱신 **WHEN** 이 시스템이 매 프레임 조회 **THEN** 그
  프레임 실제 값(또는 `decayed_progress`)과 정확히 일치.
- **AC-INT-03 (potato-stat Hard 계약 — 유일한 소스)** **GIVEN** potato-stat이
  `OnWitherEntered`/`OnRecovered` 각각 정확히 1회 발행(potato-stat
  AC-ST-04/06 확정) **WHEN** 구독 **THEN** 이 시스템은 `neglectMeter` 등을
  직접 폴링하는 대체 경로가 없음 — potato-stat 미응답 시 위기/회복 이벤트
  레이어는 영구 `Idle`.
- **AC-INT-04 (날씨 Soft + CROSSFADE_DURATION 재사용)** **GIVEN**
  weather-system `CROSSFADE_DURATION=3.0s` **WHEN** 라이브 전환 **THEN** 정확히
  3.0s 재사용(자체 타이머 정의 시 회귀).
- **AC-INT-05 (생애주기 Soft — 미응답 시 전이 자체는 정상)** **GIVEN**
  `OnStageAdvanced` 미발행 가정 **WHEN** stage 실제 전환 **THEN** lifecycle
  핵심 전이는 정상, 이 시스템의 SFX만 무음.
- **AC-INT-06 (설정/접근성 볼륨 — 미설계, 보류)**: 인터페이스(필드명/갱신
  타이밍) 미확정이라 구체적 GIVEN/WHEN 불가 — 설정 시스템 확정 즉시 채운다
  (`costume-system.md` AC-INT-03과 동일 처리).

### VIII. 사운드 카테고리 라우팅 (AC-VA — 데이터 계약, 음색/정서 판단 아님)

- **AC-VA-01 (날씨→앰비언트 1:1)** **GIVEN**
  `GetCurrentWeather()∈{맑음,비/안개,한파/폭설}` **WHEN** 루프 선택 **THEN**
  정확히 대응 카테고리 1개, 폴백 없음.
- **AC-VA-02 (제스처 타입→프로필 1:1)** **GIVEN**
  `gestureType∈{창문,난로,물뿌리개}` **WHEN** 선택 **THEN** 교차 매핑 없음.
- **AC-VA-03 (이벤트 타입→SFX 1:1)** **GIVEN**
  `eventType∈{StageAdvanced,WitherEntered,Recovered}` **WHEN** 선택 **THEN**
  F3은 재생 여부만 결정, SFX 아이덴티티는 승자 이벤트 고유값 그대로.

### 이 GDD 범위 밖으로 명시적으로 남긴 것 (테스트 불가 / Feel 전용, ADVISORY)

- 사운드가 실제로 "이완"으로 읽히는지(Player Fantasy Design Test) — 청취 판단.
- 터치 케어 §5의 제스처별 "성격"이 실제 에셋에서 그대로 읽히는지 — 콘텐츠 심사.
- `T_TIMBRE_*`/`T_DUCK_*`/`D_*`/`AMBIENT_BASE_GAIN`/`GESTURE_BASE_GAIN` 값
  자체의 "자연스러움" — 공식의 정확한 소비는 AC-F로 자동화되나 최종 상수값은
  플레이테스트 몫(Tuning Knobs가 이미 "미검증"으로 표시).
- 물뿌리개 지터가 "반복감 방지"로 체감되는지.
- 덕킹이 "펌핑처럼 튀지 않는지".
- `AC-INT-06`(설정/접근성 볼륨 구독) — 그 시스템 미설계로 구체화 불가.

## Open Questions

| 질문 | 배경 | 담당 | 해소 시점 |
|---|---|---|---|
| 볼륨/음소거의 정확한 구독 인터페이스(AC-INT-06) | 필드명·갱신 타이밍 등이 설정/접근성 시스템(Alpha) 미설계로 확정 불가 | ui-programmer/systems-designer | 설정/접근성 시스템 `/design-system` 진행 시 |
| 미검증 수치 상수 일괄(`T_TIMBRE_ATTACK/RELEASE`, `SIMULTANEOUS_WINDOW_MS`, `AMBIENT_BASE_GAIN`/`GESTURE_BASE_GAIN` 정확한 dB) | Tuning Knobs에 안전범위만 제시, 프로토타입 근거 없는 신규 값들이라 실측 검증 필요 | audio-director/sound-designer | 실 오디오 에셋 적용 후 믹싱 패스, 또는 Vertical Slice 플레이테스트 |
| 실제 오디오 에셋 제작(샘플 녹음/합성) | 이 GDD는 에셋 목록·믹싱 로직만 확정 — 실제 사운드 소스는 별도 프로덕션 작업 | sound-designer | Pre-Production 에셋 제작 단계, `/asset-spec` 실행 후 |
| Hard/Soft 라벨링 컨벤션 불일치(qa-lead 경미 지적) | 이 GDD는 터치 케어·potato-stat과의 관계를 "정체성 절반" 논리로 Hard 표기했으나, Hard/Soft 판정 기준 문구("응답 없으면 대상 시스템이 아예 멈추는가")를 문자 그대로 적용하면 이 시스템의 어떤 Upstream도 게임플레이를 멈추지 않아 전부 Soft여야 함 — 프로젝트 전반에서 "기능 정지" vs "정체성 손상"을 같은 라벨로 섞어 쓰는 관행이 있음(이 GDD만의 문제 아님) | systems-designer | 프로젝트 전반 Hard/Soft 판정 기준 재정의 논의 시(`/architecture-review` 또는 컨벤션 정리 세션) |
