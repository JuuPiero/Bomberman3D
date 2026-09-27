using Photon.Realtime;

/// <summary>Keys and helpers for Photon custom properties.</summary>
public static class NetProps
{
    // Player properties
    public const string Slot = "slot";   // int 0..3, assigned by the master client, decides spawn corner and color
    public const string Ready = "ready"; // bool

    // Room properties (listed in the lobby)
    public const string Host = "host";   // host nickname, shown in the room browser

    public static int GetSlot(this Photon.Realtime.Player player)
    {
        return player.CustomProperties.TryGetValue(Slot, out object value) && value is int slot ? slot : -1;
    }

    public static bool IsReady(this Photon.Realtime.Player player)
    {
        return player.CustomProperties.TryGetValue(Ready, out object value) && value is bool ready && ready;
    }

    public static string GetHost(this RoomInfo room)
    {
        return room.CustomProperties.TryGetValue(Host, out object value) && value is string host ? host : "";
    }
}
