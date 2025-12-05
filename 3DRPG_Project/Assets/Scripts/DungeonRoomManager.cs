using System.Collections.Generic;
using UnityEngine;

public class DungeonRoom : MonoBehaviour
{
    [Header("Settings")]
    public DungeonDoor roomDoor; // 이 방을 클리어하면 열릴 문
    public GameObject monsterPrefab; // 생성할 몬스터 프리펩
    public List<Transform> spawnPoints; // 몬스터가 생성될 위치들

    [Header("Status")]
    public List<LivingEntity> liveMonsters = new List<LivingEntity>();  // 생성된 몬스터들 리스트
    public bool isCleared = false;  // 방 클리어 여부

    private void Start()
    {
        SpawnMonsters();
    }

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
            Debug.Log($"방 {gameObject.name} 자동 클리어 (몬스터 없음)");
        }
    }

    private void HandleMonsterDeath(LivingEntity monster)
    {
        if (liveMonsters.Contains(monster))
        {
            liveMonsters.Remove(monster);
        }

        CheckRoomClear();
    }

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
            Debug.Log($"방 {gameObject.name} 클리어");
        }
    }
}

