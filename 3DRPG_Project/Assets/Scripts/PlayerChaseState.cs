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

        if (player.target == null || player.target.IsDead)
        {
            player.ChangeState(new PlayerIdleState(player));
            return;
        }

        // 거리 계산
        float distance = Vector3.Distance(player.transform.position, player.target.transform.position);

        // 사거리 도달 시 공격
        if (distance <= player.attackRange)
        {        
            player.ChangeState(new PlayerAttackState(player));
            return; 
        }

        // 이동 로직 (CharacterMove와 유사하지만 타겟 방향으로)
        MoveToTarget();
    }

    private void MoveToTarget()
    {
        Vector3 direction = (player.target.transform.position - player.transform.position).normalized;
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
