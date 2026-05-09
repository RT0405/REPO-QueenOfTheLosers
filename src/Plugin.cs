using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using QueenOfTheLosers.Modifiers;
using UnityEngine;

namespace QueenOfTheLosers;

[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("nickklmao-REPOConfig-1.2.3", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("QueenOfTheLosers", "Queen Of The Losers", "1.1.1")]
public class QueenOfTheLosers : BaseUnityPlugin
{
    internal static QueenOfTheLosers Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
#pragma warning disable IDE1006 // Naming Styles
    private ManualLogSource _logger => base.Logger;
#pragma warning restore IDE1006 // Naming Styles
    internal Harmony? Harmony { get; set; }

    internal ConfigEntry<string>? TitleConfig;
    internal ConfigEntry<float>? DeathHeadPitchConfig;
    internal ConfigEntry<float>? DeathHeadInflectionConfig;
    internal ConfigEntry<float>? TTSPitchConfig;

    internal void Awake()
    {
        Instance = this;
        PlayerSettingSyncer.Init();

        // Prevent the plugin from being deleted
        transform.parent = null;
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        TitleConfig = Config.Bind("Title", "Title Selection", "RULER", new ConfigDescription("A replacement for the title of KING in the \"King of the Losers\" screen", new AcceptableValueList<string>("QUEEN", "RULER", "KING", "MONARCH")));
        TitleChange.Init(TitleConfig);

        DeathHeadPitchConfig = Config.Bind("Death Head Voice", "Pitch", 1f, new ConfigDescription("A modifier for the pitch of your voice while you're possesing your head, Vanilla is 1.0. Note: extreme values can make your voice unintelligible", new AcceptableValueRange<float>(0.8f, 1.8f)));
        DeathHeadInflectionConfig = Config.Bind("Death Head Voice", "Low Battery Inflection", 0.5f, new ConfigDescription("Changes the inflection that is applied when you're running out of battery, Vanilla is 0.5. Note: extreme values can make your voice even more unintelligible", new AcceptableValueRange<float>(0.4f, 1.8f)));
        DeathHeadVoice.Init(DeathHeadPitchConfig, DeathHeadInflectionConfig);

        TTSPitchConfig = Config.Bind("TTS", "Pitch", 0f, new ConfigDescription("A modifier for the pitch of your TTS voice, Vanilla is 0", new AcceptableValueRange<float>(-0.5f, 1f)));
        TTSVoicePitch.Init(TTSPitchConfig);
    }
    internal void Update()
    {
        PlayerSettingSyncer.SendIfDirty();
    }
    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }
    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }
}
