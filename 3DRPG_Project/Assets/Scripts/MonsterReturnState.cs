using UnityEngine;

public class MonsterReturnState : MonsterState
{
    public MonsterReturnState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        monster.target = null;

        if (monster.navMeshAgent != null)
        {
            monster.navMeshAgent.SetDestination(monster.SpawnPosition);
            monster.navMeshAgent.isStopped = false;
        }

        if (monster.animator != null)
        {
            monster.animator.SetBool("isMove", true);
        }
    }

    public override void Execute()
    {
        // 1. 도착 체크
        if (!monster.navMeshAgent.pathPending && monster.navMeshAgent.remainingDistance <= 0.1f)
        {
            monster.ChangeState(new MonsterIdleState(monster));
            return;
        }

        // 2. 복귀 중 플레이어 발견 체크
        LivingEntity target = monster.FindPlayer();
        if (target != null)
        {
            // 발견했다면 "때릴 수 있는 위치인가?" 체크
            if (monster.IsTargetReachable(target))
            {
                monster.target = target;
                monster.ChangeState(new MonsterChaseState(monster));
            }
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
