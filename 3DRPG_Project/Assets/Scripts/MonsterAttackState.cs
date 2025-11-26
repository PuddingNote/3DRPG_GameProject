using UnityEngine;

public class MonsterAttackState : MonsterState
{
    private float attackCooldown = 1.5f; // 공격 쿨타임
    private float lastAttackTime;

    public MonsterAttackState(MonsterController monster) : base(monster)
    {
    }

    public override void Enter()
    {
        // 공격 시작 시 즉시 공격 한번 수행
        lastAttackTime = 0f; 
        
        // 이동 멈춤
        if (monster.navMeshAgent != null) 
        {
            monster.navMeshAgent.ResetPath();
        }
    }

    public override void Execute()
    {
        if (monster.target == null || monster.target.IsDead)
        {
            monster.ChangeState(new MonsterIdleState(monster));
            return;
        }

        // 타겟 바라보기
        Vector3 direction = (monster.target.transform.position - monster.transform.position).normalized;
        direction.y = 0;
        monster.transform.rotation = Quaternion.LookRotation(direction);

        // 거리 체크: 멀어지면 다시 추적
        float distance = Vector3.Distance(monster.transform.position, monster.target.transform.position);
        if (distance > monster.attackRange + 0.5f) // 0.5f는 버퍼(떨림 방지)
        {
            monster.ChangeState(new MonsterChaseState(monster));
            return;
        }

        // 쿨타임 체크 및 공격
        lastAttackTime += Time.deltaTime;
        if (lastAttackTime >= attackCooldown)
        {
            Attack();
            lastAttackTime = 0f;
        }
    }

    private void Attack()
    {
        // 애니메이션 재생 예정
        // monster.animator.SetTrigger("Attack");
        Debug.Log("몬스터가 플레이어 어택");

        // 데미지 처리
        if (monster.target != null)
        {
            monster.target.TakeDamage(5); // 몬스터 공격력 5 가정
        }
    }

    public override void Exit()
    {
        
    }
}
