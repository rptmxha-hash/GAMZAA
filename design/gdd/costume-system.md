# 코스튬 시스템 (기본, 1~2종)

> **Status**: Designed (pending review)
> **Author**: user + systems-designer + art-director + qa-lead
> **Last Updated**: 2026-07-21
> **Implements Pillar**: Pillar 2 (지켜주고 싶은 마음)
> **Creative Director Review (CD-GDD-ALIGN)**: skipped — Lean mode

## Overview

코스튬 시스템은 계정이 보유한 코스튬 목록과, 현재 감자에게 실제로 착장된 코스튬
하나를 소유·관리하는 데이터 계층이다. 핵심 상태는 두 가지뿐이다 — (1) 보유 코스튬
컬렉션(MVP는 1~2종을 처음부터 전부 보유, 잠금해제 로직 없음 — 가챠/뽑기 시스템
(Alpha)이 추가 코스튬의 잠금해제를 담당할 때 이 시스템은 그 결과를 소비만 하도록
확장될 것), (2) 현재 감자에 착장된 코스튬 ID(또는 맨몸을 뜻하는 null). 이 시스템은
스프라이트를 직접 렌더링하지 않는다 — 착장 여부·어떤 코스튬인지를 소유하고 조회
인터페이스로 노출할 뿐, 실제 시각화(케이지 앵커 포인트 부착)는 art-bible이 이미
확정한 렌더링 계층이 이 상태를 읽어 처리한다.

플레이어는 이 시스템과 단 한 순간에 능동적으로 상호작용한다 — 감자가 수확 단계
(Harvest-ready)에 도달했을 때, 보유한 코스튬 중 하나를 선택해 입힌다(art-bible
확정 전제: 코스튬은 수확 이후에만 착장, 자라는 중엔 맨몸). 그 외의 시간에는 완전히
수동적이다 — 이미 착장된 코스튬은 이름짓기&수확카드가 스냅샷으로 기록하거나 감자
마을에 전시될 때까지 그대로 유지된다.

이 시스템이 없다면 감자는 수확 이후에도 영원히 맨몸이며, Pillar 2(지켜주고 싶은
마음)의 표현 축 — "내가 기른 이 감자만의 모습" — 이 존재하지 않는다. 이름짓기&
수확카드가 기록할 간직 카드도, 감자 마을 컬렉션의 개체 구별(art-bible 확정: 코스튬
실루엣이 마을에서 개체 구별을 전담)도 이 시스템의 데이터 없이는 성립하지 않는다.

## Player Fantasy

> *(`creative-director` 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

플레이어가 코스튬을 고르는 순간 느껴야 할 감정은 수집욕이 아니라 **작은 배웅
의식**이다 — 이 감자와 함께한 4일을 마무리하며, 마지막으로 내가 골라 입혀 보내는
손길이다. art-bible이 이미 이 순간을 "그림책 표지/헌정 페이지를 넘기는" 레이아웃으로
확정했고(Section 2 §5), 무드 서술어는 "뭉클한·아쉬운·자랑스러운·잔잔한"이다 — 코스튬
선택 UI 자체도 이 톤 안에 있어야 한다(화려한 뽑기 연출이 아니라 다정하게 넘기는
페이지).

game-concept.md의 MDA 우선순위상 Expression은 4순위(Fantasy/Sensation/Submission보다
낮음)이며, Player Type Appeal 표에서 Achievers는 "보조 동기"로만 명시되어 있다 — 즉
이 시스템의 1차 정서 엔진은 수집 성취감이 아니라 Pillar 2의 Core Need인
Relatedness(양육적 애착)다. **Design test**: 코스튬 선택 화면이 "몇 종 모았는지"
진행률·배지·희귀도 강조로 흘러가면 → 반려하고 "이 아이에게 무엇을 입혀줄까" 톤으로
되돌린다.

MVP는 1~2종뿐이라 "선택의 폭"이 주는 재미는 거의 없다 — 그래서 이 순간의 정서적
무게는 선택지 수가 아니라 "내가 직접 골라줬다"는 행위 자체에서 나와야 한다. 코스튬
30종이 갖춰지는 post-MVP에서야 Discovery(발견) 미학이 보조적으로 붙는다
(game-concept.md MDA 5순위).

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 lean에서도 상담 필수.)*

### Core Rules

1. **두 가지 영속 상태만 소유**: `ownedCostumeIds`(보유 목록, MVP는 계정 생성 시 고정
   1~2개 전부 채움 — 잠금해제 로직 없음)과 `equippedCostumeId`(현재 감자에 착장된
   코스튬, nullable — null=맨몸). 외형(실루엣·색 램프 등)은 art-bible/렌더링 계층
   소유, 이 시스템은 ID만 다룬다.
2. **착장은 자기방어적 게이트**: `Equip(costumeId)`는 호출 시점에 생애주기 진행의
   `stage`를 직접 재확인해 `stage != HarvestReady`면 호출자가 무엇을 보내든
   무시(no-op)한다(lifecycle-progression Edge Case와 동일한 "호출자 미신뢰" 철학).
