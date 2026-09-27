using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Player : MonoBehaviourPun, IPunObservable
{
    /// <summary>Every player currently in the scene (local and remote).</summary>
    public static readonly List<Player> All = new();
    public static event Action<Player> Spawned;
    public static event Action<Player> Despawned;

    [field: SerializeField] public Animator Anim { get; private set; }
    [field: SerializeField] public Rigidbody RB { get; private set; }
    [field: SerializeField] public StateMachine StateMachine { get; private set; }


    public Vector3 InputDirection { get; private set; }
    public float speed = 3f;
    public int maxBomb = 1;
    public int explosionRange = 1;

    public event Action OnPlayerDeath;
    public event Action OnStatsChanged;
    public bool isDead = false;

    public float explodeDelay = 2f;

    const int MaxBombLimit = 8;
    const int MaxRangeLimit = 8;
    const float MaxSpeedLimit = 10f;

    private Collider _collider;

    /// <summary>Lobby slot (0..3): spawn corner and player color.</summary>
    public int Slot { get; private set; }
    public int ActorNumber => photonView.OwnerActorNr;
    public string NickName => photonView.Owner != null && !string.IsNullOrEmpty(photonView.Owner.NickName) ? photonView.Owner.NickName : "Player";
    public bool IsLocal => photonView.IsMine;

    void Awake()
    {
        // Players are spawned through PhotonNetwork.Instantiate; a copy left in the scene is not used.
        if (!photonView.isRuntimeInstantiated)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Anim = GetComponentInChildren<Animator>();
        RB = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        StateMachine = new StateMachine();

        // PhotonView skips its "auto find observables" for objects created by PhotonNetwork.Instantiate,
        // so the prefab's list (TransformView only) is used as is. Add this script so the movement
        // direction (and therefore Idle/Walk animations) reaches the other clients. Every client runs
        // this, so the serialization order stays the same everywhere.
        photonView.ObservedComponents ??= new List<Component>();
        photonView.ObservedComponents.RemoveAll(component => component == null);
        if (!photonView.ObservedComponents.Contains(this))
            photonView.ObservedComponents.Add(this);

        object[] data = photonView.InstantiationData;
        Slot = data != null && data.Length > 0 && data[0] is int slot ? slot : 0;

        // Remote players are moved by PhotonTransformView, not by physics.
        if (!photonView.IsMine) RB.isKinematic = true;

        All.Add(this);
    }

    void Start()
    {
        StateMachine.AddState(new PlayerIdleState(this, "Idle"));
        StateMachine.AddState(new PlayerWalkState(this, "Walk"));
        StateMachine.AddState(new PlayerDieState(this, "Die"));

        StateMachine.Initialize(StateMachine.GetState<PlayerIdleState>());

        Spawned?.Invoke(this);
        GameManager.Instance?.OnPlayerSpawned(this);
    }

    void OnDestroy()
    {
        if (All.Remove(this)) Despawned?.Invoke(this);
    }

    private void HandleInput()
    {
        GameManager gm = GameManager.Instance;
        if (isDead || gm == null || !gm.CanControl)
        {
            InputDirection = Vector3.zero;
            return;
        }

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
        if (photonView.IsMine)
        {
            HandleInput();
            HandleFlip();
            CheckFire();
        }
        // Remote players run the state machine too, with the direction received from their owner.
        StateMachine?.Update();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine || isDead) return;
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

    private void CheckFire()
    {
        if (isDead || BombManager.Instance == null || GridManager.Instance == null) return;
        if (GameManager.Instance == null || GameManager.Instance.Phase != MatchPhase.Playing) return;

        if (BombManager.Instance.IsOnFire(GridManager.Instance.WorldToCell(transform.position)))
            Kill();
    }

    private void PlaceBomb()
    {
        BombManager.Instance?.TryPlaceBomb(this);
    }

    /// <summary>Kills this player if it belongs to this client. Deaths are decided by the owner.</summary>
    public void Kill()
    {
        if (!photonView.IsMine || isDead) return;
        photonView.RPC(nameof(RPC_Die), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_Die()
    {
        if (isDead) return;
        isDead = true;
        speed = 0f;
        InputDirection = Vector3.zero;

        if (!RB.isKinematic) RB.linearVelocity = Vector3.zero;
        RB.isKinematic = true;
        _collider.enabled = false; // dead players don't block anyone

        OnPlayerDeath?.Invoke();
        GameManager.Instance?.OnPlayerDied(this);
    }

    /// <summary>Local player only: apply a picked-up power-up.</summary>
    public void ApplyItem(Item.ItemType type)
    {
        switch (type)
        {
            case Item.ItemType.Bomb:
                maxBomb = Mathf.Min(maxBomb + 1, MaxBombLimit);
                break;
            case Item.ItemType.Explosion:
                explosionRange = Mathf.Min(explosionRange + 1, MaxRangeLimit);
                break;
            case Item.ItemType.Speed:
                speed = Mathf.Min(speed + 1f, MaxSpeedLimit);
                break;
        }
        OnStatsChanged?.Invoke();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Only the movement direction is sent (position/rotation go through PhotonTransformView),
        // so remote players play the same Idle/Walk animation as on the owner's screen.
        if (stream.IsWriting)
        {
            int x = Mathf.RoundToInt(InputDirection.x) + 1;
            int z = Mathf.RoundToInt(InputDirection.z) + 1;
            stream.SendNext((byte)(x | (z << 2)));
        }
        else
        {
            int packed = (byte)stream.ReceiveNext();
            InputDirection = new Vector3((packed & 3) - 1, 0f, ((packed >> 2) & 3) - 1);
        }
    }
}
