using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        // 대기 상태 진입 시 애니메이션 멈춤 처리
        player.UpdateAnimation(false);
    }

    public override void Execute()
    {
        // 자동 모드 활성화 시 AutoState로 전환
        if (player.isAutoMode)
        {
            player.ChangeState(new PlayerAutoState(player));
            return;
        }

        // 입력이 감지되면 MoveState로 전환
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 입력값이 약간이라도 있으면 이동으로 간주
        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            player.ChangeState(new PlayerMoveState(player));
        }
    }

    public override void Exit()
    {
        
    }
}
