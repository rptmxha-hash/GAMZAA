// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does analog touch (drag/long-press) feel soothing with bleed+sound feedback?
// Date: 2026-07-09
using UnityEngine;

// Builds the entire test scene at runtime from code-generated placeholder shapes —
// no hand-authored scene/prefab assets needed. Attach this to one empty GameObject
// in an otherwise empty scene and press Play.
public class PrototypeBootstrap : MonoBehaviour
{
    void Awake()
    {
        SetupCamera();
        CreateSoil();
        CreateWindow();
        CreateStove();
        CreateWateringCan();
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.backgroundColor = new Color(0.96f, 0.94f, 0.89f); // Paper Cream placeholder
        cam.transform.position = new Vector3(0, 0, -10);
    }

    void CreateSoil()
    {
        var go = new GameObject("Soil (placeholder, non-interactive)");
        go.transform.position = new Vector3(0f, -2f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidSprite(new Color(0.55f, 0.4f, 0.28f), 512, 128);
        sr.sortingOrder = 0;
        go.transform.localScale = new Vector3(6f, 1.5f, 1f);
    }

    void CreateWindow()
    {
        var go = new GameObject("Window (drag to open/close)");
        go.transform.position = new Vector3(-2.2f, 1.5f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidSprite(new Color(0.75f, 0.82f, 0.85f), 128, 128);
        sr.sortingOrder = 1;
        go.transform.localScale = Vector3.one * 1.2f;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        var drag = go.AddComponent<DragGestureObject>();
        drag.kind = DragKind.Window;
        drag.bleedColor = new Color(0.7f, 0.8f, 0.9f);
        drag.visual = sr;
    }

    void CreateStove()
    {
        var go = new GameObject("Stove (long-press to warm)");
        go.transform.position = new Vector3(2.2f, -0.5f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidSprite(new Color(0.3f, 0.25f, 0.22f), 128, 128);
        sr.sortingOrder = 1;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var ring = new GameObject("GlowRing");
        ring.transform.SetParent(go.transform);
        ring.transform.localPosition = Vector3.zero;
        var ringSr = ring.AddComponent<SpriteRenderer>();
        ringSr.sprite = BleedFX.MakeSoftCircleSprite(new Color(1f, 0.6f, 0.2f, 0.8f));
        ringSr.sortingOrder = 2;
        ring.transform.localScale = Vector3.one * 0.2f;

        var press = go.AddComponent<LongPressGestureObject>();
        press.visual = sr;
        press.glowRing = ring.transform;
    }

    void CreateWateringCan()
    {
        var go = new GameObject("WateringCan (drag around to water)");
        go.transform.position = new Vector3(0f, 2f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidSprite(new Color(0.5f, 0.65f, 0.75f), 96, 96);
        sr.sortingOrder = 1;
        go.transform.localScale = Vector3.one * 0.8f;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        var drag = go.AddComponent<DragGestureObject>();
        drag.kind = DragKind.WateringCan;
        drag.bleedColor = new Color(0.6f, 0.75f, 0.85f);
        drag.visual = sr;
    }

    static Sprite MakeSolidSprite(Color color, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}
