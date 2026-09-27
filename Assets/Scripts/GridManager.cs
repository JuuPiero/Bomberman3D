using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("GamePlay tilemap")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private Grid _grid;

    // Logical map built from the scene colliders. Every client builds the same map from the same scene,
    // and bricks are only removed through explosion events, so the map stays identical everywhere.
    private readonly HashSet<Vector2Int> _walls = new();
    private readonly Dictionary<Vector2Int, GameObject> _bricks = new();
    private RectInt _bounds;
    private bool _built;

    public static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    void Awake()
    {
        Instance = this;
        _grid = GetComponent<Grid>();
    }

    public Vector3 GetCellSize()
    {
        return _grid.cellSize;
    }


    public Vector3Int GetPositionOfObjectInGrid(Vector3 postion)
    {
        return _tilemap.WorldToCell(postion);
    }

    public Vector3 GetPostionCellCenter(Vector3 postion)
    {
        Vector3Int cellPos = _tilemap.WorldToCell(postion);
        Vector3 spawnPos = _tilemap.GetCellCenterWorld(cellPos);
        // spawnPos.y = _tilemap.transform.position.y;
        spawnPos.y = 2.5f;
        return spawnPos;
    }

    public void SnapToGrid(Transform trans)
    {
        trans.position = GetPostionCellCenter(trans.position);
    }

    public bool IsInsideMap(Vector3 worldPos)
    {
        Vector3Int cell = _tilemap.WorldToCell(worldPos);
        if (_tilemap.HasTile(cell)) return true;

        EnsureBuilt();
        return _bounds.Contains(new Vector2Int(cell.x, cell.y));
    }

    #region Logical grid

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3Int cell = _tilemap.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y); // grid swizzle is XZY: cell.y is the world Z axis
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 pos = _tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        pos.y = 2.5f;
        return pos;
    }

    public bool IsWall(Vector2Int cell)
    {
        EnsureBuilt();
        return _walls.Contains(cell) || !_bounds.Contains(cell);
    }

    public bool HasBrick(Vector2Int cell)
    {
        EnsureBuilt();
        return _bricks.TryGetValue(cell, out GameObject brick) && brick != null;
    }

    public bool IsBlocked(Vector2Int cell) => IsWall(cell) || HasBrick(cell);

    public void DestroyBrick(Vector2Int cell)
    {
        EnsureBuilt();
        if (_bricks.Remove(cell, out GameObject brick) && brick != null)
            Destroy(brick);
    }

    /// <summary>Destroys bricks around a cell (used to give every spawn corner room to escape).</summary>
    public void ClearBricksAround(Vector2Int center, int radius)
    {
        DestroyBrick(center);
        foreach (Vector2Int dir in Directions)
            for (int i = 1; i <= radius; i++)
                DestroyBrick(center + dir * i);
    }

    /// <summary>The four inner corners of the map: top-left, bottom-right, top-right, bottom-left.</summary>
    public Vector2Int[] GetCornerCells()
    {
        EnsureBuilt();
        int minX = _bounds.xMin + 1, maxX = _bounds.xMax - 2;
        int minY = _bounds.yMin + 1, maxY = _bounds.yMax - 2;
        return new[]
        {
            new Vector2Int(minX, maxY),
            new Vector2Int(maxX, minY),
            new Vector2Int(maxX, maxY),
            new Vector2Int(minX, minY),
        };
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        int wallLayer = LayerMask.NameToLayer("Wall");
        int brickLayer = LayerMask.NameToLayer("Breakable");
        var min = new Vector2Int(int.MaxValue, int.MaxValue);
        var max = new Vector2Int(int.MinValue, int.MinValue);

        foreach (Collider col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (col.isTrigger) continue;
            int layer = col.gameObject.layer;
            if (layer != wallLayer && layer != brickLayer) continue;

            Vector2Int cell = WorldToCell(col.bounds.center);
            if (layer == wallLayer)
            {
                _walls.Add(cell);
                min = Vector2Int.Min(min, cell);
                max = Vector2Int.Max(max, cell);
            }
            else
            {
                // Use the top-most object of the brick so the whole brick gets destroyed.
                Transform root = col.transform;
                while (root.parent != null && root.parent.gameObject.layer == brickLayer) root = root.parent;
                _bricks[cell] = root.gameObject;
            }
        }

        _bounds = _walls.Count > 0
            ? new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1)
            : new RectInt(-1000, -1000, 2000, 2000);
    }

    #endregion
}
