// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does analog touch (drag/long-press) feel soothing with bleed+sound feedback?
// Date: 2026-07-09
using UnityEngine;
using UnityEngine.InputSystem;

public enum DragKind { Window, WateringCan }

// Shared drag-gesture behavior for the Window (drag to open, per Section 2/7 of the
// art bible: partial-travel threshold + hysteresis so it can be reopened/closed
// repeatedly for repeatability testing) and the Watering Can (freeform drag = puffs).
[RequireComponent(typeof(Collider2D))]
public class DragGestureObject : MonoBehaviour
{
    public DragKind kind = DragKind.Window;
    public Color bleedColor = Color.white;
    public SpriteRenderer visual;

    Vector3 anchorPos;
    bool isDragging = false;
    float dragProgress = 0f; // 0..1, Window-only
    bool openFired = false;  // Window-only, allows repeat open/close
    float lastPuffTime = 0f;

    const float PUFF_INTERVAL = 0.12f;
    const float OPEN_THRESHOLD = 0.65f;        // matches Section 7 UX rec (~60-70% travel)
    const float CLOSE_RESET_THRESHOLD = 0.35f; // hysteresis so it doesn't re-fire on tiny jitter
    const float DRAG_RANGE = 1.5f;             // world units for full open progress

    AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        anchorPos = transform.position;
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
                isDragging = true;
        }

        if (isDragging && pointer.press.isPressed)
            HandleDrag(worldPos);

        if (pointer.press.wasReleasedThisFrame)
            isDragging = false;
    }

    void HandleDrag(Vector3 worldPos)
    {
        if (Time.time - lastPuffTime > PUFF_INTERVAL)
        {
            lastPuffTime = Time.time;
            BleedFX.Spawn(worldPos, bleedColor, maxScale: 0.5f, lifetime: 0.6f);
            PlaySoftTone();
        }

        if (kind == DragKind.Window)
        {
            float dist = Mathf.Abs(worldPos.x - anchorPos.x);
            dragProgress = Mathf.Clamp01(dist / DRAG_RANGE);

            if (visual != null)
                visual.color = Color.Lerp(bleedColor * 0.6f, Color.white, dragProgress);

            if (!openFired && dragProgress >= OPEN_THRESHOLD)
            {
                openFired = true;
                BleedFX.Spawn(transform.position, Color.white, maxScale: 1.6f, lifetime: 1.0f);
                PlaySoftTone(pitchUp: true);
            }
            else if (openFired && dragProgress <= CLOSE_RESET_THRESHOLD)
            {
                openFired = false; // allow the "open" moment to fire again on the next push-out
            }
        }
    }

    void PlaySoftTone(bool pitchUp = false)
    {
        AudioClip clip = kind == DragKind.Window ? ProceduralAudio.MakeRainTone() : ProceduralAudio.MakeWaterTone();
        audioSource.pitch = pitchUp ? 1.15f : 1f;
        audioSource.PlayOneShot(clip, 0.5f);
    }
}
