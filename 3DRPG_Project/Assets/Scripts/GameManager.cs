using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Prefabs")]
    public GameObject playerPrefab;        // 플레이어 원본
    public GameObject mainCameraPrefab;    // 메인 카메라 원본 (CinemachineBrain 포함)
    public GameObject virtualCameraPrefab; // 팔로우 카메라 원본

    [Header("Current Instances")]
    public GameObject currentPlayer;
    public CinemachineVirtualCamera currentVirtualCamera;

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
            Debug.LogWarning("SpawnPoint가 없습니다! (0,0,0)에서 시작합니다.");
        }

        // 2. 플레이어가 없으면 생성, 있으면 위치 이동
        if (currentPlayer == null)
        {
            currentPlayer = Instantiate(playerPrefab, startPos, startRot);
            DontDestroyOnLoad(currentPlayer); // 플레이어도 계속 유지
        }
        else
        {
            currentPlayer.GetComponent<CharacterController>().enabled = false; // 이동 전 물리 충돌 방지
            currentPlayer.transform.position = startPos;
            currentPlayer.transform.rotation = startRot;
            currentPlayer.GetComponent<CharacterController>().enabled = true;
        }

        // 3. 메인 카메라가 없으면 생성
        if (Camera.main == null)
        {
            //Instantiate(mainCameraPrefab);
            GameObject mainCam = Instantiate(mainCameraPrefab);
            DontDestroyOnLoad(mainCam);

        }

        // 4. 가상 카메라 생성 및 타겟 연결
        if (currentVirtualCamera == null)
        {
            GameObject vCamObj = Instantiate(virtualCameraPrefab);
            currentVirtualCamera = vCamObj.GetComponent<CinemachineVirtualCamera>();
            DontDestroyOnLoad(vCamObj);
        }

        // 5. 카메라 타겟 재연결
        if (currentVirtualCamera != null && currentPlayer != null)
        {
            // Player 안에 있는 Pivot 찾기
            Transform pivot = currentPlayer.transform.Find("PlayerCameraPivot");
            if (pivot == null) 
            {
                pivot = currentPlayer.transform; // 예외처리: Pivot 없으면 그냥 플레이어 몸통으로
            }

            currentVirtualCamera.Follow = pivot;
            currentVirtualCamera.LookAt = pivot;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

