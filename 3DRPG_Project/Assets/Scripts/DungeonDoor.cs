using System.Collections;
using UnityEngine;

public class DungeonDoor : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public bool isLocked = true;    // 문 잠김 여부
    public float openHeight = 4f;   // 문 열리는 높이
    public float openSpeed = 4f;    // 문 열리는 속도
    
    [Tooltip("플레이어가 문을 열기 위해 이동할 위치 (할당 안되있으면 본체 위치 사용)")]
    public Transform interactionPoint; 

    private bool isOpen = false;    // 문 열림 여부
    public bool IsOpen => isOpen;   // 외부에서 확인 가능하도록 프로퍼티 추가

    // 상호작용 위치 반환 프로퍼티 (없으면 자기 자신 반환)
    public Vector3 InteractionPosition
    {
        get
        {
            if (interactionPoint != null)
            {
                return interactionPoint.position;
            }

            return transform.position;
        }
    }

    private Vector3 initialPosition;    // 문 초기 위치 (기본 위치)
    private Vector3 targetPosition;     // 문 목표 위치 (열린 위치)

    private void Awake()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition + Vector3.up * openHeight;
    }

    // 문 열기 상호작용
    public void Interact()
    {
        if (isOpen)
        {
            return;
        }

        if (isLocked)
        {
            //Debug.Log("문이 잠겨있습니다. 주변의 몬스터를 모두 처치하세요.");
            return;
        }

        OpenDoor();
    }

    // 문 잠금 해제
    public void Unlock()
    {
        isLocked = false;
        //Debug.Log("문의 잠금이 해제되었습니다.");
    }

    // 문 열기
    private void OpenDoor()
    {
        isOpen = true;
        StartCoroutine(OpenRoutine());
    }

    // 문 열리는 코루틴
    private IEnumerator OpenRoutine()
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, openSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
        gameObject.SetActive(false);
    }
}
