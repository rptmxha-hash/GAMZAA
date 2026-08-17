# Unity — Deprecated APIs (Don't use X → Use Y)

*Last verified: 2026-07-09*

Quick "don't → do" reference for Unity 6.5. When writing C# or shaders, prefer the
right-hand column.

| Don't use (deprecated/obsolete in 6.5) | Use instead | Notes |
|---|---|---|
| `Object.GetInstanceID()` / `InstanceID` as identity | `EntityId` | Hard error in 6.5 (warned in 6.4). |
| Built-In Render Pipeline (BIRP) shaders / Standard shader | URP shaders + Shader Graph | BIRP deprecated; project is URP. |
| URP `SetupRenderPasses` in Renderer Features | Render Graph + `AddRenderPasses` | Compatibility Mode removed in 6.4. |
| `VisualElement.transform` (UI Toolkit) | Current UI Toolkit transform/style APIs | Deprecated. |
| Dynamic Batching | SRP Batcher + GPU instancing / 2D batching | Slated for removal. |
| OptiX denoiser (baked GI) | OIDN (Open Image Denoise) | OptiX removed in 6.7. Low 2D relevance. |
| Standalone Lobby/Matchmaker/Multiplay/Relay SDKs | Unity "Building Blocks" | N/A — single-player project. |

## Verification rule for agents

If an API is not listed here and you are uncertain whether it changed in Unity 6.x,
**WebSearch** `"Unity 6.5 <API name>"` or check the official manual
(`docs.unity3d.com/6000.5/...`) before writing code. Do not assume pre-Unity-6 signatures.
