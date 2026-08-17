# Systems Index: GAMZAA — 포근한 감자 일기 (가제)

> **Status**: Approved (lean mode)
> **Created**: 2026-07-09
> **Last Updated**: 2026-07-09
> **Source Concept**: design/gdd/game-concept.md

---

## Overview

GAMZAA는 감자 한 알을 4일 생애주기 동안 아날로그 터치(창문 드래그·난로 롱프레스·물뿌리개)로
돌보는 방치형 힐링 모바일 게임이다. 기계적 범위는 크게 세 축으로 나뉜다 — ①**케어 루프**
(터치 인터랙션 → 감자 상태 → 날씨 → 표정 피드백), ②**안전망**(시듦 감지 → 광고 부활 →
온실막 보호 → 오프라인 진행 계산 — Pillar 3 "벌주지 않는 힐링"의 실제 구현체), ③**애착·수익
루프**(코스튬 → 수확 카드 → 감자 마을, 그리고 여기 맞물리는 부활/가챠/부스터 광고). 세이브·
날씨 타이머·오프라인 계산 같은 기반 시스템 없이는 이 게임이 "방치형"이라는 정체성 자체가
성립하지 않으므로, 이 셋은 스코프 티어와 무관하게 MVP 최우선 설계 대상이다.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | 터치 케어 인터랙션 | Gameplay | MVP | Designed (pending review) | [touch-care-interaction.md](touch-care-interaction.md) | 감자 상태 관리, 날씨 시스템 |
| 2 | 감자 상태 관리 | Core | MVP | Designed (pending review) | [potato-stat-system.md](potato-stat-system.md) | (none — Foundation) |
| 3 | 날씨 시스템 | Core | MVP | Designed (pending review) | [weather-system.md](weather-system.md) | 감자 상태 관리, 세이브/영속성 |
| 4 | 생애주기 진행 | Core | MVP | Designed (pending review) | [lifecycle-progression-system.md](lifecycle-progression-system.md) | 감자 상태 관리, 세이브/영속성 |
| 5 | 시듦/위기 & 회복 | Gameplay | MVP | Designed (pending review) | [wither-crisis-recovery-system.md](wither-crisis-recovery-system.md) | 감자 상태 관리, 날씨 시스템, 오프라인 진행 계산 |
| 6 | 온실막 보호 | Gameplay | MVP | Designed (pending review) | [greenhouse-shield-system.md](greenhouse-shield-system.md) | 감자 상태 관리, 광고 SDK 통합 계층, 오프라인 진행 계산 |
| 7 | 코스튬 시스템 (기본, 1~2종) | Progression | MVP | Designed (pending review) | [costume-system.md](costume-system.md) | 생애주기 진행, 세이브/영속성 |
| 8 | 이름짓기 & 수확 카드 | Progression | MVP | Not Started | — | 생애주기 진행, 코스튬 시스템, 표정/애니메이션 |
| 9 | 감자 마을 / 컬렉션 (inferred) | Progression | Vertical Slice | Not Started | — | 이름짓기 & 수확 카드 |
| 10 | 부활 광고 시스템 | Economy | MVP | Designed (pending review) | [revive-ad-system.md](revive-ad-system.md) | 시듦/위기 & 회복, 광고 SDK 통합 계층 |
| 11 | 가챠 / 뽑기 시스템 | Economy | Alpha | Not Started | — | 코스튬 시스템, 광고 SDK 통합 계층 |
| 12 | 성장 부스터(Rain) 시스템 | Economy | Alpha | Not Started | — | 생애주기 진행, 광고 SDK 통합 계층 |
| 13 | ASMR 적응형 오디오 | Audio | MVP | Designed (pending review) | [asmr-adaptive-audio.md](asmr-adaptive-audio.md) | 터치 케어 인터랙션, 날씨 시스템, 감자 상태 관리, 생애주기 진행 |
| 14 | 표정 / 애니메이션 시스템 (inferred) | Gameplay | MVP | Designed (pending review) | [expression-animation-system.md](expression-animation-system.md) | 감자 상태 관리, 날씨 시스템, 생애주기 진행 |
| 15 | 오프라인 진행 계산 (inferred) | Core | MVP | Designed (pending review) | [offline-progress-calculation.md](offline-progress-calculation.md) | 날씨 시스템, 세이브/영속성, 감자 상태 관리, 생애주기 진행 |
| 16 | 세이브 / 영속성 시스템 (inferred) | Persistence | MVP | Designed (pending review) | [save-persistence-system.md](save-persistence-system.md) | (none — Foundation) |
| 17 | 광고 SDK 통합 계층 (inferred) | Economy | MVP | Designed (pending review) | [ad-sdk-integration-layer.md](ad-sdk-integration-layer.md) | (none — Foundation) |
| 18 | 알림 시스템 (inferred) | UI | Vertical Slice | Not Started | — | 시듦/위기 & 회복, 오프라인 진행 계산 |
| 19 | 홈 화면 위젯 시스템 (inferred) | UI | Alpha | Not Started | — | 표정/애니메이션, 시듦/위기 & 회복 |
| 20 | 온보딩 (1회성 힌트, inferred) | Meta | Vertical Slice | Not Started | — | 터치 케어 인터랙션 |
| 21 | 설정 / 접근성 시스템 (inferred) | Meta | Alpha | Not Started | — | 알림 시스템, ASMR 오디오, 터치 케어 인터랙션 |
| 22 | 분석 / 텔레메트리 시스템 (inferred) | Meta | Alpha | Not Started | — | (none — Foundation) |

