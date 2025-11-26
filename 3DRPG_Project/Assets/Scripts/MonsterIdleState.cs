using UnityEngine;

public class MonsterIdleState : MonsterState
{
    public MonsterIdleState(MonsterController monster) : base(monster) { }

    public override void Enter()
    {
        // idle 애니메이션 재생 예정
    }

    public override void Execute()
    {
        // 플레이어 탐색
        LivingEntity target = monster.FindPlayer();

        // 플레이어를 발견했다면 추적 상태로 전환
        if (target != null)
        {
            monster.target = target;
            monster.ChangeState(new MonsterChaseState(monster));
        }
    }

    public override void Exit()
    {
        
    }
}
