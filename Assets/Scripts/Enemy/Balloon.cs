using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Balloon : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float gridSize = 2f; // kích thước 1 ô
     private Vector3 currentDirection;
    private Vector3 targetPosition;
    private bool isMoving = false;

    private readonly Vector3[] directions = new Vector3[]
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };

    void Start()
    {
        // SnapToGrid();
        ChooseNewDirection();
    }

    void Update()
    {
        if (isMoving)
        {
            MoveToTarget();
        }
        else
        {
            TryMove();
        }
    }

    void TryMove()
    {
        Vector3 nextPos = transform.position + currentDirection * gridSize;

        if (IsWalkable(nextPos))
        {
            targetPosition = nextPos;
            isMoving = true;
            RotateTo(currentDirection);
        }
        else
        {
            ChooseNewDirection();
        }
    }

    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    void ChooseNewDirection()
    {
        List<Vector3> validDirs = new List<Vector3>();

        foreach (Vector3 dir in directions)
        {
            Vector3 checkPos = transform.position + dir * gridSize;
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
        Collider[] hits = Physics.OverlapSphere(pos, 0.05f);
        foreach (var hit in hits)
        {
            Transform current = hit.transform;
            while (current != null)
            {
                GameObject obj = current.gameObject;
                if (obj.layer == LayerMask.NameToLayer("Wall") || obj.layer == LayerMask.NameToLayer("Breakable"))
                {
                    return false;
                }

                if (obj.CompareTag("Bomb"))
                {
                    return false;
                }

                current = current.parent;
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

    void SnapToGrid()
    {
        // chỉnh vị trí về đúng ô để tránh lệch lưới
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
        pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
        transform.position = pos;
    }
}