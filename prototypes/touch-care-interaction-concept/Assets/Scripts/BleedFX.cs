// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does analog touch (drag/long-press) feel soothing with bleed+sound feedback?
// Date: 2026-07-09
using System.Collections;
using UnityEngine;

// Spawns a soft-edged circle at a touch point that grows and fades over ~1s.
// Placeholder for Section 1's "watercolor bleed reaction at the touch point."
public class BleedFX : MonoBehaviour
{
    public static void Spawn(Vector3 worldPos, Color color, float maxScale = 1.2f, float lifetime = 1.0f)
    {
        var go = new GameObject("BleedFX");
        go.transform.position = worldPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSoftCircleSprite(color);
        sr.sortingOrder = 10;
        go.transform.localScale = Vector3.one * 0.05f;
        var runner = go.AddComponent<BleedFX>();
        runner.StartCoroutine(runner.Animate(sr, go.transform, maxScale, lifetime));
    }

    IEnumerator Animate(SpriteRenderer sr, Transform t, float maxScale, float lifetime)
    {
        float elapsed = 0f;
        Color startColor = sr.color;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / lifetime;
            float scale = Mathf.Lerp(0.05f, maxScale, 1f - Mathf.Pow(1f - p, 3f)); // ease-out grow
            t.localScale = Vector3.one * scale;
            var c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, p); // fade out
            sr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }

    public static Sprite MakeSoftCircleSprite(Color color)
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = Mathf.Pow(alpha, 1.5f); // soft falloff — crude "bleed" edge
                var c = color;
                c.a = alpha * color.a;
                pixels[y * size + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
