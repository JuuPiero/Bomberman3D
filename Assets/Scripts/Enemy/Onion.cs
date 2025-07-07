using UnityEngine;

public class Onion : Balloon
{
    [SerializeField] protected Player _player;

    protected override void Awake()
    {
        base.Awake();
        _player = FindFirstObjectByType<Player>();
    }


    protected override void ChooseNewDirection()
    {
        Vector3 playerPos = _player.transform.position;
        Vector3 dirToPlayer = playerPos - transform.position;

        Vector3 gridCellSize = GridManager.Instance.GetCellSize();

        // Nếu người chơi cùng hàng (Z) hoặc cột (X) và không bị chắn
        if (Mathf.Abs(dirToPlayer.x) < 0.1f)
        {
            Vector3 dir = dirToPlayer.z > 0 ? Vector3.forward : Vector3.back;
            if (IsClearPath(transform.position, dir))
            {
                _currentDirection = dir;
                return;
            }
        }
        else if (Mathf.Abs(dirToPlayer.z) < 0.1f)
        {
            Vector3 dir = dirToPlayer.x > 0 ? Vector3.right : Vector3.left;
            if (IsClearPath(transform.position, dir))
            {
                _currentDirection = dir;
                return;
            }
        }

        // Nếu không nhìn thấy player, fallback như Balloon
        base.ChooseNewDirection();
    }

    private bool IsClearPath(Vector3 from, Vector3 dir)
    {
        Vector3 next = from + dir * GridManager.Instance.GetCellSize().x;

        // Check liên tục trên đường đi cho đến khi ra khỏi bản đồ hoặc gặp chướng ngại
        while (GridManager.Instance.IsInsideMap(next))
        {
            Collider[] hits = Physics.OverlapSphere(next, 0.45f);
            foreach (var hit in hits)
            {
                if (hit.gameObject.layer == LayerMask.NameToLayer("Wall") || 
                    hit.gameObject.layer == LayerMask.NameToLayer("Breakable") ||
                    hit.CompareTag("Bomb"))
                {
                    return false;
                }

                if (hit.CompareTag("Player"))
                {
                    return true;
                }
            }

            next += dir * GridManager.Instance.GetCellSize().x;
        }

        return false;
    }
}