---

## Categories

| Category | Description | 이 프로젝트의 예 |
|----------|-------------|-----------------|
| **Core** | 모든 것이 의존하는 기반 시스템 | 감자 상태 관리, 날씨 시스템, 생애주기 진행, 오프라인 진행 계산 |
| **Gameplay** | 게임을 재밌게 만드는 시스템 | 터치 케어 인터랙션, 시듦/위기&회복, 온실막 보호, 표정/애니메이션 |
| **Progression** | 플레이어가 시간에 따라 성장하는 방식 | 코스튬, 이름짓기&수확카드, 감자 마을 |
| **Economy** | 자원 생성·소비 | 부활 광고, 가챠, 성장 부스터, 광고 SDK 통합 |
| **Persistence** | 저장 상태와 연속성 | 세이브/영속성 |
| **UI** | 플레이어 대상 정보 표시 | 알림, 홈 화면 위젯 |
| **Audio** | 사운드·음악 | ASMR 적응형 오디오 |
| **Meta** | 코어 루프 바깥 시스템 | 온보딩, 설정/접근성, 분석/텔레메트리 |

> Narrative 카테고리는 제외 — GDD "Narrative: N/A" (명시적 서사 아크 없음).

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | 코어 루프가 성립하려면 반드시 필요. 이게 없으면 "재밌는가"조차 검증 불가 | 첫 플레이 가능 프로토타입 | 최우선 설계 |
| **Vertical Slice** | 완결된 한 화면(리텐션 루프)을 보여주는 데 필요 | 버티컬 슬라이스/데모 | 두 번째 설계 |
| **Alpha** | 모든 기능이 러프하게라도 존재 | 알파 마일스톤 | 세 번째 설계 |
| **Full Vision** | 폴리시·엣지케이스 — 신규 시스템이 아니라 기존 시스템의 콘텐츠 확장(코스튬 30종, 라이브옵스 등)으로 처리 | 베타/릴리스 | 필요 시 |

---

## Dependency Map

### Foundation Layer (의존성 없음)

1. **감자 상태 관리** — 습도/온도/수분 및 온실막 보호 상태(`shieldActiveUntil`)를 담는 핵심 데이터 모델. 모든 상위 시스템의 기반
2. **세이브/영속성 시스템** — 감자 상태·날씨 타이머·컬렉션을 앱 재시작 후에도 유지
3. **광고 SDK 통합 계층** — 부활/가챠/부스터 광고 3종 모두가 의존하는 기술 기반
4. **분석/텔레메트리 시스템** — 다른 시스템의 이벤트를 관측하는 인프라, 자체 게임 데이터 의존 없음

### Core Layer (Foundation에 의존)

