using UnityEngine;

public class PlayerAutoState : PlayerState
{
    private DungeonRoomManager currentRoom;     // 현재 방 정보
    private float pathUpdateRate = 0.2f;        // 경로 업데이트 주기
    private float lastPathUpdateTime;           // 마지막 경로 업데이트 시간

    public PlayerAutoState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        // 진입 시에는 애니메이션을 켜지 않음 (MoveTo가 필요할 때 켤 것임)
        // player.UpdateAnimation(true);
        
        // NavMeshAgent 설정
        if (player.agent != null)
        {
            player.agent.speed = player.moveSpeed; // 속도 동기화
            
            if (!player.agent.isOnNavMesh)
            {
                // 가까운 NavMesh로 이동 시도
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(player.transform.position, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    player.agent.Warp(hit.position);
                }
            }
        }
    }

    public override void Execute()
    {
        // 1. 수동 조작 개입 확인
        if (CheckManualInput()) 
        {
            return;
        }

        // 2. 자동 모드 해제 확인
        if (!player.isAutoMode)
        {
            player.ChangeState(new PlayerIdleState(player));
            return;
        }

        // 문 상호작용 중이면 잠시 정지
        if (player.isDoorInteracting)
        {
            player.UpdateAnimation(false);
            return;
        }

        // 3. 현재 방 정보 갱신
        currentRoom = player.currentRoom;

        // 4. 행동 결정
        DecideNextAction();
    }

    // 수동 조작 개입 확인
    private bool CheckManualInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            player.ToggleAutoMode();
            return true;
        }
        return false;
    }

    // 행동 결정
    private void DecideNextAction()
    {
        // 방 정보가 없으면 할 게 없음 (대기)
        if (currentRoom == null) 
        {
            return;
        }
        
        // 방 안의 몬스터 처리 (공식 루트)
        if (currentRoom.liveMonsters.Count > 0)
        {
            LivingEntity target = GetClosestMonster(currentRoom.liveMonsters);
            if (target != null)
            {
                EngageTarget(target);
            }
        }
        // 몬스터가 없다면 이동 (문 처리 -> 다음 웨이포인트)
        else
        {
            ProcessMovement();
        }
    }

    // 몬스터와 교전
    private void EngageTarget(LivingEntity target)
    {
        player.target = target;
        float distance = Vector3.Distance(player.transform.position, target.transform.position);

        // 사거리 내 진입 시
        if (distance <= player.attackRange)
        {
            // 1. 이동 정지
            if (player.agent != null) 
            {
                player.agent.ResetPath();
                player.agent.velocity = Vector3.zero; // 즉시 정지
            }
            
            // 2. 이동 애니메이션 끄기 (확실하게!)
            player.UpdateAnimation(false);

            // 3. 쿨타임 체크: 아직 쿨타임 중이라면 여기서 끝냄 (Idle 대기 효과)
            if (Time.time - player.lastAttackCooldown < player.attackCooldown)
            {
                // 타겟 바라보기 (선택 사항)
                Vector3 dir = (target.transform.position - player.transform.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                }
                return; 
            }
            
            // 4. 쿨타임 끝났으면 공격 상태 전환
            player.ChangeState(new PlayerAttackState(player)); 
        }
        else
        {
            // 사거리 밖이면 추적
            MoveTo(target.transform.position);
        }
    }

    // 이동 처리 (문 -> 웨이포인트)
    private void ProcessMovement()
    {
        // 1. 문이 존재하고, 아직 열리지 않았으며, 잠금이 해제된 상태라면 -> 문 열러 가기
        if (currentRoom.roomDoor != null && !currentRoom.roomDoor.IsOpen && !currentRoom.roomDoor.isLocked)
        {
            // DungeonDoor의 InteractionPosition을 사용하여 거리 계산 및 이동
            Vector3 doorPos = currentRoom.roomDoor.InteractionPosition;

            float distToDoor = Vector3.Distance(player.transform.position, doorPos);
            
            // 상호작용 사거리
            if (distToDoor <= player.interactionRange)
            {
                // 문 앞에 도달했으면 공통 코루틴으로 1초간 대기 후 진행
                player.DoorInteractWithDelay(currentRoom.roomDoor);
            }
            else
            {
                // 문 위치로 이동
                MoveTo(doorPos);
            }
            return;
        }

        // 2. 문이 열렸거나 없으면 -> 다음 웨이포인트로 이동
        if (currentRoom.nextWaypoint != null)
        {
            float distToWaypoint = Vector3.Distance(player.transform.position, currentRoom.nextWaypoint.position);
            
            // 도착했으면 멈춤 (다음 방 진입 대기)
            if (distToWaypoint < 1.5f)
            {
                if (player.agent != null) 
                {
                    player.agent.ResetPath();
                }
            }
            else
            {
                MoveTo(currentRoom.nextWaypoint.position);
            }
        }
    }

    private void MoveTo(Vector3 destination)
    {
        if (player.agent == null || !player.agent.isActiveAndEnabled) 
        {
            return;
        }
        
        // 이동 명령 시 애니메이션 켜기
        player.UpdateAnimation(true);

        if (Time.time - lastPathUpdateTime > pathUpdateRate)  
        {
            lastPathUpdateTime = Time.time;
            player.agent.SetDestination(destination);
        }
    }

    // 방에 등록된 몬스터 중 가장 가까운 놈
    private LivingEntity GetClosestMonster(System.Collections.Generic.List<LivingEntity> monsters)
    {
        LivingEntity closest = null;
        float minDist = float.MaxValue;

        foreach (var mon in monsters)
        {
            if (mon == null || mon.IsDead) 
            {
                continue;
            }

            float d = Vector3.Distance(player.transform.position, mon.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = mon;
            }
        }
        return closest;
    }

    public override void Exit()
    {
        if (player.agent != null && player.agent.isActiveAndEnabled)
        {
            player.agent.ResetPath();
        }
        player.UpdateAnimation(false);
    }
}
