# 터치 케어 인터랙션 (Touch Care Interaction)

> **Status**: Designed (pending review)
> **Author**: user + game-designer + ux-designer + systems-designer + art-director + qa-lead
> **Last Updated**: 2026-07-18
> **Implements Pillar**: Pillar 1 (손끝의 위안)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode (Phase-gate 규칙상
> lean은 스킵 대상. 최고위험 시스템이라 프로덕션 전 수동 검토를 특히 강하게 권장.)

## Overview

터치 케어 인터랙션은 플레이어의 아날로그 제스처(창문 드래그·난로 롱프레스·물뿌리개
조작)를 인식·완성 판정하고, 완성된 제스처마다 감자 상태 관리의
`AdjustMoisture/Humidity/Temperature(delta)`를 `TOUCH_DELTA(30)`만큼 호출하는 입력
처리 계층이다. 게임 내에서 플레이어가 손가락으로 직접 화면을 만지는 **유일한
지점**이며, `game-concept.md`의 "10초 테스트"("날씨에 맞춰 손으로 감자를 돌보고...")
와 Core Loop의 Moment-to-Moment(30초) 단계가 실질적으로 이 시스템 하나로 구현된다.

이 시스템은 데이터 계층(제스처 인식·진행률 추적·완성 판정·히스테리시스)이면서 동시에
그 자체로 플레이어가 직접 체감하는 표현 계층이다 — 프로토타입이 실증했듯, 이 시스템이
만드는 것은 "스탯이 조정됐다"는 결과가 아니라 "차오름(진행) → 완성(확산)"이라는 손맛
그 자체이며, 그 손맛의 질이 곧 MDA 1순위 미학(Submission)과 2순위(Sensation)가 실제로
전달되는지를 결정한다.