3. **착장은 SET, 누적/스택 아님**: `Equip()` 여러 번 호출해도 매번 덮어쓰기뿐이며
   확정 전까지 몇 번이든 바꿔볼 수 있다.
4. **소유하지 않은 코스튬은 착장 불가**: `costumeId`가 `ownedCostumeIds`에 없으면
   `Equip()`은 실패(no-op)한다 — MVP는 항상 전량 보유라 실무상 발생하지 않지만,
   가챠 도입 이후를 위한 방어선.
5. **생애주기 리셋 시 착장도 함께 리셋**: 새 생애주기 시작 시 `equippedCostumeId`는
   `null`로 되돌아간다 — `ownedCostumeIds`는 리셋되지 않는다(영구 소유).
6. **과거 수확 개체의 착장 기록은 보존하지 않음**: 이 시스템은 오직 "지금 이 감자"만
   소유한다 — 수확 스냅샷은 이름짓기&수확카드가 보존한다(그 시스템 소유).

### States and Transitions

| 상태 | 진입 조건 | 이탈 조건 |
|---|---|---|
| **Unequipped(맨몸)** | 신규 세이브 또는 새 생애주기 시작(Core Rule 5 리셋) | `Equip()` 성공 호출 → Equipped |
| **Equipped(costumeId)** | `Equip(costumeId)` 성공 호출 | 다른 `Equip()` 성공 호출(다른 Equipped로 재진입) 또는 새 생애주기 리셋 → Unequipped |

### Interactions with Other Systems

| 시스템 | 관계 | 강도 | 인터페이스 |
|---|---|---|---|
| 생애주기 진행 | 조회(이 시스템 → lifecycle) | Hard | `stage` 읽어 `Equip()` 게이트 재확인(Core Rule 2) |
| 이름짓기 & 수확 카드 | 피호출자(orchestrator가 이 시스템을 호출) | Hard | `GetOwnedCostumes()`/`GetEquippedCostume()`/`Equip(costumeId)` 노출. 수확 확정 시 그 시스템이 `equippedCostumeId`를 스냅샷으로 복사해가고, 새 생애주기 시작 시 이 시스템의 리셋(Core Rule 5)을 트리거한다 — 정확한 호출 시퀀스는 Open Questions(그 시스템 미설계)로 이월 |
| 가챠/뽑기 시스템(Alpha, 잠정) | 피호출자(가챠가 이 시스템을 호출) | Soft(MVP 미존재) | `AddOwnedCostume(costumeId)` — 가챠 보상 지급 시 보유 목록에 추가. 이 GDD는 인터페이스만 잠정 노출, 정확한 계약은 그 시스템 설계 시 확정 |
| 감자 마을/컬렉션(Vertical Slice, 잠정) | 직접 연동 없음 | — | 과거 수확 개체의 착장 기록은 이 시스템이 아니라 이름짓기&수확카드의 스냅샷을 경유한다(Core Rule 6) |

## Formulas

> *(`systems-designer` 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션. 핵심
> 결론: 이 시스템은 순수 규칙 기반이라 공식 2개면 충분하며, 그 이상은 과잉설계.
> 생애주기 리셋(Core Rule 5)은 대입뿐이라 Formulas가 아니라 Edge Cases에 배치.)*

### F1. `CanEquip`

`CanEquip(stage) = 1[stage = HarvestReady]`

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 생장 단계 | `stage` | enum | {Sprout, Growing, Plump, HarvestReady} | `Equip()` 호출 시점에 생애주기 진행 시스템에서 실시간 재조회하는 현재 생장 단계. `stageIndex(G)==3`(레지스트리 등록 공식)과 동치 |
| 결과 | `CanEquip` | bool | {false, true} | 지금 이 순간 착장 시도가 시점 게이트를 통과하는지 여부 |

**Output Range:** bool, 중간값 없음(정의상 이진값, 클램프 대상 아님).

**Example:** `G=68h` → `stageIndex(68)=⌊68/24⌋=2` → `stage=Plump` → `CanEquip(Plump)=false`.
`G=72h` → `stageIndex(72)=3` → `stage=HarvestReady` → `CanEquip(HarvestReady)=true`.
`G=500h`(장기 방치) → `stageIndex` 클램프로 여전히 3 → `CanEquip=true`(lifecycle Core
Rule 4와 정합 — 장기 방치해도 착장 가능 상태 무기한 유지).

---

### F2. `Equip`

`equippedCostumeId' = costumeId, if CanEquip(stage) ∧ (costumeId ∈ ownedCostumeIds);
else equippedCostumeId(변화 없음)`

