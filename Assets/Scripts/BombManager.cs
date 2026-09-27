using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Places and explodes bombs for every player.
/// - The owner of a player places its bomb locally and tells the others (no latency for the owner).
/// - Only the master client decides when bombs explode and what they destroy (fire, bricks, chain
///   reactions, item drops), applies it locally and sends the result to the others.
/// - Each client kills its own player when it stands in fire (see <see cref="Player"/>).
/// </summary>
public class BombManager : MonoBehaviour, IOnEventCallback
{
    public static BombManager Instance { get; private set; }

    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private GameObject _explosionPrefab;
    [Tooltip("How long a fire cell stays lethal after an explosion.")]
    [SerializeField] private float _fireDuration = 0.6f;
    [SerializeField] private float _explosionLifetime = 0.8f;

    private readonly Dictionary<int, Bomb> _bombs = new();
    private readonly HashSet<int> _explodedIds = new();
    private readonly Dictionary<Vector2Int, float> _fireUntil = new();
    private readonly List<Bomb> _dueBombs = new();
    private int _localBombCounter;

    void Awake()
    {
        Instance = this;
        if (_bombPrefab == null || _explosionPrefab == null)
            Debug.LogError("[BombManager] Bomb/Explosion prefab not assigned. Run Tools > Bomberman > Setup UI Toolkit + Multiplayer.", this);
    }

    void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public bool IsOnFire(Vector2Int cell)
    {
        return _fireUntil.TryGetValue(cell, out float until) && Time.time < until;
    }

    public bool HasBombAt(Vector2Int cell)
    {
        foreach (Bomb bomb in _bombs.Values)
            if (bomb.Cell == cell) return true;
        return false;
    }

    public int CountBombs(int ownerActor)
    {
        int count = 0;
        foreach (Bomb bomb in _bombs.Values)
            if (bomb.OwnerActor == ownerActor) count++;
        return count;
    }

    /// <summary>Called by the local player. Returns false when the bomb can't be placed.</summary>
    public bool TryPlaceBomb(Player owner)
    {
        GridManager grid = GridManager.Instance;
        Vector2Int cell = grid.WorldToCell(owner.transform.position);

        if (CountBombs(owner.ActorNumber) >= owner.maxBomb) return false;
        if (HasBombAt(cell) || grid.IsBlocked(cell)) return false;

        int id = (owner.ActorNumber << 16) | (++_localBombCounter & 0xFFFF);
        double explodeAt = PhotonNetwork.Time + owner.explodeDelay;

        SpawnBomb(id, owner.ActorNumber, cell, owner.explosionRange, explodeAt);
        NetEvents.Raise(NetEvents.PlaceBomb,
            new object[] { id, NetEvents.PackCell(cell), owner.explosionRange, explodeAt },
            ReceiverGroup.Others);
        return true;
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient || _bombs.Count == 0) return;

        double now = PhotonNetwork.Time;
        _dueBombs.Clear();
        foreach (Bomb bomb in _bombs.Values)
            if (bomb.ExplodeAt <= now) _dueBombs.Add(bomb);

