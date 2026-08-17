# 세이브 / 영속성 시스템 (Save/Persistence System)

> **Status**: In Design
> **Author**: user + systems-designer
> **Last Updated**: 2026-07-10
> **Implements Pillar**: Pillar 2 (지켜주고 싶은 마음), Pillar 3 (벌주지 않는 힐링)

## Overview

세이브/영속성 시스템은 게임의 다른 모든 시스템이 자신의 상태를 "무엇을 저장할지"는
몰라도 안전하게 디스크에 기록하고 다시 읽어올 수 있게 해주는 범용 인프라 계층이다.
이 시스템 자체는 감자·날씨·코스튬이 무엇인지 전혀 알지 못한다 — 대신 다른 시스템들이
"이 이름으로 이 데이터를 저장해줘"라고 등록하면, 앱이 백그라운드로 전환되거나 종료될
때 그것들을 한꺼번에 직렬화하고, 다음 실행 시 복원한다. 이 시스템이 없다면 감자는
매번 앱을 열 때마다 처음부터 다시 심어야 하고, 애써 수확한 감자 마을도 매번 사라진다
— 방치형 게임이라는 장르 자체가 성립하지 않는다.

## Player Fantasy

이 시스템 자체에는 고유한 플레이어 판타지가 없다 — 플레이어는 세이브 파일이나 직렬화
과정을 결코 의식하지 않는다. 대신 이 시스템은 Pillar 2(지켜주고 싶은 마음)와 Pillar 3
(벌주지 않는 힐링)이 시간을 뛰어넘어 성립하기 위한 전제조건이다: "내가 이름 붙인 감자가
내일도 거기 있을 것"이라는 무의식적 신뢰, "어제 방치했어도 오늘 다시 돌아오면 그대로
이어진다"는 안심은 모두 이 시스템이 조용히, 실패 없이 작동한다는 가정 위에 서 있다. 이
시스템이 잘 만들어졌다는 증거는 역설적으로 "플레이어가 세이브라는 것의 존재 자체를 한
번도 떠올리지 않는 것"이다 — 반대로 플레이어가 이 시스템을 의식하는 유일한 순간은
무언가 잘못됐을 때뿐이며, 그것은 이 게임에서 가장 심각한 신뢰 붕괴다.

> *(`creative-director` 미상담 — lean 모드. 프로덕션 전 수동 검토 권장.)*

## Detailed Design

> *(전문 에이전트 미상담 — lean 모드. Section D·H만 lean에서도 상담 필수.)*

### Core Rules

1. **등록 기반 직렬화**: 다른 시스템은 고유 키(문자열)와 함께 자신의 저장 가능한 데이터를
   이 시스템에 등록한다. 이 시스템은 등록된 데이터의 실제 의미를 전혀 모른 채 키-값
   저장소처럼만 다룬다.
2. **저장 트리거**: 앱이 백그라운드로 전환될 때(`OnApplicationPause`), 명시적 저장 요청
   시, 또는 중요 상태 변화 직후 자동 저장.
3. **로드 트리거**: 앱 최초 실행 또는 재개 시, 등록된 모든 시스템에 저장 데이터를 배포.
4. **단일 세이브 슬롯**: MVP 범위에서는 슬롯 하나만 존재(클라우드 동기화·다중 프로필
   없음 — 기획서에 해당 요구사항 없음).
5. **원자적 쓰기**: 저장 도중 크래시로 파일이 손상되지 않도록, 임시 파일에 먼저 쓰고
   원자적으로 교체(write-to-temp + rename)한다.

### States and Transitions

| 상태 | 진입 조건 | 이탈 조건 |
|---|---|---|
| **Uninitialized** | 앱 프로세스 시작 직후 | 로드 시작 → Loading |
| **Loading** | 로드 트리거 발생 | 성공 → Ready / 손상 발견 → LoadFailed |
| **Ready** | 로드 완료, 정상 게임플레이 | 저장 트리거 → Saving(비차단, Ready와 병행 가능) |
| **Saving** | 저장 트리거 발생 | 저장 완료(성공/실패 무관) → 항상 Ready로 복귀(게임플레이는 차단하지 않음) |
| **LoadFailed** | 세이브 파일 손상·파싱 실패 | 복구 절차(Edge Cases) 완료 → **항상 Ready로 전이**(`.bak` 복구 성공이든 이중 손상 후 기본값 초기화든 동일) |

### Interactions with Other Systems

이 시스템은 아무것도 의존하지 않는 Foundation이다. 다른 시스템들이 이 시스템의 API
(`Register`/`RequestSave`/데이터 배포)를 호출한다.

| 시스템 | 관계 | 인터페이스(잠정) |
|---|---|---|
| 감자 상태 관리 | 이 시스템에 등록(호출자) | `Register("potato_stats", {moisture, humidity, temperature, neglectMeter})` — 4개 수치 필드 묶음(일반 등록). `RegisterUtcTimestamp("potato_lastUpdatedUtc", ...)`와 `RegisterUtcTimestamp("potato_shieldActiveUntil", ...)` — 두 UTC 값 모두 Formulas §5의 유효성 검사 게이트를 통과(potato-stat-system.md 확정, 2026-07-18) |
| 날씨 시스템 | 이 시스템에 등록(호출자) | `Register("weather_schedule_seed", {seed, scheduleStartUtc, cutoverUtc?})` — 날씨 자체가 아니라 재계산 기준점만 저장(weather-system.md 확정, 2026-07-12) |
| 생애주기 진행 | 이 시스템에 등록(호출자) | `Register("lifecycle", {plantedUtc, growthHoursAccumulated, lastTickUtc})`(lifecycle-progression-system.md 확정, 2026-07-15). `lastTickUtc`는 `RegisterUtcTimestamp`로 등록 |
| 코스튬 시스템 | 이 시스템에 등록(호출자) | `Register("costume", {ownedCostumeIds, equippedCostumeId})` — 두 필드 모두 UTC 타임스탬프가 아니므로 일반 `Register()`(`RegisterUtcTimestamp` 아님, §5 유효성 검사 미적용). `equippedCostumeId`는 nullable ID(costume-system.md 확정, 2026-07-20) |
| 오프라인 진행 계산 | 이 시스템을 통해 로드된 `lastUpdatedUtc` 등을 간접 소비 | 직접 등록자는 아님, Ready 상태 이후에만 정확한 값 보장 |

## Formulas

> *(`systems-designer` 상담 완료 — lean 모드에서도 Formulas는 상담 필수 섹션.)*