| 변수명 | 기호 | 타입 | 범위 | 설명 |
|---|---|---|---|---|
| 착장 시도 대상 | `costumeId` | ID (string) | 유효 코스튬 ID 전체 집합 | 호출자를 신뢰하지 않고 두 게이트(stage·소유권) 모두 재검증 |
| 보유 코스튬 집합 | `ownedCostumeIds` | set&lt;ID&gt; | `\|ownedCostumeIds\| ≥ 1`, 크기 상한 없음 | 계정 보유 코스튬 집합(읽기 전용 참조). MVP 1~2 → post-MVP 확장에도 이 공식의 로직은 불변 — 멤버십 검사는 집합 크기에 의존하지 않음 |
| 생장 단계 | `stage` | enum | {Sprout, Growing, Plump, HarvestReady} | `Equip()` 호출 시점 재조회(F1 재사용) |
| 착장 상태(사전/사후) | `equippedCostumeId`, `equippedCostumeId'` | nullable ID | `ownedCostumeIds ∪ {null}` | 호출 전/후 착장 상태 |

**Output Range:** `equippedCostumeId'`는 항상 `ownedCostumeIds ∪ {null}`의 원소로
유계 — 소유하지 않은 값으로는 절대 설정 불가. 게이트 실패 시 부분 부작용 없이 이전
값 완전 보존(no-op).

**Example (4가지 분기 모두):**
1. **성공**: `ownedCostumeIds={"beanie_01","scarf_01"}`, `stage=HarvestReady`, 사전
   `equippedCostumeId=null` → `Equip("scarf_01")` → 두 게이트 통과 →
   `equippedCostumeId'="scarf_01"`
2. **재선택(SET, 누적 아님 — Core Rule 3)**: 이어서 `Equip("beanie_01")` → 두 게이트
   통과 → `equippedCostumeId'="beanie_01"`(직전 값 완전 덮어씀)
3. **stage 게이트 실패(Core Rule 2)**: `stage=Plump`, `Equip("beanie_01")` →
   `CanEquip=false` → `equippedCostumeId'`=이전 값 그대로(no-op)
4. **소유권 게이트 실패(Core Rule 4)**: `stage=HarvestReady`,
   `ownedCostumeIds={"beanie_01","scarf_01"}`, `Equip("unowned_99")` →
   `CanEquip=true`이나 `costumeId ∉ ownedCostumeIds` → `equippedCostumeId'`=이전
   값 그대로(no-op)

**상수 요약(레지스트리 등록 후보)**: `CanEquip`(formula), `Equip`(formula) — 둘 다
불리언 게이트/SET mutator 패턴으로 `IsTimestampValid`/`ActivateShield` 선례와 동일
계보.

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드.)*

- **If 새 생애주기가 시작됨(다운스트림 시스템이 리셋을 트리거)**: `equippedCostumeId`는
  `null`로 무조건 대입(Core Rule 5) — `ownedCostumeIds`는 손대지 않는다(영구 소유).
  대입 자체에 조건 분기가 없다(F1·F2와 달리 계산이 아니므로 Formulas가 아닌 여기
  기술).
- **If `stage=HarvestReady`이면서 동시에 `isWithered=true`(시듦, lifecycle
  AC-EC-04에서 이미 확인된 조합)인 상태에서 `Equip()` 호출**: `CanEquip`은 `stage`만
  참조하므로 여전히 `true` — 착장은 정상 성공한다. 코스튬 착장은 스탯 위기와 완전히
  독립적인 순수 미학 행위이며, `neglectMeter`나 `isWithered`를 조회하지 않는다.
- **If 보유 코스튬이 정확히 1개뿐**: 자동 착장은 일어나지 않는다 — `Equip()`은 개수와
  무관하게 항상 명시적 호출을 요구한다(Core Rule 규칙에 개수 분기 없음). UI가 유일한
  선택지를 미리 선택해서 보여줄지는 UI 레이어의 재량이며 이 시스템의 규칙을 바꾸지
  않는다.
- **If 착장 성공 이후 `stage`가 뒤로 돌아가야 하는 상황**: 발생하지 않는다 —
  `HarvestReady`는 종착점(lifecycle-progression Core Rule 4)이라 다음
  `OnHarvestCompleted()` 호출 전까지 자연 이탈이 없다. 따라서 착장 상태는 다음
  리셋(Core Rule 5)까지 안정적으로 유지되며, "착장 취소" 같은 별도 처리는 불필요하다.
- **If 플레이어가 착장 후 다시 맨몸으로 되돌리고 싶어함(Unequip)**: 이 시스템은
  그 경로를 의도적으로 제공하지 않는다(사용자 확정, 2026-07-20) — 한 번 착장한
  뒤에는 오직 **다른 보유 코스튬으로 재선택**(Core Rule 3, SET)만 가능하고,
  맨몸(`null`)으로의 명시적 복귀는 새 생애주기 리셋(Core Rule 5)을 통해서만
  일어난다. Player Fantasy의 "항상 입혀 보낸다"는 배웅 의식 톤과 정합 — 맨몸으로
  되돌리는 선택지는 그 정서적 프레이밍과 맞지 않는다.
