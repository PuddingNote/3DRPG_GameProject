using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class NPCController : MonoBehaviour, IInteractable
{
    [Header("Basic Info")]
    [Tooltip("NPC 이름")]
    public string npcName;

    [Tooltip("이 NPC가 줄 수 있는 퀘스트 목록")]
    public List<QuestData> questList;

    
    [Header("Dialogues")]
    [Tooltip("이 NPC가 하는 기본 랜덤 대사 목록")]
    public List<string> randomDialogues;


    [Header("Camera Setting")]
    [Tooltip("NPC 기준 카메라가 떨어질 거리 (NPC 정면에서 바라보는 위치)")]
    //[SerializeField]
    private float dialogueCameraDistance = 5.0f;

    [Tooltip("NPC 높이의 몇 % 지점을 바라볼지")]
    //[SerializeField] 
    private float lookHeightRatio = 0.7f;

    [Tooltip("카메라 높이를 바라볼 지점 기준으로 얼마나 더 올릴지(미세 보정, 월드 단위)")]
    //[SerializeField] 
    private float cameraHeightOffsetFromLook = 0.7f;

    [Tooltip("카메라가 앵커까지 이동하는 시간(초)")]
    //[SerializeField] 
    private float cameraMoveDuration = 1.0f;

    [Tooltip("NPC 상호작용 시작 후 플레이어를 숨기기까지의 지연 시간(초)")]
    //[SerializeField] 
    private float playerHideDelay = 0.5f;


    private bool isInteracting = false;         // NPC 상호작용 중인지 여부
    private CinemachineBrain cachedBrain;       // 메인 카메라의 CinemachineBrain
    private bool cachedBrainEnabled;            // 메인 카메라의 CinemachineBrain 활성화 여부
    private Vector3 cachedCameraPosition;       // 메인 카메라의 원래 위치
    private Quaternion cachedCameraRotation;    // 메인 카메라의 원래 회전
    private Coroutine hidePlayerCoroutine;      // 플레이어 숨기기 코루틴
    
    public void Interact()
    {
        if (isInteracting)
        {
            return;
        }

        StartCoroutine(InteractionRoutine());
    }

    private IEnumerator InteractionRoutine()
    {
        isInteracting = true;

        // 1. 입력 잠금
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerInputLocked(true);
        }

        // 2. 플레이어 숨기기 (지연 적용)
        if (hidePlayerCoroutine != null)
        {
            StopCoroutine(hidePlayerCoroutine);
        }
        hidePlayerCoroutine = StartCoroutine(HidePlayerAfterDelay());

        // 3. 메인 카메라를 직접 제어하기 위해 CinemachineBrain을 잠시 비활성화
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cachedBrain = mainCamera.GetComponent<CinemachineBrain>();
            if (cachedBrain != null)
            {
                cachedBrainEnabled = cachedBrain.enabled;
                cachedBrain.enabled = false;
            }

            cachedCameraPosition = mainCamera.transform.position;
            cachedCameraRotation = mainCamera.transform.rotation;

            // NPC 크기에 맞는 머리 높이 계산 (스케일이 커져도 자동 대응)
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float lookY = bounds.min.y + bounds.size.y * lookHeightRatio;
            Vector3 lookTargetPos = new Vector3(transform.position.x, lookY, transform.position.z);

            float cameraY = lookY + cameraHeightOffsetFromLook;
            Vector3 anchorPos = new Vector3(transform.position.x, cameraY, transform.position.z) + transform.forward * dialogueCameraDistance;

            yield return MoveCamera(mainCamera.transform, cachedCameraPosition, cachedCameraRotation, anchorPos, lookTargetPos, cameraMoveDuration);
        }

        // 4. 대화 데이터 준비
        List<Quest> availableQuests = GetAvailableQuests();
        string dialogue = GetRandomDialogue();

        // 5. UI 호출 (종료 시 콜백으로 EndInteraction 전달)
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowDialogue(npcName, dialogue, availableQuests, EndInteraction);
        }
        else
        {
            // UI가 없으면 바로 종료 루틴 실행해서 원상복구
            //Debug.Log("DialogueUI Instance is null!");
            EndInteraction();
        }
    }

    // 대화 종료 콜백
    private void EndInteraction()
    {
        StartCoroutine(EndInteractionRoutine());
    }

    // 대화 종료 코루틴
    private IEnumerator EndInteractionRoutine()
    {
        // 종료 시점에 숨김 지연 코루틴이 남아있다면 취소 (대화 끝났는데 갑자기 사라지는 상황 방지)
        if (hidePlayerCoroutine != null)
        {
            StopCoroutine(hidePlayerCoroutine);
            hidePlayerCoroutine = null;
        }

        // 1. 카메라는 무빙 없이 즉시 원래 상태로 복귀
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = cachedCameraPosition;
            mainCamera.transform.rotation = cachedCameraRotation;
        }

        if (cachedBrain != null)
        {
            cachedBrain.enabled = cachedBrainEnabled;
        }

        // 2. 플레이어 보이기
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerVisible(true);
            
            // 3. 입력 잠금 해제
            GameManager.Instance.SetPlayerInputLocked(false);
        }

        isInteracting = false;
        yield return null;
    }

    // 플레이어 숨기기 코루틴
    private IEnumerator HidePlayerAfterDelay()
    {
        if (playerHideDelay > 0f)
        {
            yield return new WaitForSeconds(playerHideDelay);
        }

        if (!isInteracting)
        {
            yield break;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerVisible(false);
        }

        hidePlayerCoroutine = null;
    }

    // 카메라 이동 코루틴
    private IEnumerator MoveCamera(Transform cameraTransform, Vector3 startPos, Quaternion startRot, Vector3 endPos, Vector3 lookTargetPos, float duration)
    {
        if (cameraTransform == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            cameraTransform.position = endPos;
            cameraTransform.rotation = Quaternion.LookRotation((lookTargetPos - endPos).normalized, Vector3.up);
            yield break;
        }

        // 타겟을 중심으로 원 궤도로 돌면서 앵커 위치로 이동
        Vector3 center = lookTargetPos;
        Vector3 startOffset = startPos - center;
        Vector3 endOffset = endPos - center;

        // Yaw(수평) 기준이 자연스럽기 때문에 XZ 평면 기준으로 각도를 계산
        Vector2 startXZ = new Vector2(startOffset.x, startOffset.z);
        Vector2 endXZ = new Vector2(endOffset.x, endOffset.z);

        float startRadius = startXZ.magnitude;
        float endRadius = endXZ.magnitude;

        // 시작/종료 반지름이 0에 가까우면 원궤도 정의가 어려우므로 직선 보간으로 fallback
        if (startRadius < 0.0001f || endRadius < 0.0001f)
        {
            float fallbackTimer = 0f;
            while (fallbackTimer < duration)
            {
                fallbackTimer += Time.deltaTime;
                float ft = Mathf.Clamp01(fallbackTimer / duration);
                ft = ft * ft * (3f - 2f * ft);

                Vector3 pos = Vector3.Lerp(startPos, endPos, ft);
                cameraTransform.position = pos;

                Vector3 toTarget = lookTargetPos - pos;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    cameraTransform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                }

                yield return null;
            }

            cameraTransform.position = endPos;
            Vector3 finalToTargetFallback = lookTargetPos - endPos;
            if (finalToTargetFallback.sqrMagnitude > 0.0001f)
            {
                cameraTransform.rotation = Quaternion.LookRotation(finalToTargetFallback.normalized, Vector3.up);
            }
            yield break;
        }

        float startAngle = Mathf.Atan2(startXZ.x, startXZ.y) * Mathf.Rad2Deg;
        float endAngle = Mathf.Atan2(endXZ.x, endXZ.y) * Mathf.Rad2Deg;
        float deltaAngle = Mathf.DeltaAngle(startAngle, endAngle); // 가장 자연스러운(짧은) 방향

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = t * t * (3f - 2f * t);

            float angle = startAngle + deltaAngle * t;
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            float y = Mathf.Lerp(startPos.y, endPos.y, t);

            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
            pos.y = y;

            cameraTransform.position = pos;

            // 매 프레임 타겟을 바라보게 회전
            Vector3 toTarget = lookTargetPos - pos;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                cameraTransform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            }

            yield return null;
        }

        cameraTransform.position = endPos;
        Vector3 finalToTarget = (lookTargetPos - endPos);
        if (finalToTarget.sqrMagnitude > 0.0001f)
        {
            cameraTransform.rotation = Quaternion.LookRotation(finalToTarget.normalized, Vector3.up);
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
