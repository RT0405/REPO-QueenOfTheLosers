using BepInEx.Configuration;
using ExitGames.Client.Photon;
using HarmonyLib;
using REPOLib.Modules;
using System.Collections.Generic;

namespace QueenOfTheLosers;

//note: clients always only send their own data, this simplifies handling in a few cases (such as when their steamID is not yet known)
public static class PlayerSettingSyncer
{
    private static NetworkedEvent SyncEvent = null!;
    private static long SequenceNumber;
    private static bool isDirty;

    public static Dictionary<string, NetworkedPlayerSetting> Settings = [];
    public static void Init()
    {
        SyncEvent = new("RT0405.QueenOfTheLosers.Sync", (e) =>
        {
            if(!RecieveMessage(e))
            {
                QueenOfTheLosers.Logger.LogWarning($"invalid message recieved in PlayerSettingSyncer");
            }
        });
    }
    public static void SendIfDirty()
    {
        if(isDirty)
            SendPlayerSettings();
    }
    public static void SetDirty()
    {
        isDirty = true;
    }
    public static void RegisterValue(string id, NetworkedPlayerSetting value)
    {
        Settings.Add(id, value);
    }
    public static void SendPlayerSettings()
    {
        if(!PlayerID.Local.HasSteamID) return;

        Hashtable settings = [];

        foreach(var pair in Settings)
        {
            var setting = pair.Value;
            if(setting.TryGetPayload(out var payload))
                settings.Add(pair.Key, payload);
        }

        SyncEvent?.RaiseEvent(new object[]
        {
            PlayerID.Local.SteamID,
            SequenceNumber++,
            settings
        }, NetworkingEvents.RaiseOthers, SendOptions.SendReliable);

        isDirty = false;
    }
    public static bool RecieveMessage(EventData e)
    {
        if(e.CustomData is not object[] arr) return false;

        if(arr.Length != 3) return false;

        if(arr[0] is not string id) return false;
        PlayerID? playerID = SemiFunc.PlayerAvatarGetFromSteamID(id); // surely there's got to be a better way to access the steam id of the sender, but for now this works, even if other players can set settings of eachother
        if(playerID == null) return false;

        if(arr[1] is not long seqNum) return false;

        if(arr[2] is not Hashtable settings) return false;
        foreach(var entry in settings)
        {
            if(entry.Key is not string key) continue;

            if(!Settings.TryGetValue(key, out var value)) continue;

            value.ProcessPayload(playerID.Value, entry.Value, seqNum);
        }
        return true;
    }
}
public abstract class NetworkedPlayerSetting
{
    public abstract bool TryGetPayload(out object payload);
    public abstract bool ProcessPayload(PlayerID playerID, object payload, long seqNum);
}
public class NetworkedPlayerSetting<T> : NetworkedPlayerSetting
{
    protected readonly Dictionary<PlayerID, (T Value, long SequenceNumber)> Values = [];
    public bool TryGetValue(PlayerAvatar avatar, out T value)
    {
        value = default!;
        PlayerID? playerID = PlayerID.FromAvatar(avatar);
        if(playerID == null) return false;
        return TryGetValue(playerID.Value, out value);
    }
    public bool TryGetValue(PlayerID playerID, out T value)
    {
        if(Values.TryGetValue(playerID, out var entry))
        {
            value = entry.Value;
            return true;
        }
        else
        {
            value = default!;
            return false;
        }
    }
    private void Set(PlayerID playerID, T value, long seqNum)
    {
        Values[playerID] = (value, seqNum);
        OnValueChanged?.Invoke(playerID, value);
    }
    public void SetLocalValue(T value)
    {
        Set(PlayerID.Local, value, 0);
        PlayerSettingSyncer.SetDirty();
    }
    public void BindConfig(ConfigEntry<T> config)
    {
        config.SettingChanged += (_, _) =>
        {
            SetLocalValue(config.Value);
        };
        SetLocalValue(config.Value);
    }
    public override bool TryGetPayload(out object payload)
    {
        if(TryGetValue(PlayerID.Local, out var value))
        {
            payload = value!;
            return true;
        }
        else
        {
            payload = null!;
            return false;
        }
    }
    public override bool ProcessPayload(PlayerID playerID, object payload, long seqNum)
    {
        //avoid updating with outdated values
        if(Values.TryGetValue(playerID, out var val) && val.SequenceNumber >= seqNum) return true;

        if(TryDeserializePayload(payload, out var value))
        {
            Set(playerID, value, seqNum);
            return true;
        }
        return false;
    }
    //only works for types suported by Photon
    public virtual bool TryDeserializePayload(object obj, out T value)
    {
        if(obj is T v)
        {
            value = v;
            return true;
        }
        else
        {
            value = default!;
            return false;
        }
    }
    public delegate void OnValueChangedDelegate(PlayerID playerID, T value);
    public event OnValueChangedDelegate? OnValueChanged;
}
[HarmonyPatch(typeof(PlayerAvatar))]
static class PlayerAvatarPatches
{
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerAvatar.Awake))]
    private static void Awake_PostFix()
    {
        PlayerSettingSyncer.SetDirty();
    }
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerAvatar.AddToStatsManagerRPC))]
    private static void AddToStatsManagerRPC_PostFix()
    {
        PlayerSettingSyncer.SetDirty();
    }
}