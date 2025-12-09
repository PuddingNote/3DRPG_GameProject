using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonRoomManager : MonoBehaviour
{
    [Header("Settings")]
    public DungeonDoor roomDoor;        // 이 방을 클리어하면 열릴 문
    public GameObject monsterPrefab;    // 생성할 몬스터 프리펩
    public List<Transform> spawnPoints; // 몬스터가 생성될 위치들
    public Transform nextWaypoint;      // 다음 방으로 가는 길목 위치
    
    [Header("Final Room Settings")]
    public bool isLastRoom = false;     // 마지막 방인지 여부
    public string nextSceneName;        // 클리어 후 이동할 씬 이름

    [Header("Status")]
    public List<LivingEntity> liveMonsters = new List<LivingEntity>();  // 생성된 몬스터들 리스트
    public bool isCleared = false;      // 방 클리어 여부

    // 플레이어 방 진입 시 호출
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.currentRoom != this)   // 이미 현재 방으로 인식되어 있다면 중복 처리하지 않음
            {
                player.currentRoom = this;
                Debug.Log($"Entered Room: {gameObject.name}");
            }
        }
    }

    private void Start()
    {
        // 마지막 방이라면 GameManager에서 복귀할 씬 이름을 가져옴
        if (isLastRoom && GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.previousSceneName))
        {
            nextSceneName = GameManager.Instance.previousSceneName;
            Debug.Log($"[DungeonRoom] 복귀할 씬 설정됨: {nextSceneName}");
        }

        SpawnMonsters();
    }

    // 몬스터 생성
    private void SpawnMonsters()
    {
        // 1. 프리팹과 스폰 포인트가 모두 있을 때만 생성 시도
        if (monsterPrefab != null && spawnPoints != null)
        {
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint == null) 
                {
                    continue;
                }

                GameObject monsterObj = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);
                LivingEntity monsterEntity = monsterObj.GetComponent<LivingEntity>();

                if (monsterEntity != null)
                {
                    liveMonsters.Add(monsterEntity);
                    monsterEntity.OnDeath += () => HandleMonsterDeath(monsterEntity);
                }
            }
        }

        // 2. 생성 후 몬스터가 한 마리도 없다면 (프리팹 미할당 or 스폰포인트 0개) 문을 열기
        if (liveMonsters.Count == 0)
        {
            isCleared = true;
            if (roomDoor != null)
            {
                roomDoor.Unlock();
            }
            //Debug.Log($"방 {gameObject.name} 자동 클리어 (몬스터 없음)");
        }
    }

    // 몬스터 사망 시 호출
    private void HandleMonsterDeath(LivingEntity monster)
    {
        if (liveMonsters.Contains(monster))
        {
            liveMonsters.Remove(monster);
        }

        CheckRoomClear();
    }

    // 방 클리어 체크
    private void CheckRoomClear()
    {
        if (isCleared) 
        {
            return;
        }

        if (liveMonsters.Count == 0)
        {
            isCleared = true;
            if (roomDoor != null)
            {
                roomDoor.Unlock();
            }

            //Debug.Log($"방 {gameObject.name} 클리어");

            // [마지막 방 처리] 몬스터 전멸 시 즉시 씬 이동
            if (isLastRoom)
            {
                //Debug.Log("던전 클리어! 마을로 이동합니다.");
                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    LoadingSceneController.LoadScene(nextSceneName);
                }
            }
        }
    }
}
