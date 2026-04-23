using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using REPOLib.Modules;
using System;
using UnityEngine;

namespace QueenOfTheLosers;

[BepInPlugin("QueenOfTheLosers", "Queen Of The Losers", "1.0.0")]
[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("nickklmao-REPOConfig-1.2.3", BepInDependency.DependencyFlags.SoftDependency)]
public class QueenOfTheLosers : BaseUnityPlugin
{
    internal static QueenOfTheLosers Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
#pragma warning disable IDE1006 // Naming Styles
    private ManualLogSource _logger => base.Logger;
#pragma warning restore IDE1006 // Naming Styles
    internal Harmony? Harmony { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    internal NetworkedEvent SyncTitle;
    internal ConfigEntry<string> TitleConfig;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public const string DefaultTitle = "RULER";

    private void Awake()
    {
        Instance = this;

        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        TitleConfig = Config.Bind("General", "Title Selection", DefaultTitle, new ConfigDescription("A replacement for the title of KING", new AcceptableValueList<string>("QUEEN", "RULER", "KING", "MONARCH")));
        TitleConfig.SettingChanged += (_, _) =>
        {
            PlayerTitleHandler.LocalTitle = TitleConfig.Value;
        };
        PlayerTitleHandler.LocalTitle = TitleConfig.Value;

        SyncTitle = new("SyncPreferredPlayerTitle", (e) =>
        {
            if(!(e.CustomData is string text && TryParseMessage(text, out var message)))
            {
                Logger.LogWarning("Recieved malformed player title sync message, ignoring");
                return;
            }

            PlayerTitleHandler.SetTitle(message.playerID, message.title);
            //Logger.LogInfo($"Setting Title for {message.playerID} to \"{message.title}\"");
        });

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }
    public static bool TryParseMessage(string message, out (string playerID, string title) value)
    {
        value = default;

        int index = message.IndexOf(':');

        if(index == -1) return false;

        // no need to allocate actual backing strings until we've validated the input
        ReadOnlySpan<char> playerID = message.AsSpan()[..index].Trim();
        ReadOnlySpan<char> title = message.AsSpan()[(index + 1)..].Trim();

        // if the client sends an empty or large title or playerID, ignore it, it's malformed, and either a bug or malicious behavior.
        if(playerID.Length == 0 || title.Length == 0) return false;
        if(playerID.Length > 64 || title.Length > 32) return false;

        // make sure there are no crazy characters
        for(int i = 0; i < title.Length; i++)
        {
            char c = title[i];
            if(!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_')
            {
                return false;
            }
        }

        value = (new(playerID), new(title));
        return true;
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