이 시스템이 없다면 감자 케어는 버튼 탭 몇 번으로 환원되고, Pillar 1(손끝의 위안)의
Design Test("촉감/소리 보상을 못 주면 단순 UI 버튼이 아니라 아날로그 조작으로
재설계하거나 뺀다")가 통째로 실패한다. 시스템 인덱스가 이 시스템을 게임 전체의
최고위험 시스템으로 지목한 이유도 여기 있다 — 손맛이 실제로 위안을 주는지는 이론이
아니라 오직 구현된 결과로만 검증되며, 프로토타입은 그 가설을 부분적으로만(난로만)
확증했다.

## Player Fantasy

> *(`creative-director` 미상담 — lean 모드, Section D/H 아님. 최고위험 시스템이므로
> 프로덕션 전 수동 검토를 강하게 권장.)*

플레이어가 이 시스템에서 느껴야 할 감정은 **목표를 달성했다는 성취감이 아니라, 만지는
행위 그 자체가 주는 감각적 이완**이다. Pillar 1이 이미 못박은 대로 "모든 상호작용은
눈보다 손과 귀로 먼저 느껴져야" 하며, 이 시스템은 정확히 그 명제를 매 순간 실행에
옮기는 유일한 장소다.

프로토타입의 증언이 이 판타지의 성공 기준을 구체화한다 — 목표는 "기능이 작동한다"가
아니라 "테스터가 이유 없이 반복하고 싶어진다"였고, 난로만 그 기준에 근접했다("난로만
그나마 난로같았다"). 이 시스템이 성공하면 플레이어는 감자를 돌봐야 해서 만지는 게
아니라, 만지는 감각 자체가 좋아서 조금 더 오래 누르고 싶어진다 — "차오름(진행) →
완성(확산)"이라는 손맛 문법이 이 감정을 만드는 구체적 장치다.

*Design test*: 어떤 제스처가 기능적으로는 완성되지만("델타 30 적용됨") 그 과정에서
플레이어가 아무 감각적 신호도 못 느낀다면 실패 — 창문/물뿌리개가 프로토타입에서 정확히
이 방식으로 실패했다. 모든 제스처는 완성 여부와 무관하게 "만지는 동안" 자체가 좋아야
한다.

## Detailed Design

> *(`game-designer` + `ux-designer` 상담 완료 — 최고위험 시스템이라 lean 모드 기본값을
> 넘어서는 상담 진행(사용자 확정). 충돌 2건(물뿌리개 입력 방식, 동시 제스처 처리)은
> 사용자 결정으로 해소. art-bible §UI/HUD "5. 터치 히트박스와 접근성" 기존 잠금
> 사항과 정합 확인.)*

### Core Rules

**공통 구조 원칙**: 세 제스처는 서로 다른 입력 원형과 서로 다른 진행 은유를 갖지만,
아래 골격 규칙(1~5, 7~13)은 예외 없이 셋 모두에 적용된다. 오직 "무엇을 표상하는가"
(규칙 6)만 제스처 고유다.

1. **입력 원형 — 세 가지**: 모든 터치 케어 제스처는 세 입력 원형 중 하나로 분류된다.
   각자 원시 입력 형태(raw touch shape)가 뚜렷이 구분되어야 한다는 것이 프로토타입의
   핵심 교훈이다.
   - **드래그형(Drag)**: 연속적 위치 변위. 창문(선형 슬라이드).
   - **홀드형(Hold)**: 연속적 접촉 지속시간, 위치 고정. 난로.
   - **다브형(Dab-Area)**: 영역 내 반복된 짧은 터치-이탈. 물뿌리개(신규 원형 —
     창문/난로 어느 쪽과도 원시 입력 형태가 겹치지 않음).

2. **진행률(progress) 공통 스키마**: 제스처가 활성 상태인 동안 매 프레임/매 다브마다
   `progress ∈ [0,1]`을 갱신한다(0에서 클램프, 음수 불가).
   - 드래그형: `progress = clamp(변위 / 완성 변위, 0, 1)`, 단 art-bible §5 확정대로
     완료 판정은 순수 거리뿐 아니라 **부분 이동(~60~70%) OR 플릭 속도** 중 먼저
     만족되는 조건으로 한다(iOS 스와이프 삭제와 동일 원리 — 가동 범위 좁은 플레이어에게도
     유리).
   - 홀드형: `progress = clamp(접촉 지속시간 / HOLD_DURATION, 0, 1)`.
   - 다브형: `progress = clamp(짙어진 흙 커버리지 비율, 0, 1)` — 다브 1회당 국소
     영역이 짙어지고, 커버리지가 전체 흙 히트존 면적에서 차지하는 비율로 진행률을
     정의.
   정확한 상수(완성 변위·`HOLD_DURATION`·커버리지 목표치)는 Formulas/Tuning Knobs
   소관.

3. **완성 임계값과 완성 이벤트**: `progress`가 그 프레임/그 다브에 최초로
   `θ_gesture` 이상이 되는 순간, 완성 이벤트가 **정확히 1회** 발생한다. 이후 접촉이
   유지돼도 추가 완성 이벤트는 없다 — 완성은 접촉 종료까지 1회로 소진.

4. **유예/감쇠 원칙(Grace & Decay) — 완만한 되돌림, 즉시 리셋 아님**: 완성 전 접촉이
   끊기면 `progress`는 즉시 0으로 스냅되지 않는다. 대신 `DECAY_GRACE_WINDOW` 동안
   서서히 원래 상태로 되돌아가며(정확한 감쇠 곡선·속도는 Tuning Knobs 소관 —
   플레이테스트로 확정), 그 사이 같은 제스처 영역을 다시 접촉하면 감쇠 중이던
   지점에서 재개된다. 세 원형 모두 동일 원칙을 공유한다(프로토타입이 검증한 창문의
   히스테리시스 패턴을 일반화한 것 — 새 가설 아님). 손이 완전히 떠난 채
   `DECAY_GRACE_WINDOW`를 초과하면 `progress`는 자연스럽게 0에 도달한다.
   *근거(Pillar 3/UI 철학 정합)*: 스탯 델타는 이미 완성 시점에만 원자적으로
   적용되므로(potato-stat-system.md §3), 이 규칙은 밸런스가 아니라 순수하게 "실수해도
   처벌받지 않는다"는 체감을 만드는 시각 레이어 결정이다.

5. **하드 중단(Hard Interrupt) 규칙**: 아래 조건에서는 유예 없이 `progress`가 즉시
   0으로 리셋되고, 대응 `Adjust*(delta)`는 **절대 호출되지 않는다**.
   - 화면/씬 전환
   - 앱 백그라운드 전환 또는 강제 종료
   포그라운드 상실 이벤트에는 유예 타이머를 신뢰성 있게 걸 수 없고, 세션 경계 자체가
   자연스러운 재시작 지점이다(Pillar 4).

6. **제스처별 고유 사양**:

   | | 창문 드래그 | 난로 롱프레스 | 물뿌리개(다브) |
   |---|---|---|---|
   | 입력 원형 | 드래그형 | 홀드형 | 다브형 |
   | 스탯 방향 | 습도 -30 | 온도 +30 | 수분 +30 |
   | 진행 은유(차오름) | progress에 비례해 유리창 김서림/안개가 점진적으로 걷힘 — 터치 위치와 직결(art-bible §2-2 기확정) | 방사형 글로우가 차오름(프로토타입 검증 사례) | 다브마다 그 지점 흙이 즉시 짙어지고 물방울 파티클 튐 — 커버리지 자체가 진행 시각(art-bible §2-4 기확정) |
   | 완성 은유(확산) | 신선한 공기가 훅 퍼지는 1회성 확산 | 글로우가 바깥으로 확장하며 잦아듦 | 흙 전체가 짙어진 상태로 안정, 옅은 김(스팀) 한 번 피어오름 |

7. **완성 이벤트의 유일한 사이드이펙트**: 완성 시 이 시스템이 만드는 게임 상태
   변화는 정확히 하나 — 대응 `Adjust*(TOUCH_DELTA)`(상수는 potato-stat-system.md
   소유, 소비만) 단일 호출. 그 외엔 `OnGestureCompleted(gestureType)` 이벤트만
   발행하며, 오디오·VFX·(향후) 카운터 로직은 전부 구독자 몫 — 이 시스템은 부가
   로직을 소유하지 않는다. **호출 순서**: `Adjust*(TOUCH_DELTA)` 호출은 항상
   `OnGestureCompleted` 발행보다 먼저 완료된다 — 이벤트 핸들러 내부에서 potato-stat
   값을 조회하는 구독자(표정/애니메이션 등)는 항상 조정 후 최신 값을 관측한다.

8. **동시 제스처 — 부드럽게 방지(사용자 확정)**: 이 시스템은 한 번에 하나의 "활성
   케어 제스처"만 추적한다. 이미 제스처가 진행 중일 때 다른 존에 새 터치가 들어오면
   **조용히 무시**된다(에러음·경고·진동 없음) — 진행 중이던 제스처는 방해 없이
   계속된다. art-bible UI 섹션의 "침묵도 안전 신호" 철학을 인터랙션 레이어로
   확장한 것. 감각 신호(시각+ASMR) 경합을 막고, 게임 설계상 플레이어가 서둘러 두
   가지를 동시에 해야 할 이유가 없다(Pillar 4).

9. **스탯 포화 상태와의 무관성(Ceiling-Agnostic)**: 이 시스템은 제스처의 어느
   시점에도 대상 스탯의 현재값을 조회하지 않는다(potato-stat-system.md를 향한
   의존은 항상 쓰기 단방향). 스탯이 이미 극값이라 수치 효과가 없더라도, 진행·완성
   은유는 평소와 완전히 동일하게 재생한다 — "만졌는데 아무 일도 안 일어난 것 같다"는
   감각은 구조적으로 발생할 수 없다.

10. **스팸/쿨다운 미도입**: 제스처 재시작에 쿨다운을 두지 않는다 —
    potato-stat-system.md가 이미 "터치 스팸은 클램프로 이득·손해 없음"을 확정했고,
    쿨다운 도입은 Pillar 1(즉각 반응) 위반이다.

11. **히트존 기하 — 제스처 원형별 상이한 적용 단위**: art-bible §5의 `≥88×88dp`
    규칙은 "면제"가 아니라 **적용되는 기하 단위가 원형에 따라 다르다.**
    - 홀드형(난로): 정지 히트존 자체가 `≥88×88dp`. 존 내부 드리프트(진짜 안
      움직이는 손은 없음)는 전부 허용.
    - 드래그형(창문): 시작 지점(grab zone)이 `≥88×88dp`. 궤적 자체는 수직 방향
      톨러런스를 넉넉히 둬 완벽한 직선을 요구하지 않음.
    - 다브형(물뿌리개): 흙 히트존 전체가 `88×88dp`를 넉넉히 상회(여러 다브 공간
      필요). 다브 1회 1회에 개별 88dp 서브존을 요구하지 않음.
    공통: 동시 노출 오브젝트 간 `≥8dp` 여백(art-bible §5 기확정).

12. **접근성 파라미터화 훅**: 세 제스처의 완성 조건(창문 변위 임계값, 난로
    `HOLD_DURATION`, 물뿌리개 커버리지 목표치)은 하드코딩이 아니라 **데이터 기반
    파라미터**로 노출한다(어차피 coding-standards.md가 요구하는 원칙이라 추가 비용
    없음). 이는 향후 "간단 터치 모드"(art-bible §5, 열린 과제·미결정)가 같은 실행
    경로를 재사용해 임계값을 낮추거나 대체할 수 있는 구조적 여지를 남긴다 — 이 GDD가
    그 모드를 설계하는 것은 아니다.

13. **1회성 제스처 시연 힌트**: 각 제스처는 최초 1회에 한해 손/화살표 글리프가
    동작을 시연하는 힌트를 보여주고 다시 반복하지 않는다(art-bible §5 기확정,
    위기/회복 전환과 동일한 1회성 논-루프 애니메이션 패턴 재사용 — 새 모션 문법
    없음, Pillar 4 유지).

### States and Transitions

세 제스처(창문/난로/물뿌리개)는 각각 독립된 상태 머신 인스턴스를 가진다(동시 진행은
Core Rule 8에 의해 최대 1개만 `Active`).

| State | 진입 조건 | 전이 |
|---|---|---|
| `Idle` | 초기/대기, `progress=0` | 새 접촉 시작(다른 제스처가 `Active`가 아닐 때, Rule 8) → `Active` |
| `Active` | 접촉 중, `progress` 갱신(Rule 2) | `progress≥θ` 최초 도달 → 완성 이벤트 발행(Rule 3), `Active` 유지(완성 플래그 set, 접촉 지속 중엔 추가 이벤트 없음) / 접촉 해제(완성 전) → `Decaying` / 접촉 해제(완성 후) → `Idle` 직접(감쇠 불필요, 이미 성공) / 하드 중단(Rule 5) → `Idle` 즉시 |
| `Decaying` | 접촉 해제, 완성 전, `DECAY_GRACE_WINDOW` 카운트다운 | 같은 존 재접촉 → `Active`(감쇠 지점에서 재개) / 윈도우 만료 또는 `progress=0` 도달 → `Idle` / 하드 중단 → `Idle` 즉시 |

`Idle`은 지속 상태를 저장하지 않는다(`progress=0`가 곧 정의). `Decaying`만이 시간
기반 전이를 가지며, 이것이 곧 Rule 4의 관대함이 상태 머신에 반영되는 지점이다.

### Interactions with Other Systems

| System | 방향 | 데이터 |
|---|---|---|
| 감자 상태 관리 | 쓰기(호출자, 단방향) | 완성 시 `AdjustMoisture/Humidity/Temperature(TOUCH_DELTA)` |
| 날씨 시스템 | 조회 | 현재 날씨(어느 스탯이 위기인지)로 UI 강조·힌트 우선순위 참고 — 코어 로직 자체는 날씨 무관하게 항상 3제스처 모두 상시 가능 |
| 표정/애니메이션 (미설계) | 신호 발행(`OnGestureCompleted`) | 다운스트림 소비 |
| ASMR 적응형 오디오 (미설계) | 신호 발행(`OnGestureCompleted`, 진행 중 `progress` 구독) | 다운스트림 소비 — 콘셉트 문서상 이 시스템의 오디오가 "정체성의 절반" |
| 온보딩(1회성 힌트, 미설계) | 신호 소비(피호출자) | Core Rule 13의 1회성 시연 힌트 트리거를 온보딩 시스템이 조율할 가능성(Hard 여부는 온보딩 GDD에서 확정) |

## Formulas

> *(`systems-designer` 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션.)*

**구조적 통일**: `θ_gesture = 1.0`(모든 제스처 공통, 고정) — 각 제스처는 자신의
원시 입력(변위/지속시간/커버리지)을 자신만의 "완성" 상수로 정규화해 `progress`가
항상 동일한 의미(0=미접촉, 1=완성)를 갖게 한다. Core Rule 3을 제스처별 예외 없이
문자 그대로 성립시키기 위한 선택.

### Formula 1 — 창문 드래그 (`drag_progress`)

The `drag_progress` formula is defined as:

`drag_progress = max(clamp(displacement / completion_displacement, 0, 1), flick_override)`

`completion_displacement = θ_drag_distance · track_length`,
`flick_override = 1 if (velocity ≥ FLICK_VELOCITY_THRESHOLD AND raw_progress ≥ FLICK_MIN_PROGRESS_FLOOR) else 0`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 변위 | displacement | float | 0–track_length | 잡은 지점부터 현재까지 이동 거리 |
| 트랙 길이 | track_length | float(UI 소유) | 에셋 종속 | 창문 슬라이드 트랙 전체 길이(이 GDD 소관 아님) |
| 거리 임계 비율 | θ_drag_distance | float(const) | 0–1 | 거리만으로 "완성"으로 치는 트랙 비율 |
| 순간 속도 | velocity | float | 0–∞ | progress-단위/초, `VELOCITY_SAMPLE_WINDOW`로 샘플링 |
| 플릭 속도 임계 | FLICK_VELOCITY_THRESHOLD | float(const) | progress-단위/초 | 즉시 완성으로 치는 속도 |
| 커밋 최소치 | FLICK_MIN_PROGRESS_FLOOR | float(const) | 0–1 | 플릭이 트리거되기 위한 최소 원시 진행률(운 좋은 헛스침 방지) |

**Output Range:** [0,1], 클램프. 어느 한쪽 조건이 만족되는 순간 정확히 1로 스냅.
**Example:** `track_length=400dp`, `θ_drag_distance=0.65` → `completion_displacement=260dp`.
`displacement=200dp`(raw=0.769), 이 순간 `velocity=3.0≥FLICK_VELOCITY_THRESHOLD(2.5)`
이고 `raw≥FLICK_MIN_PROGRESS_FLOOR(0.15)` → `flick_override=1` → `drag_progress=1.0`
(260dp 도달 전에도 완성).

*`θ_drag_distance=0.65`는 프로토타입 값을 그대로 유지(미검증 이월, art-bible §5의
"약 60~70%"와 정합). 기존 "35% 히스테리시스 밴드"(위치 기반 freeze 모델)는 새
시간 기반 감쇠 모델(Formula 4)로 대체 — 서로 다른 축이라 직접 환산하지 않음.*

### Formula 2 — 난로 롱프레스 (`hold_progress`)

The `hold_progress` formula is defined as:

`hold_progress = clamp(hold_duration / HOLD_DURATION_STOVE, 0, 1)`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 경과 홀드 시간 | hold_duration | float | 0–∞ | 이번 접촉 구간의 연속 접촉 시간 |
| 홀드 소요시간 상수 | HOLD_DURATION_STOVE | float(const) | 1.2(초) | 완성에 필요한 시간 |

**Output Range:** [0,1], 클램프, 시간에 선형. 가속·곡선 없는 순수 스칼라.
**Example:** `hold_duration=0.9s` → `0.75`. `hold_duration=1.2s` → `1.0`, 완성.

*프로토타입이 유일하게 "그럴듯함"을 검증한 값 — 변경 근거 없이 그대로 유지.
(글로우의 시각적 반경 자체가 이징 곡선을 쓸 수는 있으나, 그건 Visual/Audio
Requirements 소관이며 이 공식의 입력-진행률 매핑은 변경되지 않는다.)*

### Formula 3 — 물뿌리개 다브 (`dab_progress`)

The `dab_progress` formula is defined as:

`dab_progress = clamp(raw_coverage / DAB_TARGET_COVERAGE, 0, 1)`,
`raw_coverage = darkened_area / zone_area`(다브 풋프린트들의 **합집합**, 단순 합 아님)

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 짙어진 면적 | darkened_area | float | 0–zone_area | 지금까지 착지한 다브 풋프린트들의 합집합 면적 |
| 존 면적 | zone_area | float(UI 소유) | 에셋 종속 | 흙 히트존 전체 면적 |
| 목표 커버리지 | DAB_TARGET_COVERAGE | float(const) | 0.75 | "다 물줬다"로 치는 물리적 커버리지 비율 |
| 다브 1회 풋프린트 비율 | DAB_FOOTPRINT_RATIO | float(const) | 0.10 | 다브 1회의 존 대비 면적(이상화, 비중첩 기준) |
| 다브 간 타임아웃 | DAB_INTERDAB_TIMEOUT | float(const) | 1.0(초) | 이 시간 안에 새 다브가 없으면 "손을 뗐다"로 간주, 감쇠 시작 |

**Output Range:** [0,1], 클램프. `raw_coverage`가 1을 넘을 수 없고 `DAB_TARGET_COVERAGE<1`
로 나누므로 전체 픽셀을 다 덮기 전에 1.0에 도달.
**구현 필수 노트**: `darkened_area`는 원 면적의 단순 합이 아니라 **합집합**(커버리지
그리드/마스크)으로 추적해야 한다 — 아니면 겹치는 다브가 이중 계산돼 의도보다 훨씬
빨리 완성돼 "여러 번 다브"라는 손맛이 조용히 깨진다.
**Example:** `zone_area=1.0` 정규화, 다브 1회=0.10(이상화). 비중첩 6회 →
`raw_coverage=0.60` → `dab_progress=0.80`. 8회 → `raw_coverage=0.80` →
`dab_progress=1.067→clamp=1.0`, 완성. 실제로는 겹침이 있어 **약 9~11회** 다브 필요.

**목표치 75%(100% 아님) 근거**: 100%는 "마지막 한 점을 찾아 헤매는" 지루함
위험(Submission·Pillar 4 위반). 75%는 흙에 눈에 띄는 변주가 남은 채로 "충분히
물줬다"로 읽힘.
**풋프린트 비율 0.10 근거**: 다른 두 제스처와 시간 투자 규모가 비슷하도록(§5) —
너무 크면(2~3회로 끝) "다브"라는 반복 접촉 정체성이 약해지고, 너무 작으면(15~20회)
지루함.

**다브형의 "접촉 끊김" 재정의**: 다브는 본질적으로 짧은 접촉-이탈의 연속이라, 매
다브 사이마다 감쇠가 시작되면 안 됨. `is_decaying = (마지막 다브 이후 경과 >
DAB_INTERDAB_TIMEOUT)` — 타임아웃 전에는 `dab_progress`가 감쇠 없이 그대로 유지되고,
타임아웃을 넘기면 그 시점 값에서 Formula 4의 공통 감쇠 곡선이 시작된다.
**Example:** `t=0.0/0.4/0.9s`에 다브, 이후 중단. `t=1.7s`(직전 다브로부터 0.8s,
`<1.0s`) → 유지. `t=1.95s`(1.05s 경과, `>1.0s`) → 그 지점부터 감쇠 시작.

### Formula 4 — 감쇠(`DECAY_GRACE_WINDOW`, 3종 공통)

The `decayed_progress` formula is defined as:

`decayed_progress(t) = progress_at_release · (1 - (t/DECAY_GRACE_WINDOW)^DECAY_CURVE_EXPONENT)`,
`0 ≤ t < DECAY_GRACE_WINDOW`; `t ≥ DECAY_GRACE_WINDOW`이면 `0`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| 해제 시점 진행률 | progress_at_release | float | 0–1 | 접촉이 끊긴(또는 다브형은 타임아웃 초과) 순간의 progress 스냅샷 |
| 해제 후 경과시간 | t | float | 0–DECAY_GRACE_WINDOW | — |
| 유예 윈도우 | DECAY_GRACE_WINDOW | float(const) | 2.0(초) | 3제스처 공통 단일 노브 |
| 감쇠 곡선 지수 | DECAY_CURVE_EXPONENT | float(const) | 2.0 | 1=선형, >1일수록 초반 완만·후반 급격("ease-in") |

**Output Range:** [0, progress_at_release], 단조 감소, `t=DECAY_GRACE_WINDOW`에서
정확히 0으로 하드 클램프(`Decaying→Idle` 전이가 점근이 아니라 깔끔한 종점을 갖도록).
**Example:** `progress_at_release=0.8`. `t=1.0s`(윈도우 절반) → `0.8×(1-0.25)=0.6`
(전반부에 25%만 손실). `t=1.8s` → `0.8×(1-0.81)=0.152`(후반부에 급가속). `t=2.0s` → 0.

**2차 ease-in 선택 근거**: `t=0`에서 기울기 0 — 시각적으로 "그 순간엔 얼어붙은
것처럼" 읽히다가 서서히 풀리며 윈도우 후반에 가속. 프로토타입이 검증한 창문의
"얼어붙었다 재시도" 히스테리시스를 시간 기반 모델로 일반화한 것 — 가장 흔한 경우
(엄지가 살짝 미끄러짐 등 짧은 실수)에서 초반 25~30%는 거의 잃지 않아 관대함이
가장 필요한 순간에 가장 안전하다.

### Formula 5 — 세션 예산 검증

| 제스처 | 집중 시 | 서두를 시 |
|---|---|---|
| 창문 드래그 | ~1.0~2.0초 | ~0.4~0.6초(플릭) |
| 난로 홀드 | 1.2초(고정) | 1.2초(고정) |
| 물뿌리개 다브 | ~2.4~5.5초(9~11회) | ~1.2~2.75초 |
| **3종 전부(전환 포함)** | **~5.6~10.7초** | **~3.4~5.15초** |

Pillar 4(30초~2분) 목표에 여유 있게 부합. 쿨다운 없음(Core Rule 10)이 이미 "더
머물고 싶으면 반복하면 된다"는 자연스러운 여유를 만들어주므로, 지루함 방지를 위해
상수를 억지로 늘리지 않는다 — `DAB_FOOTPRINT_RATIO<0.07`(다브 과다) 또는
`DAB_TARGET_COVERAGE>0.85`(마지막 한 점 헌팅)로 밀면 오히려 지루함 위험.

## Tuning Knob Candidates(레지스트리 등록은 5b에서)

| 상수 | 값 | 안전범위 | 비고 |
|---|---|---|---|
| θ_drag_distance | 0.65 | 0.55–0.75 | 프로토타입 이월, 미검증 |
| FLICK_VELOCITY_THRESHOLD | 2.5 (progress-단위/초) | 1.5–4.0 | 신규 |
| FLICK_MIN_PROGRESS_FLOOR | 0.15 | 0.05–0.25 | 신규 |
| VELOCITY_SAMPLE_WINDOW | 50ms | 33–100ms | 신규(프레임수 아닌 시간 기준 — 30fps 스로틀 고려) |
| HOLD_DURATION_STOVE | 1.2s | 검증됨 — 재프로토타입 없이 변경 금지 | 유일한 검증치 |
| DAB_TARGET_COVERAGE | 0.75 | 0.65–0.85 | 신규 |
| DAB_FOOTPRINT_RATIO | 0.10 | 0.07–0.15 | 신규 |
| DAB_INTERDAB_TIMEOUT | 1.0s | 0.6–1.5s | 신규 |
| DECAY_GRACE_WINDOW | 2.0s | 1.5–3.0s | 신규, 3종 공통 단일 노브 |
| DECAY_CURVE_EXPONENT | 2.0 | 1.5–3.0 | 신규, 1.0=선형 폴백 |
| θ_gesture | 1.0(고정, 전 제스처 공통) | 구조적 상수 — 튜닝 대상 아님 | Core Rule 3 참조용 |

**교차 시스템 후보**: `DECAY_GRACE_WINDOW`·`DECAY_CURVE_EXPONENT`·`HOLD_DURATION_STOVE`는
표정/애니메이션·ASMR 적응형 오디오(둘 다 미설계, 이미 `progress` 구독자로 등록됨)가
직접 참조할 가능성 높음.

## Edge Cases

> *(Specialist agent 미상담 — lean 모드.)*

- **If 드래그 중 손가락이 되돌아온다면(변위가 줄어듦)**: `Active` 상태에서
  `progress`는 항상 **현재 변위**를 반영한다(고점 유지 아님) — 뒤로 되돌리면
  `progress`도 함께 줄어든다. 단, `Decaying` 상태의 관대함(Formula 4)은 오직
  **접촉이 끊긴 뒤**에만 적용되며, 접촉을 유지한 채 되돌리는 것과는 별개다.

- **If 같은 제스처 존에 두 개 이상의 터치가 동시에 닿는다면**: 최초 터치 ID만
  `progress`를 구동하고 추가 터치는 무시된다(이중 입력으로 완성을 가속하는 것을
  방지) — Core Rule 8("동시 제스처 부드러운 방지")과는 별개 규칙으로, 이건 동일
  존 내부 문제다.

- **If 한 제스처가 `Decaying` 상태인 동안 플레이어가 다른 제스처를 시작한다면**:
  허용된다 — Core Rule 8의 "활성 제스처 1개 제한"은 `Active` 상태에만 적용되고
  `Decaying`은 실질적으로 유휴에 가까우므로 새 제스처의 시작을 막지 않는다. 기존
  `Decaying` 인스턴스는 자신의 감쇠를 그대로 계속 진행한다(중단되지 않음).

- **If 앱이 `Decaying` 상태 도중 백그라운드로 전환되거나 강제 종료된다면**: Core
  Rule 5(하드 중단)가 적용되어 `progress`는 즉시 0으로 리셋된다 — 감쇠 타이머는
  세션을 넘어 유지되지 않는다(재개 시 `DECAY_GRACE_WINDOW`가 이미 흘렀다고
  계산하지 않음, 단순히 처음부터).

- **If 감자가 시듦(Wither) 상태라면**: 이 시스템의 세 제스처는 모두 비활성화된다
  (터치해도 `Idle`에서 `Active`로 전이하지 않음) — potato-stat-system.md의
  `IsWithering()`을 조회해 게이팅한다. 시듦 회복은 광고 시청 또는 무료 체류
  경로(`wither-crisis-recovery-system.md` 확정, 2026-07-18 — 부활 광고 시청,
  또는 위기 중 누적 30분 포그라운드 체류)로만 가능하며 이 시스템의 손맛
  제스처로는 대체되지 않는다(Pillar 3 — 벌주는 대신 명확한 다른 경로를 제시).
  정확한 게이팅 UX(터치 시 아무 반응 없음 vs. 짧은 안내)는 `wither-crisis-recovery-system.md`
  UI Requirements 소관.

- **If 물뿌리개 다브가 같은 지점에 반복해서 떨어진다면(겹침)**: Formula 3의
  합집합 추적 덕에 겹치는 다브는 `raw_coverage`에 추가로 기여하지 않는다 — 같은
  자리만 계속 다브해서는 완성에 도달할 수 없고, 존 전체에 걸쳐 다브해야 한다
  (설계 의도).

## Dependencies

**Upstream (이 시스템이 의존):**

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 감자 상태 관리 | Hard | `AdjustMoisture/Humidity/Temperature(TOUCH_DELTA)` 호출(쓰기, 단방향) + `IsWithering()` 조회(시듦 시 게이팅) |
| 날씨 시스템 | Soft | 현재 활성 날씨(enum)로 터치 피드백 톤 보정 — 핵심 완성 계약에는 관여 안 함(weather-system.md 확정) |

**Downstream (이 시스템에 의존, 전부 미설계):**

| 시스템 | 강도 | 인터페이스 |
|---|---|---|
| 표정/애니메이션 | Hard | `OnGestureCompleted(gestureType)` 구독 |
| ASMR 적응형 오디오 | Hard | `OnGestureCompleted` + 진행 중 `progress` 구독. 콘셉트 문서상 오디오가 "정체성의 절반" |
| 온보딩(1회성 힌트) | Soft | Core Rule 13의 1회성 시연 힌트 트리거 조율 가능성 |
| 시듦/위기 & 회복 (미설계) | — (역방향 없음) | 이 시스템은 potato-stat의 `IsWithering()`을 직접 조회하므로 미설계 시스템에 직접 의존하지 않음(잠정 설계 — 그 시스템이 다른 게이팅 계약을 요구하면 재확인 필요) |

**교차 문서 수정(2026-07-18)**: `potato-stat-system.md` Dependencies 표의 "터치 케어
인터랙션" 행에 `IsWithering()` 조회 인터페이스 추가(기존엔 조정 메서드 호출만
명시).

## Tuning Knobs

| 노브 | 값 | 안전범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `θ_drag_distance` | 0.65 | 0.55~0.75 | 살짝만 밀어도 완성 — "밀었다"는 느낌 전에 끝남 | 끝까지 밀어야 완성 — 손맛보다 노동처럼 느껴짐(art-bible §5 "약 60~70%" 상한 근접) |
| `FLICK_VELOCITY_THRESHOLD` | 2.5 progress-단위/초 | 1.5~4.0 | 평범한 드래그도 우발적으로 플릭 판정 — 거리 기반 손맛 자체가 무의미해짐 | 의도적인 빠른 스와이프도 인정 안 됨 — 플릭 단축 경로가 사실상 없는 것과 같아짐 |
| `FLICK_MIN_PROGRESS_FLOOR` | 0.15 | 0.05~0.25 | 거의 안 밀고도 빠르게만 스치면 완성 — 완성이 "닿기만 해도 됨"처럼 느껴짐 | 플릭이 사실상 거리 임계값과 다를 바 없어져 단축 경로의 의미 상실 |
| `HOLD_DURATION_STOVE` | 1.2s | 검증됨 — 재프로토타입 없이 변경 금지 | (미검증 변경 시) 너무 짧으면 "차오름" 느낄 새 없이 끝남 | 너무 길면 Sensation이 지루함으로 반전 |
| `DAB_TARGET_COVERAGE` | 0.75 | 0.65~0.85 | 다브 몇 번만에 끝나 "뿌리기"라는 반복 행위감 상실 | 마지막 한 점을 찾아 헤매는 지루함(Pillar 3/4 위반 위험) |
| `DAB_FOOTPRINT_RATIO` | 0.10 | 0.07~0.15 | 다브가 15~20회로 늘어나 지루함 | 2~3회로 끝나 "다브형"이라는 입력 정체성이 흐려짐(홀드형과 체감 구분 안 감) |
| `DAB_INTERDAB_TIMEOUT` | 1.0s | 0.6~1.5s | 다브 사이 자연스러운 리듬(숨 고르기)에도 감쇠 시작 — 부당하게 처벌받는 느낌 | 손을 뗀 지 한참 지나도 진행이 유지돼 "그만뒀다"는 신호가 늦게 옴 |
| `DECAY_GRACE_WINDOW` | 2.0s | 1.5~3.0s | 살짝 미끄러진 손가락도 처음부터 다시 — Pillar 3 위반 | 감쇠가 너무 오래 남아 "그만뒀는데도 안 사라짐"이 어색하게 느껴짐 |
| `DECAY_CURVE_EXPONENT` | 2.0(2차 ease-in) | 1.5~3.0(1.0=선형 폴백) | 선형에 가까워질수록 해제 즉시 눈에 띄게 줄어 "안전하다"는 신호가 약해짐 | 과도하게 크면 후반 낙차가 너무 급격해 막판에 갑자기 사라지는 것처럼 보임 |

**노브 간 상호작용**: `DAB_FOOTPRINT_RATIO`와 `DAB_TARGET_COVERAGE`는 함께 실제
다브 횟수(9~11회 추정, Formula 3 §5)를 결정한다 — 하나만 바꾸면 세션 예산
검증(Formula 5)을 재실행해야 함. `DECAY_GRACE_WINDOW`·`DECAY_CURVE_EXPONENT`는
3제스처 공통 단일 노브라, 바꾸면 세 제스처 전부의 체감이 동시에 바뀐다(개별 조정
불가 설계).

## Visual/Audio Requirements

> *(`art-director` 상담 완료 — Gameplay 카테고리라 스킬 규칙상 엄격 필수는 아니지만,
> 이 GDD의 핵심이 시각/청각 손맛이라 필수로 취급(사용자 확정). 신규 무드·팔레트·형태
> 방향은 정의하지 않는다 — art-bible Section 1 원칙 3, Shape Language Tier 2, Color
> System, UI/HUD §4~5의 기존 어휘를 그대로 쓴다.)*

### 0. 매핑 원칙

세 제스처의 진행과 감쇠 시각은 예외 없이 **`progress` 값 하나를 입력받는 순수 매핑
함수**로 설계한다 — `Active`에서 값이 오르든 `Decaying`에서 Formula 4에 따라 값이
내리든, 같은 함수에 그 순간의 `progress`를 넣기만 하면 된다. Formula 4의 2차
ease-in 곡선이 이미 "초반 완만·후반 급격"을 값 자체에서 보장하므로, 매핑 함수만
올바르면 감쇠는 자동으로 "얼어붙었다 서서히 풀리는" 느낌으로 재생된다(물뿌리개
예외는 §3 참고).

### 1. 제스처별 진행(차오름) 시각 사양

**창문 드래그**: 진행은 전역 알파 페이드가 아니라 **위치 연동 와이프**다 — 잡은
지점부터 현재 위치까지 닦인 구간은 즉시 맑음으로, 아직 지나지 않은 구간은
김서림/뿌연 막을 유지(경계선에만 종이결 번짐 가장자리). "손끝이 지나간 자리만
닦인다"는 art-bible §2-2를 문자 그대로 구현. **플릭 완성 시**: 물리적 와이프
거리가 낮아도(`FLICK_MIN_PROGRESS_FLOOR=0.15`) 완성될 수 있는데, 이때 완성
은유(확산)의 정의역을 "그 순간 남아있는 안개 전체"로 규정 — 플릭이 걸리면 와이프+
확산이 합쳐져 창 전체가 한 번에 맑아진다.

**상시 결로막 + 날씨 오버레이 중첩(사용자 확정)**: 창문 유리는 날씨와 무관하게
항상 얇은 결로/뿌연 막(baseline haze, 기상학적 "안개" 아닌 장식적 유리 질감)을
두른다 — Core Rule 9(날씨 무관 은유 재생)를 항상 성립시키는 층. Rain/Fog 날씨
활성 시 `weather-system.md`가 소유한 더 짙은 안개 레이어가 그 위에 겹쳐 얹히고,
같은 와이프 동작이 두 레이어를 동시에 걷어낸다 — Rain/Fog 중의 창문 완성은 다른
날씨보다 살짝 더 극적으로 "훅 트인다"(부가 효과, 별도 규칙 아님). 색: baseline
haze는 Slate Blue-Gray/Frost Blue-White(날씨 전용 예약색)를 쓰지 않고 Paper Cream
저채도·저불투명 워시를 쓴다.

**난로 롱프레스**: 방사형 글로우 반경이 `progress`에 **선형** 비례
(`radius = progress × max_radius`) — 프로토타입이 유일하게 검증한 값(1.2초)이 이
단순 매핑에서 나온 결과이므로 지금은 이징을 새로 얹지 않는다. 색: Amber Ochre
Base(코어)→Light(가장자리), Color System §4 그대로. 한파 중 감자 볼의 Chilled→Base
전환은 별개 레이어(Color System 기존 규칙)로 이 섹션이 재정의하지 않는다.

**물뿌리개 다브**: 다브 1회 착지 즉시(페이드 없이 스냅) 그 지점 흙이 짙은 톤으로
전환 + 물방울 파티클 — art-bible §2-4 그대로. `progress`는 짙어진 영역의
합집합(Formula 3)이므로 커버리지 자체가 진행 시각이며 별도 게이지 불필요. 창문/
난로는 연속 스칼라지만 다브는 본질적으로 **불연속 단계**(다브마다 계단식 증가) —
다브형 입력 원형의 정체성이므로 억지로 보간하지 않는다.

### 2. 완성(확산) 시각 사양 (θ=1.0, 1회성)

| | 이펙트 | 방향성 | 색 |
|---|---|---|---|
| 창문 | 잔여 안개 전부 + 화면 안쪽으로 훅 퍼지는 신선한 공기 결 | 안쪽(창→방) | 중립/맑은 톤 |
| 난로 | 글로우가 반경 바깥으로 한 번 확장하며 잦아듦 | 바깥쪽(코어→가장자리) | Amber Ochre Base→Light |
| 물뿌리개 | 옅은 김(스팀) 한 번, 흙 전체가 짙어진 상태로 안정 | 위쪽(흙→공중) | 중립 저채도(날씨 예약색 아님) |

완성 후 접촉이 유지되는 동안엔 확산 비트(약 0.4~0.7초, 정확한 ms는 Asset Standards
소관)가 1회 재생된 뒤 "차 있는 상태"로 조용히 정지(추가 펄스·루프 없음). 손을
떼면 art-bible §2-1의 "접촉점 반응은 손을 떼면 1초 내로 정지 상태로 가라앉는다"를
그대로 적용.

### 3. Decaying 시각 처리

창문·난로는 §0의 매핑 원칙으로 별도 애니메이션 없이 자동 해결(안개가 서서히
되돌아옴 / 글로우 반경이 줄어듦, 둘 다 2차 ease-in 곡선을 그대로 반영해 해제 직후엔
거의 안 변하다가 후반 가속). **물뿌리개는 예외**: 개별 다브의 이진 마스크
합집합이라 "어떤 다브부터 지울지"가 정의되지 않으므로, 짙어진 영역 전체의
불투명도를 `decayed_progress(t)/progress_at_release` 비율로 균일하게 낮춘다(물이
골고루 마르는 은유). *이 메커니즘은 프로토타입 미검증 신규 제안 — 실 아트 적용 후
재검증 권장.* 감쇠 중 재접촉은 세 제스처 모두 별도 처리 없이 자동 해결(같은
`progress` 값을 매핑 함수에 계속 넣을 뿐).

### 4. 교차 제스처 시각 일관성

세 완성 이펙트는 표면 이미지는 다르지만 공통 골격 — "한 지점에서 한 번, 부드럽게,
짧게 퍼지는 수채 확산" — 을 공유한다. 차이는 확산 방향(안쪽/바깥쪽/위쪽)과 색뿐이며
Section 1 원칙 1("같은 안료 계보")과 일치한다. **권장**: 하나의 "소프트 방사 확산"
VFX 베이스 템플릿을 만들고 색·방향·지속시간만 바꿔 재사용(텍스처 메모리·솔로 스코프
양쪽에 유리, technical-artist 협의 필요). **재검증 플래그**: 창문·물뿌리개는
placeholder로만 검증됐고 "느낌이 안 난다"는 부정 신호를 받았으므로, 실제 수채
텍스처·사운드 적용 후 **반드시 짧은 재검증** 필요(프로토타입 리포트 권고 재확인) —
난로만 이미 검증됨.

### 5. 제스처별 오디오 아이덴티티 (성격 명세 — 정식 SFX 스펙 아님)

ASMR 적응형 오디오 시스템은 미설계 — 정확한 샘플·구현은 그 GDD 몫, 이 섹션은
성격/정서만 명세. 구독 인터페이스는 Dependencies가 이미 정의(`OnGestureCompleted`
+ 진행 중 `progress` 구독).

**공통 금지 원칙**: 완성 사운드는 "성공 차임/딩" 류의 게임 UI 보상음 문법을 쓰지
않는다(UI/HUD §4의 "러버호스 UI 모션" 안티필러와 동일 논리) — 숨을 내쉬는 것 같은
유기적 "해소"로 읽혀야 함.

| | 진행 중 | 진행 중 성격 | 완성 비트 |
|---|---|---|---|
| 창문 | 연속 | 유리 위 마찰음, 진행에 따라 음색이 "눅눅함→맑음"으로 변화(피치 아님) | 짧고 가벼운 "숨을 내쉬는" 바람 소리 |
| 난로 | 연속 | 낮고 따뜻한 허밍/잔불 소리, 음색의 온기·배음이 짙어짐(피치 아님) | 잉걸불이 자리잡는 부드러운 "정착" 소리 |
| 물뿌리개 | 이산(다브당 1회) | 다브마다 작은 물방울/토닥임, 매번 미세하게 랜덤 변주(반복감 방지) | 옅은 스팀이 빠지는 짧은 "쉬-" |

**피치 상승 지양**: art-bible 에너지 레벨 척도에 "다급함(frenetic/urgent)" 등급
자체가 없으므로, 진행 중 사운드가 피치로 다급함을 표현하면 척도 위반 — 음색(질감)
변화만 사용.

### 6. 1회성 제스처 시연 힌트 글리프

art-bible UI/HUD §3 기능 아이콘 어휘(단일 굵기 Ink Umber, Tier 2 단순 기하), Core
Rule 13이 지정한 위기/회복 전환과 동일한 1회성 논-루프 애니메이션 패턴 재사용(새
모션 문법 없음).

| 제스처 | 글리프 동작 | 구별 신호 |
|---|---|---|
| 창문(드래그) | 손끝 글리프가 드래그 축을 따라 시작→끝까지 한 번 이동 | 이동 = "옮기는 동작" |
| 난로(홀드) | 손끝 글리프가 한 지점에 고정된 채 짧게 머문 뒤 사라짐 | 이동 없음 = "누르고 기다림" |
| 물뿌리개(다브) | 손끝 글리프가 2~3개 지점을 짧게 여러 번 톡톡 짚음 | 반복 접촉-이탈 = "여러 번 두드림" |

색은 세 글리프 모두 Ink Umber(기능 아이콘 표준)로 통일, 완성 색을 쓰지 않는다
(힌트=순수 기능 안내, 정서 레이어 아님). 글리프 자체는 접촉점 번짐 반응을
트리거하지 않는다(art-bible §1 원칙 3 예외는 실제 플레이어 접촉에만 적용). 정확한
트리거 시점은 온보딩(미설계) 소관으로 열어둠 — 이 섹션은 모양·애니메이션만 정의.

📌 **Asset Spec** — Visual/Audio requirements가 정의되었습니다. art bible 승인 이후이므로
`/asset-spec system:touch-care-interaction`을 실행해 에셋별 시각 명세·생성 프롬프트를
뽑을 수 있습니다.

## UI Requirements

신규 HUD/화면 없음 — 진행률은 숫자 게이지가 아니라 Visual/Audio Requirements가
정의한 제스처 고유 시각 은유(안개 걷힘/글로우/흙 짙어짐) 자체로 전달된다(art-bible
"숫자 카운트다운 금지" 원칙과 일치). 히트존 크기·여백은 이미 Core Rule 11 +
art-bible §5가 확정. 1회성 시연 글리프는 Visual/Audio Requirements §6에서 다룸.

"간단 터치 모드" 접근성 토글이 향후 실제 설정 화면으로 구현된다면 그건 별도
시스템(설정/접근성 시스템, 현재 Alpha 스코프 미설계)의 UI Requirements 소관이며,
이 GDD는 Core Rule 12(파라미터화 훅)로 그 확장 여지만 남긴다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션.)*

