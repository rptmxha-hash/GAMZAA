// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does analog touch (drag/long-press) feel soothing with bleed+sound feedback?
// Date: 2026-07-09
using UnityEngine;
using UnityEngine.InputSystem;

// Stove long-press behavior. Per Section 7 of the art bible: no numeric countdown —
// the bleed/glow completing its fill IS the "held long enough" signal.
[RequireComponent(typeof(Collider2D))]
public class LongPressGestureObject : MonoBehaviour
{
    public Color bleedColor = new Color(1f, 0.55f, 0.2f); // warm orange placeholder
    public SpriteRenderer visual;
    public Transform glowRing; // child sprite that scales with hold progress

    const float HOLD_DURATION = 1.2f;
    const float PUFF_INTERVAL = 0.15f;

    float holdTime = 0f;
    bool isPressing = false;
    bool completed = false;
    float lastPuffTime = 0f;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(pointer.position.ReadValue());
        worldPos.z = 0f;

        if (pointer.press.wasPressedThisFrame)
        {
            var hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null && hit.gameObject == gameObject)
            {
                isPressing = true;
                holdTime = 0f;
                completed = false;
            }
        }

        if (isPressing && pointer.press.isPressed)
        {
            holdTime += Time.deltaTime;
            float progress = Mathf.Clamp01(holdTime / HOLD_DURATION);

            if (glowRing != null)
                glowRing.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.4f, progress);

            if (Time.time - lastPuffTime > PUFF_INTERVAL)
            {
                lastPuffTime = Time.time;
                BleedFX.Spawn(worldPos, bleedColor, maxScale: 0.3f + 0.3f * progress, lifetime: 0.5f);
            }

            if (!completed && progress >= 1f)
            {
                completed = true;
                BleedFX.Spawn(transform.position, bleedColor, maxScale: 2.0f, lifetime: 1.4f);
                audioSource.pitch = 0.9f;
                audioSource.PlayOneShot(ProceduralAudio.MakeFireTone(0.4f), 0.6f);
            }
        }

        if (pointer.press.wasReleasedThisFrame)
        {
            isPressing = false;
            if (!completed)
            {
                // released before completion — ring relaxes back down, no penalty (Pillar 3)
                holdTime = 0f;
                if (glowRing != null) glowRing.localScale = Vector3.one * 0.2f;
            }
            else
            {
                // fully warmed — hold the glow briefly, then reset so it can be retested
                Invoke(nameof(ResetForRetest), 2.0f);
            }
        }
    }

    void ResetForRetest()
    {
        completed = false;
        holdTime = 0f;
        if (glowRing != null) glowRing.localScale = Vector3.one * 0.2f;
    }
}