- **If 리셋 트리거가 lifecycle이 실제로 새 Sprout를 시작하지 않은 상태에서
  조기/중복으로 호출됨(다운스트림 버그)**: lifecycle-progression의 동일한 방어
  철학(AC-EC-03, 호출자 미신뢰)을 따라 무시(no-op)한다 — 이 시스템의 리셋은 lifecycle
  이 실제로 새 생애주기를 시작한 경우에만 발동해야 하며, 정확한 트리거 시퀀싱은
  Open Questions로 이월한다.
- **If 가챠(Alpha)가 이미 보유한 코스튬을 중복 지급 시도**: `ownedCostumeIds`는
  set이므로 자연히 멱등(idempotent)하다 — 중복 추가로 인한 부작용이 없어 별도 처리가
  필요 없다.

## Dependencies

### Upstream (이 시스템이 의존)

| 시스템 | 방향 | 강도 | 인터페이스 |
|---|---|---|---|
| 생애주기 진행 | 조회(이 시스템 → lifecycle) | Hard | `stage` 읽기 → `CanEquip(stage)` 게이트(Formulas F1) |
| 세이브/영속성 시스템 | 등록(이 시스템 → save-persistence) | Hard | `Register("costume", {ownedCostumeIds, equippedCostumeId})` — qa-lead 상담(2026-07-20)에서 발견된 갭: `ownedCostumeIds`/`equippedCostumeId`가 영속 상태임에도 어느 문서에도 저장 등록 키가 없었음(`neglectMeter`/`shieldActiveUntil` 선례와 동일 패턴의 실제 버그 위험) — 이 GDD에서 신규 확정, `save-persistence-system.md` Interactions/Dependencies 표 교차 수정 완료 |

### Downstream (이 시스템에 의존)

| 시스템 | 방향 | 강도 | 인터페이스 |
|---|---|---|---|
| 이름짓기 & 수확 카드 | 호출(그 시스템 → 이 시스템) | Hard | `GetOwnedCostumes()`/`GetEquippedCostume()`/`Equip(costumeId)` 호출, 수확 확정 시 `equippedCostumeId`를 스냅샷으로 복사, 새 생애주기 시작 시 이 시스템의 리셋 트리거(Edge Cases 참고) — 정확한 호출 시퀀스는 Open Questions(그 시스템 미설계)로 이월 |
| 가챠/뽑기 시스템(Alpha) | 호출(가챠 → 이 시스템) | Soft(MVP 미구현) | `AddOwnedCostume(costumeId)` — 인터페이스만 잠정 노출, 정확한 계약은 그 시스템 설계 시 확정 |

**Hard vs Soft 판정 기준**: lifecycle-progression-system.md와 동일 — 이 시스템이
응답하지 않을 때 대상 시스템이 아예 작동을 멈추면 Hard, 연출/콘텐츠만 줄어들고
핵심 루프는 유지되면 Soft.

**양방향 일관성 확인**: `lifecycle-progression-system.md`는 이미 자신의 Downstream
표에 코스튬 시스템을 Hard로 정확히 기록해두고 있어(2026-07-16 확정) 추가 수정이
필요 없다. `systems-index.md` Dependency Map도 동일한 방향(코스튬 시스템 →
생애주기 진행, 가챠/뽑기 시스템 → 코스튬 시스템)으로 이미 일치한다.

## Tuning Knobs

| 노브 | 기본값 | 안전 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `N_OWNED_MVP`(MVP 보유 코스튬 수) | 1~2(game-concept.md 확정) | {1, 2} | (=1) 수확 순간 실질적 선택지가 없음(Edge Case "보유 코스튬 1개뿐" 경로만 발생), Expression 미학(game-concept.md MDA 4순위) 효과가 거의 사라짐 | (=2 초과) 이 GDD·systems-index 스코프 밖 — 3종 이상은 game-concept.md MVP Definition("코스튬 1~2종만")의 재협상이 필요하며, art-bible이 이미 경고한 솔로 개발 아트 물량 부담이 배로 늘어남 |

**노브 간 상호작용**: 없음 — 이 노브가 이 시스템의 유일한 튜닝 가능 값이다. 순수
규칙 기반 시스템이라 rate·decay·threshold 곡선이 존재하지 않으며, `CanEquip`/`Equip`
(Formulas)은 `N_OWNED_MVP`가 1이든 2든 로직이 전혀 바뀌지 않는다(멤버십 검사는 집합
크기에 의존하지 않음, F2 변수 정의 참고).

## Visual/Audio Requirements

> *(`art-director` 상담 완료 — 이 시스템은 캐릭터/시각 표현 카테고리라 Visual/Audio가
> 필수 섹션.)*

### 1. 착장/재선택 연출 (신규 확정)

