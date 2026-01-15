using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class DungeonRoomManager : MonoBehaviour
{
    [Header("Settings")]
    public DungeonDoor roomDoor;        // 이 방을 클리어하면 열릴 문
    public GameObject monsterPrefab;    // 생성할 몬스터 프리펩
    public List<Transform> spawnPoints; // 몬스터가 생성될 위치들
    public Transform nextWaypoint;      // 다음 방으로 가는 길목 위치
    
    [Header("Final Room Settings")]
    public bool isLastRoom = false;     // 마지막 방인지 여부
    public string nextSceneName;        // 클리어 후 이동할 씬 이름

    [Header("Status")]
    public List<LivingEntity> liveMonsters = new List<LivingEntity>();  // 생성된 몬스터들 리스트
    public bool isCleared = false;      // 방 클리어 여부
    
    private Transform lastDeadMonsterTransform; // 마지막으로 죽은 몬스터 위치

    // 플레이어 방 진입 시 호출
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.currentRoom != this)   // 이미 현재 방으로 인식되어 있다면 중복 처리하지 않음
            {
                player.currentRoom = this;
                //Debug.Log($"Entered Room: {gameObject.name}");
            }
        }
    }

    private void Start()
    {
        // 마지막 방이라면 GameManager에서 복귀할 씬 이름을 가져옴
        if (isLastRoom && GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.previousSceneName))
        {
            nextSceneName = GameManager.Instance.previousSceneName;
            //Debug.Log($"[DungeonRoom] 복귀할 씬 설정됨: {nextSceneName}");
        }

        SpawnMonsters();
    }

    // 몬스터 생성
    private void SpawnMonsters()
    {
        // 1. 프리팹과 스폰 포인트가 모두 있을 때만 생성 시도
        if (monsterPrefab != null && spawnPoints != null)
        {
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint == null) 
                {
                    continue;
                }

                GameObject monsterObj = Instantiate(monsterPrefab, spawnPoint.position, spawnPoint.rotation);
                LivingEntity monsterEntity = monsterObj.GetComponent<LivingEntity>();

                if (monsterEntity != null)
                {
                    liveMonsters.Add(monsterEntity);
                    monsterEntity.OnDeath += () => HandleMonsterDeath(monsterEntity);
                }
            }
        }

        // 2. 생성 후 몬스터가 한 마리도 없다면 (프리팹 미할당 or 스폰포인트 0개) 문을 열기
        if (liveMonsters.Count == 0)
        {
            isCleared = true;
            if (roomDoor != null)
            {
                roomDoor.Unlock();
            }
            //Debug.Log($"방 {gameObject.name} 자동 클리어 (몬스터 없음)");
        }
    }

    // 몬스터 사망 시 호출
    private void HandleMonsterDeath(LivingEntity monster)
    {
        if (liveMonsters.Contains(monster))
        {
            liveMonsters.Remove(monster);
            lastDeadMonsterTransform = monster.transform; // 위치 저장 (연출용)
        }

        CheckRoomClear();
    }

    // 방 클리어 체크
    private void CheckRoomClear()
    {
        if (isCleared) 
        {
            return;
        }

        if (liveMonsters.Count == 0)
        {
            isCleared = true;
            if (roomDoor != null)
            {
                roomDoor.Unlock();
            }

            //Debug.Log($"방 {gameObject.name} 클리어");

            // [마지막 방 처리] 몬스터 전멸 시 클리어 연출 시작
            if (isLastRoom)
            {
                StartCoroutine(BossClearSequence());
            }
        }
    }

    // 마지막 방 클리어 시 호출 (연출)
    private IEnumerator BossClearSequence()
    {
        // 연출 중에는 미니맵 캔버스를 숨김 처리(보스 슬로우모션/클로즈업 동안)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PushMinimapHidden(this);
        }

        // 0. 연출 시작 즉시 플레이어 조작 차단
        if (GameManager.Instance.currentPlayer != null)
        {
            PlayerController player = GameManager.Instance.currentPlayer.GetComponent<PlayerController>();
            if (player != null)
            {
                // 자동 모드 해제 및 Agent 정지
                if (player.isAutoMode) 
                {
                    player.ToggleAutoMode();
                }
                player.isAutoMode = false;
                
                if (player.agent != null)
                {
                    if (player.agent.isActiveAndEnabled)
                    {
                        player.agent.isStopped = true;
                        player.agent.ResetPath();
                        player.agent.velocity = Vector3.zero;
                    }
                    player.agent.enabled = false;
                }

                // 입력 잠금 및 상태 전환
                player.SetInputLock(true);
                player.ChangeState(new PlayerIdleState(player));
            }
        }

        // 1. 슬로우 모션 (1.5초간 0.2배속)
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(1.5f);
        Time.timeScale = 1.0f;

        // 2. 몬스터 줌인
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain != null) 
            {
                brain.enabled = false;
            }

            if (lastDeadMonsterTransform != null)
            {
                Vector3 monsterPos = lastDeadMonsterTransform.position;
                Vector3 currentCamPos = mainCam.transform.position;

                // 1. 몬스터에서 카메라를 바라보는 방향 계산 (현재 앵글 유지)
                Vector3 directionToCam = (currentCamPos - monsterPos).normalized;
                
                // 방향이 너무 위쪽이거나 이상하면 몬스터 정면 기준으로 잡음
                if (directionToCam.y > 0.9f || directionToCam == Vector3.zero) 
                {
                    directionToCam = (lastDeadMonsterTransform.forward + Vector3.up).normalized;
                }

                // 2. 고정된 거리값 설정 (줌 레벨 무시)
                float startDist = 7.0f;
                float endDist = 4.0f;
                float height = 3.0f;

                Vector3 startPos = monsterPos + (directionToCam * startDist) + (Vector3.up * height);
                Vector3 endPos = monsterPos + (directionToCam * endDist) + (Vector3.up * (height * 0.7f)); // 살짝 낮아지면서 줌인

                // 3. 진입 연출: 현재 위치에서 시작 위치로 빠르게 이동
                Vector3 initialCamPos = mainCam.transform.position;
                Quaternion initialCamRot = mainCam.transform.rotation;
                Quaternion targetLookRot = Quaternion.LookRotation(monsterPos + Vector3.up * 1.0f - startPos);

                float entryDuration = 1.0f;
                float entryTimer = 0f;

                while (entryTimer < entryDuration)
                {
                    entryTimer += Time.deltaTime;
                    float t = entryTimer / entryDuration;
                    t = t * t * (3f - 2f * t);

                    mainCam.transform.position = Vector3.Lerp(initialCamPos, startPos, t);
                    mainCam.transform.rotation = Quaternion.Slerp(initialCamRot, targetLookRot, t);
                    yield return null;
                }

                yield return new WaitForSeconds(1.5f);
            }
        }

        // 3. 플레이어 카메라 연출
        if (GameManager.Instance.currentPlayer != null)
        {
            PlayerController player = GameManager.Instance.currentPlayer.GetComponent<PlayerController>();
            
            if (player != null)
            {
                // 연출 좌표 계산 (플레이어 기준)
                Transform pTr = player.transform;
                Vector3 center = pTr.position + Vector3.up * 1.5f;

                // 1. 시작: 왼쪽 45도 + 약간 아래
                Vector3 leftOffset = (Quaternion.Euler(0, 45, 0) * pTr.forward) * 5.0f;
                Vector3 startPos = center + leftOffset + (Vector3.down * 0.5f);

                // 2. 중간: 오른쪽 45도 + 약간 위
                Vector3 rightOffset = (Quaternion.Euler(0, -45, 0) * pTr.forward) * 5.0f;
                Vector3 midPos = center + rightOffset + (Vector3.up * 1.5f);

                // 3. 끝: 정면
                Vector3 frontOffset = pTr.forward * 6.0f; 
                Vector3 endPos = center + frontOffset + Vector3.up * 1.0f;

                // 카메라 무빙 실행
                yield return StartCoroutine(PlayerDynamicCamera(mainCam, pTr, startPos, midPos, endPos));
            }
        }

        // 4. 1초 대기 후 결과 UI 출력
        yield return new WaitForSeconds(1.0f);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowDungeonResult(nextSceneName);
            }
            //else
            //
            //   // GameManager가 없으면 바로 이동 (비상용)
            //   LoadingSceneController.LoadScene(nextSceneName);
            //
        }

        // 결과 UI가 자체적으로 미니맵을 숨기므로, 연출용 숨김 요청은 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PopMinimapHidden(this);
        }
    }

    // 플레이어 주변을 도는 카메라 연출
    private IEnumerator PlayerDynamicCamera(Camera cam, Transform target, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        if (cam == null) 
        {
            yield break;
        }

        // 1. 왼쪽에서 오른쪽으로 이동
        cam.transform.position = p1;
        cam.transform.LookAt(target.position + Vector3.up * 1.2f);
        
        float duration = 1.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            t = t * t * (3f - 2f * t);

            cam.transform.position = Vector3.Lerp(p1, p2, t);
            cam.transform.LookAt(target.position + Vector3.up * 1.2f);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        // 2. 정면으로 이동
        timer = 0f;
        duration = 1.5f;
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        
        // 최종 바라볼 각도 미리 계산
        Vector3 lookTarget = target.position + Vector3.up * 1.0f;
        Quaternion endRot = Quaternion.LookRotation(lookTarget - p3);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            t = t * t * (3f - 2f * t);

            cam.transform.position = Vector3.Lerp(startPos, p3, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        
        cam.transform.position = p3;
        cam.transform.LookAt(lookTarget);
    }
}
