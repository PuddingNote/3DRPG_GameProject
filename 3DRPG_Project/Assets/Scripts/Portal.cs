using UnityEngine;

public class Portal : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public DungeonData dungeonData;     // 연결할 던전 데이터
    public DungeonEntranceUI dungeonUI; // 씬에 있는 UI 참조

    public void Interact()
    {
        // 플레이어 멈추기 (Idle 전환)
        if (GameManager.Instance.currentPlayer != null)
        {
            PlayerController pc = GameManager.Instance.currentPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                // 강제로 Idle 상태로 전환 및 이동 정지
                pc.ChangeState(new PlayerIdleState(pc));
                
                // NavMeshAgent가 유효한 상태일 때만 경로 초기화
                if (pc.agent != null && pc.agent.isActiveAndEnabled && pc.agent.isOnNavMesh)
                {
                    pc.agent.ResetPath();
                }
                
                // UI 조작을 위해 플레이어 입력 잠금
                pc.SetInputLock(true);
            }
        }

        // UI 띄우기
        if (dungeonUI != null && dungeonData != null)
        {
            dungeonUI.OpenUI(dungeonData);
        }
    }
}