`Equip()` 성공 시 부착 파츠는 **즉시 나타남/사라짐**(보간 없음, 신규 상수 없음).
이 이벤트는 `OnStageAdvanced`(생장 단계 실루엣 모프)나 `BlendMode`/
`POSE_BLEND_DURATION`(포즈·윤곽선 크로스페이드)과는 **완전히 별개** — 코스튬은
그 4채널(포즈 카테고리 + 3스탯 윤곽선 블렌드)에 속하지 않는, 앵커 포인트에
얹히는 직교 레이어다.

Formulas(F2)가 이미 "확정 전까지 몇 번이든 바꿔볼 수 있다"(SET 의미론)로 잠가둔
대로, 반복 재선택 시에도 매번 동일하게 즉시 적용된다 — 매 재선택마다 연출을
재생하면 expression-animation-system §3이 세운 원칙("잦은 이벤트에 정점급 무게를
주면 반복될수록 특별함이 희석된다")과 충돌한다. 이 순간의 정서적 무게(Player
Fantasy — "작은 배웅 의식")는 캐릭터 애니메이션이 아니라 **선택 패널 자체의
진입/퇴장**이 담당한다(art-bible "그림책 표지/헌정 페이지를 넘기는" 레이아웃 인용
— 정확한 트랜지션 명세는 UI Requirements 참고).

### 2. 형태·재질 규칙 (art-bible 인용, 신규 아님)

- 앵커 포인트 3곳(정수리/옆구리 바깥쪽/등 뒤), 표정 제외 구역(눈꺼풀 위치 윤곽선·
  볼·아랫배), 잎-갭 규칙(모자류는 수확 크라운이 일부 노출되도록 공간을 남김),
  구별 위계(실루엣 1차 → 색 램프 2차 → 패턴 3차), 재채색은 버텍스 컬러 틴트 —
  전부 art-bible Character Design Direction §(c)·Asset Standards가 이미 잠근
  값이며 이 GDD는 재정의하지 않는다.
- 코스튬은 수확(Harvest-ready) 실루엣 하나에만 맞으면 된다 — 생장 4단계 전부
  대응 불필요(art-bible 확정: 코스튬은 수확 이후에만 착장).

### 3. 렌더링 소유권 공백 (확인됨 — Open Questions로 이월)

`expression-animation-system.md`를 전수 확인한 결과, 코스튬 파츠를 앵커 포인트에
실제로 부착·렌더링하는 책임의 소유자가 **현재 프로젝트 전체에 존재하지 않는다**
(추측이 아니라 확인된 공백 — 그 문서는 "감자 마을 화면은 범위 밖"이라는 제외
문장 하나만 갖고 있을 뿐, 코스튬 부착 자체를 Interactions/Downstream 어디에도
올리지 않았다). 이 GDD는 렌더링을 직접 수행하지 않으므로(Overview·Detailed
Design 확정) 이 공백이 이 시스템 자체를 블로킹하지는 않지만, 정확한 소유자(가장
유력한 후보: 이름짓기 & 수확 카드 시스템 — `Equip()` 호출과 같은 자리에서 렌더러도
함께 갱신하는 구조)는 Open Questions로 이월하며, 그 시스템 `/design-system` 진행
시 반드시 해결해야 한다.

### 4. 오디오

이 시스템은 사운드를 직접 재생하지 않으며, 착장에 대한 별도 이벤트 훅도 노출하지
않는다(Detailed Design에 이벤트 자체가 정의되어 있지 않음 — `Equip()`은 상태
mutator일 뿐). 실제 SFX 설계는 위 §3과 같은 Open Question에 묶어 ASMR 적응형
오디오 시스템(아직 미설계)의 몫으로 이월한다.

## UI Requirements

1. **패널 소유권 경계**: 코스튬 선택 UI의 레이아웃·진입 트리거·화면 배치는 이
   시스템이 아니라 **이름짓기 & 수확 카드**가 소유한다(Dependencies 확정) — 이
   시스템은 오직 "무엇을 보여줄지"(`GetOwnedCostumes()`/`GetEquippedCostume()`)
   데이터만 조회 인터페이스로 노출한다.
2. **표시 정보**: 보유 코스튬 각각의 썸네일(art-bible 실루엣 인용) + 현재 착장
   여부를 가리키는 시각 신호. 탭 즉시 `Equip()`이 호출되어 반영된다 — 별도
   미리보기 상태는 없다(Visual/Audio §1, Formulas F2와 일치, WYSIWYG).
3. **히트박스**: art-bible UI/HUD가 이미 잠근 ≥88dp 최소 히트박스를 그대로
   적용한다(신규 결정 아님).
4. **보유 코스튬이 1개뿐일 때**: 자동 선택 여부는 UI 레이어의 재량이지만(Edge
   Case 참고), 선택지가 하나뿐이더라도 "이미 착장됨" 확인 표시는 반드시 있어야
   한다 — 유일한 선택지라고 해서 착장 상태 확인 신호를 생략하지 않는다.
5. **패널 진입/퇴장 트랜지션**: art-bible이 확정한 "그림책 표지/헌정 페이지를
   넘기는" 레이아웃을 인용한다(Visual/Audio §1과 교차참조) — 정확한 트랜지션
   지속시간·연출 디테일은 이름짓기 & 수확 카드 시스템의 UX 설계 시 확정한다
   (Open Questions로 이월).

