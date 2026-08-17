// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does analog touch (drag/long-press) feel soothing with bleed+sound feedback?
// Date: 2026-07-09
using UnityEngine;

// Runtime-generated placeholder tones — NOT real ASMR audio. Just enough to test
// whether touch response timing (visual + sound synced to the gesture) feels good.
// Swap for real recorded ASMR clips before drawing any conclusion about "does the
// SOUND itself feel soothing" — this only tests the response TIMING.
public static class ProceduralAudio
{
    public static AudioClip MakeRainTone(float duration = 0.25f)
    {
        return MakeClip(duration, (t) => Random.Range(-1f, 1f), lowpass: true);
    }

    public static AudioClip MakeFireTone(float duration = 0.4f)
    {
        float freq = 220f;
        return MakeClip(duration, (t) =>
        {
            float flutter = 1f + 0.15f * Random.Range(-1f, 1f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * flutter;
        }, lowpass: false);
    }

    public static AudioClip MakeWaterTone(float duration = 0.18f)
    {
        float freq = 660f;
        return MakeClip(duration, (t) =>
        {
            float decay = Mathf.Exp(-t * 12f);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * decay;
        }, lowpass: false);
    }

    delegate float SampleFunc(float t);

    static AudioClip MakeClip(float duration, SampleFunc fn, bool lowpass)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
        var data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            data[i] = fn(t);
        }

        if (lowpass)
        {
            // crude moving-average filter so raw noise doesn't sound harsh
            var smoothed = new float[sampleCount];
            int window = 12;
            for (int i = 0; i < sampleCount; i++)
            {
                float sum = 0f;
                int count = 0;
                for (int w = -window; w <= window; w++)
                {
                    int idx = i + w;
                    if (idx >= 0 && idx < sampleCount) { sum += data[idx]; count++; }
                }
                smoothed[i] = sum / count;
            }
            data = smoothed;
        }

        // gentle fade in/out to avoid clicks
        int fade = Mathf.Min(200, sampleCount / 4);
        for (int i = 0; i < fade; i++)
        {
            float f = (float)i / fade;
            data[i] *= f;
            data[sampleCount - 1 - i] *= f;
        }

        var clip = AudioClip.Create("ProceduralTone", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