### Core Rules 검증 (AC-ST-##)

- **AC-ST-01 (세 입력 원형 분류)** **GIVEN** 창문·난로·물뿌리개 컨트롤러 초기화 **WHEN** 입력 원형을 조회 **THEN** 창문=Drag, 난로=Hold, 물뿌리개=Dab-Area로 정확히 분류되고 서로 다른 raw input 소스를 구독한다.
- **AC-ST-02 (progress 공통 클램프)** **GIVEN** 세 제스처 각각 `Active` **WHEN** 원시 입력이 완성치를 초과 **THEN** `progress`가 `[0,1]`로 클램프, 음수 불가.
- **AC-ST-03 (완성 이벤트 정확히 1회)** **GIVEN** `progress`가 `θ_gesture(1.0)`에 최초 도달 **WHEN** 접촉이 계속 유지된다 **THEN** `OnGestureCompleted`는 최초 도달 프레임에서 정확히 1회만 발행.
- **AC-ST-04 (유예/감쇠 — 즉시 리셋 아님)** **GIVEN** `Active`, `progress=0.5`인 채 접촉 해제 **WHEN** 해제 직후 조회 **THEN** 즉시 0 아님, `Decaying` 전이, `decayed_progress(t=0)=0.5`.
- **AC-ST-05 (감쇠 중 재접촉 — 감쇠 지점에서 재개)** **GIVEN** `Decaying`, `t=1.0s`(`decayed_progress≈0.6`) **WHEN** 같은 존 재접촉 **THEN** `progress`는 0이 아닌 ≈0.6에서 재개, `Active` 전이.
- **AC-ST-06 (하드 중단 — 즉시 0)** **GIVEN** `Active`(완성 전) 또는 `Decaying` **WHEN** 씬 전환/백그라운드/강제종료 **THEN** 유예 없이 즉시 0, `Adjust*` 미호출.
- **AC-ST-07 (제스처별 스탯 방향 매핑)** **GIVEN** 세 제스처 각각 완성 **WHEN** 사이드이펙트 관찰 **THEN** 창문=`AdjustHumidity(-30)`/난로=`AdjustTemperature(+30)`/물뿌리개=`AdjustMoisture(+30)`만 호출.
- **AC-ST-08 (완성의 유일한 사이드이펙트)** **GIVEN** 완성 **WHEN** 파이프라인 관찰 **THEN** 게임 상태 변화는 `Adjust*` 1회뿐, 그 외엔 `OnGestureCompleted` 발행만 — 오디오/VFX/카운터 직접 호출 없음.
- **AC-ST-09 (동시 제스처 — 다른 존, 조용히 무시)** **GIVEN** X가 `Active` **WHEN** 다른 존(Y)에 새 터치 **THEN** Y는 `Idle` 유지(전이 없음, 에러 없음), X는 방해 없이 계속.
- **AC-ST-10 (Ceiling-Agnostic)** **GIVEN** 대상 스탯이 극값인 스텁 **WHEN** 제스처 정상 진행·완성 **THEN** 진행·완성 은유가 완전히 동일하게 재생, potato-stat getter 미호출(스파이 확인).
- **AC-ST-11 (쿨다운 없음)** **GIVEN** 방금 완성되어 `Idle` **WHEN** 즉시 재시작 **THEN** 대기 없이 즉시 `Active`.
- **AC-ST-12 (히트존 기하 — 설정값 검증)** **GIVEN** 각 제스처 히트존 설정값 **WHEN** 조회 **THEN** 홀드형 정지존 ≥88×88dp, 드래그형 grab zone ≥88×88dp, 다브형 존 전체가 88×88dp 상회, 여백 ≥8dp. *(설정값만 EditMode 검증 — 실기 DPI 렌더 결과는 수동 QA.)*
- **AC-ST-13 (접근성 파라미터화 훅)** **GIVEN** `θ_drag_distance` 값이 다른 두 인스턴스 **WHEN** 동일 `displacement`로 평가 **THEN** 완성 판정이 설정값에 따라 달라짐(하드코딩 아님 확인).
- **AC-ST-14 (1회성 시연 힌트)** **GIVEN** `hasShownHint=false` **WHEN** 해당 존 최초 유효 접촉 **THEN** 힌트 트리거 + `hasShownHint=true`. **WHEN** 이후 재트리거 **THEN** 힌트 재요청 없음.

