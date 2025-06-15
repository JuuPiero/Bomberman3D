using UnityEngine;

public class Player : MonoBehaviour
{
    public GameObject bombPrefab;
    public GameObject explosionPrefab;


    [field: SerializeField] public Animator Anim { get; private set; }
    [field: SerializeField] public Rigidbody RB { get; private set; }
    [field: SerializeField] public StateMachine StateMachine { get; private set; }

    private TilemapDetector _detector;

    public Vector3 InputDirection { get; private set; }
    public float speed = 3f;
    public int maxBomb = 1;
    public int explosionRange = 1;


    void Awake()
    {
        Anim = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody>();
        StateMachine = new StateMachine();

        _detector = GetComponentInChildren<TilemapDetector>();
    }

    void Start()
    {
        StateMachine.AddState(new PlayerIdleState(this, "Idle"));
        StateMachine.AddState(new PlayerWalkState(this, "Walk"));
        StateMachine.Initialize();
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        InputDirection = new Vector3(horizontal, 0f, vertical);

        if (InputDirection.sqrMagnitude > 0.01f)
        {
            // Lấy góc quay từ hướng di chuyển
            Quaternion targetRotation = Quaternion.LookRotation(InputDirection);
            // Xoay dần cho mượt
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (Input.GetButtonDown("Jump"))
        {
            // var pos = GridManager.Instance.GetPostionCellCenter(transform.position);
            // print(pos);
            // _detector.DetectTileAndBomb(transform.position);
            PlaceBomb();
        }

        StateMachine?.Update();
    }

    void FixedUpdate()
    {
        RB.linearVelocity = new Vector3(InputDirection.x * speed, RB.linearVelocity.y, InputDirection.z * speed);

        StateMachine?.FixedUpdate();
    }

    void PlaceBomb()
    {
        Vector3 placePos = GridManager.Instance.GetPostionCellCenter(transform.position);
        Instantiate(bombPrefab, placePos, Quaternion.identity);
    }

    
}
