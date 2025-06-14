using System.Collections;
using UnityEngine;
public class Bomb : MonoBehaviour
{
    public GameObject explosionPrefab;
    public float explodeDelay = 2f;
    public int explosionRange = 1;
    public float cellSize = 2f;

    private Collider _collider;

    private Vector3[] _directions = new Vector3[]
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };
    void Awake()
    {
        _collider = GetComponentInChildren<Collider>();
    }

    void Start()
    {
        StartCoroutine(Explode());
    }

    IEnumerator Explode()
    {
        yield return new WaitForSeconds(explodeDelay);

        ExplodeAt(transform.position);

        foreach (var dir in _directions)
        {
            for (int i = 1; i <= explosionRange; i++)
            {
                Vector3 pos = transform.position + dir * i * cellSize;

                if (CheckObstacle(pos)) break;

                ExplodeAt(pos);
            }
        }

        Destroy(gameObject); // xoá bom
    }

    void ExplodeAt(Vector3 pos)
    {
        GameObject explosion = Instantiate(explosionPrefab, pos, Quaternion.identity);
        Destroy(explosion, 0.8f); 
    }

    bool CheckObstacle(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 0.5f);

        foreach (var hit in hits)
        {
            Transform current = hit.transform;
            // Dò lên đến cha nếu cha có tag/layer phù hợp
            while (current != null)
            {
                GameObject obj = current.gameObject;

                if (obj.layer == LayerMask.NameToLayer("Wall"))
                {
                    return true;
                }

                if (obj.layer == LayerMask.NameToLayer("Breakable"))
                {
                    Debug.Log("Phá gạch");
                    Destroy(obj);
                    return true;
                }

                if (obj.CompareTag("Player"))
                {
                    Debug.Log("Chết");
                    // Destroy(obj);
                    return true;
                }
                current = current.parent;
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