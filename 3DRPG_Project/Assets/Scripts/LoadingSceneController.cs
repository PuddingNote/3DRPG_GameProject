using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image loadingCircle; // Filled Type Image

    private static string nextSceneName;

    // 설정값
    private const float MIN_LOADING_TIME = 3.0f; // 최소 로딩 시간 (3초)

    public static void LoadScene(string sceneName)
    {
        // GameManager가 있고 패널이 설정되어 있다면 연출 사용
        if (GameManager.Instance != null && GameManager.Instance.transitionPanel != null)
        {
            GameManager.Instance.LoadSceneWithTransition(sceneName);
        }
        else
        {
            LoadSceneDirectly(sceneName);
        }
    }

    // GameManager에서 호출하는 실제 로딩 시작 함수
    public static void LoadSceneDirectly(string sceneName)
    {
        nextSceneName = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }

    private void Start()
    {
        StartCoroutine(LoadSceneProcess());
    }

    // 씬 로딩 처리
    private IEnumerator LoadSceneProcess()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false; // 씬 자동 전환 방지

        float timer = 0f;
        loadingCircle.fillAmount = 0f;

        // 1단계: 0% ~ 75% 구간
        // 목표: 3초의 75%(2.25초) 동안 0.75까지 채우기.
        // 단, 실제 로딩(op.progress)이 0.9 미만이면 0.75에서 멈춰야 함.
        while (loadingCircle.fillAmount < 0.75f)
        {
            timer += Time.deltaTime;

            // 시간 기반 목표 진행률 (최대 0.75)
            float targetProgress = Mathf.Clamp(timer / MIN_LOADING_TIME, 0f, 0.75f);

            // Lerp로 부드럽게 이동
            loadingCircle.fillAmount = Mathf.Lerp(loadingCircle.fillAmount, targetProgress, Time.deltaTime * 5f);

            // 탈출 조건:
            // 1. 시간이 충분히 지났고 (2.25초 이상)
            // 2. 실제 로딩도 끝났고 (0.9 이상)
            // 3. 시각적으로도 75% 근처에 도달했을 때
            if (timer >= (MIN_LOADING_TIME * 0.75f) && op.progress >= 0.9f && loadingCircle.fillAmount >= 0.74f)
            {
                break;
            }

            yield return null;
        }

        // 2단계: 75% ~ 100% 구간 (마무리)
        // 남은 시간(약 0.75초) 동안 100%까지 채우기
        float remainTime = MIN_LOADING_TIME * 0.25f; 
        float startFill = loadingCircle.fillAmount;
        float endTimer = 0f;

        while (endTimer < remainTime)
        {
            endTimer += Time.deltaTime;
            loadingCircle.fillAmount = Mathf.Lerp(startFill, 1f, endTimer / remainTime);
            yield return null;
        }

        loadingCircle.fillAmount = 1f;
        
        // 100% 상태 잠시 보여주기
        yield return new WaitForSeconds(0.1f);

        op.allowSceneActivation = true;
    }
}