> 📌 **UX Flag — 코스튬 시스템**: 이 시스템은 UI 요구사항을 갖고 있으나, 실제 패널의
> 레이아웃·진입 트리거는 이름짓기 & 수확 카드 시스템이 소유한다. Pre-Production
> 단계에서 `/ux-design`을 실행할 때는 이 시스템 단독이 아니라 **이름짓기 & 수확
> 카드 화면**의 UX 스펙으로 통합 작성하는 것을 권장한다. 스토리에서 이 UI를
> 참조할 때는 이 GDD가 아니라 그 UX 스펙(`design/ux/[screen].md`)을 인용해야 한다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — 2026-07-20. lean 모드에서도 Acceptance Criteria는 상담
> 필수 섹션. 상태 전이 기준은 `lifecycle-progression-system.md`·`potato-stat-system.md`
> 와 동일하게 `AC-ST-##` 접두사를 쓴다.)*

아래 기준은 Detailed Design·Formulas·Edge Cases에서 정의된 필드·상수명을 그대로
사용한다. 각 항목은 GDD 전체를 읽지 않은 QA 테스터도 독립적으로 재현·판정할 수
있도록 정확한 시작 조건과 기대값을 명시한다.

### 초기 상태 및 상태 전이 (Core Rules 1, 3, 5)

- **AC-ST-01 (신규 세이브 초기 상태 — Core Rule 1)** **GIVEN** 신규 계정 생성
  직후, 세이브 데이터 없음 **WHEN** `GetOwnedCostumes()`/`GetEquippedCostume()`
  조회 **THEN** `ownedCostumeIds`는 `N_OWNED_MVP`개(1~2개) 전부를 포함하고(잠금
  해제 로직 없음), `equippedCostumeId`는 `null`(Unequipped) — States and
  Transitions 표의 "신규 세이브" 진입 조건과 일치.
- **AC-ST-02 (Equip 성공 — Unequipped → Equipped)** **GIVEN**
  `equippedCostumeId=null`, `ownedCostumeIds={"beanie_01","scarf_01"}`,
  `stage=HarvestReady` **WHEN** `Equip("scarf_01")` 호출 **THEN**
  `equippedCostumeId'="scarf_01"` — Unequipped → Equipped(scarf_01).
- **AC-ST-03 (재선택 — Equipped → Equipped, Core Rule 3)** **GIVEN** AC-ST-02
  직후 `equippedCostumeId="scarf_01"` **WHEN** 동일 스테이지에서
  `Equip("beanie_01")` 재호출 **THEN** `equippedCostumeId'="beanie_01"`(완전
  덮어씀) — 두 값이 누적되거나 리스트로 쌓이지 않는다(단일 nullable ID 필드).
- **AC-ST-04 (생애주기 리셋 — Equipped → Unequipped, Core Rule 5)** **GIVEN**
  `equippedCostumeId="beanie_01"`(Equipped) **WHEN** 새 생애주기 시작(리셋 트리거)
  발생 **THEN** `equippedCostumeId'=null`(Unequipped) — 표의 리셋 이탈 조건과
  일치.

### 착장 게이트 (Formulas F1 — CanEquip, Core Rule 2)

- **AC-F-01 (게이트 폐쇄 — Growing/Plump)** **GIVEN** `G=68h` **WHEN**
  `CanEquip(stage)` 평가 **THEN** `stageIndex(68)=⌊68/24⌋=2` → `stage=Plump` →
  `CanEquip=false`.
- **AC-F-02 (경계값 — 하한 미도달, 71.99h)** **GIVEN** `G=71.99h` **WHEN**
  `CanEquip(stage)` 평가 **THEN** `stage=Plump` → `CanEquip=false`.
- **AC-F-03 (경계값 도달 — 개방, 72.0h)** **GIVEN** `G=72.0h` **WHEN**
  `CanEquip(stage)` 평가 **THEN** `stageIndex(72)=3` → `stage=HarvestReady` →
  `CanEquip=true`(하한 inclusive).
- **AC-F-04 (장기 방치에도 게이트 유지 — 상한 클램프)** **GIVEN** `G=500h`
  **WHEN** `CanEquip(stage)` 평가 **THEN** `stageIndex` 클램프로 `stage`는 여전히
  HarvestReady → `CanEquip=true`.

### 착장 SET 동작 (Formulas F2 — Equip, Core Rules 2·3·4)

- **AC-F-05 (성공)** **GIVEN** `ownedCostumeIds={"beanie_01","scarf_01"}`,
  `stage=HarvestReady`, `equippedCostumeId=null` **WHEN** `Equip("scarf_01")`
  **THEN** `equippedCostumeId'="scarf_01"`.