### Formula 검증 (AC-F-##)

- **AC-F-01 (거리 기반 완성)** **GIVEN** `track_length=400dp, θ_drag_distance=0.65` **WHEN** `displacement=260dp` **THEN** `drag_progress=1.0`.
- **AC-F-02 (플릭 오버라이드)** **GIVEN** `displacement=200dp`(raw=0.769) **WHEN** `velocity=3.0≥2.5` 이고 `raw≥0.15` **THEN** `flick_override=1`→`drag_progress=1.0`.
- **AC-F-03 (플릭 최소 커밋 미달)** **GIVEN** `velocity≥임계` 이지만 `raw=0.05<0.15` **WHEN** 평가 **THEN** `flick_override=0`, 순수 거리값만 반영.
- **AC-F-04 (홀드 선형 진행)** **GIVEN** `HOLD_DURATION_STOVE=1.2s` **WHEN** `hold_duration=0.9s` **THEN** `hold_progress=0.75`.
- **AC-F-05 (홀드 완성·상한 클램프)** **GIVEN** 동일 **WHEN** `hold_duration≥1.2s` **THEN** `hold_progress=1.0` 클램프 유지.
- **AC-F-06 (다브 합집합 커버리지)** **GIVEN** 비중첩 다브 6회(각 0.10) **WHEN** 평가 **THEN** `raw_coverage=0.60`, `dab_progress=0.80`.
- **AC-F-07 (다브 완성 클램프)** **GIVEN** 비중첩 다브 8회 **WHEN** 평가 **THEN** `raw_coverage=0.80`, `dab_progress=1.0`(완성).
- **AC-F-08 (합집합 vs 단순합 — 이중계산 회귀 방지)** **GIVEN** 동일 좌표에 다브 5회 반복 **WHEN** `raw_coverage` 재계산 **THEN** `darkened_area`는 단일 풋프린트(≈0.10)에서 불변 — 단순합(0.50) 아님.
- **AC-F-09 (다브간 타임아웃)** **GIVEN** `t=0.0/0.4/0.9s` 다브 후 중단, `DAB_INTERDAB_TIMEOUT=1.0s` **WHEN** `t=1.7s`(0.8s 경과) **THEN** 유지. **WHEN** `t=1.95s`(1.05s 경과) **THEN** 감쇠 시작.
- **AC-F-10 (감쇠 곡선 전반부)** **GIVEN** `progress_at_release=0.8` **WHEN** `t=1.0s` **THEN** `decayed_progress=0.6`.
- **AC-F-11 (감쇠 곡선 후반부 가속)** **GIVEN** 동일 **WHEN** `t=1.8s` **THEN** `decayed_progress=0.152`.
- **AC-F-12 (윈도우 만료 — 정확히 0)** **GIVEN** 동일 **WHEN** `t≥2.0s` **THEN** `decayed_progress=0` 정확히(점근 아님).
- **AC-F-13 (예산 상수 정합성 — 회귀 가드)** **GIVEN** 현재 튜닝 노브 값 **WHEN** 3제스처 집중 모드 이론적 상한 산출 **THEN** 문서화된 상한(~10.7초)과 일치 — 노브 변경 시 이 테스트가 실패해야 예산표 미갱신을 잡아냄.