이 시스템의 포뮬러들은 게임플레이 밸런스가 아니라 **생존성(survivability)**을 위한 것이다.
Pillar 3(벌주지 않는 힐링) 관점에서 세이브 유실은 이 게임이 낼 수 있는 최악의 버그이므로,
아래 모든 상수는 성능이나 디스크 I/O 절약보다 항상 "절대 잃지 않는다" 쪽으로 치우쳐
설계됐다. 모바일 OS는 `OnApplicationPause`/`OnApplicationQuit`을 호출하지 않고도 언제든
프로세스를 강제 종료할 수 있다는 전제(특히 저사양 Android)가 1번과 3번 포뮬러의 핵심
근거다.

### 1. 방어적 주기 저장 (OS 강제 종료 대비)

**규칙**: `OnApplicationPause`/`OnApplicationQuit` 훅에만 의존하지 않는다 — 앱이 포그라운드에
떠 있는 동안에도 일정 주기로 "저장되지 않은 변경이 있는가"를 확인해, 있다면 훅 발생 여부와
무관하게 강제로 즉시 기록한다. 이 검사는 디바운스(§2)를 우회한다.

`ShouldFlush(t) = isDirty ∧ (t - t_lastWriteSuccess) ≥ T_AUTOSAVE_SEC`, 매 `T_POLL_SEC`마다 평가

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 미저장 변경 존재 여부 | `isDirty` | bool | {0,1} | 마지막 성공적 쓰기 이후 `RequestSave()`가 1회 이상 호출됐는가. 쓰기 성공 시 즉시 0으로 리셋 |
| 현재 시각 | `t` | float (UTC epoch sec) | ≥0 | 벽시계 UTC 시각(`DateTime.UtcNow` 기반, 프레임 시각 아님 — 앱 일시정지 중에도 정확) |
| 마지막 성공 쓰기 시각 | `t_lastWriteSuccess` | float (UTC epoch sec) | ≥0 | 원인(방어적/디바운스 완료/pause/quit) 무관하게 가장 최근 성공한 원자적 쓰기 시각 |
| 방어적 저장 주기 | `T_AUTOSAVE_SEC` | float (const) | **30**(사용자 확정) | 이 시간 이상 미저장 변경이 방치되면 강제 flush |
| 폴링 간격 | `T_POLL_SEC` | float (const) | **5** (튜닝 범위 1–10) | 검사 자체의 빈도 — 트리거 임계값이 아니라 해상도(granularity) |
| 결과 | `ShouldFlush` | bool | {0,1} | true면 디바운스 우회하고 즉시 쓰기 실행 |

**Output Range:** 불리언 게이트. 정상 플레이에서 마지막 성공 저장 이후 최악의 데이터 유실
윈도우는 `T_AUTOSAVE_SEC + T_POLL_SEC ≈ 35초`로 상한이 걸린다. `isDirty=false`(변경 없음)일
때는 절대 쓰기가 발생하지 않으므로, 방치형 게임 특성상 앱을 켜놓고 아무 조작도 안 하는
시간이 길어도 불필요한 디스크 I/O가 없다.

**Example:** 마지막 성공 저장 `t_lastWriteSuccess = 1,000,000s`. 플레이어가 `t=1,000,010s`에
물뿌리개 제스처 완료 → `RequestSave()` → `isDirty=true`. `T_POLL_SEC=5`마다 검사:
`t=1,000,015`(경과 15s, false), `t=1,000,020`(20s, false), ..., `t=1,000,030`(경과
30s, **30≥30 → true**) → 강제 flush 실행, `t_lastWriteSuccess=1,000,030`으로 갱신. 만약
`t=1,000,029.9`에 OS가 프로세스를 예고 없이 강제 종료했다면, 마지막 성공 저장 이후 최대
약 30초 분량의 변경만 유실된다 — `TOUCH_Δ=30`(레지스트리) 기준으로 실질적으로는 제스처
1회 정도다.

*Design note*: 왜 이벤트 카운트 트리거를 별도로 두지 않았는가 — 이 시스템에 등록되는
시스템 수(감자 상태·날씨·생애주기, 약 3~4개)가 적어 "짧은 시간에 몰린 변경 개수"로
트리거할 실익이 없다. 대신 §2의 디바운스가 자체 상한(`T_MAX_COALESCE_SEC`)을 갖고 있어
요청이 몰릴 때의 지연 상한은 이미 별도로 보장된다. 즉 "시간 기반"만으로 충분하다고
판단했다 — 필요 이상으로 두 메커니즘을 겹치게 만들지 않기 위함.

---

### 2. 세이브 요청 디바운스 / 코얼레싱

**규칙**: 여러 시스템이 짧은 시간 안에 연속으로 `RequestSave()`를 호출해도, 실제 디스크
쓰기는 "요청이 잠잠해졌을 때" 또는 "너무 오래 미뤄졌을 때" 중 먼저 오는 조건에 1회만
실행한다.

`ExecuteWrite(t) = [(t - t_lastRequestTime) ≥ T_DEBOUNCE_SEC] ∨ [(t - t_firstPendingRequestTime) ≥ T_MAX_COALESCE_SEC]`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 마지막 요청 시각 | `t_lastRequestTime` | float (UTC epoch sec) | ≥0 | 가장 최근 `RequestSave()` 호출 시각. `isDirty`·`t_firstPendingRequestTime`과 동일하게, 쓰기 성공 시 함께 리셋된다(명시적 확정) |
| 대기 구간 최초 요청 시각 | `t_firstPendingRequestTime` | float (UTC epoch sec) | ≥0 | 마지막 성공 쓰기 이후 첫 `RequestSave()` 호출 시각 — 쓰기 성공 시 초기화됨 |
| 디바운스 정적 구간 | `T_DEBOUNCE_SEC` | float (const) | **2.0** | 이만큼 새 요청이 없으면 "요청 폭주가 끝났다"고 판단, 즉시 기록 |
| 코얼레싱 상한 | `T_MAX_COALESCE_SEC` | float (const) | **8.0** | 요청이 끊이지 않아도 이 시간이 지나면 강제로 기록(기아 상태 방지) |
| 결과 | `ExecuteWrite` | bool | {0,1} | true면 대기 중인 모든 등록 데이터를 한 번에 직렬화·기록 |

**Output Range:** 실효 쓰기 지연은 항상 `[T_DEBOUNCE_SEC, T_MAX_COALESCE_SEC] = [2s, 8s]`
구간 안에 있다 — 고립된 단발 요청이면 요청 후 2초 안에, 요청이 끊임없이 몰려도 최초
요청 후 8초 안에는 반드시 1회 기록된다. `T_MAX_COALESCE_SEC(8s) < T_AUTOSAVE_SEC(30s)`로
설계해, 이 메커니즘이 §1의 백스톱보다 항상 먼저 개입한다.