- **AC-F-06 (재선택 — SET, Core Rule 3)** **GIVEN** AC-F-05 직후 **WHEN**
  `Equip("beanie_01")` **THEN** `equippedCostumeId'="beanie_01"`(완전 대체).
- **AC-F-07 (stage 게이트 실패 — no-op, Core Rule 2)** **GIVEN** `stage=Plump`,
  사전 `equippedCostumeId=X` **WHEN** `Equip("beanie_01")` **THEN**
  `CanEquip=false` → `equippedCostumeId`는 변화 없이 `X` 유지.
- **AC-F-08 (소유권 게이트 실패 — no-op, Core Rule 4)** **GIVEN**
  `stage=HarvestReady`, `ownedCostumeIds={"beanie_01","scarf_01"}`, 사전
  `equippedCostumeId=X` **WHEN** `Equip("unowned_99")` **THEN** `CanEquip=true`
  이나 `costumeId ∉ ownedCostumeIds` → `equippedCostumeId`는 변화 없이 `X` 유지.

### 엣지 케이스

- **AC-EC-01 (리셋 시 ownedCostumeIds 보존 — Core Rule 5)** **GIVEN**
  `ownedCostumeIds={"beanie_01","scarf_01"}`, `equippedCostumeId`가 `"beanie_01"`
  이거나 이미 `null`인 두 경우 모두 **WHEN** 새 생애주기 리셋 **THEN**
  `equippedCostumeId'=null`로 무조건 대입되지만 `ownedCostumeIds`는 리셋 전후
  완전히 동일한 집합으로 유지(원소 변화 없음).
- **AC-EC-02 (시듦과 독립 — 스탯 위기 무시)** **GIVEN** `stage=HarvestReady`,
  `isWithered=true` **WHEN** `Equip(costumeId)`(보유 중인 값) **THEN** `CanEquip`
  은 `stage`만 참조하므로 `true` → 착장 정상 성공.
- **AC-EC-03 (보유 1개뿐이어도 자동 착장 없음)** **GIVEN**
  `ownedCostumeIds={"beanie_01"}`, `stage=HarvestReady`, `equippedCostumeId=null`
  **WHEN** 명시적 `Equip()` 호출 없이 임의 시간 경과 **THEN** `equippedCostumeId`
  는 여전히 `null`.
- **AC-EC-04 (착장 안정성 — 재확인 없이 유지)** **GIVEN** `Equip("scarf_01")`
  성공 직후 **WHEN** 이후 추가 `Equip()` 호출 없이 `G`가 계속 증가 **THEN**
  `equippedCostumeId`는 다음 리셋(AC-ST-04) 전까지 `"scarf_01"`로 불변 — 암묵적
  unequip이나 자동 초기화 없음(Unequip 자체가 존재하지 않음, Edge Cases 확정).
- **AC-EC-05 (가챠 중복 지급 — 멱등)** **GIVEN**
  `ownedCostumeIds={"beanie_01","scarf_01"}` **WHEN**
  `AddOwnedCostume("beanie_01")`(이미 보유) **THEN** `ownedCostumeIds`는 여전히
  동일 집합, `equippedCostumeId`는 변경되지 않는다.
- **AC-EC-06 (조기/중복 리셋 트리거) — 테스트 불가, Open Question 확정 전까지
  보류**: 이 Edge Case를 검증하려면 (a) 리셋을 트리거하는 정확한 메서드/이벤트명,
  (b) "진짜 새 Sprout 시작"과 "조기/중복 호출"을 구분하는 판정 조건이 필요하나
  이 GDD에 아직 정의되어 있지 않다 — 구체적 GIVEN/WHEN을 쓸 수 없다. Open
  Questions에 정식 등재하고 인터페이스 확정 즉시 이 자리를 채운다.

### 연동 계약 (Cross-System Interactions)

- **AC-INT-01 (게이트는 호출자 무관 — 매번 실시간 재조회)** **GIVEN** 임의의
  호출자(이름짓기&수확카드 UI 등) **WHEN** `Equip(costumeId)` 호출 **THEN**
  `CanEquip(stage)` 판정은 매번 호출 시점에 lifecycle의 `stage`를 새로 조회하며,
  호출자 신원에 따른 게이트 생략 경로가 없다 — `lifecycle-progression-system.md`
  AC-INT-04/05와 정합.
