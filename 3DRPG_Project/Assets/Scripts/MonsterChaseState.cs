using UnityEngine;

public class MonsterChaseState : MonsterState
{
    public MonsterChaseState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        // 이동 애니메이션 재생 예정
    }

    public override void Execute()
    {
        if (monster.target == null || monster.target.IsDead)
        {
            monster.ChangeState(new MonsterIdleState(monster));
            return;
        }

        // 1. 플레이어와의 거리 계산
        float distanceToTarget = Vector3.Distance(monster.transform.position, monster.target.transform.position);
        float distanceToSpawn = Vector3.Distance(monster.transform.position, monster.SpawnPosition);

        // 2. 복귀 조건 체크: 스폰 위치에서 너무 멀어지면 복귀 (나중에 ReturnState 추가 예정)
        if (distanceToSpawn > monster.returnDistance)
        {
            // monster.ChangeState(new MonsterReturnState(monster));

            // 일단은 Idle로
            monster.target = null;
            monster.ChangeState(new MonsterIdleState(monster));
            return;
        }

        // 3. 공격 범위 안에 들어왔다면 공격 상태로
        if (distanceToTarget <= monster.attackRange)
        {
            // 멈추고 공격 준비
            if (monster.navMeshAgent != null) 
            {
                monster.navMeshAgent.ResetPath();
            }

            monster.ChangeState(new MonsterAttackState(monster));
            return; 
        }

        // 4. 추적 진행 (NavMeshAgent 사용)
        if (monster.navMeshAgent != null)
        {
            monster.navMeshAgent.SetDestination(monster.target.transform.position);
        }
    }

    public override void Exit()
    {
        // 이동 멈춤
        if (monster.navMeshAgent != null) 
        {
            monster.navMeshAgent.ResetPath();
        }
    }
}
