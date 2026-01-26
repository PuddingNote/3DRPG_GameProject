using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Items/Item VFX Library", fileName = "ItemVfxLibrary_")]
public class ItemRarityVfxLibrary : ScriptableObject
{
    [Serializable]
    public class RarityPrefab
    {
        public ItemRarity rarity;
        public GameObject vfxPrefab;
    }

    [SerializeField] private List<RarityPrefab> prefabs = new List<RarityPrefab>();

    public GameObject GetPrefab(ItemRarity rarity)
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            RarityPrefab entry = prefabs[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.rarity != rarity)
            {
                continue;
            }

            return entry.vfxPrefab;
        }

        return null;
    }
}
