using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.AI;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Scene Transition")]
    public RectTransform transitionPanel;   // 화면 가림막 패널
    public float transitionDuration = 0.5f; // 애니메이션 시간

    [Header("Prefabs")]
    public GameObject playerPrefab;         // 플레이어 원본
    public GameObject mainCameraPrefab;     // 메인 카메라 원본
    public GameObject virtualCameraPrefab;  // 팔로우 카메라 원본
    public GameObject dungeonResultUIPrefab; // 던전 결과 UI 프리팹

    [Header("Current Instances")]
    public GameObject currentPlayer;        // 현재 플레이어 오브젝트
    public CinemachineVirtualCamera currentVirtualCamera;    // 현재 팔로우 카메라

    [Header("Data")]
    public Vector3 lastTownPosition;        // 마을에서의 마지막 위치
    public bool hasSavedPosition = false;   // 저장된 위치가 있는지 여부
    public string previousSceneName;        // 이전 씬 이름 (던전 입장 전 씬)

    private void Awake()
    {
        // 싱글톤 패턴: 게임 내에 단 하나만 존재하도록 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 씬 로드될 때마다 호출될 함수 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 씬 로딩이 끝날 때마다 호출
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log($"[GameManager] Scene Loaded: {scene.name}");

        // 1. 스폰 포인트 찾기
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        Vector3 startPos = Vector3.zero;
        Quaternion startRot = Quaternion.identity;

        if (spawnPoint != null)
        {
            startPos = spawnPoint.transform.position;
            startRot = spawnPoint.transform.rotation;
        }
        else
        {
            //Debug.Log("SpawnPoint가 없습니다! (0,0,0)에서 시작합니다.");
        }

        // 저장된 이전 씬으로 복귀했고 저장된 위치가 있다면 덮어쓰기
        if (hasSavedPosition && !string.IsNullOrEmpty(previousSceneName) && scene.name == previousSceneName)
        {
            startPos = lastTownPosition;
            //Debug.Log($"[{previousSceneName}] 복귀 위치 로드: {startPos}");
            hasSavedPosition = false; 
            previousSceneName = ""; // 사용 후 초기화
        }

        // 2. 플레이어 처리 (생성 or 이동)
        if (currentPlayer == null)
        {
            currentPlayer = Instantiate(playerPrefab, startPos, startRot);
            DontDestroyOnLoad(currentPlayer); 
        }
        else
        {
            // 상태 초기화 (위치 이동 전에 먼저 수행)
            ResetPlayerState();

            // 위치 이동 (물리 간섭 방지)
            ForceMovePlayer(startPos, startRot);
        }

        // 3. 메인 카메라 처리
        if (Camera.main == null)
        {
            GameObject mainCam = Instantiate(mainCameraPrefab);
            DontDestroyOnLoad(mainCam);
        }
        
        // 시네머신 브레인이 꺼져있다면 복구 
        if (Camera.main != null)
        {
            CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null && !brain.enabled)
            {
                brain.enabled = true;
            }
        }

        // 4. 가상 카메라 처리
        if (currentVirtualCamera == null)
        {
            GameObject vCamObj = Instantiate(virtualCameraPrefab);
            currentVirtualCamera = vCamObj.GetComponent<CinemachineVirtualCamera>();
            DontDestroyOnLoad(vCamObj);
        }

        // 5. 카메라 타겟 재연결
        SetupCameraTargets();

        // 6. 카메라 시점 리셋 (플레이어 뒤쪽 보기)
        if (currentPlayer != null)
        {
            PlayerController pc = currentPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.ResetCameraRotation();
                
                // 7. 씬 진입 직후 1초간 입력 잠금 (이동 방지)
                pc.LockInput(1.0f);
            }
        }

        // 8. 씬 전환 효과 (커튼 걷기: 중앙 -> 위)
        if (scene.name != "LoadingScene")
        {
            StartCoroutine(AnimatePanel(Vector2.zero, new Vector2(0, Screen.height)));
        }
        else
        {
            // 로딩 씬 진입 시에는 화면을 가린 상태 유지
            if (transitionPanel != null)
            {
                transitionPanel.gameObject.SetActive(true);
                transitionPanel.anchoredPosition = Vector2.zero;
            }
        }
    }

    // 던전 결과창 출력
    public void ShowDungeonResult(string nextSceneName)
    {
        if (dungeonResultUIPrefab != null)
        {
            GameObject uiObj = Instantiate(dungeonResultUIPrefab);
            DungeonResultUI resultUI = uiObj.GetComponent<DungeonResultUI>();
            if (resultUI != null)
            {
                resultUI.Setup(nextSceneName);
            }
        }
        //else
        //{
        //    Debug.Log("DungeonResultUI Prefab이 GameManager에 할당되지 않았습니다.");
        //    LoadingSceneController.LoadScene(nextSceneName);
        //}
    }

    // 외부 호출용: 연출과 함께 씬 이동
    public void LoadSceneWithTransition(string sceneName)
    {
        StartCoroutine(TransitionProcess(sceneName));
    }

    // 씬 이동 연출 처리
    private IEnumerator TransitionProcess(string sceneName)
    {
        // 1. 커튼 올리기 (아래 -> 중앙)
        yield return StartCoroutine(AnimatePanel(new Vector2(0, -Screen.height), Vector2.zero));

        // 2. 실제 로딩 시작
        LoadingSceneController.LoadSceneDirectly(sceneName);
    }

    // 커튼 애니메이션 처리
    private IEnumerator AnimatePanel(Vector2 startPos, Vector2 endPos)
    {
        if (transitionPanel == null) 
        {
            yield break;
        }

        transitionPanel.gameObject.SetActive(true);
        float timer = 0f;
        
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            t = t * t * (3f - 2f * t); // SmoothStep
            
            transitionPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        transitionPanel.anchoredPosition = endPos;
        
        // 화면을 다 걷어냈으면(위로 올라갔으면) 비활성화
        if (endPos.y > 0)
        {
             transitionPanel.gameObject.SetActive(false);
        }
    }

    // 플레이어 위치 강제 이동
    private void ForceMovePlayer(Vector3 pos, Quaternion rot)
    {
        if (currentPlayer == null) 
        {
            return;
        }

        NavMeshAgent agent = currentPlayer.GetComponent<NavMeshAgent>();
        CharacterController cc = currentPlayer.GetComponent<CharacterController>();

        // 1. 물리 간섭 제거 (CharacterController가 켜져 있으면 transform.position 변경이 무시될 수 있음)
        if (cc != null) 
        {
            cc.enabled = false;
        }
        
        // 2. Agent는 어차피 꺼져있어야 정상이지만, 혹시 켜져 있다면 끔
        if (agent != null) 
        {
            agent.enabled = false;
        }

        // 3. 위치 이동
        currentPlayer.transform.position = pos;
        currentPlayer.transform.rotation = rot;

        // 4. 수동 모드 복구
        if (cc != null) 
        {
            cc.enabled = true;
        }
    }

    // 카메라 타겟 설정
    private void SetupCameraTargets()
    {
        if (currentVirtualCamera != null && currentPlayer != null)
        {
            Transform pivot = currentPlayer.transform.Find("PlayerCameraPivot");
            if (pivot == null) 
            {
                pivot = currentPlayer.transform; 
            }

            currentVirtualCamera.Follow = pivot;
            currentVirtualCamera.LookAt = pivot;

            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.virtualCamera = currentVirtualCamera;
                Cinemachine3rdPersonFollow cm3rdPerson = currentVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if (cm3rdPerson != null)
                {
                    playerController.thirdPersonFollow = cm3rdPerson;
                }
            }
        }
    }

    // 마을 위치 저장
    public void SaveTownPosition(Vector3 position)
    {
        lastTownPosition = position;
        hasSavedPosition = true;
        //Debug.Log($"마을 위치 저장: {lastTownPosition}");
    }

    // 이전 씬 이름 저장
    public void SavePreviousSceneName(string sceneName)
    {
        previousSceneName = sceneName;
        //Debug.Log($"이전 씬 저장: {previousSceneName}");
    }

    // 플레이어 상태 초기화
    private void ResetPlayerState()
    {
        if (currentPlayer == null) 
        {
            return;
        }

        PlayerController pc = currentPlayer.GetComponent<PlayerController>();
        if (pc != null)
        {
            // 1. 플레이어 오브젝트 재부팅 (체력 회복, 기본 스탯 초기화)
            pc.gameObject.SetActive(false);
            pc.gameObject.SetActive(true);
            pc.currentHp = pc.maxHp;

            // 2. 자동 모드 해제 (NavMeshAgent 끄기, CharacterController 켜기)
            if (pc.isAutoMode)
            {
                pc.ToggleAutoMode();
            }
            
            // 3. 상태 강제 초기화 (Idle)
            pc.ChangeState(new PlayerIdleState(pc));
            
            // 4. 타겟 정보 초기화
            pc.target = null;
            pc.interactionTarget = null;
            pc.interactionTransform = null;
            pc.currentRoom = null; // 던전 방 정보 초기화

            //Debug.Log($"플레이어 상태 초기화 완료: {pc.name}");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
