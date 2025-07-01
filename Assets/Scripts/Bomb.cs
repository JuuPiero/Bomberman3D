using System;
using System.Collections;
using UnityEngine;
public class Bomb : MonoBehaviour
{
    public GameObject explosionPrefab;
    public float explodeDelay = 2f;
    public int explosionRange = 1;
    public float cellSize = 2f;
    [SerializeField] private Collider _collider;

    public event Action OnExploded;

    private Vector3[] _directions = new Vector3[]
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };
    void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    void Start()
    {
        StartCoroutine(Explode());
    }

    IEnumerator Explode()
    {
        yield return new WaitForSeconds(explodeDelay);
        OnExploded?.Invoke();
        ExplodeAt(transform.position);

        foreach (var dir in _directions)
        {
            for (int i = 1; i <= explosionRange; i++)
            {
                Vector3 pos = transform.position + dir * i * GridManager.Instance.GetCellSize().x;
                pos = GridManager.Instance.GetPostionCellCenter(pos);

                if (CheckObstacle(pos)) break;

                ExplodeAt(pos);
            }
        }

        Destroy(gameObject); // xoá bom
    }

    void ExplodeAt(Vector3 pos)
    {
        GameObject explosion = Instantiate(explosionPrefab, pos, Quaternion.identity);
        Destroy(explosion, 0.8f); // xóa explosion
    }

    // void OnDrawGizmos()
    // {
    //     foreach (var dir in _directions)
    //     {
    //         for (int i = 1; i <= explosionRange; i++)
    //         {
    //             Vector3 pos = transform.position + dir * i * GridManager.Instance.GetCellSize().x;
    //             pos = GridManager.Instance.GetPostionCellCenter(pos);
    //             Gizmos.DrawWireSphere(pos, GridManager.Instance.GetCellSize().x / 2f);
    //         }
    //     }
    // }

    bool CheckObstacle(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 0.99f);

        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Wall")) return true;
            if (hit.gameObject.layer == LayerMask.NameToLayer("Breakable"))
            {
                // Debug.Log("Phá gạch");
                Destroy(hit.gameObject);
                ItemManager.Instance?.PlaceItem(pos);
                return true;
            }

            if (hit.CompareTag("Enemy"))
            {
                // Debug.Log("Enemy");
                Destroy(hit.gameObject);
                return false;
            }

            if (hit.CompareTag("Player"))
            {
                Player player = hit.GetComponent<Player>();
                // Debug.Log("Chết");
                player.Die();
                return false;
            }
        }

        return false;
    }


    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _collider.isTrigger = false;
        }
    }
} 