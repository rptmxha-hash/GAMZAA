# Unity — Breaking Changes (training cutoff → 6.5)

*Last verified: 2026-07-09*

Changes that can break code written against pre-Unity-6 knowledge. Ordered by
likelihood of impact on this project (2D mobile, C#, URP, single-player).

## HIGH impact

### `InstanceID` / `GetInstanceID()` → `EntityId`
- **What changed**: The long-standing `int GetInstanceID()` identity API is replaced by
  `EntityId`. Warnings appear in Unity 6.4; **hard errors in Unity 6.5**.
- **Action**: Use `EntityId` for object identity. Do not persist `GetInstanceID()` values
  or rely on the `int` return type.

### Built-In Render Pipeline (BIRP) deprecated
- **What changed**: BIRP is deprecated in 6.5. Development is focused on **URP**; HDRP is in
  maintenance mode. BIRP receives fixes through the Unity 6.7 LTS lifecycle, then removal.
- **Action**: This project uses **URP**. Do not author BIRP shaders/materials or assume the
  legacy Standard shader. Use URP/2D shaders and Shader Graph.

### URP `SetupRenderPasses` deprecated → Render Graph + `AddRenderPasses`
- **What changed**: Scriptable Renderer Features using `SetupRenderPasses` must be rewritten
  using the **Render Graph** system and the `AddRenderPasses` API. URP Compatibility Mode
  was fully removed in 6.4 (dependent methods are hard-obsolete).
- **Action**: Any custom renderer feature must target Render Graph. Avoid tutorials predating
  Unity 6 for custom render passes.

## MEDIUM impact

### `VisualElement.transform` deprecated (UI Toolkit)
- **What changed**: The `VisualElement.transform` API is deprecated.
- **Action**: Use the current UI Toolkit transform/style APIs. Relevant if UI is built with
  UI Toolkit (UXML/USS).

### Dynamic Batching deprecated
- **What changed**: Dynamic batching is deprecated and will be removed in a future release.
- **Action**: Rely on SRP Batcher (URP) and GPU instancing / 2D sprite batching instead.

## LOW / not applicable to this project

### Standalone multiplayer SDKs deprecated (Lobby, Matchmaker, Multiplay, Relay)
- Consolidated into Unity's "Building Blocks" approach.
- **N/A** — this game is single-player.

### OptiX denoiser → OIDN (Open Image Denoise)
- Baked-lighting denoiser change. Low relevance for a 2D game.
