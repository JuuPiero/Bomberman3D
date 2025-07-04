using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    public Portal portal;
    
    public List<GameObject> itemPrefabs = new();
    [Range(0f, 1f)]
    public float spawnChance = 0.3f; // Tỷ lệ sinh item khi phá gạch, mặc định 30%

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        GridManager.Instance.SnapToGrid(portal.transform);
    }

    public void PlaceItem(Vector3 pos)
    {
        if (portal.transform.position == pos)
        {

            portal.gameObject.SetActive(true);
            return;
        }

        // Random 0-1, nếu nhỏ hơn spawnChance thì sinh item
        if (Random.value < spawnChance && itemPrefabs.Count > 0)
        {
            // Chọn ngẫu nhiên 1 prefab
            int index = Random.Range(0, itemPrefabs.Count);
            Instantiate(itemPrefabs[index], pos, Quaternion.identity);
        }
    }
}