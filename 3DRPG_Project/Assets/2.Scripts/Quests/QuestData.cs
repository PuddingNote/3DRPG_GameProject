using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Scriptable Object/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Basic Info")]
    public int questID;             // 퀘스트 고유 ID
    public string questTitle;       // 퀘스트 제목
    [TextArea]
    public string questDescription; // 퀘스트 설명 (UI 표시용)

    [Header("Dialogues")]
    [TextArea]
    public string startDialogue;    // 퀘스트 시작 시 대사 (NPC가 하는 말) (예: "저 좀 도와주세요!")
    [TextArea]
    public string progressDialogue; // 퀘스트 진행 중 대사 (예: "아직 멀었나요?")
    [TextArea]
    public string completeDialogue; // 퀘스트 완료 시 대사 (예: "정말 감사합니다!")

    [Header("Chain Quest")]
    public QuestData nextQuest;     // 연계 퀘스트 (이 퀘스트 완료 시 해금될 퀘스트, 나중에 구현)
}
