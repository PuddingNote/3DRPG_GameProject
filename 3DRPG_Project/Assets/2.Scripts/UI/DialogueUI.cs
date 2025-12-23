using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    private enum DialogueState
    {
        None,
        Greeting,       // 1. 인사 (랜덤 대사)
        Selection,      // 2. 퀘스트 목록 확인
        Confirmation,   // 3. 퀘스트 설명 및 수락/거절 (NEW)
        Result          // 4. 결과 대사 (시작 대사 or 완료 대사)
    }

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;  // 대화 UI 패널
    [SerializeField] private Text npcNameText;          // NPC 이름 텍스트
    [SerializeField] private Text dialogueText;         // 대사 내용 텍스트
    [SerializeField] private Button screenTouchButton;  // 전체 화면 터치용 버튼
    
    [Header("Quest List UI")]
    [SerializeField] private Transform actionExitButtonContainer;   // 버튼들이 생성될 부모 (퀘스트 버튼, 종료 버튼)
    [SerializeField] private GameObject actionButtonPrefab; // 생성할 퀘스트 버튼 프리팹
    [SerializeField] private GameObject exitButtonPrefab;   // 생성할 종료 버튼 프리팹

    [Header("Accept/Reject UI")]
    [SerializeField] private GameObject acceptRejectButtonContainer;    // 버튼들이 생성될 부모 (수락 버튼, 거절 버튼)
    [SerializeField] private Button acceptButton;       // 수락 버튼
    [SerializeField] private Button rejectButton;       // 거절 버튼

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f; // 타이핑 속도

    private DialogueState currentState = DialogueState.None; 
    private List<Quest> currentQuests; 
    private int selectedQuestID = -1;   // 현재 선택된 퀘스트 ID

    // 타이핑 효과 관련 변수
    private bool isTyping = false;
    private string fullText = "";
    private Coroutine typingCoroutine;

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

        // 초기화
        if (dialoguePanel != null) 
        {
            dialoguePanel.SetActive(false);
        }
        if (screenTouchButton != null) 
        {
            screenTouchButton.onClick.AddListener(OnScreenTouch);
        }
        
        // 컨테이너들 숨김 처리
        if (actionExitButtonContainer != null) 
        {
            actionExitButtonContainer.gameObject.SetActive(false);
        }
        if (acceptRejectButtonContainer != null) 
        {
            acceptRejectButtonContainer.SetActive(false);
        }

        // 수락/거절 버튼 리스너
        if (acceptButton != null) 
        {
            acceptButton.onClick.AddListener(OnAcceptClicked);
        }
        if (rejectButton != null) 
        {
            rejectButton.onClick.AddListener(OnRejectClicked);
        }
    }

    // 1단계 시작
    public void ShowDialogue(string npcName, string randomDialogue, List<Quest> activeQuests)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerInputLocked(true);
        }

        currentQuests = activeQuests;

        if (npcNameText != null)
        {
            npcNameText.text = npcName;
        }
        
        SetState(DialogueState.Greeting, randomDialogue);
    }

    private void SetState(DialogueState state, string text = "")
    {
        currentState = state;

        // 텍스트 갱신 (내용이 있을 때만 타이핑 시작)
        if (!string.IsNullOrEmpty(text) && dialogueText != null) 
        {
            // 이전 코루틴이 있다면 중지
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            
            fullText = text;
            typingCoroutine = StartCoroutine(TypeDialogue(text));
        }

        // 상태별 UI 활성/비활성 처리
        bool showList = (state == DialogueState.Selection);
        bool showConfirm = (state == DialogueState.Confirmation);
        
        if (actionExitButtonContainer != null) 
        {
            actionExitButtonContainer.gameObject.SetActive(showList);
        }
        if (acceptRejectButtonContainer != null) 
        {
            acceptRejectButtonContainer.SetActive(showConfirm);
        }
        
        // 스킵 기능을 위해 화면 터치 버튼은 항상 활성화
        if (screenTouchButton != null) 
        {
            screenTouchButton.interactable = true;
        }

        // Selection 단계 진입 시 버튼 생성
        if (state == DialogueState.Selection)
        {
            UpdateQuestButtons(currentQuests);
        }
    }

    // 타이핑 코루틴
    private IEnumerator TypeDialogue(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void OnScreenTouch()
    {
        // 1. 타이핑 중이면 즉시 완료 (스킵)
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            if (dialogueText != null)
            {
                dialogueText.text = fullText;
            }
            isTyping = false;
            return;
        }

        // 2. 타이핑이 끝났으면 다음 단계로 진행
        switch (currentState)
        {
            case DialogueState.Greeting:
                // 인사 -> 선택 단계
                SetState(DialogueState.Selection);
                break;
            case DialogueState.Result:
                // 결과 -> 종료
                CloseDialogue();
                break;
            
            // Selection, Confirmation 단계에서는 버튼 클릭을 유도해야 하므로 배경 터치 무시
            case DialogueState.Selection:
            case DialogueState.Confirmation:
                break;
        }
    }

    // 2단계: 리스트에서 퀘스트 버튼 클릭
    private void OnQuestButtonClicked(int questID, QuestState state)
    {
        // 타이핑 중이면 버튼 클릭 막기 (선택 사항, 여기서는 허용하되 즉시 완료 처리 후 넘어감)
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = fullText;
            isTyping = false;
        }

        selectedQuestID = questID;

        if (state == QuestState.NotStarted)
        {
            // 수락 전 확인 단계로 이동 (3단계)
            Quest quest = QuestManager.Instance.GetQuest(questID);
            string desc = (quest != null) ? quest.data.questDescription : "설명 없음";
            
            SetState(DialogueState.Confirmation, desc);
        }
        else if (state == QuestState.CanComplete)
        {
            // 완료는 즉시 처리 (4단계 직행)
            QuestManager.Instance.CompleteQuest(questID);
            
            Quest quest = QuestManager.Instance.GetQuest(questID);
            string completeMsg = (quest != null) ? quest.data.completeDialogue : "완료되었습니다.";
            
            SetState(DialogueState.Result, completeMsg);
        }
    }

    // 3단계: 수락 버튼 클릭
    private void OnAcceptClicked()
    {
        if (isTyping) 
        {
            return; // 타이핑 중 클릭 방지
        }

        if (selectedQuestID != -1)
        {
            QuestManager.Instance.AcceptQuest(selectedQuestID);
            
            Quest quest = QuestManager.Instance.GetQuest(selectedQuestID);
            string startMsg = (quest != null) ? quest.data.startDialogue : "퀘스트를 수락했습니다.";
            
            // 결과 단계로 이동 (4단계)
            SetState(DialogueState.Result, startMsg);
        }
    }

    // 3단계: 거절 버튼 클릭
    private void OnRejectClicked()
    {
        CloseDialogue();
    }

    private void UpdateQuestButtons(List<Quest> quests)
    {
        if (actionExitButtonContainer != null)
        {
            foreach (Transform child in actionExitButtonContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (actionExitButtonContainer == null) 
        {
            return;
        }

        if (quests != null && quests.Count > 0 && actionButtonPrefab != null)
        {
            foreach (var quest in quests)
            {
                if (quest.state == QuestState.InProgress) 
                {
                    continue;
                }

                GameObject buttonObj = Instantiate(actionButtonPrefab, actionExitButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();

                if (buttonText != null)
                {
                    if (quest.state == QuestState.NotStarted)
                    {
                        buttonText.text = $"{quest.data.questTitle} (확인)";
                    }
                    else if (quest.state == QuestState.CanComplete)
                    {
                        buttonText.text = $"{quest.data.questTitle} (완료)";
                    }
                }

                int questID = quest.data.questID;
                QuestState questState = quest.state;
                button.onClick.AddListener(() => OnQuestButtonClicked(questID, questState));
            }
        }

        if (exitButtonPrefab != null)
        {
            GameObject exitButtonObj = Instantiate(exitButtonPrefab, actionExitButtonContainer);
            Button exitButton = exitButtonObj.GetComponent<Button>();
            
            if (exitButton != null) 
            {
                exitButton.onClick.AddListener(CloseDialogue);
            }
        }
    }

    // 대화 UI 닫기
    private void CloseDialogue()
    {
        // 닫을 때 코루틴 정지
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        isTyping = false;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 플레이어 잠금 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerInputLocked(false);
        }
        
        currentState = DialogueState.None;
    }
}
