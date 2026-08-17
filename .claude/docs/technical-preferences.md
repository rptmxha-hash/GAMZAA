# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6.5
- **Language**: C#
- **Rendering**: Universal Render Pipeline (URP) — recommended for 2D mobile; confirm at project creation
- **Physics**: Unity 2D physics (Box2D-based) — 2D game

## Input & Platform

<!-- Written by /setup-engine. Read by /ux-design, /ux-review, /test-setup, /team-ui, and /dev-story -->
<!-- to scope interaction specs, test helpers, and implementation to the correct input methods. -->

- **Target Platforms**: Mobile (iOS / Android)
- **Input Methods**: Touch
- **Primary Input**: Touch
- **Gamepad Support**: None
- **Touch Support**: Full
- **Platform Notes**: 아날로그 터치 제스처(드래그·롱프레스)가 핵심 조작. **세로(portrait) 방향 우선**. 저사양 기기 및 배터리 효율 고려. 호버 전용 상호작용 금지. Unity Input System 사용 권장(EnhancedTouch).

## Naming Conventions

C# / Unity conventions:

- **Classes**: PascalCase (e.g., `PotatoController`)
- **Public fields / properties**: PascalCase (e.g., `MoveSpeed`, `Moisture`)
- **Private fields**: `_camelCase` (e.g., `_currentMoisture`, `_isWithered`)
- **Methods**: PascalCase (e.g., `TakeMoisture()`, `ApplyWeather()`)
- **Events / delegates**: PascalCase, past tense for event names (e.g., `MoistureChanged`, `PotatoHarvested`)
- **Files**: PascalCase matching class (e.g., `PotatoController.cs`)
- **Scenes / Prefabs**: PascalCase matching root object (e.g., `PotatoController.prefab`, `Garden.unity`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE (e.g., `MaxMoisture`, `GROWTH_THRESHOLD`)

## Performance Budgets

- **Target Framerate**: 60fps (모바일 — 배터리/발열 고려해 idle 화면은 30fps 스로틀 검토)
- **Frame Budget**: 16.6ms
- **Draw Calls**: [엔진 기본 — 에셋 생산 후 프로파일링. 2D URP 배칭 목표]
- **Memory Ceiling**: [프로토타입 후 저사양 기기 기준으로 설정]

## Testing

- **Framework**: Unity Test Framework (NUnit 기반) — run `/test-setup` to scaffold
- **Minimum Coverage**: [프로토타입 후 설정 — 로직/공식 시스템은 필수 커버리지]
- **Required Tests**: 날씨 상태 전이, 수분/온습도 공식, 오프라인 진행 계산, 시듦/되돌림 로직 (Logic 스토리는 자동 유닛 테스트 BLOCKING)

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- [None configured yet — add as architectural decisions are made]

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- [None configured yet — add as dependencies are approved]

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]

## Engine Specialists

<!-- Written by /setup-engine when engine is configured. -->
<!-- Read by /code-review, /architecture-decision, /architecture-review, and team skills -->
<!-- to know which specialist to spawn for engine-specific validation. -->

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# review — primary covers it)
- **Shader Specialist**: unity-shader-specialist (Shader Graph, HLSL, URP materials)
- **UI Specialist**: unity-ui-specialist (UI Toolkit UXML/USS, UGUI Canvas, runtime UI)
- **Additional Specialists**: unity-addressables-specialist (에셋 로딩·메모리·코스튬/컨텐츠 카탈로그). unity-dots-specialist는 방치형 특성상 당분간 불필요.
- **Routing Notes**: 아키텍처·일반 C# 리뷰는 primary. 렌더링/이펙트는 shader specialist. 모든 UI 구현은 UI specialist. 에셋 관리(코스튬·컨텐츠 로딩)는 Addressables specialist. ECS/Jobs/Burst가 실제로 필요해지기 전엔 DOTS specialist 미사용.

### File Extension Routing

<!-- Skills use this table to select the right specialist per file type. -->

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs files) | unity-specialist |
| Shader / material files (.shader, .shadergraph, .mat) | unity-shader-specialist |
| UI / screen files (.uxml, .uss, Canvas prefabs) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| Asset management (Addressables groups, content catalogs) | unity-addressables-specialist |
| Native extension / plugin files (.dll, native plugins) | unity-specialist |
| General architecture review | unity-specialist |