### Edge Cases 검증 (AC-EC-##)

- **AC-EC-01 (드래그 되돌림 — 고점 유지 아님)** **GIVEN** `displacement` 0→300dp 진행 후 200dp로 되돌림(접촉 유지) **WHEN** 재평가 **THEN** 고점 아닌 현재 변위 기준 재계산, `Decaying` 전이 없음.
- **AC-EC-02 (동일 존 이중 터치)** **GIVEN** 터치 A가 이미 구동 중 **WHEN** 같은 존에 터치 B 추가 **THEN** B 무시, `progress`는 A만 반영.
- **AC-EC-03 (Decaying 중 다른 제스처 시작 허용)** **GIVEN** X가 `Decaying` **WHEN** 다른 존에서 Y 시작 **THEN** Y 정상 전이, X의 감쇠는 중단 없이 계속.
- **AC-EC-04 (Decaying 중 하드 중단 — 세션 비이월)** **GIVEN** `Decaying`, `t=0.5s` **WHEN** 백그라운드/강제종료 후 재시작 **THEN** `progress=0`(`Idle`), 잔여 감쇠시간 이월 안 됨.
- **AC-EC-05 (시듦 중 게이팅)** **GIVEN** `IsWithering()=true` **WHEN** 아무 제스처 존 터치 **THEN** `Idle→Active` 전이 없음, 세 제스처 모두 동일.
- **AC-EC-06 (물뿌리개 동일 지점 반복 — 완성 불가)** **GIVEN** 한 지점에만 20회 다브 **WHEN** 평가 **THEN** `darkened_area`가 단일 풋프린트를 못 넘어 `dab_progress<1.0` 유지.

