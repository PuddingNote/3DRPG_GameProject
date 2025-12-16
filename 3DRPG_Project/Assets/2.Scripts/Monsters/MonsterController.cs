using UnityEngine;
using UnityEngine.AI;

public class MonsterController : LivingEntity
{
    [Header("Combat Settings")]
    public float attackCooldown = 1.5f;     // 공격 쿨타임
    public float lastAttackTime = 0f;       // 마지막 공격 시간

    [Header("AI Settings")]
    public float detectionRange = 10f;      // 탐색 범위
    public float attackRange = 2f;          // 공격 범위
    public float moveSpeed = 5.5f;          // 이동 속도
    public float returnDistance = 30f;      // 복귀 거리 (너무 멀리 가면 강제 복귀용)

    [Header("Target")]
    public LivingEntity target;             // 현재 타겟 (플레이어)
    
    // 시작 위치/회전 저장 (복귀용)
    public Vector3 SpawnPosition { get; private set; }
    public Quaternion SpawnRotation { get; private set; }
    
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
        SpawnRotation = transform.rotation;
    }

    private void Start()
    {
        // NavMeshAgent 속도 설정
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = 0f;
            navMeshAgent.autoBraking = true;
        }

        // 초기 상태: Idle
        ChangeState(new MonsterIdleState(this));
    }

    private void Update()
    {
        if (IsDead) 
        {
            return;
        }

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
        base.Die(); 
        
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }
        
        Collider collider = GetComponent<Collider>();
        if (collider != null) 
        {
            collider.enabled = false;
        }

        Destroy(gameObject, 3f);
    }

    public LivingEntity FindPlayer()
    {
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

    // 플레이어가 "NavMesh 끝부분 + 공격 사거리" 내에 있는가? 판단
    public bool IsTargetReachable(LivingEntity targetEntity)
    {
        if (targetEntity == null || navMeshAgent == null)
        {
            return false;
        }

        // 몬스터의 현재 위치에서 타겟까지의 경로 계산
        NavMeshPath path = new NavMeshPath();
        if (navMeshAgent.CalculatePath(targetEntity.transform.position, path))
        {
            // 경로의 끝점 (갈 수 있는 한계점)
            Vector3 finalPoint = path.corners[path.corners.Length - 1];
            
            // 끝점과 타겟의 거리
            float distanceToEnd = Vector3.Distance(finalPoint, targetEntity.transform.position);

            // 그 거리가 공격 사거리 이내라면 "닿는다"고 판단 (오차 고려하여 -0.1f)
            return distanceToEnd <= attackRange - 0.1f;
        }
        
        return false;
    }

    // 애니메이션 이벤트에서 호출할 실제 공격 함수
    public void OnAttackHit()
    {
        if (target != null && !target.IsDead)
        {
            // 사거리 체크 한 번 더 (공격 중에 도망갔을 수도 있으니)
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance <= attackRange) // 약간의 여유 범위 (+ 0.5f 줬다가 일단 지움)
            {
                target.TakeDamage(5);
                //Debug.Log($"{name}이 {target.name}에게 데미지를 줌");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
