using System;
using System.Diagnostics.CodeAnalysis;

namespace QueenOfTheLosers;

public struct PlayerID : IEquatable<PlayerID>
{
    private static PlayerID local;
    public static PlayerID Local
    {
        get
        {
            local.steamID ??= SemiFunc.PlayerAvatarLocal()?.steamID!;
            return local;
        }
    }

    public static bool LocalSteamIDFound => Local.HasSteamID;

    private string steamID;
    public string SteamID => steamID ??= Local.steamID;

    public PlayerAvatar? Avatar => GameDirector.instance == null ? null : SemiFunc.PlayerAvatarGetFromSteamID(SteamID);
    public bool IsLocal => SteamID == Local.SteamID;
    public bool HasSteamID => SteamID != null;

    [return: NotNullIfNotNull(nameof(avatar))]
    public static PlayerID? FromAvatar(PlayerAvatar avatar) => avatar == null ? null : new() { steamID = avatar.steamID };

    [return: NotNullIfNotNull(nameof(avatar))]
    public static implicit operator PlayerID?(PlayerAvatar avatar) => FromAvatar(avatar);

    public override string ToString() => $"PlayerID: {SteamID?.ToString() ?? "Local Unknown"}";
    public override int GetHashCode() => SteamID == Local.SteamID ? 0 : SteamID.GetHashCode();
    public override readonly bool Equals(object? obj) => obj is PlayerID iD && Equals(iD);
    public readonly bool Equals(PlayerID other) => this == other;
    public static bool operator ==(PlayerID left, PlayerID right) => left.SteamID == right.SteamID;
    public static bool operator !=(PlayerID left, PlayerID right) => left.SteamID != right.SteamID;
}