### Integration 검증 (AC-INT-##)

- **AC-INT-01 (`Adjust*` 정확히 1회 — 방향/크기)** **GIVEN** potato-stat 어댑터 스파이 **WHEN** 세 제스처 각각 완성 **THEN** 각각 정확한 `Adjust*(±30)` 1회만, 다른 메서드 미호출. 완성 후 접촉 유지돼도 추가 호출 없음.
- **AC-INT-02 (`IsWithering()` 게이팅 — 호출 계약)** **GIVEN** `IsWithering()` 스텁이 `true` **WHEN** `Idle` 존 터치 **THEN** `Active` 전이 시도 전에 `IsWithering()` 호출(스파이 확인) 후 차단.
- **AC-INT-03 (`OnGestureCompleted`와 `Adjust*` 호출 순서)** **GIVEN** 완성 조건 도달 **WHEN** 파이프라인 실행 **THEN** `Adjust*` 호출이 `OnGestureCompleted` 발행보다 먼저 완료(Core Rule 7 확정 문구와 일치).
- **AC-INT-04 (진행 중 `progress` 구독 — 실시간성)** **GIVEN** `Active` 또는 `Decaying` **WHEN** 구독자가 매 프레임 조회 **THEN** 그 프레임 최신값과 항상 일치, 배치·지연 없음.