1. **날씨 시스템** — depends on: 감자 상태 관리, 세이브/영속성
2. **생애주기 진행** — depends on: 감자 상태 관리, 세이브/영속성
3. **오프라인 진행 계산** — depends on: 날씨 시스템, 세이브/영속성, 감자 상태 관리, 생애주기 진행(lifecycle-progression-system.md 확정, 2026-07-15 — 오프라인 갭을 시듦 구간으로 분할해 생애주기 진행의 `Tick()`도 함께 호출해야 함)

### Feature Layer (Core에 의존)

1. **터치 케어 인터랙션** — depends on: 감자 상태 관리, 날씨 시스템
2. **표정/애니메이션 시스템** — depends on: 감자 상태 관리, 날씨 시스템, 생애주기 진행
3. **시듦/위기 & 회복** — depends on: 감자 상태 관리, 날씨 시스템, 오프라인 진행 계산
4. **온실막 보호** — depends on: 감자 상태 관리(Hard — `ActivateShield` 호출 + `isWithered`/`isShielded` 조회), 광고 SDK 통합 계층(Hard — 신규 `shield` adType), 오프라인 진행 계산(Soft — `OnOfflineProgressApplied` 게이팅 타이밍). 시듦/위기 & 회복과는 **Sibling**(공유 `isWithered`로 조정, 직접 의존 아님). 보호 상태 필드는 감자 상태 관리에 귀속 — 순환 해소(greenhouse-shield-system.md 확정, 2026-07-18. 능동 설치가 실제 소유 범위)
5. **부활 광고 시스템** — depends on: 시듦/위기 & 회복, 광고 SDK 통합 계층
6. **코스튬 시스템** — depends on: 생애주기 진행, 세이브/영속성 시스템(`Register("costume", {ownedCostumeIds, equippedCostumeId})`, costume-system.md 확정, 2026-07-20 — 이전까지 누락돼 있던 실제 갭이었음)
7. **성장 부스터(Rain) 시스템** — depends on: 생애주기 진행, 광고 SDK 통합 계층
8. **가챠/뽑기 시스템** — depends on: 코스튬 시스템, 광고 SDK 통합 계층
9. **ASMR 적응형 오디오** — depends on: 터치 케어 인터랙션(Hard), 날씨 시스템(Soft), 감자 상태 관리(Hard — `OnWitherEntered`/`OnRecovered` 훅 소유자, `asmr-adaptive-audio.md` 확정, 2026-07-21), 생애주기 진행(Soft — `OnStageAdvanced` 훅)

### Presentation Layer (Feature에 의존)

1. **이름짓기 & 수확 카드** — depends on: 생애주기 진행, 코스튬 시스템, 표정/애니메이션(Harvest-proud 퇴장 연출 트리거, expression-animation-system.md 확정, 2026-07-18)
2. **감자 마을/컬렉션** — depends on: 이름짓기 & 수확 카드
3. **알림 시스템** — depends on: 시듦/위기 & 회복, 오프라인 진행 계산
4. **홈 화면 위젯 시스템** — depends on: 표정/애니메이션, 시듦/위기 & 회복

### Polish Layer (전체에 의존)

1. **온보딩(1회성 힌트)** — depends on: 터치 케어 인터랙션
2. **설정/접근성 시스템** — depends on: 알림 시스템, ASMR 오디오, 터치 케어 인터랙션

---

## Recommended Design Order

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | 감자 상태 관리 | MVP | Foundation | systems-designer | M |
| 2 | 세이브/영속성 시스템 | MVP | Foundation | systems-designer | M |
| 3 | 광고 SDK 통합 계층 | MVP | Foundation | systems-designer | S |
| 4 | 날씨 시스템 | MVP | Core | systems-designer | L |
| 5 | 생애주기 진행 | MVP | Core | systems-designer | M |
| 6 | 오프라인 진행 계산 | MVP | Core | systems-designer | M |
| 7 | 터치 케어 인터랙션 | MVP | Feature | game-designer | L |
| 8 | 표정/애니메이션 시스템 | MVP | Feature | systems-designer | M |
| 9 | 시듦/위기 & 회복 | MVP | Feature | game-designer | M |
| 10 | 온실막 보호 | MVP | Feature | systems-designer | S |
| 11 | 부활 광고 시스템 | MVP | Feature | economy-designer | S |
| 12 | 코스튬 시스템 (기본) | MVP | Feature | game-designer | S |
| 13 | ASMR 적응형 오디오 | MVP | Feature | sound-designer | M |
| 14 | 이름짓기 & 수확 카드 | MVP | Presentation | game-designer | S |
| 15 | 감자 마을/컬렉션 | Vertical Slice | Presentation | game-designer | M |
| 16 | 알림 시스템 | Vertical Slice | Presentation | ux-designer | S |
| 17 | 온보딩(1회성 힌트) | Vertical Slice | Polish | ux-designer | S |
| 18 | 가챠/뽑기 시스템 | Alpha | Feature | economy-designer | M |
| 19 | 성장 부스터(Rain) 시스템 | Alpha | Feature | economy-designer | S |
| 20 | 홈 화면 위젯 시스템 | Alpha | Presentation | ui-programmer | S |
| 21 | 설정/접근성 시스템 | Alpha | Polish | accessibility-specialist | S |
| 22 | 분석/텔레메트리 시스템 | Alpha | Foundation | analytics-engineer | S |

