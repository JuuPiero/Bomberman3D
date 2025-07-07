using UnityEngine;
using System.Collections.Generic;

public class Balloon : Enemy
{

   
    protected override void ChooseNewDirection()
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
            _currentDirection = validDirs[Random.Range(0, validDirs.Count)];
        }
        else
        {
            _currentDirection = Vector3.zero;
        }
    }
   
}