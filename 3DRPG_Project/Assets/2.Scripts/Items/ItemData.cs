using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Items/Item Data", fileName = "ItemData_")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("추후 저장/로드, 네트워크 등을 고려한 고유 ID. (예: sword_001)")]
    public string itemId;



    [Header("Display")]
    public string itemName;
    public Sprite icon;



    [Header("Attributes")]
    public ItemRarity rarity = ItemRarity.Common;
    public EquipSlot equipSlot = EquipSlot.Weapon;



    [Header("Stacking")]
    [Tooltip("장비는 보통 1. 소모품/재료 등을 대비해 확장용으로 두자.")]
    [Min(1)]
    public int maxStack = 1;

    public bool IsStackable()
    {
        return maxStack > 1;
    }
}
