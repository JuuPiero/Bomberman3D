using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Custom Photon event codes used by gameplay. Events don't need a PhotonView,
/// and in offline mode (Story) PUN dispatches them locally, so the same code runs in both modes.
/// </summary>
public static class NetEvents
{
    public const byte PlaceBomb = 1;     // owner  -> others : [bombId, cell, range, explodeAt]
    public const byte Explode = 2;       // master -> others : [bombIds, fireCells, brickCells, burnedItemIds, spawnedItems]
    public const byte RequestPickup = 3; // player -> master : itemId
    public const byte ItemTaken = 4;     // master -> all    : [itemId, actorNumber]
    public const byte RoundStart = 5;    // master -> all    : start time (PhotonNetwork.Time)
    public const byte RoundEnd = 6;      // master -> all    : winner actor number (-1 = draw)

    public static void Raise(byte code, object content, ReceiverGroup receivers)
    {
        var options = new RaiseEventOptions { Receivers = receivers };
        PhotonNetwork.RaiseEvent(code, content, options, SendOptions.SendReliable);
    }

    // Grid cells travel as a single int: low 16 bits = x, high 16 bits = y (both signed).
    public static int PackCell(Vector2Int cell) => (cell.x & 0xFFFF) | (cell.y << 16);
    public static Vector2Int UnpackCell(int packed) => new Vector2Int((short)(packed & 0xFFFF), packed >> 16);
}