**Example (정상 코얼레싱):** `t=0.0`에 물뿌리개 완료 → `RequestSave()` →
`t_firstPending=0.0, t_lastRequest=0.0`. `t=1.0`에 창문 드래그도 완료 → 다시
`RequestSave()` → `t_lastRequest=1.0`(`t_firstPending`은 그대로 0.0). 이후 요청 없음.
`t=3.0` 검사: `3.0-1.0=2.0 ≥ 2.0` → true → 이 시점에 1회만 기록되어, 두 제스처가
디스크 쓰기 1번으로 합쳐진다.

**Example (기아 방지 상한 작동):** 어떤 시스템이 버그로 1.5초 간격으로 `RequestSave()`를
계속 호출한다고 가정(매 호출이 정적 구간 타이머를 리셋시킴). `t_firstPending=0.0`은
그대로 유지된다. `t=8.0` 검사: `8.0-0.0=8.0 ≥ 8.0` → true → 요청이 끊이지 않아도
강제로 기록 실행. 디바운스 로직 하나만 있었다면(상한 없이) 요청이 계속되는 한 영원히
기록되지 않았을 것이다.

*Design note*: `OnApplicationPause`/`OnApplicationQuit` 및 §1의 방어적 저장은 이 디바운스를
완전히 우회하고 즉시 기록한다 — "곧 죽을 수도 있는 순간"에 2초조차 기다리는 건 Pillar 3
관점에서 감수할 수 없는 리스크이기 때문이다.

---

### 3. 쓰기 실패 재시도 백오프

**규칙**: 원자적 쓰기(temp 파일 쓰기 또는 rename)가 실패하면(디스크 풀, 권한 오류, 일시적
파일 잠금 등) 지수 백오프로 재시도한다. 컨텍스트에 따라 두 프로파일 중 하나를 쓴다 —
`OnApplicationPause`/`OnApplicationQuit`에서 강제 발동된 쓰기는 OS가 부여하는 짧은 종료
유예 시간 안에 끝나야 하므로 훨씬 촘촘하고 짧은 예산을, 그 외(§1 방어적 저장, §2 디바운스
완료 후 저장)는 여유 있는 예산을 쓴다.

`delay(n) = min(T_BASE · Mⁿ⁻¹, T_CAP)`, `n = 1 … N_MAX`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 재시도 순번 | `n` | int | 1–`N_MAX` | 몇 번째 재시도인지 |
| 기본 지연 | `T_BASE` | float (const) | 프로파일별 상이 | 첫 재시도 전 대기 시간 |
| 배수 | `M` | float (const) | **2** | 매 재시도마다 대기 시간 2배 |
| 지연 상한 | `T_CAP` | float (const) | 프로파일별 상이 | 대기 시간 최댓값 |
| 최대 재시도 횟수 | `N_MAX` | int (const) | 프로파일별 상이 | 이 횟수를 넘기면 포기하고 실패 처리 |
| 결과 | `delay(n)` | float (sec) | 0–`T_CAP` | n번째 재시도 전 대기 시간 |

| 프로파일 | `T_BASE` | `T_CAP` | `N_MAX` | 총 시도 횟수(초기+재시도) | 최악 총 대기시간 |
|---|---|---|---|---|---|
| **Foreground**(§1 방어적 저장, §2 디바운스 완료) | 0.5s | 4.0s | 4 | 5회 | 0.5+1.0+2.0+4.0 = **7.5s** |
| **Pause/Quit**(강제 flush) | 0.1s | 0.3s | 2 | 3회 | 0.1+0.2 = **0.3s** |

**Output Range:** 재시도 총 소요 시간은 Foreground에서 최대 7.5초, Pause/Quit에서 최대
0.3초로 상한이 걸린다. 모든 재시도가 실패하면: (a) 원자적 쓰기 원칙(temp 파일 쓰기 →
rename) 덕분에 rename 전 단계에서 실패한 것이므로 기존에 있던 유효한 세이브 파일은
손대지 않은 채 그대로 남는다 — 최악의 경우도 "새 변경분만 기록 못함"이지 "기존 세이브가
망가짐"은 아니다. (b) `isDirty`는 여전히 true로 유지되어, §1의 다음 방어적 저장 주기가
자동으로 재시도한다 — 플레이어가 아무것도 안 해도 자가 치유됨. (c) 1회성 `OnSaveFailed`
이벤트를 내부적으로 발행해 상위 레이어(UI/로깅)가 선택적으로 반응할 수 있게 한다 — 구체적
UX 처리는 Edge Cases 섹션에서 별도로 정의.

**Example:** Foreground 저장 중 백신 프로그램의 일시적 파일 잠금으로 초기 시도 실패.
`delay(1)=0.5s` 대기 후 재시도 → 또 실패(잠금 아직 안 풀림) → `delay(2)=1.0s` 대기 후
재시도 → 성공. 총 경과 1.5초, 4회 가용 재시도 중 2회만 사용, `isDirty=false`로 리셋,
`t_lastWriteSuccess` 갱신.

*Design note*: 지터(jitter)를 넣지 않았다 — 이 시스템은 로컬 파일 I/O만 다루고 여러
클라이언트가 동시에 같은 파일을 두고 경쟁하는 네트워크형 상황(thundering herd)이 아니므로
불필요한 복잡도로 판단.

---

### 4. 세이브 파일 무결성 검증

**규칙**: 쓰기 시점에 페이로드의 해시를 같은 파일 안에 함께 기록하고, 로드 시 다시 계산해
비교한다. 해시를 별도 사이드카 파일로 분리하지 않는 이유는, 그러면 원자적 교체가 파일
2개에 걸쳐 일어나야 해서 그 자체가 또 다른 크래시 취약점이 되기 때문이다 — 이미 잠긴
"write-to-temp + rename" 원칙과 정합성을 유지하려면 체크섬도 같은 파일, 같은 rename
안에 있어야 한다.

**파일 레이아웃(바이트, little-endian):**
```
[0..3]    MAGIC            (4B)  — 포맷 시그니처, 예: 0x475A4131 ("GZA1")
[4..11]   saveFileCreatedUtc (8B, int64 unix epoch sec) — 최초 세이브 생성 시 1회만 기록, 이후 매 쓰기마다 그대로 복사
[12..15]  payloadLength L  (4B, uint32)
[16..31]  payloadHash H    (16B, MD5)
[32..32+L-1] payload       (L bytes, UTF8 직렬화된 등록 key-value 데이터)
```

