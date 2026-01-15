using UnityEngine;

/// <summary>
/// 이 컴포넌트가 활성화되어 있는 동안, GameManager에 "미니맵 숨김" 요청을 등록.
/// - 여러 UI가 동시에 켜져도(대화 + 던전입장 + 결과 UI 등) 참조카운트처럼 안전하게 동작.
/// - OnDisable/OnDestroy에서 자동 해제되어 누수로 인해 미니맵이 영구적으로 숨는 문제를 방지.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinimapVisibilityBlocker : MonoBehaviour
{
    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PushMinimapHidden(this);
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PopMinimapHidden(this);
        }
    }

    private void OnDestroy()
    {
        // Disable이 호출되지 않는 파괴 경로도 있으므로 방어적으로 한 번 더 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PopMinimapHidden(this);
        }
    }
}
