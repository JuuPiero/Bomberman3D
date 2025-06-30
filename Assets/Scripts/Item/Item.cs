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

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            switch (type)
            {
                case ItemType.Bomb:
                    player.maxBomb += 1;
                    break;
                case ItemType.Explosion:
                    player.explosionRange += 1;
                    break;
                case ItemType.Speed:
                    player.speed += 1f;
                    break;
                default:
                    break;
            }
            Destroy(gameObject);
        }
    }
}