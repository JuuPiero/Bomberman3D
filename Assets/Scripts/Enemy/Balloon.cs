using UnityEngine;
using System.Collections.Generic;

public class Balloon : MonoBehaviour
{
    public Rigidbody RB { get; private set; }
    public float moveSpeed = 2f;
    public float gridSize = 2f; 
    [SerializeField] private Vector3 currentDirection;

    private readonly Vector3[] _directions = new Vector3[]
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };
    void Awake()
    {
        RB = GetComponent<Rigidbody>();
    }

    void Start()
    {
        SnapToGrid();
        ChooseNewDirection();
    }

   
    void FixedUpdate()
    {
        if (currentDirection != Vector3.zero)
        {
            RB.linearVelocity = new Vector3(moveSpeed * currentDirection.x, 0f, moveSpeed * currentDirection.z);
        }
    }

    void ChooseNewDirection()
    {
        List<Vector3> validDirs = new List<Vector3>();

        foreach (Vector3 dir in _directions)
        {
            Vector3 checkPos = transform.position + dir * GridManager.Instance.GetCellSize().x;
            if (IsWalkable(checkPos))
                validDirs.Add(dir);
        }

        if (validDirs.Count > 0)
        {
            currentDirection = validDirs[Random.Range(0, validDirs.Count)];
        }
        else
        {
            currentDirection = Vector3.zero;
        }
    }

    bool IsWalkable(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos , 0.5f);
        foreach (var hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Wall") || hit.gameObject.layer == LayerMask.NameToLayer("Breakable"))
            {
                return false;
            }

            if (hit.CompareTag("Bomb"))
            {
                return false;
            }
        }
        return true;
    }

    void RotateTo(Vector3 dir)
    {
        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = rot;
        }
    }

    void OnDrawGizmos()
    {
        foreach (var dir in _directions)
        {
            Gizmos.DrawWireSphere(transform.position + dir * gridSize, 0.5f);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Player player = other.gameObject.GetComponent<Player>();
            player.Die();
            ChooseNewDirection();
            return;
        }
        

        if (other != null)
        {
            SnapToGrid();
            ChooseNewDirection();
            RotateTo(currentDirection);
        }

    }

    void SnapToGrid()
    {
        // chỉnh vị trí về đúng ô để tránh lệch lưới
        transform.position = GridManager.Instance.GetPostionCellCenter(transform.position);
    }
}