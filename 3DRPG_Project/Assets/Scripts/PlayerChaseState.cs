using UnityEngine;

public class PlayerChaseState : PlayerState
{
    public PlayerChaseState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.UpdateAnimation(true);
    }

    public override void Execute()
    {
        // 이동 입력 감지 (WASD) -> 입력이 있으면 추적 중단하고 이동 상태로 전환
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            player.ChangeState(new PlayerMoveState(player));
            return;
        }

        // 추적할 타겟 결정 (몬스터 vs 상호작용 대상)
        Transform chaseTarget = null;
        float stopDistance = 0f;
        bool isAttackTarget = false;

        if (player.target != null && !player.target.IsDead)
        {
            chaseTarget = player.target.transform;
            stopDistance = player.attackRange;
            isAttackTarget = true;
        }
        else if (player.interactionTarget != null && player.interactionTransform != null)
        {
            chaseTarget = player.interactionTransform;
            stopDistance = 2.5f; // 상호작용 사거리
            isAttackTarget = false;
        }
        else
        {
            // 타겟이 없으면 Idle로 복귀
            player.ChangeState(new PlayerIdleState(player));
            return;
        }

        // 거리 계산
        float distance = Vector3.Distance(player.transform.position, chaseTarget.position);

        // 사거리 도달 시 행동
        if (distance <= stopDistance)
        {        
            if (isAttackTarget)
            {
                player.ChangeState(new PlayerAttackState(player));
            }
            else
            {
                // 상호작용 실행
                player.interactionTarget.Interact();
                
                // 상호작용 후 타겟 해제 및 Idle 전환
                player.interactionTarget = null;
                player.interactionTransform = null;
                player.ChangeState(new PlayerIdleState(player));
            }
            return; 
        }

        // 이동 로직
        MoveToTarget(chaseTarget);
    }

    private void MoveToTarget(Transform target)
    {
        Vector3 direction = (target.position - player.transform.position).normalized;
        direction.y = 0;

        // 회전
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, Time.deltaTime * player.rotationSpeed);
        }

        // 이동 (PlayerController의 Move 함수 사용)
        player.Move(direction);
    }

    public override void Exit()
    {
        player.UpdateAnimation(false);
    }
}
