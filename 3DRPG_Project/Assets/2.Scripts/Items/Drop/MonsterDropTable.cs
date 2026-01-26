using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Items/Monster Drop Table", fileName = "DropTable_")]
public class MonsterDropTable : ScriptableObject
{
    [Serializable]
    public class DropEntry
    {
        [Tooltip("드랍할 아이템")]
        public ItemData item;

        [Range(0f, 100f)]
        [Tooltip("드랍 확률 (0~100%)")]
        public float dropChancePercent = 100f;

        [Min(0)]
        [Tooltip("최소 개수")]
        public int minQuantity = 1;

        [Min(1)]
        [Tooltip("최대 개수")]
        public int maxQuantity = 1;
    }

    [Serializable]
    public struct DropResult
    {
        public ItemData item;
        public int quantity;
    }

    // 드랍 테이블 내용 저장
    [SerializeField] private List<DropEntry> entries = new List<DropEntry>();

    // 드랍 테이블 내용 읽기 전용
    public IReadOnlyList<DropEntry> Entries => entries;

    // 드랍 테이블 내용을 기반으로 드랍 결과를 반환
    public List<DropResult> RollDrops()
    {
        List<DropResult> results = new List<DropResult>();

        for (int i = 0; i < entries.Count; i++)
        {
            DropEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.item == null)
            {
                continue;
            }

            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll > entry.dropChancePercent)
            {
                continue;
            }

            int minQ = Mathf.Max(1, entry.minQuantity);
            int maxQ = Mathf.Max(1, entry.maxQuantity);
            if (maxQ < minQ)
            {
                int temp = minQ;
                minQ = maxQ;
                maxQ = temp;
            }

            int quantity = UnityEngine.Random.Range(minQ, maxQ + 1);
            results.Add(new DropResult
            {
                item = entry.item,
                quantity = quantity
            });
        }

        return results;
    }
}
