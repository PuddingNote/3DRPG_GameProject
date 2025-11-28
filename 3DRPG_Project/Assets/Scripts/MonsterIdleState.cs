using UnityEngine;

public class MonsterIdleState : MonsterState
{
    private float idleTimer = 0f; // 멍때림 방지 타이머

    public MonsterIdleState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        if (monster.animator != null)
        {
            monster.animator.SetBool("isMove", false);
        }
        idleTimer = 0f;
    }

    public override void Execute()
    {
        // 1. 타겟 검증 및 탐색
        if (monster.target == null)
        {
            LivingEntity foundTarget = monster.FindPlayer();
            
            // 발견했고 + 도달 가능(NavMesh 끝 + 사거리)하다면 타겟 설정
            if (foundTarget != null && monster.IsTargetReachable(foundTarget))
            {
                monster.target = foundTarget;
            }
        }
        // 타겟이 있었는데 죽었거나 도달 불가능해졌다면? -> 일단 유지하고 행동 결정에서 처리하거나 null 처리
        else if (monster.target.IsDead)
        {
            monster.target = null;
        }

        // 2. 행동 결정
        if (monster.target != null)
        {
            // 타겟이 존재함
            float distance = Vector3.Distance(monster.transform.position, monster.target.transform.position);

            // A. 공격 사거리 안인가?
            if (distance <= monster.attackRange)
            {
                // 쿨타임 체크
                if (Time.time - monster.lastAttackTime >= monster.attackCooldown)
                {
                    monster.ChangeState(new MonsterAttackState(monster));
                    return;
                }
                else
                {
                    // 쿨타임 중 -> 바라보며 대기 (타이머 초기화)
                    RotateToTarget(monster.target.transform.position);
                    idleTimer = 0f; 
                }
            }
            // B. 사거리 밖인가?
            else
            {
                // 이미 타겟을 잡고 있는 상태라면,
                // NavMesh 밖으로 나갔더라도(IsTargetReachable == false),
                // 일단 Chase 상태로 전환하여 "갈 수 있는 데까지" 추격하도록 함.
                // (ChaseState에서 NavMesh 끝에 도달했을 때 최종적으로 포기 여부를 결정)
                monster.ChangeState(new MonsterChaseState(monster));
                return;
            }
        }
        
        // 3. 타겟이 없는 경우 (혹은 포기한 경우)
        if (monster.target == null)
        {
            float distToSpawn = Vector3.Distance(monster.transform.position, monster.SpawnPosition);

            // 스폰 위치가 아니라면?
            if (distToSpawn > 0.5f)
            {
                // 멍때림 타이머 작동
                idleTimer += Time.deltaTime;
                
                // 3초 이상 멍때리면 강제 복귀
                if (idleTimer >= 3.0f)
                {
                    monster.ChangeState(new MonsterReturnState(monster));
                }
            }
            else
            {
                // 스폰 위치라면 회전 정렬
                monster.transform.rotation = Quaternion.Slerp(monster.transform.rotation, monster.SpawnRotation, Time.deltaTime * 2f);
                idleTimer = 0f;
            }
        }
    }

    private void RotateToTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - monster.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            monster.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    public override void Exit() { }
}