`IsIntegrityValid = readSucceeded ∧ (magic == MAGIC) ∧ (len(P_loaded) == L_stored) ∧ (MD5(P_loaded) == H_stored)`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 읽기 성공 여부 | `readSucceeded` | bool | {0,1} | 파일이 헤더 크기(32B)보다 짧거나 예외 발생 시 false |
| 시그니처 | `MAGIC` | uint32 (const) | 고정값 | 이 시스템이 만든 파일인지 최소 검증 |
| 로드된 페이로드 | `P_loaded` | bytes | 가변 | 파일에서 실제로 읽힌 payload 구간 |
| 저장된 길이 | `L_stored` | uint32 | ≥0 | 헤더에 기록된 payload 길이 |
| 저장된 해시 | `H_stored` | 16 bytes | — | 쓰기 시점에 계산된 MD5(payload) |
| 결과 | `IsIntegrityValid` | bool | {0,1} | false면 파일을 신뢰하지 않고 손상 처리 |

**Output Range:** 불리언 게이트. `false`면 States and Transitions 표에 이미 정의된
`LoadFailed` 상태로 전이한다 — 구체적 복구 전략(백업 롤백 vs. 신규 세이브)은 Edge Cases
섹션에서 정의할 항목으로 남긴다. MD5 128비트 해시 충돌로 손상을 놓칠 확률은 사실상 0
(약 1/2¹²⁸)이므로, "가벼운 무결성 검사"로 충분하며 별도 크립토 라이브러리 의존성 없이
.NET BCL(`System.Security.Cryptography.MD5`)만으로 구현 가능 — Unity Mono/IL2CPP 양쪽에서
항상 사용 가능.

**Example:** payload가 512바이트 JSON 문자열. 쓰기 시 `H_stored = MD5(P) = "9e10...a419"`
(16바이트 해시, 표기상 축약). 로드 시 정확히 512바이트를 다시 읽어
`MD5(P_loaded)`를 계산했더니 저장된 해시와 정확히 일치 → `IsIntegrityValid=true`. 만약
저장 매체의 드문 비트 플립으로 1바이트가 변형됐다면 재계산된 해시는 거의 확실히
불일치하고, `LoadFailed`로 전이한다.

**`.bak` 이중 원자적 쓰기 시퀀스(정밀화, qa-lead 검토 반영)**: Edge Cases의 롤링 백업이
그 자체로 크래시에 안전하려면, `.bak` 갱신도 주 파일과 동일한 원자적 원칙을 따라야
한다 — 단순 raw copy로 하면 복사 도중 죽었을 때 `.bak`이 조용히 손상될 수 있기 때문이다.
쓰기 사이클은 다음 순서로 진행한다:

1. `temp2`에 (현재 주 파일의 기존 유효 payload를) 기록 → `temp2 → .bak`으로 원자적 rename
2. `temp1`에 새 payload를 기록 → `temp1 → 주 파일`로 원자적 rename

두 rename 모두 §3의 재시도 프로파일(트리거 컨텍스트에 따라 Foreground 또는 Pause/Quit)
대상이다 — "temp 파일 쓰기 또는 rename"이라는 §3의 재시도 범위가 이 4단계(2개 temp
쓰기 + 2개 rename) 전체로 확장된다. `.bak`도 주 파일과 동일한 §4 레이아웃(MAGIC·해시
포함)을 쓰므로 `IsIntegrityValid`로 동일하게 검증 가능하다.

---

### 5. 로드 시 `lastUpdatedUtc` 유효성 검사

**규칙**: `potato-stat-system.md`가 노출하는 `lastUpdatedUtc`는 오프라인 진행 계산의
경과시간 산정에 직결되므로, 이 시스템은 값을 그대로 등록자에게 넘기기 전에 "말이 되는
과거 UTC 시각인가"를 먼저 검사한다. `potato-stat-system.md`는 이미 **런타임에** 시계가
과거로 조작된 경우를 `경과시간 = max(0, now - lastUpdatedUtc)`로 처리하도록 잠겨있다
(AC-EC-04) — 이 §5는 그보다 한 단계 앞서, **저장된 값 자체가 구조적으로 신뢰할 만한가**를
로드 시점에 걸러내는 게이트다. 두 검사는 서로 다른 레이어에서 서로 다른 문제(저장된 값의
타당성 vs. 런타임 시계 조작)를 잡으므로 충돌하지 않고 보완적이다.

`IsTimestampValid(t_loaded) = (t_loaded ≥ T_EPOCH_FLOOR) ∧ (t_loaded ≤ t_deviceNow + ε_FUTURE)`

`T_EPOCH_FLOOR = saveFileCreatedUtc` (§4 파일 헤더에서 읽음 — `lastUpdatedUtc`는 논리적으로
이 세이브 파일 자체가 생성된 시점보다 이를 수 없다)

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| 로드된 원값 | `t_loaded` | int64 (UTC epoch sec) | — | 등록 데이터에서 읽은 `lastUpdatedUtc` 원시값 |
| 하한(세이브 생성 시각) | `T_EPOCH_FLOOR` | int64 (UTC epoch sec) | §4의 `saveFileCreatedUtc` | 이 세이브 파일이 최초로 만들어진 시각 — 이보다 이전 값은 구조적으로 불가능 |
| 현재 기기 시각 | `t_deviceNow` | int64 (UTC epoch sec) | ≥0 | 로드 시점 기기 UTC 시계 |
| 미래 허용 오차 | `ε_FUTURE` | int (const, sec) | **300**(5분, 튜닝 범위 60–900) | 기기 시계 드리프트/타임존 재동기화 등 정상적 오차 허용폭 |
| 결과 | `IsTimestampValid` | bool | {0,1} | false면 이 값을 그대로 넘기지 않음 |

**Output Range:** 불리언 게이트. `false`일 때 이 시스템은 등록자에게 넘기는 값을
`t_deviceNow`(즉 "방금 갱신된 것처럼")로 대체한다 — Pillar 3 편향: 잘못된 값에서 큰 경과
시간을 추정하면 최악의 경우 이름 붙인 감자가 부당하게 시드는 결과로 이어질 수 있고, 이는
이 게임에서 가장 심각한 실패다. 반대로 경과 0으로 클램프하면 최악의 경우도 "이번 세션엔
오프라인 보상을 못 받음" 정도이며 이는 안전한 방향의 손해다. 이 대체값은
`potato-stat-system.md`의 기존 `max(0, now - lastUpdatedUtc)` 공식에 그대로 흘러 들어가
자연스럽게 경과시간=0을 산출한다 — 별도 폴백 로직을 새로 만들 필요 없이 기존 잠긴 공식과
합성된다. 최초 실행(세이브 파일 자체가 없는 경우)에는 이 검사가 실행되지 않고
`potato-stat-system.md`의 AC-EC-05(신규 세이브 초기값) 경로를 그대로 탄다.