- **AC-INT-02 (스냅샷 선행 순서 계약 — Core Rule 6, BLOCKING) — 강제 메커니즘
  미확정, 테스트 불가**: `equippedCostumeId` 확정 상태를 이름짓기&수확카드가
  스냅샷으로 캡처하는 시점은 반드시 리셋 트리거(AC-ST-04)보다 **선행**해야 한다
  — 순서가 뒤집히면 스냅샷이 `null`을 기록해 Core Rule 6("과거 수확 개체의 착장
  기록 보존")이 조용히 깨진다. 이 순서를 강제하는 메커니즘이 현재 어느 GDD에도
  없다(qa-lead 발견, 2026-07-20) — **BLOCKING**: 이름짓기 & 수확 카드 시스템
  `/design-system` 진행 시 반드시 해결해야 하며, 해결 전까지 이 기준은 구체적
  GIVEN/WHEN을 쓸 수 없다(Open Questions 참고).
- **AC-INT-03 (가챠 인터페이스 — 범위 밖으로 명시적으로 남김)**:
  `AddOwnedCostume(costumeId)`의 시그니처(단일 vs 배치)·반환값·호출 시점은
  가챠/뽑기 시스템(Alpha) 미설계로 확정 불가 — 그 GDD 완료 시까지 테스트 대상에서
  제외(`lifecycle-progression-system.md`의 "성장 부스터 보너스 제외" 컨벤션과
  동일).
- **AC-INT-04 (세이브/로드 라운드트립 — 신규 세이브 의존성 검증)** **GIVEN**
  `ownedCostumeIds={"beanie_01","scarf_01"}`, `equippedCostumeId="scarf_01"`
  **WHEN** 세이브/영속성 시스템이 `Register("costume", {...})`로 직렬화한 뒤 앱을
  재시작해 다시 로드 **THEN** 로드 직후 `GetOwnedCostumes()`는 동일한 두 값을,
  `GetEquippedCostume()`는 `"scarf_01"`을 그대로 반환한다 — 저장 등록 스키마
  누락(이번 세션에서 발견·수정된 갭)으로 인한 리셋이 발생하지 않는다.

### 이 GDD 범위 밖으로 명시적으로 남긴 것 (테스트 불가 항목)

- **AC-EC-06** — 리셋 트리거를 실제로 호출하는 정확한 메서드/이벤트명이 미정.
- **AC-INT-02** — 스냅샷-리셋 순서를 강제하는 메커니즘 미확정(**BLOCKING**,
  이름짓기 & 수확 카드 설계 시 해결).
- **AC-INT-03** — 가챠/뽑기 시스템(Alpha) GDD 미설계.
- **렌더링 소유권(Visual/Audio §3)** — 이 GDD가 이미 자인한 공백. 소유자 확정 시
  그 시스템 GDD에 "Equip() 호출과 같은 프레임에 렌더러 갱신" 류의 AC-INT 추가
  필요.

## Open Questions

| 질문 | 배경 | 담당 | 해소 시점 |
|---|---|---|---|
| 렌더링 소유권 — 코스튬 부착을 실제로 그리는 책임자 미정 | `expression-animation-system.md` 전수 확인 결과 코스튬 부착을 자신의 Interactions/Downstream 어디에도 올리지 않음(확인된 공백, art-director 조사) — 이 GDD는 렌더링을 소유하지 않으므로(Overview 확정) 자체 블로킹은 아님 | art-director/unity-specialist | 이름짓기 & 수확 카드 시스템 `/design-system` 진행 시 |
| 스냅샷-리셋 순서 계약(AC-INT-02) | `equippedCostumeId` 확정 스냅샷 캡처가 새 생애주기 리셋(Core Rule 5)보다 반드시 선행해야 하나, 강제 메커니즘이 어느 GDD에도 없음(qa-lead 발견) — 순서가 뒤집히면 Core Rule 6이 조용히 깨짐 | game-designer/systems-designer | **BLOCKING** — 이름짓기 & 수확 카드 시스템 `/design-system` 진행 시 반드시 해결 |
| 리셋 트리거의 정확한 메서드/이벤트명(AC-EC-06) | Core Rule 5의 "리셋"을 실제로 호출하는 인터페이스 이름·시그니처가 미정 — 위 순서 계약 질문과 함께 해결될 가능성이 높음 | systems-designer | 이름짓기 & 수확 카드 시스템 `/design-system` 진행 시(위 항목과 연계) |
| 가챠 `AddOwnedCostume`의 정확한 계약(AC-INT-03) | 단일 vs 배치 지급, 반환값, 호출 시점 등이 가챠/뽑기 시스템(Alpha) 미설계로 확정 불가 | economy-designer/systems-designer | 가챠/뽑기 시스템 `/design-system` 진행 시 |
| 선택 패널 진입/퇴장 트랜지션의 정확한 연출(UI Requirements §5) | art-bible "그림책 페이지 넘김" 레이아웃을 인용만 했을 뿐, 정확한 지속시간·이징 등은 미확정 | ux-designer/art-director | 이름짓기 & 수확 카드 시스템 UX 스펙(`/ux-design`) 작성 시 |
| MVP 코스튬의 정확한 수(`N_OWNED_MVP`=1 또는 2)와 실제 실루엣 | Tuning Knobs는 안전범위 {1,2}만 확정 — 실제 어느 쪽인지, 어떤 실루엣 2종을 production할지는 아트 에셋 생산 일정에 달림(art-bible이 이미 "코스튬 부품 라이브러리의 정확한 실루엣 개수·생산 일정"을 열린 과제로 기록해둔 것과 동일 항목) | art-director | 아트 에셋 생산 일정 확정 시 |
