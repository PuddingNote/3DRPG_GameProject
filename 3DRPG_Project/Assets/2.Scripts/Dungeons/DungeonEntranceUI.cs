using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DungeonEntranceUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Text dungeonNameText;      // "일반 던전"
    [SerializeField] private Button enterButton;        // 입장하기 버튼
    [SerializeField] private Button closeButton;        // 닫기 버튼

    private Text enterButtonText;

    [Header("Stage Selection")]
    [SerializeField] private Transform stageButtonContainer; // 버튼들이 들어갈 부모 오브젝트
    [SerializeField] private GameObject stageButtonPrefab;   // 생성할 버튼 프리팹

    private DungeonData currentDungeonData; // 현재 선택된 던전 데이터
    private DungeonStage selectedStage;     // 현재 선택된 스테이지 정보

    private void Start()
    {
        if (enterButton != null)
        {
            enterButtonText = enterButton.GetComponentInChildren<Text>();
        }

        enterButton.onClick.AddListener(OnEnterClick);
        closeButton.onClick.AddListener(OnCloseClick);
        
        gameObject.SetActive(false);
    }

    // 던전 입장 UI 열기
    public void OpenUI(DungeonData data)
    {
        currentDungeonData = data;
        selectedStage = null;
        
        // UI가 열릴 때 플레이어 입력 잠금
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerInputLocked(true);
        }

        UpdateDungeonInfo();
        CreateStageButtons();
        
        // 처음엔 첫 번째 자동 선택
        if (currentDungeonData.stages.Count > 0)
        {
            SelectStage(currentDungeonData.stages[0]);
        }
        //else
        //{
        //    if (enterButtonText != null) 
        //    {
        //        enterButtonText.text = "입장 불가";
        //    }
        //    enterButton.interactable = false;
        //}

        gameObject.SetActive(true);
    }

    // 던전 정보 갱신
    private void UpdateDungeonInfo()
    {
        if (currentDungeonData != null)
        {
            dungeonNameText.text = currentDungeonData.dungeonName;
        }
    }

    // 스테이지 버튼들을 동적으로 생성
    private void CreateStageButtons()
    {
        // 기존 버튼들 제거 (자식 오브젝트 모두 삭제)
        foreach (Transform child in stageButtonContainer)
        {
            Destroy(child.gameObject);
        }

        if (currentDungeonData == null) 
        {
            return;
        }

        foreach (var stage in currentDungeonData.stages)
        {
            GameObject btnObj = Instantiate(stageButtonPrefab, stageButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            Text btnText = btnObj.GetComponentInChildren<Text>();

            if (btnText != null) 
            {
                btnText.text = stage.stageName; // "1-1", "1-2"
            }

            // 버튼 클릭 시 해당 스테이지 선택
            btn.onClick.AddListener(() => SelectStage(stage));
        }
    }

    // 스테이지 선택 시 호출
    private void SelectStage(DungeonStage stage)
    {
        selectedStage = stage;
        
        if (enterButtonText != null)
        {
            enterButtonText.text = $"{stage.stageName} 진입";
        }

        enterButton.interactable = true;
    }

    private void OnEnterClick()
    {
        if (selectedStage != null)
        {
            CloseUI();

            // GameManager에 데이터 저장 (마을 위치 등)
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.currentPlayer != null)
                {
                    GameManager.Instance.SaveTownPosition(GameManager.Instance.currentPlayer.transform.position);
                }
                GameManager.Instance.SavePreviousSceneName(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }

            // 선택된 스테이지의 씬으로 이동
            LoadingSceneController.LoadScene(selectedStage.sceneName);
        }
    }

    private void OnCloseClick()
    {
        CloseUI();
    }

    private void CloseUI()
    {
        gameObject.SetActive(false);
        
        // UI 닫을 때 플레이어 잠금 해제 (GameManager를 통해 범용적으로 처리)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerInputLocked(false);
        }
    }
}
