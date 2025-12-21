using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text npcNameText;      // NPC 이름 텍스트
    [SerializeField] private Text dialogueText;     // 대사 내용 텍스트
    
    [Header("Quest Buttons")]
    [SerializeField] private Transform buttonContainer;     // 버튼들이 생성될 부모 (Vertical Layout Group 권장)
    [SerializeField] private GameObject actionButtonPrefab; // 생성할 퀘스트 버튼 프리팹
    [SerializeField] private GameObject exitButtonPrefab;   // 생성할 종료 버튼 프리팹

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    // 대화 UI 표시
    public void ShowDialogue(string npcName, string dialogue, List<Quest> activeQuests)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // 플레이어 잠금
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerInputLocked(true);
        }

        // 텍스트 설정
        if (npcNameText != null) 
        {
            npcNameText.text = npcName;
        }
        if (dialogueText != null) 
        {
            dialogueText.text = dialogue;
        }
        
        // 버튼 생성 로직 호출
        UpdateQuestButtons(activeQuests);
    }

    // 퀘스트 버튼 업데이트
    private void UpdateQuestButtons(List<Quest> quests)
    {
        // 1. 기존 버튼 삭제
        if (buttonContainer != null)
        {
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (buttonContainer == null) 
        {
            return;
        }

        // 2. 퀘스트 버튼 동적 생성 (퀘스트가 있을 경우에만)
        if (quests != null && quests.Count > 0 && actionButtonPrefab != null)
        {
            foreach (var quest in quests)
            {
                // 진행 중인 퀘스트는 버튼을 만들지 않음 (완료 가능 상태거나 시작 전일 때만)
                if (quest.state == QuestState.InProgress) 
                {
                    continue;
                }

                GameObject buttonObj = Instantiate(actionButtonPrefab, buttonContainer);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();

                // 텍스트 설정
                if (buttonText != null)
                {
                    if (quest.state == QuestState.NotStarted)
                    {
                        buttonText.text = $"{quest.data.questTitle} (수락)";
                    }
                    else if (quest.state == QuestState.CanComplete)
                    {
                        buttonText.text = $"{quest.data.questTitle} (완료)";
                    }
                }

                // 클릭 이벤트 설정 (클로저 변수 캡처 주의)
                int questID = quest.data.questID;
                QuestState qState = quest.state;
                
                button.onClick.AddListener(() => OnQuestButtonClicked(questID, qState));
            }
        }

        // 3. 마지막에 종료 버튼 생성 (항상 존재)
        if (exitButtonPrefab != null)
        {
            GameObject exitButtonObj = Instantiate(exitButtonPrefab, buttonContainer);
            Button exitButton = exitButtonObj.GetComponent<Button>();
            
            //// 종료 버튼 텍스트 설정 (프리팹에 이미 "대화 종료"라고 처리)
            //Text exitButtonText = exitButtonObj.GetComponentInChildren<Text>();
            //if (exitButtonText != null)
            //{
            //    exitButtonText.text = "대화 종료";
            //}

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(CloseDialogue);
            }
        }
    }

    // 퀘스트 버튼 클릭 이벤트
    private void OnQuestButtonClicked(int questID, QuestState state)
    {
        if (state == QuestState.NotStarted)
        {
            QuestManager.Instance.AcceptQuest(questID);
        }
        else if (state == QuestState.CanComplete)
        {
            QuestManager.Instance.CompleteQuest(questID);
        }
        
        CloseDialogue();
    }

    // 대화 UI 닫기
    private void CloseDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 플레이어 잠금 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerInputLocked(false);
        }
    }
}