---

## Circular Dependencies

- **오프라인 진행 계산 ↔ 온실막 보호 ↔ 시듦/위기 & 회복**: 오프라인 계산은 온실막 활성
  여부를 알아야 감쇠를 계산할 수 있고, 온실막은 회복이 트리거해야 켜지며, 회복은 오프라인
  계산의 감쇠 결과로 위기를 판정한다 — 3중 순환.
  **해결책(사용자 확정, 2026-07-09)**: "온실막 보호" 상태를 별도 시스템이 소유하지 않고
  **감자 상태 관리(Foundation)의 필드**(예: `shieldActiveUntil` 타임스탬프)로 승격한다.
  오프라인 진행 계산은 이제 온실막 보호 시스템이 아니라 Foundation 데이터를 직접 읽고,
  온실막 보호 시스템은 **능동 설치 시**(플레이어가 광고를 보고 미리 온실막을 설치할 때)
  그 필드에 `ActivateShield`로 값을 쓰는 역할을 맡아 순환이 끊긴다 — 회복 경로가 부여하는
  온실막은 시듦/위기 & 회복이 소유하므로 온실막 보호의 실제 소유 범위는 "능동(사전) 설치
  경로"다(`greenhouse-shield-system.md` 확정, 2026-07-18).

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| 터치 케어 인터랙션 | Design | 아날로그 손맛이 실제로 "위안"을 주는지 프로토타입으로 검증되지 않음 — 이 게임 전체의 핵심 미지수 | `/prototype`을 이 시스템 GDD보다 먼저(또는 병행) 실행해 학습을 GDD에 반영 |
| 날씨 시스템 | Technical | 24h 백그라운드 타이머 — 앱 종료·타임존 변경·기기 시계 조작 악용 방지가 까다로움(솔로 첫 게임 기준) | 서버 시간 기준 검증 또는 클램프 로직을 GDD 단계에서 명세, Unity 6.5 레퍼런스 재확인 |
| 오프라인 진행 계산 | Technical | 장기 미접속·기기 변경 등 엣지케이스 누락 위험 | GDD Edge Cases 섹션에서 철저히 명세, 유닛 테스트 필수(코딩 표준상 Logic 스토리는 BLOCKING) |
| 부활 광고 시스템 / 광고 SDK 통합 계층 | Technical / Scope | 광고 보상 무결성·치팅 방지, 수익의 50%가 여기 의존(기획서) | 검증된 SDK 채택, 서버 측 보상 검증 고려 |
| 코스튬 시스템 | Scope | 최종 30종까지 확장 시 솔로 개발 물량 부담 | 아트 바이블이 이미 부품 라이브러리 방식(실루엣×색램프)으로 완화 — GDD도 이 구조를 그대로 반영 |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 22 |
| Design docs started | 13 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 13/14 |
| Vertical Slice systems designed | 0/3 |

---

## Next Steps

- [ ] Design MVP-tier systems first, in the order above (`/design-system [system-name]` or `/map-systems next`)
- [ ] 최우선 권장: `/prototype 터치 케어 인터랙션` — 최대 미지수를 GDD 작성 전에 검증
- [ ] Run `/design-review` on each completed GDD
- [ ] Run `/gate-check pre-production` when MVP systems are designed
- [ ] Validate the highest-risk systems with `/vertical-slice` before committing to Production
