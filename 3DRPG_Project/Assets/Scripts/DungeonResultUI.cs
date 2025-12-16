using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DungeonResultUI : MonoBehaviour
{
    [Header("UI Components")]
    public Text touchText;         // "화면을 터치해..." 텍스트
    public Button screenButton;    // 화면 전체를 덮는 투명 버튼 (터치 감지용)

    private string nextSceneName;  // 이동할 씬 이름
    private bool isInputEnabled = false; // 입력 가능 여부

    // 결과창 설정
    public void Setup(string sceneName)
    {
        nextSceneName = sceneName;
        isInputEnabled = false;

        // 텍스트 깜빡임 효과 시작
        if (touchText != null)
        {
            StartCoroutine(BlinkTextRoutine());
        }

        // 버튼 이벤트 연결
        if (screenButton != null)
        {
            screenButton.onClick.RemoveAllListeners();
            screenButton.onClick.AddListener(OnScreenTouch);
        }

        StartCoroutine(EnableInputRoutine());
    }

    // 텍스트 깜빡임 효과 처리
    private IEnumerator BlinkTextRoutine()
    {
        while (true)
        {
            float alpha = (Mathf.Sin(Time.time * 3.0f) + 1.0f) / 2.0f; // 0~1 사이 반복
            if (touchText != null)
            {
                Color color = touchText.color;
                color.a = alpha;
                touchText.color = color;
            }
            yield return null;
        }
    }

    // UI가 켜지자마자 실수로 눌리는 것 방지용 (1초 딜레이)
    private IEnumerator EnableInputRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        isInputEnabled = true;
    }

    // 화면 터치 이벤트 처리
    private void OnScreenTouch()
    {
        if (!isInputEnabled) 
        {
            return;
        }

        // 마을로 이동
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            LoadingSceneController.LoadScene(nextSceneName);
        }
        //else
        //{
        //    Debug.Log("이동할 씬 이름이 설정되지 않았습니다.");
        //}
    }
}