**Example 1 (구조적으로 불가능한 값):** `saveFileCreatedUtc = 1,800,000,000`(최초 설치 시
고정). 로드된 `lastUpdatedUtc = 1,799,999,000`(세이브 파일이 생성되기 약 11.6일 *전*) →
`t_loaded < T_EPOCH_FLOOR` → `IsTimestampValid=false` → `t_deviceNow`로 대체.

**Example 2 (정상 오차 범위 내):** `t_deviceNow = 1,800,500,000`, `lastUpdatedUtc =
1,800,500,120`(2분 미래 — 경미한 기기 시계 드리프트). `120 ≤ ε_FUTURE(300)` →
`IsTimestampValid=true`, 원값 그대로 전달(이후 `potato-stat-system.md`의 자체
`max(0, now-lastUpdatedUtc)`가 어차피 0으로 클램프하므로 이중 안전망).

**Example 3 (명백한 시계 조작):** `lastUpdatedUtc = t_deviceNow + 3년`(오프라인 보상을
노리고 시계를 미래로 돌린 뒤 저장, 이후 시계를 되돌린 경우) → `ε_FUTURE(300s)`를 압도적으로
초과 → `IsTimestampValid=false` → `t_deviceNow`로 대체, 부당 이득 차단.

*Design note*: 이 검사는 `RegisterUtcTimestamp(key, getter, setter)`로 등록된 필드에만
적용된다(Detailed Design Interactions 표, 사용자 확정) — 도메인 의미는 몰라도 "이건
타임스탬프다"라는 타입 힌트만으로 §5를 수행할 수 있다.

## Edge Cases

> *(전문 에이전트 미상담 — lean 모드.)*

- **If 세이브 파일 로드 시 무결성 검증(§4) 실패라면**: 롤링 백업 1세대를 유지한다 — 새
  저장을 원자적으로 교체하기 직전, 기존 유효 파일을 `.bak`으로 보존한다. 주 파일이
  손상됐으면 `.bak`으로 로드를 재시도한다. `.bak`도 손상이면 기본값(신규 세이브와 동일
  경로)으로 시작하되, `SaveLostWarning` 플래그를 1회 노출한다 — 실제 UI 처리는 이
  Foundation 시스템의 범위 밖(향후 설정 시스템 몫).

- **If 세이브 파일 자체가 없다면(최초 실행)**: LoadFailed가 아니라 정상적인 신규 상태로
  처리한다(`potato-stat-system.md`의 AC-EC-05 경로를 그대로 탐).

- **If 쓰기가 진행 중(Saving)인데 새 `RequestSave()`가 들어온다면**: 동시 쓰기를 두 번
  실행하지 않는다 — §2의 디바운스 대기창만 연장되고, 현재 진행 중인 쓰기가 끝난 뒤 다음
  사이클에서 처리된다.

- **If 앱이 temp 파일 쓰기와 rename 사이에 강제 종료된다면**: 원자적 쓰기 원칙(§4)에 의해
  기존 유효 파일은 손대지 않은 채 그대로 남는다 — 별도 처리 불필요, 이미 설계로 보장됨.

- **If 디스크 공간 부족 등으로 재시도(§3)가 모두 실패한다면**: `isDirty`는 true로
  유지되고, §1의 방어적 저장이 다음 30초 주기에 자동 재시도한다 — 저장 공간이 실제로
  복구되면 플레이어 개입 없이 자가 치유된다. 연속 실패 횟수를 노출해 향후 설정/알림
  시스템이 경고를 표시할 수 있게 하되, 그 UI 자체는 이 GDD 범위 밖. **단, 이 자가치유는
  앱이 계속 실행 중일 때만 성립한다** — 아래 "인정된 좋은 위험" 항목 참고.

- **If `OnApplicationQuit` 컨텍스트에서 재시도(§3, Pause/Quit 프로파일)가 전부 실패하고
  그 직후 프로세스가 실제로 종료된다면(인정된 좋은 위험, Known Acceptable Risk)**: 메모리에만
  있던 `isDirty` 플래그와 미저장 변경분은 프로세스와 함께 소멸하며, 다음 실행 시점의
  §1 방어적 저장이 개입할 기회 자체가 없다 — 이 경로는 §1/§3이 전제하는 "최대 ~35초
  유실"의 자가치유 서사가 적용되지 않는 유일한 예외다. 이 실패는 "디스크 쓰기가 지속적으로
  실패하는 상태"와 "정확히 그 순간 앱이 종료되는 것"이 동시에 일어나야 하므로 실제
  발생 확률은 매우 낮다고 판단해, 추가 완화 장치(예: 별도 경량 dirty 신호를 디스크에
  선행 기록) 없이 **인정된 위험으로 남긴다** — 솔로 개발 첫 게임 스코프에서 이 희귀
  복합 실패에 대한 추가 구현 복잡도는 정당화되지 않는다는 판단(사용자 확정, 2026-07-10).
  향후 재검토 여지는 Open Questions에 기록.

- **If 등록된 시스템 하나의 직렬화 콜백이 예외를 던진다면**: 항목별로 격리한다(try/catch
  per entry) — 한 시스템의 버그가 나머지 시스템의 저장까지 막지 않는다. **실패한 항목의
  이번 사이클 값은 생략되지 않고, 직전 성공적으로 저장된 값을 그대로 유지한다**(payload는
  키 단위로 갱신되며, 실패한 키는 이번 쓰기에서 갱신 대상에서 제외될 뿐 삭제되지 않음) —
  Pillar 3(최대한 데이터를 보존)에 따라 "생략"보다 "직전 값 유지"를 명시적으로 택함.

- **If 세이브 포맷이 향후 버전에서 바뀐다면(마이그레이션)**: MVP 범위 밖으로 명시적으로
  제외한다 — 아직 출시 전이라 구버전 세이브가 존재하지 않음. Open Questions에 후속
  과제로 기록.

## Dependencies

**이 시스템이 의존하는 것**: 없음 — Foundation, 시스템 인덱스와 일치.

**이 시스템에 의존하는 것**:

| 시스템 | 의존 강도 | 인터페이스 |
|---|---|---|
| 날씨 시스템 | Hard | `Register`/`RegisterUtcTimestamp` |
| 생애주기 진행 | Hard | 상동 |
| 코스튬 시스템 | Hard | `Register("costume", {ownedCostumeIds, equippedCostumeId})`(costume-system.md 확정, 2026-07-20) |
| 오프라인 진행 계산 | Hard(간접) | 이 시스템이 로드한 `lastUpdatedUtc`를 소비 |
| 감자 상태 관리 | Hard(등록자 — Detailed Design 참고) | `RegisterUtcTimestamp("potato_lastUpdatedUtc", ...)` |

