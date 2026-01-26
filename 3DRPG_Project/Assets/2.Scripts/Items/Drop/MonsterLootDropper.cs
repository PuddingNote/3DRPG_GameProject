using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 사망 시:
/// 1) 드랍 테이블 확률로 아이템을 추려내기
/// 2) "처치 순간" 인벤토리에 즉시 추가
/// 3) 드랍/흡수 연출(VFX)을 스폰
/// </summary>
[DisallowMultipleComponent]
public class MonsterLootDropper : MonoBehaviour
{
    [Header("Drop Data")]
    public MonsterDropTable dropTable;



    [Header("VFX")]
    [Tooltip("등급별 VFX 프리팹 라이브러리")]
    public ItemRarityVfxLibrary vfxLibrary;

    [Tooltip("한 번의 드랍에서 생성할 VFX 최대 개수(과도한 스폰 방지). 인벤 수량에는 영향 없음.")]
    [Min(0)]
    public int maxVfxSpawnCount = 30;

    [Tooltip("VFX를 스폰할 기준 위치. 비워두면 몬스터 transform을 사용.")]
    public Transform spawnOrigin;



    [Header("VFX Spawn Timing")]
    [Tooltip("드랍 VFX를 '푸슈슉' 느낌으로 순차 스폰할 간격(초).")]
    [Min(0f)]
    public float vfxSpawnInterval = 0.15f;

    private LivingEntity entity;
    private bool hasHandledDeath;
    private Coroutine vfxSpawnRoutine;

    private void Awake()
    {
        entity = GetComponent<LivingEntity>();
    }

    private void OnEnable()
    {
        hasHandledDeath = false;

        if (entity != null)
        {
            entity.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (entity != null)
        {
            entity.OnDeath -= HandleDeath;
        }

        if (vfxSpawnRoutine != null)
        {
            StopCoroutine(vfxSpawnRoutine);
            vfxSpawnRoutine = null;
        }
    }

    // 몬스터 사망 시 드랍 처리
    private void HandleDeath()
    {
        if (hasHandledDeath)
        {
            return;
        }

        hasHandledDeath = true;

        if (dropTable == null)
        {
            return;
        }

        List<MonsterDropTable.DropResult> drops = dropTable.RollDrops();
        if (drops == null || drops.Count == 0)
        {
            return;
        }

        // 1. 인벤토리 즉시 추가
        for (int i = 0; i < drops.Count; i++)
        {
            MonsterDropTable.DropResult drop = drops[i];
            if (drop.item == null)
            {
                continue;
            }

            InventoryService.Instance.AddItem(drop.item, drop.quantity);
        }

        // 2. VFX 스폰 + 연출 (인벤 추가와 무관)
        Transform playerTr = null;
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            playerTr = GameManager.Instance.currentPlayer.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTr = playerObj.transform;
            }
        }

        Vector3 originPos = (spawnOrigin != null) ? spawnOrigin.position : transform.position;
        if (vfxSpawnRoutine != null)
        {
            StopCoroutine(vfxSpawnRoutine);
        }
        vfxSpawnRoutine = StartCoroutine(SpawnVfxForDropsSequential(drops, originPos, playerTr));
    }

    // 드랍 결과를 기반으로 VFX를 '낮은 등급 → 높은 등급' 순서로 순차 스폰
    private IEnumerator SpawnVfxForDropsSequential(List<MonsterDropTable.DropResult> drops, Vector3 originPos, Transform playerTr)
    {
        if (vfxLibrary == null)
        {
            yield break;
        }

        int spawned = 0;

        // RollDrops()가 dropTable.entries 순서대로 결과를 내므로, 별도 정렬 없이 '리스트 순서(낮은→높은)'를 그대로 유지한다.
        for (int i = 0; i < drops.Count; i++)
        {
            MonsterDropTable.DropResult drop = drops[i];
            if (drop.item == null)
            {
                continue;
            }

            GameObject prefab = vfxLibrary.GetPrefab(drop.item.rarity);
            if (prefab == null)
            {
                continue;
            }

            int vfxCount = Mathf.Max(1, drop.quantity);
            for (int n = 0; n < vfxCount; n++)
            {
                if (maxVfxSpawnCount > 0 && spawned >= maxVfxSpawnCount)
                {
                    yield break;
                }

                GameObject vfxObj = Instantiate(prefab, originPos, Quaternion.identity);
                DroppedItemVFX vfx = vfxObj.GetComponent<DroppedItemVFX>();
                if (vfx == null)
                {
                    vfx = vfxObj.AddComponent<DroppedItemVFX>();
                }

                vfx.Play(originPos, playerTr);
                spawned++;

                float interval = vfxSpawnInterval;
                if (interval > 0f)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
        }

        vfxSpawnRoutine = null;
    }
}
