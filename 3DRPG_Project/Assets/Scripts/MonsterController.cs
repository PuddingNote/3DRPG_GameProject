using UnityEngine;
using UnityEngine.AI;

public class MonsterController : LivingEntity
{
    [Header("AI Settings")]
    public float detectionRange = 10f;      // 탐색 범위
    public float attackRange = 2f;          // 공격 범위
    public float moveSpeed = 5.5f;          // 이동 속도
    public float returnDistance = 20f;      // 복귀 거리 (스폰 위치로부터)

    [Header("Target")]
    public LivingEntity target;             // 현재 타겟 (플레이어)
    
    // 시작 위치 저장 (복귀용)
    public Vector3 SpawnPosition { get; private set; }
    
    // 컴포넌트 참조
    public NavMeshAgent navMeshAgent;
    public Animator animator;

    // FSM
    private IState currentState;

    protected override void Awake()
    {
        base.Awake();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        SpawnPosition = transform.position;
    }

    private void Start()
    {
        // NavMeshAgent 속도 설정
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = attackRange;
        }

        // 초기 상태: Idle
        ChangeState(new MonsterIdleState(this));
    }

    private void Update()
    {
        currentState?.Execute();
    }

    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public override void Die()
    {
        base.Die(); // LivingEntity의 Die 실행 (IsDead 설정 및 이벤트 호출)
        
        // 나중에는 쓰러지는 애니메이션 재생 후 일정 시간 뒤에 파괴하도록 수정 예정
        // 지금은 즉시 오브젝트 파괴
        Destroy(gameObject);
    }

    // 플레이어 탐색 함수 (가장 가까운 플레이어 찾기)
    public LivingEntity FindPlayer()
    {
        // 범위 내 Player 태그를 가진 오브젝트들을 찾음
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
        
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                return collider.GetComponent<LivingEntity>();
            }
        }
        return null;
    }

    // 디버그용 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
