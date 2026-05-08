using BepInEx.Configuration;
using HarmonyLib;

namespace QueenOfTheLosers.Modifiers;

// this relies on the fact that vanilla has this TTSPitchChange field with is never set anywhere and only used to add to the TTS pitch
// it's not great but it works
[HarmonyPatch]
public static class TTSVoicePitch
{
    static NetworkedPlayerSetting<float>? Pitch;
    public static void Init(ConfigEntry<float> pitch)
    {
        Pitch = new();
        PlayerSettingSyncer.RegisterValue("QueenOfTheLosers.TTSVoicePitchSetting", Pitch);
        Pitch.OnValueChanged += (playerID, value) =>
        {
            if(playerID.Avatar?.voiceChat != null)
                playerID.Avatar.voiceChat.TTSPitchChange = value;
        };
        Pitch.BindConfig(pitch);
    }

    [HarmonyPatch]
    static class Patches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(TTSVoice), nameof(TTSVoice.Start))]
        private static void TTS_Start_Postfix(TTSVoice __instance)
        {
            if(Pitch?.TryGetValue(__instance.playerAvatar, out float pitch) ?? false)
                __instance.playerAvatar.voiceChat.TTSPitchChange = pitch;
        }
    }
}