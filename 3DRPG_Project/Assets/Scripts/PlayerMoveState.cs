using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        
    }

    public override void Execute()
    {
        // 실제 이동 로직 수행
        player.CharacterMove();

        // 입력이 없으면 IdleState로 전환
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
        {
            player.ChangeState(new PlayerIdleState(player));
        }
    }

    public override void Exit()
    {
        // 이동 상태 종료 시 애니메이션 끔
        player.UpdateAnimation(false);
    }
}
