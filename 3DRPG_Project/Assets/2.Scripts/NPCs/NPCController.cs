using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    [Header("Basic Info")]
    public string npcName;              // NPC 이름
    public List<QuestData> questList;   // 이 NPC가 줄 수 있는 퀘스트 목록

    [Header("Dialogues")]
    public List<string> randomDialogues; // 기본 랜덤 대사 목록
    
    public void Interact()
    {
        // 1. 관련된 모든 퀘스트 가져오기 (시작 전, 진행 중, 완료 가능)
        List<Quest> availableQuests = GetAvailableQuests();
        
        // 2. 무조건 랜덤 대사(인사)부터 시작
        string dialogue = GetRandomDialogue();

        // 3. UI 띄우기
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowDialogue(npcName, dialogue, availableQuests);
        }
        else
        {
            Debug.Log("DialogueUI Instance is null!");
        }
    }

    // 상호작용 가능한 퀘스트 목록 추출
    private List<Quest> GetAvailableQuests()
    {
        List<Quest> result = new List<Quest>();
        if (QuestManager.Instance == null) 
        {
            return result;
        }

        foreach (var data in questList)
        {
            if (data == null) 
            {
                continue;
            }
            Quest quest = QuestManager.Instance.GetQuest(data.questID);
            
            if (quest != null)
            {
                // 완료 가능, 시작 전, 진행 중인 퀘스트 모두 포함 (이미 완료된 퀘스트는 제외)
                if (quest.state != QuestState.Completed)
                {
                    result.Add(quest);
                }
            }
        }
        return result;
    }

    /*
    // 대사 우선순위 로직
    private string GetProperDialogue(List<Quest> quests)
    {
        if (quests == null || quests.Count == 0) 
        {
            return GetRandomDialogue();
        }

        // 1순위: 완료 가능한 퀘스트
        Quest quest = quests.Find(x => x.state == QuestState.CanComplete);
        if (quest != null) 
        {
            return quest.data.completeDialogue;
        }

        // 2순위: 아직 시작 안 한 퀘스트
        quest = quests.Find(x => x.state == QuestState.NotStarted);
        if (quest != null) 
        {
            return quest.data.startDialogue;
        }

        // 3순위: 진행 중인 퀘스트
        quest = quests.Find(x => x.state == QuestState.InProgress);
        if (quest != null) 
        {
            return quest.data.progressDialogue;
        }
        return GetRandomDialogue();
    }
    */

    // 기본 대사 랜덤 반환
    public string GetRandomDialogue()
    {
        if (randomDialogues != null && randomDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, randomDialogues.Count);
            return randomDialogues[randomIndex];
        }
        return "기본값 인사";
    }
}
