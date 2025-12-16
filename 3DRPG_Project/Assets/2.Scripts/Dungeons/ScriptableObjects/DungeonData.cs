using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DungeonStage
{
    public string stageName;        // 스테이지 이름 (예: "1-1", "1-2")
    public string sceneName;        // 이동할 씬 이름 (예: "DungeonScene")
}

[CreateAssetMenu(fileName = "New Dungeon Data", menuName = "Scriptable Object/Dungeon Data", order = 1)]
public class DungeonData : ScriptableObject
{
    [Header("Dungeon Info")]
    public string dungeonName;      // 던전 전체 이름 (예: "일반 던전")
    
    [Header("Stages")]
    public List<DungeonStage> stages = new List<DungeonStage>(); // 스테이지 리스트
}