### Visual/Audio 검증 (AC-VA-##)

- **AC-VA-01 (progress→시각 매핑 단조성)** **GIVEN** 각 제스처 매핑 함수 `f(progress)` **WHEN** 0→1 샘플 평가 **THEN** 세 제스처 모두 단조 비감소.
- **AC-VA-02 (난로 글로우 — 선형 매핑)** **GIVEN** `max_radius` **WHEN** `progress=0.75` **THEN** `radius=0.75×max_radius`.
- **AC-VA-03 (Decaying 자동 해소 — 창문/난로)** **GIVEN** Formula 4 시퀀스 값들 **WHEN** §0 매핑 함수에 대입 **THEN** AC-VA-01과 동일 함수로 산출한 값과 정확히 일치(전용 감쇠 코드 경로 없음 확인).
- **AC-VA-04 (물뿌리개 감쇠 — 균일 페이드 비율, 미검증 신규안)** **GIVEN** `progress_at_release=0.8` **WHEN** `decayed_progress(t=1.0s)=0.6` **THEN** 불투명도 배율=0.75로 균일 산출. *(비율 산술만 자동화 — "물이 마르는 것처럼 보이는가"는 실 아트 적용 후 스크린샷+리드 사인오프로 별도 재검증, ADVISORY.)*
- **AC-VA-05 (baseline haze + 날씨 오버레이 — 레이어 독립성, 사용자 확정 신규안)** **GIVEN** 날씨=맑음 **WHEN** `progress` 변화 **THEN** baseline haze만 반응. **GIVEN** 날씨=Rain/Fog **WHEN** 동일 **THEN** 두 레이어가 같은 `progress`를 공유해 함께 걷힘. *(로직 계약만 검증 — 실제 시각 임팩트는 문서가 "부가 효과"로 명시, 정식 판정 기준 없음.)*
- **AC-VA-06 (완성 비트 1회성)** **GIVEN** 완성 **WHEN** 이펙트 재생 후 접촉 유지 **THEN** 확산 비트 정확히 1회, 추가 펄스·루프 없음.
- **AC-VA-07 (1회성 힌트 — 글리프 선택)** **GIVEN** 세 제스처 각각 최초 트리거 **WHEN** 힌트 요청 **THEN** 창문=이동형/난로=고정-머묾형/물뿌리개=반복 톡톡형 애니메이션ID가 정확히 선택.
- **AC-VA-08 (물뿌리개 진행 — 불연속 계단식)** **GIVEN** 다브 착지 **WHEN** 그 프레임 시각 갱신 **THEN** 즉시 스냅(보간 없음) — 창문/난로(연속)와 구조적으로 다른 갱신 방식 고정.

