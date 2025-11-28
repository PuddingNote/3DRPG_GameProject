using UnityEngine;

public class MonsterChaseState : MonsterState
{
    public MonsterChaseState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        if (monster.animator != null)
        {
            monster.animator.SetBool("isMove", true);
        }
    }

    public override void Execute()
    {
        // 1. 타겟 체크
        if (monster.target == null || monster.target.IsDead)
        {
            monster.ChangeState(new MonsterIdleState(monster));
            return;
        }

        // 2. 거리 계산
        float distanceToTarget = Vector3.Distance(monster.transform.position, monster.target.transform.position);
        
        // 3. 공격 사거리 진입 체크
        // (이동 중이라도 공격 가능하면 즉시 공격)
        if (distanceToTarget <= monster.attackRange)
        {
            monster.ChangeState(new MonsterAttackState(monster));
            return;
        }

        // 4. 이동 (NavMeshAgent)
        if (monster.navMeshAgent != null)
        {
            // 무조건 타겟 위치로 이동 시도 (NavMeshAgent가 알아서 갈 수 있는 데까지 감)
            monster.navMeshAgent.SetDestination(monster.target.transform.position);

            // 5. 이동 완료 체크 (더 이상 갈 곳이 없는지?)
            if (!monster.navMeshAgent.pathPending && monster.navMeshAgent.remainingDistance <= 0.1f)
            {
                // 도착했는데 공격 사거리가 안 닿는다면? (NavMesh 끝부분까지 갔는데 못 때리는 상황)
                if (distanceToTarget > monster.attackRange)
                {
                    // 포기하고 복귀
                    monster.target = null; // 타겟 해제 (재추격 방지)
                    monster.ChangeState(new MonsterReturnState(monster));
                    return;
                }
            }
        }
        
        // 안전장치: 너무 멀리 움직였으면 복귀
        if (Vector3.Distance(monster.transform.position, monster.SpawnPosition) > monster.returnDistance)
        {
            monster.target = null; // 타겟 해제
            monster.ChangeState(new MonsterReturnState(monster));
        }
    }

    public override void Exit()
    {
        if (monster.navMeshAgent != null) 
        {
            monster.navMeshAgent.ResetPath();
        }

        if (monster.animator != null)
        {
            monster.animator.SetBool("isMove", false);
        }
    }
}
