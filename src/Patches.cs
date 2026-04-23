using ExitGames.Client.Photon;
using HarmonyLib;
using REPOLib.Modules;
using Steamworks;
using System.Collections.Generic;

namespace QueenOfTheLosers;

[HarmonyPatch(typeof(ArenaMessageWinUI))]
static class ExamplePlayerControllerPatches
{
    [HarmonyPrefix, HarmonyPatch(nameof(ArenaMessageWinUI.ArenaText))]
    private static void ArenaText_Prefix(ref string? __0)
    {
        if(PlayerTitleHandler.TryGetTitle(SessionManager.instance.crownedPlayerSteamID, out string title))
            __0 = __0?.Replace("KING", title);
    }
}
//[HarmonyPatch(typeof(SteamClient))]
//static class SteamClientPatches
//{
//    [HarmonyPostfix, HarmonyPatch(nameof(SteamClient.Init))]
//    private static void Init_PostFix()
//    {
//        PlayerTitleHandler.UpdateLocalTitle();
//    }
//}
[HarmonyPatch(typeof(PlayerAvatar))]
static class PlayerAvatarPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerAvatar.AddToStatsManagerRPC))]
    private static void AddToStatsManagerRPC_PostFix()
    {
        PlayerTitleHandler.SendSyncMessage();
    }
}
static class PlayerTitleHandler
{
    internal static Dictionary<string, string> playerTitles = [];
    public static bool TryGetTitle(string steamID, out string title) => playerTitles.TryGetValue(steamID, out title);
    public static void SetTitle(string steamID, string title) => playerTitles[steamID] = title;
    public static void UpdateLocalTitle() => LocalTitle = QueenOfTheLosers.Instance.TitleConfig.Value;

    private static string localTitle = QueenOfTheLosers.DefaultTitle;
    public static string LocalTitle
    {
        get => localTitle;
        set
        {
            localTitle = value;

            SendSyncMessage();
        }
    }
    public static void SendSyncMessage()
    {
        if(SteamClient.IsValid)
        {
            QueenOfTheLosers.Instance?.SyncTitle?.RaiseEvent($"{PlayerAvatar.instance.steamID}:{localTitle}", NetworkingEvents.RaiseAll, SendOptions.SendReliable);
        }
    }
}