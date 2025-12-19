using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Quest Database")]
    // 게임 내 모든 퀘스트 데이터를 관리 (Inspector에서 할당)
    public List<QuestData> allQuestData = new List<QuestData>();

    // 런타임 퀘스트 상태 관리 (Key: QuestID)
    private Dictionary<int, Quest> questDictionary = new Dictionary<int, Quest>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeQuests();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 1. 초기화: ScriptableObject 데이터를 기반으로 런타임 객체(Quest) 생성
    private void InitializeQuests()
    {
        foreach (var data in allQuestData)
        {
            if (data != null && !questDictionary.ContainsKey(data.questID))
            {
                questDictionary.Add(data.questID, new Quest(data));
            }
        }
    }

    // 2. 퀘스트 정보 가져오기
    public Quest GetQuest(int questID)
    {
        if (questDictionary.ContainsKey(questID))
        {
            return questDictionary[questID];
        }
        return null;
    }

    // 특정 퀘스트 데이터로 퀘스트 객체 찾기
    public Quest GetQuest(QuestData data)
    {
        if (data != null)
        {
            return GetQuest(data.questID);
        }
        return null;
    }

    // 3. 퀘스트 수락
    public void AcceptQuest(int questID)
    {
        Quest quest = GetQuest(questID);
        if (quest != null && quest.state == QuestState.NotStarted)
        {
            quest.state = QuestState.InProgress;
            Debug.Log($"[QuestManager] 퀘스트 수락: {quest.data.questTitle} (ID: {questID})");

            // [테스트용] 3초 후 자동 완료 가능 상태 전환
            StartCoroutine(TestCompleteRoutine(quest));
        }
    }

    // 4. 퀘스트 완료
    public void CompleteQuest(int questID)
    {
        Quest quest = GetQuest(questID);
        if (quest != null && quest.state == QuestState.CanComplete)
        {
            quest.state = QuestState.Completed;
            Debug.Log($"[QuestManager] 퀘스트 완료: {quest.data.questTitle} (ID: {questID})");

            // TODO: 보상 지급 로직 추가 예정

            // 연계 퀘스트 처리 (다음 퀘스트가 있다면 자동 수락 혹은 해금 등 처리 가능)
            if (quest.data.nextQuest != null)
            {
                // 여기서는 단순히 로그만 남김
                Debug.Log($"[QuestManager] 연계 퀘스트 발견: {quest.data.nextQuest.questTitle}");
            }
        }
    }

    // [테스트용] 3초 대기 후 완료 가능 상태로 변경
    private IEnumerator TestCompleteRoutine(Quest quest)
    {
        Debug.Log($"[QuestManager] 테스트: 3초 대기 시작 ({quest.data.questTitle})");
        yield return new WaitForSeconds(3.0f);
        
        if (quest.state == QuestState.InProgress)
        {
            quest.state = QuestState.CanComplete;
            Debug.Log($"[QuestManager] 테스트: 3초 경과! 퀘스트 완료 가능 상태로 변경 ({quest.data.questTitle})");
        }
    }
}
