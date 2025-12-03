using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour, IInteractable
{
    [Tooltip("이동할 씬의 이름 입력")]
    public string sceneName;

    public void Interact()
    {
        Debug.Log($"[Portal] {sceneName}으로 이동합니다.");
        
        // 씬 이동 실행
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("이동할 씬 이름이 설정되지 않았습니다.");
        }
    }
}
