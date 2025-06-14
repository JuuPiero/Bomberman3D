using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapDetector : MonoBehaviour
{
    public Tilemap tilemap; // Gán Tilemap trong Inspector
    public Player _player;
    void Awake()
    {
        _player = GetComponentInParent<Player>();
        // print(tilemap.cellSize);
    }
    // void Update()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         // Raycast từ chuột vào Tilemap
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             // Tính cell
    //             Vector3Int cellPos = tilemap.WorldToCell(hit.point);
    //             Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);

    //             print(spawnPos);
    //             // Kiểm tra đã có gì ở ô đó chưa (tuỳ cách bạn lưu prefab hoặc parent)
                
    //             Instantiate(_player.bombPrefab, spawnPos, Quaternion.identity);
    //             Debug.Log("Đã cuốc đất tại: " + cellPos);
    //             // Collider[] hits = Physics.OverlapSphere(spawnPos, 0.1f);
    //             // bool hasObject = hits.Length > 0;
    //             // if (!hasObject)
    //             // {
    //             //     // Spawn đất đã cuốc (prefab 3D)
    //             //     Instantiate(_player.bombPrefab, spawnPos, Quaternion.identity);
    //             //     Debug.Log("Đã cuốc đất tại: " + cellPos);
    //             // }
    //         }
    //     }
    // }
    public void DetectTileAndBomb(Vector3 position)
    {
        Vector3Int cellPos = tilemap.WorldToCell(_player.transform.position);
        Vector3 spawnPos = tilemap.GetCellCenterWorld(cellPos);
        spawnPos.y = 2f;
        Instantiate(_player.bombPrefab, spawnPos, Quaternion.identity);
    }
}