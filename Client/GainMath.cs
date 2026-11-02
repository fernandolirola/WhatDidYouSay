using System;

namespace WhatDidYouSay;

internal enum SoundKind { Normal, Gunshot, Grenade, Plane }

internal static class GainMath
{
    public static float FromPercent(float increase) =>
        float.IsNaN(increase) || float.IsInfinity(increase) ? 1f : 1f + Math.Max(0f, Math.Min(2000f, increase)) / 100f;

    public static float Select(SoundKind kind, float shots, float grenades, float plane)
    {
        switch (kind)
        {
            case SoundKind.Gunshot: return shots;
            case SoundKind.Grenade: return grenades;
            case SoundKind.Plane: return plane;
            default: return 1f;
        }
    }

    // Audio thread: no Unity APIs, allocations, or logging. Apply once to the original buffer.
    public static void Apply(float[] samples, float gain)
    {
        if (gain == 1f) return;
        for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
    }
}
