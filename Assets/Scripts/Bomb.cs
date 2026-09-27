using UnityEngine;

/// <summary>
/// Visual + physical bomb. Timing and explosion are driven by <see cref="BombManager"/> so every
/// client explodes the same bombs with the same result.
/// </summary>
public class Bomb : MonoBehaviour
{
    [SerializeField] private Collider _collider;

    public int Id { get; private set; }
    public int OwnerActor { get; private set; }
    public Vector2Int Cell { get; private set; }
    public int Range { get; private set; }
    public double ExplodeAt { get; private set; }

    void Awake()
    {
        if (_collider == null) _collider = GetComponent<Collider>();
    }

    public void Init(int id, int ownerActor, Vector2Int cell, int range, double explodeAt)
    {
        Id = id;
        OwnerActor = ownerActor;
        Cell = cell;
        Range = range;
        ExplodeAt = explodeAt;
    }

    void FixedUpdate()
    {
        // The bomb starts as a trigger so whoever placed it can walk off, then becomes solid.
        if (!_collider.isTrigger) return;

        Bounds bounds = _collider.bounds;
        foreach (Collider hit in Physics.OverlapBox(bounds.center, bounds.extents))
        {
            if (hit.CompareTag("Player")) return;
        }
        _collider.isTrigger = false;
    }
}
