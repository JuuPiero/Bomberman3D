using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Power-up items. The master client rolls drops when bricks are destroyed (inside the explosion
/// event) and arbitrates pickups so an item can only be taken by one player.
/// </summary>
public class ItemManager : MonoBehaviour, IOnEventCallback
{
    public static ItemManager Instance { get; private set; }

    public Portal portal;

    public List<GameObject> itemPrefabs = new();
    [Range(0f, 1f)]
    public float spawnChance = 0.3f; // Tỷ lệ sinh item khi phá gạch, mặc định 30%

    private readonly Dictionary<int, Item> _items = new();
    private readonly HashSet<int> _claimed = new();   // master: items already given away
    private readonly HashSet<int> _requested = new(); // local: pickups waiting for the master's answer
    private int _itemCounter;
    private bool _portalEnabled = true;
    private Vector2Int _portalCell;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void Start()
    {
        if (portal == null) return;
        GridManager.Instance.SnapToGrid(portal.transform);
        _portalCell = GridManager.Instance.WorldToCell(portal.transform.position);
    }

    /// <summary>Battle mode has no exit portal.</summary>
    public void DisablePortal()
    {
        _portalEnabled = false;
        if (portal != null) portal.gameObject.SetActive(false);
    }

    private bool IsPortalCell(Vector2Int cell) => _portalEnabled && portal != null && _portalCell == cell;

    public void OnBrickDestroyed(Vector2Int cell)
    {
        if (IsPortalCell(cell)) portal.gameObject.SetActive(true);
    }

    /// <summary>Master only: ids of items standing in the given fire cells.</summary>
    public int[] GetItemIdsAt(HashSet<Vector2Int> cells)
    {
        var ids = new List<int>();
        foreach (KeyValuePair<int, Item> pair in _items)
            if (pair.Value != null && cells.Contains(pair.Value.Cell)) ids.Add(pair.Key);
        return ids.ToArray();
    }

    /// <summary>Master only: random drops for destroyed bricks, as (id, cell, prefabIndex) triplets.</summary>
    public int[] RollDrops(List<Vector2Int> bricks)
    {
        var drops = new List<int>();
        if (itemPrefabs.Count == 0) return drops.ToArray();

        foreach (Vector2Int cell in bricks)
        {
            if (IsPortalCell(cell)) continue;
            if (Random.value >= spawnChance) continue;

            int id = (PhotonNetwork.LocalPlayer.ActorNumber << 16) | (++_itemCounter & 0xFFFF);
            drops.Add(id);
            drops.Add(NetEvents.PackCell(cell));
            drops.Add(Random.Range(0, itemPrefabs.Count));
        }
        return drops.ToArray();
    }

    public void SpawnItems(int[] drops)
    {
        for (int i = 0; i + 2 < drops.Length; i += 3)
        {
            int id = drops[i];
            int prefabIndex = drops[i + 2];
            if (_items.ContainsKey(id) || prefabIndex < 0 || prefabIndex >= itemPrefabs.Count) continue;

            Vector2Int cell = NetEvents.UnpackCell(drops[i + 1]);
            GameObject go = Instantiate(itemPrefabs[prefabIndex], GridManager.Instance.CellToWorld(cell), Quaternion.identity);
            Item item = go.GetComponent<Item>();
            item.Init(id, cell);
            _items.Add(id, item);
        }
    }

    public void RemoveItems(int[] ids)
    {
        foreach (int id in ids)
            RemoveItem(id);
    }

    /// <summary>Called by an item when the local player touches it.</summary>
    public void RequestPickup(Item item)
    {
        if (!_requested.Add(item.Id)) return;
        NetEvents.Raise(NetEvents.RequestPickup, item.Id, ReceiverGroup.MasterClient);
    }

    private Item RemoveItem(int id)
    {
        _claimed.Remove(id);
        _requested.Remove(id);
        if (!_items.Remove(id, out Item item) || item == null) return null;
        Destroy(item.gameObject);
        return item;
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case NetEvents.RequestPickup:
            {
                if (!PhotonNetwork.IsMasterClient) return;
                int id = (int)photonEvent.CustomData;
                if (_items.ContainsKey(id) && _claimed.Add(id))
                    NetEvents.Raise(NetEvents.ItemTaken, new object[] { id, photonEvent.Sender }, ReceiverGroup.All);
                break;
            }
            case NetEvents.ItemTaken:
            {
                var data = (object[])photonEvent.CustomData;
                int id = (int)data[0];
                int actor = (int)data[1];
                Item item = RemoveItem(id);
                if (item != null && actor == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    AudioManager.Instance?.PlaySFX("GetItem");
                    GameManager.Instance?.LocalPlayer?.ApplyItem(item.type);
                }
                break;
            }
        }
    }
}
