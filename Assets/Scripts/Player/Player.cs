using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public GameObject bombPrefab;
    public GameObject explosionPrefab;


    [field: SerializeField] public Animator Anim { get; private set; }
    [field: SerializeField] public Rigidbody RB { get; private set; }
    [field: SerializeField] public StateMachine StateMachine { get; private set; }


    public Vector3 InputDirection { get; private set; }
    public float speed = 3f;
    public int maxBomb = 1;
    private int _currentBombCount = 0;
    public int explosionRange = 1;

    public event Action OnPlayerDeath;
    public bool isDead = false;

    public float explodeDelay = 2f;

    void Awake()
    {
        Anim = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody>();
        StateMachine = new StateMachine();
    }

    void Start()
    {
        StateMachine.AddState(new PlayerIdleState(this, "Idle"));
        StateMachine.AddState(new PlayerWalkState(this, "Walk"));
        StateMachine.AddState(new PlayerDieState(this, "Die"));

        StateMachine.Initialize(StateMachine.GetState<PlayerIdleState>());
    }

    private void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        InputDirection = new Vector3(horizontal, 0f, vertical);
        if (Input.GetButtonDown("Jump"))
        {
            PlaceBomb();
        }
    }
    private void Update()
    {
        HandleInput();
        HandleFlip();
        StateMachine?.Update();
    }

    private void FixedUpdate()
    {
        RB.linearVelocity = new Vector3(InputDirection.x * speed, RB.linearVelocity.y, InputDirection.z * speed);
        StateMachine?.FixedUpdate();
    }
    private void HandleFlip()
    {
        if (InputDirection.sqrMagnitude > 0.01f && !isDead)
        {
            Quaternion targetRotation = Quaternion.LookRotation(InputDirection);
            // Xoay dần cho mượt
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void PlaceBomb()
    {
        if (_currentBombCount >= maxBomb) return;
        AudioManager.Instance?.PlaySFX("PlaceBomb");
        Vector3 placePos = GridManager.Instance.GetPostionCellCenter(transform.position);
        GameObject bombGO = Instantiate(bombPrefab, placePos, Quaternion.identity);
        Bomb bomb = bombGO.GetComponent<Bomb>();
        bomb.explosionRange = explosionRange;
        bomb.explodeDelay = explodeDelay;
        _currentBombCount++;

        bomb.OnExploded += () => _currentBombCount--;
    }
    public void Die()
    {
        isDead = true;
        speed = 0f;
        OnPlayerDeath?.Invoke();
    }
}
