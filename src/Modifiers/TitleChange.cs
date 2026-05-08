using BepInEx.Configuration;
using HarmonyLib;

namespace QueenOfTheLosers.Modifiers;

[HarmonyPatch]
public class TitleChange : NetworkedPlayerSetting<string>
{
    public static TitleChange? Instance;
    public static void Init(ConfigEntry<string> config)
    {
        Instance = new();
        PlayerSettingSyncer.RegisterValue("QueenOfTheLosers.TitleChange", Instance);
        Instance.BindConfig(config);
    }

    public override bool TryDeserializePayload(object obj, out string value)
    {
        if(!base.TryDeserializePayload(obj, out value)) return false;

        // some lightweight sanity checking
        if(value.Length > 16) return false;
        for(int i = 0; i < value.Length; i++)
        {
            if(char.IsLetterOrDigit(value[i])) continue;
            if(value[i] == ' ') continue;
            return false;
        }

        // It's worth noting that we explicitly do not verify that this is a part of preset list of titles.
        // The goal was to allow players to set their own title, and they can add a patch to change it to whatever they want if needed, without waiting for me to update the mod.

        return true;
    }

    // These are here in case someone wants to add a patch to make it work with their language.
    public static string ReplaceText = "KING";
    public static string? ReplaceTitle(string? text, string title)
    {
        return text?.Replace(ReplaceText, title, System.StringComparison.InvariantCultureIgnoreCase);
    }

    [HarmonyPatch]
    static class Patches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ArenaMessageWinUI), nameof(ArenaMessageWinUI.ArenaText))]
        private static void ArenaMessageWinUI_ArenaText_Prefix(ref string? __0, bool __1)
        {
            //Will not work for translations that do not use the word KING, 
            if(__1 && Instance!.TryGetValue(SessionManager.instance.CrownedPlayerGet(), out string title))
            {
                __0 = ReplaceTitle(__0, title);
            }
        }
    }
}
