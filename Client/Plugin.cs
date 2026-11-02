using System;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT.Airdrop;
using HarmonyLib;
using UnityEngine;

namespace WhatDidYouSay;

[BepInPlugin(Id, "WHAT DID YOU SAY?", "1.0.0")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "com.zezinho.whatdidyousay";
    private Harmony? _harmony;
    internal static volatile float GunshotGain = 1f;
    internal static volatile float GrenadeGain = 1f;
    internal static volatile float PlaneGain = 1f;
    private ConfigEntry<bool> _enabled = null!;
    private ConfigEntry<float> _shots = null!;
    private ConfigEntry<float> _grenades = null!;
    private ConfigEntry<float> _plane = null!;

    private void Awake()
    {
        _enabled = Config.Bind("General", "Enabled", true, "Enable the meme volume boost.");
        var range = new AcceptableValueRange<float>(0f, 2000f);
        _shots = Config.Bind("Volume boost", "Gunshots increase %", 2000f, new ConfigDescription("Extra sample gain for gunshots. 0 = normal; 2000 = 21x. Includes suppressed shots and tails.", range));
        _grenades = Config.Bind("Volume boost", "Grenade explosions increase %", 2000f, new ConfigDescription("Extra sample gain for the grenade explosion audio group. 0 = normal; 2000 = 21x.", range));
        _plane = Config.Bind("Volume boost", "Airdrop plane increase %", 2000f, new ConfigDescription("Extra sample gain for the airdrop plane engine. 0 = normal; 2000 = 21x.", range));
        Config.SettingChanged += Refresh;
        Refresh(null, null!);
        _harmony = new Harmony(Id);
        try
        {
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("WHAT DID YOU SAY? 1.0.0: gunshots, grenade explosions and airdrop engine gain enabled. Settings: F12.");
        }
        catch (Exception ex)
        {
            _harmony.UnpatchSelf();
            GunshotGain = GrenadeGain = PlaneGain = 1f;
            Logger.LogError($"Audio hooks could not be installed; boost disabled: {ex}");
        }
    }

    private void Refresh(object? sender, SettingChangedEventArgs args)
    {
        GunshotGain = _enabled.Value ? GainMath.FromPercent(_shots.Value) : 1f;
        GrenadeGain = _enabled.Value ? GainMath.FromPercent(_grenades.Value) : 1f;
        PlaneGain = _enabled.Value ? GainMath.FromPercent(_plane.Value) : 1f;
    }

    private void OnDestroy()
    {
        Config.SettingChanged -= Refresh;
        GunshotGain = GrenadeGain = PlaneGain = 1f;
        _harmony?.UnpatchSelf();
    }

    internal static void Mark(BetterSource source, SoundKind kind)
    {
        if (source == null) return;
        Mark(source.source1, kind);
        if (source is SuperSource super && super.source2 != source.source1) Mark(super.source2, kind);
    }

    private static void Mark(AudioSource source, SoundKind kind)
    {
        if (source == null) return;
        var filter = source.GetComponent<MemeGain>();
        if (filter == null && kind != SoundKind.Normal) filter = source.gameObject.AddComponent<MemeGain>();
        if (filter != null) filter.Kind = kind;
    }

    // Every audio-pool borrow sets its release listener before SetActive. This catches
    // weapon queues and grenade/distant playback, without scanning the scene or clip names.
    [HarmonyPatch(typeof(BetterSource), nameof(BetterSource.SetActive))]
    private static class ActivatePatch
    {
        private static void Postfix(BetterSource __instance, bool active)
        {
            if (!active || !Singleton<BetterAudio>.Instantiated) return;
            var groups = Singleton<BetterAudio>.Instance.SourceGroups;
            if (groups == null) return;
            var listener = __instance.ReleaseListener;
            var kind = SoundKind.Normal;
            if (listener != null)
            {
                if (ReferenceEquals(listener, groups[(int)BetterAudio.AudioSourceGroupType.Gunshots])) kind = SoundKind.Gunshot;
                else if (ReferenceEquals(listener, groups[(int)BetterAudio.AudioSourceGroupType.Grenades])) kind = SoundKind.Grenade;
            }
            Mark(__instance, kind);
        }
    }

    // The plane borrows a general nonspatial source, so identify it at the engine's
    // playback entry point before the first sample, rather than boosting that whole pool.
    [HarmonyPatch(typeof(ClientAirPlane), nameof(ClientAirPlane.PlaySound))]
    private static class PlanePatch
    {
        private static void Prefix(ClientAirPlane __instance) => Mark(__instance.SoundSource, SoundKind.Plane);
    }

    [HarmonyPatch(typeof(BetterSource), nameof(BetterSource.Release))]
    private static class ReleasePatch
    {
        private static void Postfix(BetterSource __instance) => Mark(__instance, SoundKind.Normal);
    }
}

// One filter per underlying AudioSource, reused with the game's pool. A plain volatile
// category plus volatile gains keeps the audio thread independent from Unity/config APIs.
[DisallowMultipleComponent]
public sealed class MemeGain : MonoBehaviour
{
    internal volatile SoundKind Kind;

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float gain = GainMath.Select(Kind, Plugin.GunshotGain, Plugin.GrenadeGain, Plugin.PlaneGain);
        GainMath.Apply(data, gain);
    }
}
