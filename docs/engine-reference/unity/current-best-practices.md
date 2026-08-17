# Unity 6.5 — Current Best Practices (since training cutoff)

*Last verified: 2026-07-09*

New capabilities and recommended practices in Unity 6.5, filtered for this project
(2D, mobile, C#, single-player idle/care game).

## Rendering (2D mobile)

- **Use URP.** BIRP is deprecated. Configure a 2D URP renderer.
- **Custom 2D lighting & shadow system** (new in 6.5): use for weather/mood atmosphere
  (soft light from the stove, dim overcast during rain) instead of faking with sprites.
- **On-tile post-processing** (all platforms in 6.5): color grading, tone mapping, vignette
  processed in a single GPU tile pass — efficient for mobile HDR. Prefer this for the
  pastel/watercolor color grade over full-screen post passes.
- **Shader function reflection API**: HLSL shader functions can be written directly and are
  auto-exposed in Shader Graph — useful for custom watercolor/paper-grain effects.

## Animation

- **Sprite BlendShape API — Free-Form Deformation (cage-based)** (new in 6.5): animate the
  potato's squash/stretch, facial expressions, and costume drape with cage deformation
  instead of large per-frame sprite sheets. Saves art volume — directly relevant to the
  solo-dev asset-load risk.

## Mobile / platform

- **Android performance improvements** ship in 6.5 — profile on target low-end devices.
- Prefer the **Input System** package (`EnhancedTouch`) for analog touch gestures
  (drag = window/humidity, long-press = stove/temperature). Do not use legacy `Input.touches`
  for new code.
- Target **portrait orientation**; design UI for one-handed reach.

## Code identity & data

- Use **`EntityId`** for object identity (not `GetInstanceID()`).
- For the 24h background weather timer / offline progression: use wall-clock
  (`DateTime.UtcNow` persisted) rather than frame time; validate against clock tampering and
  timezone changes server-side or with sanity clamps. (This is a known project tech risk.)

## Asset management

- Use **Addressables** for costumes and content that grow over time (30 costumes planned) to
  control memory on low-end devices — load on demand, release when leaving the village/dress-up.

## Testing

- **Unity Test Framework** (NUnit) for logic: weather state transitions, moisture/temp
  formulas, offline progression math, wither/rewind logic. Keep these EditMode-testable
  (pure C#, dependency-injected) per project coding standards.