**소프트 계약 — 감자 상태 관리와의 데이터 정합성**: 시스템 인덱스 그래프는 두 Foundation
시스템을 서로 독립으로 그리지만, 실제로는 Formulas §5(이 GDD)와 `potato-stat-system.md`의
`경과시간 = max(0, now-lastUpdatedUtc)`(AC-EC-04) 공식이 같은 필드를 두 레이어에서 각각
검증하는 보완 관계다 — 저장된 값 자체의 구조적 타당성(이 GDD)과 런타임 시계 조작 방지
(감자 상태 관리)를 각각 담당한다. 이 관계는 인덱스의 방향성 화살표로는 표현되지 않지만,
두 GDD 모두 서로를 인용해야 정합성이 유지된다.

## Tuning Knobs

| 노브 | 현재값 | 권장 범위 | 너무 낮으면 | 너무 높으면 |
|---|---|---|---|---|
| `T_AUTOSAVE_SEC` | 30 | 15~120 | 디스크 쓰기 빈도 증가(배터리·I/O 부담) | OS 강제 종료 시 유실 윈도우 증가 — Pillar 3 위반 |
| `T_POLL_SEC` | 5 | 1~10 | 검사 오버헤드 증가(효과는 미미) | `T_AUTOSAVE_SEC` 트리거 감지가 늦어져 실효 유실 윈도우가 커짐 |
| `T_DEBOUNCE_SEC` | 2.0 | 1.0~5.0 | 코얼레싱 효과 감소, 쓰기 빈도 증가 | 단발 요청도 불필요하게 오래 대기 |
| `T_MAX_COALESCE_SEC` | 8.0 | `T_DEBOUNCE_SEC`보다 크고 `T_AUTOSAVE_SEC`보다 작게 유지 | 요청이 몰릴 때 코얼레싱 효과 감소 | `T_AUTOSAVE_SEC`에 근접하면 두 메커니즘의 역할 구분이 무의미해짐 |
| Foreground 재시도(`T_BASE=0.5/T_CAP=4.0/N_MAX=4`) | — | 총 대기 5~15초 유지 | 일시적 오류에 너무 빨리 포기 | 포그라운드 UX 방해(체감 지연) |
| Pause/Quit 재시도(`T_BASE=0.1/T_CAP=0.3/N_MAX=2`) | — | 총 대기 OS 종료 유예시간(보통 수 초) 이내 유지 | 정상 지연에도 재시도 없이 포기 | OS가 강제 종료하기 전에 못 끝내 재시도 자체가 무의미해짐 |
| `ε_FUTURE` | 300(5분) | 60~900 | 정상적인 기기 시계 드리프트도 조작으로 오판 | 실제 시계 조작을 눈감아줌(악용 가능) |

## Visual/Audio Requirements

Player Fantasy 섹션에 명시한 대로, 이 시스템은 원칙적으로 무음·무표시여야 한다 — 플레이어가
저장 과정을 의식하는 순간은 이미 신뢰가 깨진 순간이다. 유일한 예외는 Edge Cases의
`SaveLostWarning` 플래그로, 이마저도 이 시스템이 직접 렌더링하지 않고 존재 여부만
노출한다 — 실제 화면 표현은 향후 설정 시스템의 몫이다.

## UI Requirements

이 시스템은 직접적인 UI를 갖지 않는다. 유일한 표면 접점은 Edge Cases의
`SaveLostWarning` 플래그를 향후 설정/접근성 시스템이 소비해 사용자에게 조용히 알리는
경로뿐이며, 그 화면 디자인은 이 GDD의 범위 밖이다.

## Acceptance Criteria

> *(`qa-lead` 상담 완료 — lean 모드에서도 Acceptance Criteria는 상담 필수 섹션. 상담 중
> 발견된 5가지 이슈는 Detailed Design·Formulas·Edge Cases에 정밀화로 반영되었다 —
> `.bak` 이중 원자적 쓰기, 직전 값 유지 폴백, LoadFailed→Ready 명시, `t_lastRequestTime`
> 리셋 명시, Quit 복합 실패는 인정된 위험으로 문서화.)*

아래 기준은 Detailed Design·Formulas·Edge Cases에서 정의된 필드·상수명을 그대로 사용한다.
이 시스템의 "상태"(Uninitialized/Loading/Ready/Saving/LoadFailed)는 States and Transitions
표에 정의된 대로 명시적으로 관리되는 상태다(`potato-stat-system.md`와 달리 파생값이 아님).

### 상태 전이

- **AC-ST-01 (초기 상태)** **GIVEN** 앱 프로세스가 방금 시작됨, 아직 로드 트리거 없음 **WHEN** 상태 조회 **THEN** 상태 = Uninitialized.
- **AC-ST-02 (Uninitialized → Loading)** **GIVEN** 상태 = Uninitialized **WHEN** 로드 트리거(최초 실행/재개) 발생 **THEN** 상태 = Loading.
- **AC-ST-03 (Loading → Ready, 정상 로드)** **GIVEN** 상태 = Loading, 세이브 파일 존재하고 `IsIntegrityValid = true` **WHEN** 로드 완료 **THEN** 상태 = Ready, 등록된 모든 시스템에 데이터 배포.
- **AC-ST-04 (Loading → LoadFailed)** **GIVEN** 상태 = Loading, 주 파일 `IsIntegrityValid = false` **WHEN** 로드 결과 판정 **THEN** 상태 = LoadFailed.
- **AC-ST-05 (파일 없음은 LoadFailed 아님)** **GIVEN** 상태 = Loading, 세이브 파일 자체가 존재하지 않음(최초 실행) **WHEN** 로드 시도 **THEN** LoadFailed를 거치지 않고 곧장 상태 = Ready, 등록된 시스템들은 각자의 기본값 경로(예: `potato-stat-system.md` AC-EC-05)를 탐.
- **AC-ST-06 (Ready → Saving, 비차단)** **GIVEN** 상태 = Ready **WHEN** 저장 트리거(Pause/명시적 요청/§1 강제 flush) 발생 **THEN** 상태 = Saving, 게임플레이는 차단되지 않음.
- **AC-ST-07 (Saving → Ready, 성패 무관 복귀)** **GIVEN** 상태 = Saving **WHEN** 쓰기가 성공 또는 재시도 소진 후 실패로 종료 **THEN** 상태는 항상 Ready로 복귀, 별도 실패 상태로 고착되지 않음.
- **AC-ST-08 (LoadFailed → Ready, `.bak` 복구 성공)** **GIVEN** 상태 = LoadFailed, 주 파일 무효·`.bak` 유효 **WHEN** 복구 절차가 `.bak`으로 재시도 **THEN** 상태 = Ready, `.bak` 데이터 배포, `SaveLostWarning` 미노출.
- **AC-ST-09 (LoadFailed → Ready, 이중 손상 시 기본값)** **GIVEN** 상태 = LoadFailed, 주 파일·`.bak` 모두 무효 **WHEN** 복구 절차 완료 **THEN** 상태 = Ready, 등록된 시스템 기본값 초기화, `SaveLostWarning` 정확히 1회 노출.

