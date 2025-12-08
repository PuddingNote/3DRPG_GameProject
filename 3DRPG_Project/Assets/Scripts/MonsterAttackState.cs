using UnityEngine;

public class MonsterAttackState : MonsterState
{
    private float stateDuration = 0f;           // 상태 지속 시간
    private float attackAnimDuration = 1.0f;    // 공격 애니메이션 길이 (실제 공격 애니메이션은 0.8초임)

    public MonsterAttackState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        // 이동 멈춤
        if (monster.navMeshAgent != null) 
        {
            monster.navMeshAgent.ResetPath();
            monster.navMeshAgent.velocity = Vector3.zero;
        }

        stateDuration = 0f;
        
        // 진입 즉시 공격 시작
        Attack();
    }

    public override void Execute()
    {
        // 타겟 바라보기
        if (monster.target != null)
        {
            Vector3 direction = (monster.target.transform.position - monster.transform.position).normalized;
            direction.y = 0;
            monster.transform.rotation = Quaternion.LookRotation(direction);
        }

        // 애니메이션 재생 시간만큼 대기 후 Idle로 복귀
        stateDuration += Time.deltaTime;
        if (stateDuration >= attackAnimDuration)
        {
            monster.ChangeState(new MonsterIdleState(monster));
        }
    }

    private void Attack()
    {
        // 공격 애니메이션 재생
        if (monster.animator != null)
        {
            monster.animator.SetTrigger("Attack");
        }
        
        // 마지막 공격 시간 기록 (MonsterController에 저장)
        monster.lastAttackTime = Time.time;
    }

    public override void Exit()
    {
        
    }
}
