using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour, IInteractable
{
    [Tooltip("이동할 씬의 이름 입력")]
    public string sceneName;

    public void Interact()
    {
        //Debug.Log($"[Portal] {sceneName}으로 이동합니다.");
        
        // 씬 이동 실행
        if (!string.IsNullOrEmpty(sceneName))
        {
            // 던전으로 가는 거라면(DungeonScene), 현재 위치(마을)를 저장
            if (sceneName == "DungeonScene" && GameManager.Instance != null)
            {
                // GameManager가 알고 있는 플레이어 위치 사용
                if (GameManager.Instance.currentPlayer != null)
                {
                    GameManager.Instance.SaveTownPosition(GameManager.Instance.currentPlayer.transform.position);
                }
                
                // 현재 씬 이름을 '이전 씬'으로 저장 (던전 클리어 후 복귀용)
                GameManager.Instance.SavePreviousSceneName(SceneManager.GetActiveScene().name);
            }

            SceneManager.LoadScene(sceneName);
        }
        else
        {
            //Debug.LogWarning("이동할 씬 이름이 설정되지 않았습니다.");
        }
    }
}