        if (_dueBombs.Count > 0) Detonate(_dueBombs);
    }

    private void SpawnBomb(int id, int ownerActor, Vector2Int cell, int range, double explodeAt)
    {
        if (_bombs.ContainsKey(id) || _explodedIds.Contains(id)) return;

        Bomb bomb = Instantiate(_bombPrefab, GridManager.Instance.CellToWorld(cell), Quaternion.identity);
        bomb.Init(id, ownerActor, cell, range, explodeAt);
        _bombs.Add(id, bomb);
        AudioManager.Instance?.PlaySFX("PlaceBomb");
    }

    /// <summary>Master only: resolve the blast of the given bombs (with chain reactions) and broadcast it.</summary>
    private void Detonate(List<Bomb> seeds)
    {
        GridManager grid = GridManager.Instance;

        var queue = new Queue<Bomb>(seeds);
        var bombIds = new List<int>();
        var chained = new HashSet<int>();
        foreach (Bomb seed in seeds)
        {
            chained.Add(seed.Id);
            bombIds.Add(seed.Id);
        }

        var fireSet = new HashSet<Vector2Int>();
        var fire = new List<int>();
        var brickSet = new HashSet<Vector2Int>();
        var bricks = new List<Vector2Int>();

        void AddFire(Vector2Int cell)
        {
            if (fireSet.Add(cell)) fire.Add(NetEvents.PackCell(cell));
        }

        while (queue.Count > 0)
        {
            Bomb bomb = queue.Dequeue();
            AddFire(bomb.Cell);

            foreach (Vector2Int dir in GridManager.Directions)
            {
                for (int i = 1; i <= bomb.Range; i++)
                {
                    Vector2Int cell = bomb.Cell + dir * i;
                    if (grid.IsWall(cell)) break;

                    AddFire(cell);

                    if (grid.HasBrick(cell))
                    {
                        if (brickSet.Add(cell)) bricks.Add(cell);
                        break;
                    }

                    Bomb other = FindBombAt(cell);
                    if (other != null)
                    {
                        if (chained.Add(other.Id))
                        {
                            bombIds.Add(other.Id);
                            queue.Enqueue(other);
                        }
                        break;
                    }
                }
            }
        }

        var brickCells = new int[bricks.Count];
        for (int i = 0; i < bricks.Count; i++) brickCells[i] = NetEvents.PackCell(bricks[i]);

        ItemManager items = ItemManager.Instance;
        int[] burnedItems = items != null ? items.GetItemIdsAt(fireSet) : new int[0];
        int[] newItems = items != null ? items.RollDrops(bricks) : new int[0];

        object[] payload = { bombIds.ToArray(), fire.ToArray(), brickCells, burnedItems, newItems };
        ApplyExplosion(payload);
        NetEvents.Raise(NetEvents.Explode, payload, ReceiverGroup.Others);
    }

    private void ApplyExplosion(object[] payload)
    {
        var bombIds = (int[])payload[0];
        var fire = (int[])payload[1];
        var bricks = (int[])payload[2];
        var burnedItems = (int[])payload[3];
        var newItems = (int[])payload[4];

        foreach (int id in bombIds)
        {
            _explodedIds.Add(id);
            if (_bombs.Remove(id, out Bomb bomb) && bomb != null)
                Destroy(bomb.gameObject);
        }

        GridManager grid = GridManager.Instance;
        float until = Time.time + _fireDuration;
        foreach (int packed in fire)
        {
            Vector2Int cell = NetEvents.UnpackCell(packed);
            _fireUntil[cell] = until;
            GameObject vfx = Instantiate(_explosionPrefab, grid.CellToWorld(cell), Quaternion.identity);
            Destroy(vfx, _explosionLifetime);
        }

        ItemManager items = ItemManager.Instance;
        foreach (int packed in bricks)
        {
            Vector2Int cell = NetEvents.UnpackCell(packed);
            grid.DestroyBrick(cell);
            items?.OnBrickDestroyed(cell);
        }

        if (items != null)
        {
            items.RemoveItems(burnedItems);
            items.SpawnItems(newItems);
        }

        AudioManager.Instance?.PlaySFX("Explosion");
    }

    private Bomb FindBombAt(Vector2Int cell)
    {
        foreach (Bomb bomb in _bombs.Values)
            if (bomb.Cell == cell) return bomb;
        return null;
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case NetEvents.PlaceBomb:
            {
                var data = (object[])photonEvent.CustomData;
                SpawnBomb((int)data[0], photonEvent.Sender, NetEvents.UnpackCell((int)data[1]), (int)data[2], (double)data[3]);
                break;
            }
            case NetEvents.Explode:
            {
                var payload = (object[])photonEvent.CustomData;
                var bombIds = (int[])payload[0];
                // Ignore duplicates (can happen right after a master client switch).
                if (bombIds.Length > 0 && _explodedIds.Contains(bombIds[0])) return;
                ApplyExplosion(payload);
                break;
            }
        }
    }
}
