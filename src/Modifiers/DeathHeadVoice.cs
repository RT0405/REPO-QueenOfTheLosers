using BepInEx.Configuration;
using HarmonyLib;

namespace QueenOfTheLosers.Modifiers;

[HarmonyPatch]
public static class DeathHeadVoice
{
    static NetworkedPlayerSetting<float>? Pitch;
    static NetworkedPlayerSetting<float>? Inflection;
    public static void Init(ConfigEntry<float> pitch, ConfigEntry<float> inflection)
    {
        Pitch = new();
        Inflection = new();
        PlayerSettingSyncer.RegisterValue("QueenOfTheLosers.DeathHeadVoicePitchSetting", Pitch);
        PlayerSettingSyncer.RegisterValue("QueenOfTheLosers.DeathHeadVoiceInflectionSetting", Inflection);
        Pitch.BindConfig(pitch);
        Inflection.BindConfig(inflection);
    }

    [HarmonyPatch]
    static class Patches
    {
        // I will be the first to admit that this is a really janky and unpleasant way to do this.
        // I first tried to copy and modify the death head audio mixer, but found that Unity (afaict) does not allow cloning or creating audio mixers at runtime

        static bool CallOverride;
        [HarmonyPrefix, HarmonyPatch(typeof(PlayerDeathHead), nameof(PlayerDeathHead.Update))]
        private static void PlayerDeathHead_Update_Prefix(PlayerDeathHead __instance)
        {
            // only enable the override if they possesing their body
            CallOverride = __instance.spectated;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(PlayerVoiceChat), nameof(PlayerVoiceChat.OverridePitch))]
        private static void PlayerVoiceChat_OverridePitch_Prefix(PlayerVoiceChat __instance, ref float _multiplier)
        {
            // as of 2026-05-03 (v0.3.2), the only place OverridePitch is called in relation to death head is in the Update method of PlayerDeathHead, in order to down pitch when the player is low battery
            // hence, this patch should only override the low battery inflection
            // should be updated if this fact changes

            if(CallOverride && Pitch!.TryGetValue(__instance.playerAvatar, out var pitch) && Inflection!.TryGetValue(__instance.playerAvatar, out var inflection))
            {
                // replace the original low battery pitch
                _multiplier = pitch * inflection;
            }
            // prevent manual override
            CallOverride = false;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerDeathHead), nameof(PlayerDeathHead.Update))]
        private static void PlayerDeathHead_Update_Postfix(PlayerDeathHead __instance)
        {
            // if a call was not overridden, call it ourself
            if(CallOverride && Pitch!.TryGetValue(__instance.playerAvatar, out var value))
            {
                CallOverride = false; // ensure our call doesn't get overidden
                __instance.playerAvatar.voiceChat.OverridePitch(value, 0.25f, 0.25f);
            }
            // prevent unrelated OverridePitch calls from being overidden
            CallOverride = false;
        }
    }
}