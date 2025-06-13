using UnityEngine;

public class Player : MonoBehaviour
{
    [field: SerializeField] public Animator Anim { get; private set; }
    [field: SerializeField] public Rigidbody RB { get; private set; }

    [field: SerializeField] public StateMachine StateMachine { get; private set; }

    public float Horizontal { get; set; }
    public float Vertical { get; set; }


    public Vector3 InputDirection { get; set; }

    public float speed = 3f;


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
        StateMachine.Initialize();
    }

    void Update()
    {
        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");
        InputDirection = new Vector3(Horizontal, 0f, Vertical);

        if (InputDirection.sqrMagnitude > 0.01f)
        {
            // Lấy góc quay từ hướng di chuyển
            Quaternion targetRotation = Quaternion.LookRotation(InputDirection);
            // Xoay dần cho mượt
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        StateMachine?.Update();
    }

    void FixedUpdate()
    {
        RB.linearVelocity = new Vector3(Horizontal * speed, RB.linearVelocity.y, Vertical * speed);

        StateMachine?.FixedUpdate();
    }
}