### 공식 검증

**§1 방어적 주기 저장 (`ShouldFlush`)**
- **AC-F-01** **GIVEN** `isDirty=false` **WHEN** `t_lastWriteSuccess` 이후 임의로 긴 시간 경과 **THEN** `ShouldFlush=false` 유지, 쓰기 없음.
- **AC-F-02** **GIVEN** `t_lastWriteSuccess=1,000,000`, `t=1,000,010`에 `RequestSave()`(`isDirty=true`) **WHEN** `t=1,000,015/020/025` 검사 **THEN** 매 시점 `ShouldFlush=false`(경과<30).
- **AC-F-03** **GIVEN** AC-F-02 연속 **WHEN** `t=1,000,030`(경과 30≥30) **THEN** `ShouldFlush=true` → 디바운스 우회 즉시 쓰기, 성공 시 `t_lastWriteSuccess=1,000,030`, `isDirty=false`.
- **AC-F-04 (최악 유실 윈도우 상한)** **GIVEN** 저장 직후 변경 발생 **WHEN** OS가 다음 폴링 틱 직전 강제 종료 **THEN** 유실 변경분은 `T_AUTOSAVE_SEC+T_POLL_SEC≈35초` 이내(단, Edge Cases의 "인정된 위험" 경로는 예외).

**§2 디바운스/코얼레싱 (`ExecuteWrite`)**
- **AC-F-05** **GIVEN** `t=0.0` 1회 요청, 이후 없음 **WHEN** `t=2.0` **THEN** `ExecuteWrite=true`(정적구간 충족), 1회 기록.
- **AC-F-06** **GIVEN** `t=0.0, t=1.0` 요청(`t_firstPending=0.0` 유지) **WHEN** `t=3.0`(`3.0-1.0=2.0≥2.0`) **THEN** `ExecuteWrite=true`, 두 요청이 쓰기 1회로 합쳐짐.
- **AC-F-07 (기아 방지)** **GIVEN** 1.5초 간격 연속 요청(`t_firstPending=0.0` 유지) **WHEN** `t=8.0`(`8.0≥8.0`) **THEN** `ExecuteWrite=true` 강제 기록, 무기한 지연되지 않음.
- **AC-F-08 (Pause/Quit·§1은 디바운스 우회)** **GIVEN** 디바운스 타이머 대기 중(2.0초 미도달) **WHEN** Pause/Quit 또는 §1 `ShouldFlush=true` 발생 **THEN** 시간 조건 무관 즉시 쓰기.

**§3 재시도 백오프 (`delay(n)`)**
- **AC-F-09 (Foreground 시퀀스)** **GIVEN** Foreground(`T_BASE=0.5,T_CAP=4.0,N_MAX=4`) **WHEN** `delay(1..4)` 계산 **THEN** `0.5/1.0/2.0/4.0`s, 총 5회 시도, 최악 대기 7.5s.
- **AC-F-10 (Pause/Quit 시퀀스)** **GIVEN** Pause/Quit(`T_BASE=0.1,T_CAP=0.3,N_MAX=2`) **WHEN** `delay(1..2)` 계산 **THEN** `0.1/0.2`s, 총 3회 시도, 최악 대기 0.3s.
- **AC-F-11 (2회 실패 후 성공)** **GIVEN** Foreground 쓰기가 초기+1차 재시도 실패 **WHEN** `delay(2)=1.0s` 후 재시도 성공 **THEN** 총 경과 1.5s, `isDirty=false`, `t_lastWriteSuccess` 갱신.
- **AC-F-12 (재시도 전량 소진 — 앱 생존 시)** **GIVEN** 모든 재시도 실패, 앱은 계속 실행 중 **WHEN** 재시도 예산 소진 **THEN** rename 미성공이므로 기존 파일 손상 없음, `isDirty=true` 유지, `OnSaveFailed` 정확히 1회.

**§4 무결성 검증 (`IsIntegrityValid`, `.bak` 포함)**
- **AC-F-13** **GIVEN** 파일 크기<32B **WHEN** 로드 **THEN** `readSucceeded=false`, `IsIntegrityValid=false`.
- **AC-F-14 (MAGIC 불일치)** **GIVEN** 나머지 일치, `magic≠MAGIC` **WHEN** 검증 **THEN** `false`.
- **AC-F-15 (길이 불일치)** **GIVEN** `len(P_loaded)≠L_stored` **WHEN** 검증 **THEN** `false`.
- **AC-F-16 (해시 불일치)** **GIVEN** payload 1바이트 변형 **WHEN** 검증 **THEN** `false`.
- **AC-F-17 (전부 일치)** **GIVEN** magic·길이·해시 모두 일치 **WHEN** 검증 **THEN** `true`.
- **AC-F-18 (`.bak` 이중 원자적 쓰기 순서)** **GIVEN** 정상 저장 사이클 시작 **WHEN** 실행 순서 확인 **THEN** `temp2→.bak` rename이 `temp1→주파일` rename보다 먼저 완료됨 — 이 순서가 역전되지 않는다.
- **AC-F-19 (`.bak` 쓰기 중 크래시 — 주 파일 무결)** **GIVEN** `temp2→.bak` rename 단계에서 프로세스 강제 종료 **WHEN** 재시작 후 확인 **THEN** 주 파일은 이번 사이클 시작 전 상태로 그대로 유효(`IsIntegrityValid=true`), 아직 갱신 전이므로 손상 없음.

