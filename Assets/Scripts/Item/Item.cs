using UnityEngine;

public class Item : MonoBehaviour
{
    public enum ItemType
    {
        Bomb,
        Explosion,
        Speed
    }
    public ItemType type;

    public Vector3 rotationSpeed = new Vector3(0, 100, 0); // quay quanh trục Y

    public int Id { get; private set; }
    public Vector2Int Cell { get; private set; }

    public void Init(int id, Vector2Int cell)
    {
        Id = id;
        Cell = cell;
    }

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Only the owner of a player asks for the pickup; the master decides who gets it.
        Player player = other.GetComponent<Player>();
        if (player == null || !player.IsLocal || player.isDead) return;

        ItemManager.Instance?.RequestPickup(this);
    }
}
