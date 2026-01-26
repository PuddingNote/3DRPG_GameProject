using System;
using System.Collections.Generic;
using UnityEngine;

// UI 없이 아이템 획득 데이터만 보관하는 인벤토리
public class InventoryService : MonoBehaviour
{
    private static InventoryService instance;

    [Header("Debug")]
    [SerializeField] private bool enableAddItemLog = true;

    public static InventoryService Instance
    {
        get
        {
            if (instance == null)
            {
                InventoryService existing = FindFirstObjectByType<InventoryService>();
                if (existing != null)
                {
                    instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("[InventoryService]");
                    instance = go.AddComponent<InventoryService>();
                }
            }

            return instance;
        }
    }

    [SerializeField] private List<ItemStack> items = new List<ItemStack>();

    // 아이템이 인벤토리에 추가되는 순간 호출 (지금은 몬스터 처치 순간). UI 연결/알림/사운드 등에 활용 가능
    public event Action<ItemData, int> OnItemAdded;

    public IReadOnlyList<ItemStack> Items => items;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 아이템을 인벤토리에 추가
    public void AddItem(ItemData item, int quantity)
    {
        if (item == null)
        {
            return;
        }

        if (quantity <= 0)
        {
            return;
        }

        int beforeTotal = GetTotalQuantity(item);
        int remaining = quantity;

        if (item.IsStackable())
        {
            while (remaining > 0)
            {
                ItemStack stack = FindStackWithSpace(item);
                if (stack == null)
                {
                    stack = new ItemStack
                    {
                        item = item,
                        quantity = 0
                    };
                    items.Add(stack);
                }

                int space = Mathf.Max(0, item.maxStack - stack.quantity);
                if (space <= 0)
                {
                    // 이론상 FindStackWithSpace가 보장하지만, 데이터 꼬임 방지
                    stack = null;
                    continue;
                }

                int toAdd = Mathf.Min(space, remaining);
                stack.quantity += toAdd;
                remaining -= toAdd;
            }
        }
        else
        {
            for (int i = 0; i < remaining; i++)
            {
                items.Add(new ItemStack
                {
                    item = item,
                    quantity = 1
                });
            }
            remaining = 0;
        }

        OnItemAdded?.Invoke(item, quantity);

        if (enableAddItemLog)
        {
            int afterTotal = GetTotalQuantity(item);
            Debug.Log(
                $"[Inventory] AddItem: '{item.itemName}' (id={item.itemId}, rarity={item.rarity}, slot={item.equipSlot}) " +
                $"+{quantity} (before={beforeTotal}, after={afterTotal}), stacks={items.Count}",
                this);
        }
    }

    // 아이템을 인벤토리에 추가할 때 빈 스택을 찾음
    private ItemStack FindStackWithSpace(ItemData item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            ItemStack stack = items[i];
            if (stack == null)
            {
                continue;
            }

            if (stack.item != item)
            {
                continue;
            }

            if (stack.quantity >= item.maxStack)
            {
                continue;
            }

            return stack;
        }

        return null;
    }

    // 아이템의 총 개수를 반환
    private int GetTotalQuantity(ItemData item)
    {
        if (item == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < items.Count; i++)
        {
            ItemStack stack = items[i];
            if (stack == null || stack.item != item)
            {
                continue;
            }

            total += stack.quantity;
        }

        return total;
    }

}
