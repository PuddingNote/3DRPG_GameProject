using System.Collections;
using UnityEngine;

public class DungeonDoor : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public bool isLocked = true;  // 문 잠김 여부
    public float openHeight = 4f;
    public float openSpeed = 4f;  // 문 열리는 속도

    private bool isOpen = false;  // 문 열림 여부
    private Vector3 initialPosition;  // 문 초기 위치
    private Vector3 targetPosition;  // 문 목표 위치

    private void Awake()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition + Vector3.up * openHeight;
    }

    public void Interact()
    {
        if (isOpen)
        {
            return;
        }

        if (isLocked)
        {
            Debug.Log("문이 잠겨있습니다. 주변의 몬스터를 모두 처치하세요.");
            return;
        }

        OpenDoor();
    }

    public void Unlock()
    {
        isLocked = false;
        Debug.Log("문의 잠금이 해제되었습니다.");
    }

    private void OpenDoor()
    {
        isOpen = true;
        StartCoroutine(OpenRoutine());
    }

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

