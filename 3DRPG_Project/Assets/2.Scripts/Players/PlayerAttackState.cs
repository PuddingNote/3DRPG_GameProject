using UnityEngine;

public class PlayerAttackState : PlayerState
{
    private float attackCooldown = 1.0f; // 전체 공격 애니메이션 시간
    private float attackDelay = 0.5f;    // 발사 딜레이 (지팡이가 적을 향할 때까지 (자연스러운 애니메이션때문에 딜레이 추가))
    private float stateDuration = 0f;    // 경과 시간
    private bool hasFired = false;       // 발사 여부

    public PlayerAttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        // 쿨타임 갱신
        player.lastAttackCooldown = Time.time;

        // 공격 애니메이션 재생
        player.animator.SetTrigger("Attack");
        
        // 변수 초기화
        stateDuration = 0f;
        hasFired = false;
    }

    public override void Execute()
    {
        // 타겟을 향해 부드럽게 회전 (수평 회전만)
        if (player.target != null)
        {
            Vector3 direction = (player.target.transform.position - player.transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, lookRotation, Time.deltaTime * 10f);
            }
        }

        // 시간 경과
        stateDuration += Time.deltaTime;

        // 딜레이 시간이 지나고 아직 발사하지 않았다면 발사
        if (stateDuration >= attackDelay && !hasFired)
        {
            FireProjectile();
            hasFired = true;
        }

        // 공격 시간이 끝나면 Idle로 복귀
        if (stateDuration >= attackCooldown)
        {
            player.ChangeState(new PlayerIdleState(player));
        }
    }

    private void FireProjectile()
    {
        if (player.fireballPrefab != null)
        {
            // 발사 위치 결정
            Vector3 spawnPos = player.firePoint.position;
            
            // 투사체 생성
            GameObject projectileObj = Object.Instantiate(player.fireballPrefab, spawnPos, player.transform.rotation);
            Fireball projectile = projectileObj.GetComponent<Fireball>();
            
            // 투사체 초기화 (방향 설정)
            if (projectile != null && player.target != null)
            {
                // 유도 기능 사용
                projectile.Initialize(player.target.transform, 10); 
            }
            else if (projectile != null)
            {
                projectile.Initialize(player.transform.forward, 10);
            }
        }
    }

    public override void Exit()
    {
        
    }
}