**범위 밖(자동화 대상 아님)**: §5 오디오 아이덴티티의 정서 명세("유기적 해소", "피치
상승 지양" 등)는 코드가 아니라 자산 방향이라 로직 AC로 성립하지 않는다 — 오디오
자산 리뷰 체크리스트(수동, Visual/Feel 증거)로만 검증(testing-standards.md "Feel
품질은 자동화 대상 아님"과 일치).

### Testability Notes

- **주입 가능한 추상화 필요**: `drag_progress`(포인터 소스+시계), `hold_progress`
  (시간 델타 구동, `DateTime.UtcNow`/`Time.time` 직접 호출 금지), `dab_progress`
  (다브 이벤트 스트림+시계, 커버리지 트래커는 렌더링과 분리된 순수 로직 컴포넌트),
  상태 머신(감쇠 카운트다운용 주입 시계 + 스크립트 가능한 가짜 터치 입력),
  potato-stat 어댑터(`Adjust*`/`IsWithering` 모킹 가능한 인터페이스).
- **`decayed_progress`는 완전한 순수 함수**로 구현(내부 상태·실시간 시계 참조 없이
  `(progress_at_release, t, DECAY_GRACE_WINDOW, DECAY_CURVE_EXPONENT)`→값) —
  offline-progress-calculation.md의 "폐형식 함수" 원칙과 동일.
- **하드 중단(Rule 5)**: 실제 엔진 콜백이 아니라 내부 진입점 메서드를 직접 호출해
  테스트(offline-progress-calculation.md Testability Notes와 동일 패턴).
- **EditMode 자동화 가능**: 모든 AC-ST/AC-F/AC-EC/AC-INT, AC-VA 중 로직 성격
  항목(매핑 단조성·선형 공식·감쇠 자동해소·균일 페이드 비율·완성 1회성·힌트 선택
  매핑·계단식 갱신) — 위 추상화가 실제로 구현된다는 전제.
- **PlayMode/실기·수동 QA 필요**: 실제 렌더 히트존 크기의 DPI 스케일링 결과, §5
  오디오 아이덴티티 전체, baseline haze+날씨 오버레이 중첩의 실제 시각 임팩트,
  다브 균일 페이드의 "마르는 느낌", 창문·물뿌리개 완성 이펙트 전반의 "느낌"(문서
  §4가 이미 명시한 재검증 항목 — 난로만 기검증).

## Open Questions

| 질문 | 상황 | 담당 | 해소 시점 |
|---|---|---|---|
| 창문·물뿌리개의 진행/완성 은유 전체(위치 연동 와이프, 즉시-짙어짐+김) 재검증 | 프로토타입이 placeholder 아트/사운드로 "느낌이 안 난다"는 부정 신호를 준 항목 — 이 GDD가 제안한 구체적 재설계는 실 아트·사운드 적용 전까지 미검증. 난로만 이미 검증됨 | art-director/사용자 플레이테스트 | 실제 수채 텍스처·ASMR 사운드 적용 직후, 짧은 재검증 세션 |
| 물뿌리개 Decaying의 "균일 불투명도 페이드" 메커니즘 | 프로토타입 근거 없는 신규 제안(Visual/Audio §3) — 논리적 정확성은 AC-VA-04로 검증되지만 "물이 마르는 것처럼 보이는가"는 실 아트 필요 | art-director | 위 재검증과 동시 |
| baseline haze + 날씨 오버레이 중첩(Visual/Audio §1)의 실제 시각 임팩트 | 이 GDD와 weather-system.md 사이의 신규 해석(사용자 확정)이나, 합성 결과의 실제 임팩트는 미검증 | art-director/technical-artist | 실 아트 적용 후 |
| 소프트 방사 확산 VFX 공유 템플릿의 구현 타당성 | 세 완성 이펙트를 하나의 셰이더/파티클 베이스로 재사용하자는 제안(Visual/Audio §4) — 실제 구현 가능성 미확인 | technical-artist | `/create-architecture` 또는 프리프로덕션 기술 검토 시 |
| `θ_drag_distance`(0.65)·`DECAY_GRACE_WINDOW`(2.0s)·`DECAY_CURVE_EXPONENT`(2.0) 등 프로토타입 이월/신규 상수 | 프로토타입이 검증한 건 홀드형 1.2초뿐 — 나머지는 안전범위만 제시된 미검증 값 | game-designer/qa-tester | Vertical Slice 플레이테스트 |
| "간단 터치 모드" 접근성 대안 조작 스킴 | art-bible §UI/HUD "5"가 이미 열린 과제로 기록(2026-07-09, WCAG 2.5.1 근거) — 이 GDD는 Core Rule 12로 파라미터화 훅만 남기고 결정하지 않음 | game-designer/ui-programmer | 설정/접근성 시스템 `/design-system` 진행 시 |
| 1회성 시연 힌트의 정확한 트리거 시점(화면 진입 즉시 vs. 지연 후) | 온보딩 시스템이 미설계라 이 GDD는 모양·애니메이션만 정의하고 트리거 시점은 위임 | ux-designer | 온보딩(1회성 힌트) 시스템 설계 시 |
| AC-INT-03이 가정한 `Adjust*`→`OnGestureCompleted` 호출 순서의 구현 검증 | Core Rule 7에 문구는 추가했으나 실제 구현 단계에서 순서 위반이 없는지는 코드 리뷰 대상 | lead-programmer | dev-story/code-review 단계 |