**§5 `lastUpdatedUtc` 유효성 (`IsTimestampValid`)**
- **AC-F-20 (하한 미만)** **GIVEN** `T_EPOCH_FLOOR=1,800,000,000`, `lastUpdatedUtc=1,799,999,000` **WHEN** 평가 **THEN** `false`, `t_deviceNow`로 대체.
- **AC-F-21 (정상 오차)** **GIVEN** `t_deviceNow=1,800,500,000`, `lastUpdatedUtc=1,800,500,120` **WHEN** 평가 **THEN** `120≤300` → `true`, 원값 유지.
- **AC-F-22 (명백한 미래 조작)** **GIVEN** `lastUpdatedUtc=t_deviceNow+3년` **WHEN** 평가 **THEN** `false`, `t_deviceNow`로 대체.
- **AC-F-23 (경계 포함, 하한)** **GIVEN** `t_loaded==T_EPOCH_FLOOR` **WHEN** 평가 **THEN** `true`.
- **AC-F-24 (경계 포함, 상한)** **GIVEN** `t_loaded==t_deviceNow+ε_FUTURE` **WHEN** 평가 **THEN** `true`.
- **AC-F-25 (`RegisterUtcTimestamp` 전용)** **GIVEN** 일반 `Register()`로 등록된 필드가 구조적으로 불가능한 값 **WHEN** 로드 **THEN** §5 검사 미적용, 원값 그대로 전달.
- **AC-F-26 (최초 실행 시 §5 미실행)** **GIVEN** 세이브 파일 없음 **WHEN** 초기화 **THEN** `IsTimestampValid` 미실행, `potato-stat-system.md` AC-EC-05 경로 적용.

### 엣지 케이스

- **AC-EC-01 (`.bak` 복구 성공)** **GIVEN** 주 파일 무효·`.bak` 유효 **WHEN** 복구 절차 **THEN** `.bak` 데이터로 로드 성공, 상태=Ready, `SaveLostWarning` 미노출.
- **AC-EC-02 (이중 손상 — 완전 초기화+경고)** **GIVEN** 주 파일·`.bak` 모두 무효 **WHEN** 복구 절차 완료 **THEN** 기본값 초기화, `SaveLostWarning` 정확히 1회.
- **AC-EC-03 (최초 설치 vs LoadFailed 구분)** **GIVEN** 세이브·`.bak` 모두 존재하지 않음 **WHEN** 로드 시도 **THEN** LoadFailed 미경유, 곧장 Ready, `SaveLostWarning` 미노출(AC-EC-02와 구분).
- **AC-EC-04 (`.bak` 세대 갱신, 정상 사이클)** **GIVEN** 주 파일에 유효한 이전 payload(P_old), 크래시 없는 정상 저장 **WHEN** 새 저장 성공 완료 **THEN** `.bak` 로드 시 P_old(직전 세대), 주 파일 로드 시 새 데이터.
- **AC-EC-05 (Saving 중 동시 요청 — 중복 쓰기 금지)** **GIVEN** 상태=Saving **WHEN** 새 `RequestSave()` **THEN** 두 번째 물리적 쓰기 동시 시작 안 함, 디바운스 대기창만 연장, Ready 상호작용 차단 없음.
- **AC-EC-06 (temp/rename 사이 강제 종료)** **GIVEN** temp 쓰기 완료, rename 실행 전 **WHEN** 이 시점 강제 종료 **THEN** 재시작 후 기존 주 파일 손상 없이 로드(`IsIntegrityValid=true`), LoadFailed 미전이.
- **AC-EC-07 (재시도 전량 실패 — 앱 생존 시 자가치유)** **GIVEN** Foreground 또는 Pause(종료 안 됨) 트리거 쓰기가 재시도까지 전부 실패 **WHEN** 앱이 계속 실행/포그라운드 복귀 **THEN** `isDirty=true` 유지, 다음 §1 주기에 자동 재시도, 디스크 공간 회복 시 자가치유.
- **AC-EC-08 (Quit 복합 실패 — 인정된 위험, 자가치유 예외)** **GIVEN** `OnApplicationQuit` 트리거 쓰기가 재시도까지 전부 실패 **WHEN** 그 직후 프로세스가 실제로 종료된다 **THEN** 해당 변경분은 영구 유실됨(다음 실행 시 자가치유 불가) — 이는 문서화된 인정된 위험이며 버그가 아니다. 이 시나리오를 재현하려면 지속적 쓰기 실패 상태를 인위적으로 강제해야 하므로 자동화 회귀 테스트가 아니라 수동/구성 테스트로 검증한다.
- **AC-EC-09 (per-entry 격리 + 직전 값 유지)** **GIVEN** 등록된 시스템 A의 getter가 예외를 던지고 B는 정상 **WHEN** 쓰기 사이클 실행 **THEN** B는 정상 기록되고, A의 키는 이번 사이클에서 갱신되지 않은 채 payload에 직전 성공값이 그대로 유지된다(삭제·생략 아님).
- **AC-EC-10 (마이그레이션 범위 밖 확인)** **GIVEN** §4 레이아웃에 스키마 버전 필드 없음 **WHEN** 레이아웃 확인 **THEN** 버전 필드·자동 마이그레이션 로직 부재 확인으로 충분, 구버전 호환성 테스트는 수행하지 않음(Open Questions로 이관).

### 이 GDD 범위 밖으로 명시적으로 남긴 것

- **세이브 포맷 마이그레이션 자체**는 Open Questions 후속 과제 — AC-EC-10은 "미구현 확인"만 담당.
- **`SaveLostWarning`의 실제 UI 표현**은 설정/접근성 시스템 GDD 소관 — 이 GDD는 플래그 노출까지만 책임.
- **클라우드 백업·다중 프로필**은 MVP 범위 밖(Open Questions) — 관련 AC 없음.
- **`OnApplicationPause`/`OnApplicationQuit`가 실제로 호출되는지 자체**는 OS/엔진 계약이라 플랫폼별 수동 검증 필요 — 이 시스템은 훅 호출 "이후"의 동작만 자동화 대상.

## Open Questions

| 질문 | 담당 | 목표 시점 |
|---|---|---|
| 세이브 포맷 마이그레이션 전략 — MVP는 명시적으로 범위 밖(Edge Cases) | systems-designer | 첫 포맷 변경이 필요해지는 시점(post-launch) |
| `SaveLostWarning` 플래그의 실제 UI 처리(문구·타이밍) | ux-designer / 설정·접근성 시스템 GDD 작성자 | 설정/접근성 시스템 GDD 작성 시 |
| 클라우드 백업·다중 프로필 지원 여부 — 기획서에 요구사항 없음, 로컬 단일 슬롯으로 MVP~Alpha 진행 | producer | Full Vision 스코프 논의 시 |
| ~~`RegisterUtcTimestamp` API가 실제로 다른 시스템(날씨 등)에도 필요한 필드가 있는지~~ **해결됨(weather-system.md, 2026-07-12)**: `scheduleStartUtc`·`cutoverUtc`는 UTC 타임스탬프지만 `lastUpdatedUtc`와 달리 보상 계산에 직결되지 않아 `IsTimestampValid` 조작 방어 대상일 필요가 없다 — 일반 `Register`로 충분, `RegisterUtcTimestamp` 확장 불필요. `lastUpdatedUtc` 하나만 이 API를 쓰는 상태 유지 | — | 해결됨 |
