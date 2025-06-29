using UnityEngine;
using UnityEngine.Tilemaps;
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set;}

    [Header("GamePlay tilemap")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private Grid _grid;

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
}