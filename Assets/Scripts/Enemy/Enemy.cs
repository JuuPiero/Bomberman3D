using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public float gridSize = 2f; 

    public Rigidbody RB { get; private set; }

    protected readonly Vector3[] _directions = new Vector3[]
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };
    [SerializeField] protected float _moveSpeed = 2f;
    [SerializeField] protected Vector3 _currentDirection;
    public int score = 100;
    protected virtual void Awake()
    {
        RB = GetComponent<Rigidbody>();
    }
    protected virtual void Start()
    {
        GridManager.Instance?.SnapToGrid(transform);
        ChooseNewDirection();
    }

    protected virtual void FixedUpdate()
    {
        if (_currentDirection != Vector3.zero)
        {
            RB.linearVelocity = new Vector3(_moveSpeed * _currentDirection.x, 0f, _moveSpeed * _currentDirection.z);
            RotateTo(_currentDirection);
        }
    }

    protected virtual void RotateTo(Vector3 dir)
    {
        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
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
            GridManager.Instance.SnapToGrid(transform);
            ChooseNewDirection();
            RotateTo(_currentDirection);
        }

    }
    protected virtual bool IsWalkable(Vector3 pos)
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


    void OnDrawGizmos()
    {
        foreach (var dir in _directions)
        {
            Gizmos.DrawWireSphere(transform.position + dir * gridSize, 0.5f);
        }
    }

    protected abstract void ChooseNewDirection();

}