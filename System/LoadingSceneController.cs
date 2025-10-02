using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 로딩 씬을 관리하고 비동기적으로 씬을 로드하는 클래스
/// </summary>
public class LoadingSceneController : MonoBehaviour
{
    private static string nextScene; // 다음으로 로드할 씬의 이름

    [Tooltip("로딩 진행률을 표시하는 슬라이더 UI")]
    public Slider progressBar;
    [Tooltip("로딩 상태 텍스트를 표시하는 UI")]
    public TextMeshProUGUI loadText;
    
    private void Start()
    {
        // ProgressBar의 값을 항상 0으로 초기화합니다.
        progressBar.value = 0f;
        
        // 모든 사운드 중지
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllSounds();
        }

        StartCoroutine(LoadSceneWithCleanup());
    }

    /// <summary>
    /// 지정된 씬으로 로딩 씬을 통해 전환합니다.
    /// </summary>
    /// <param name="sceneName">로드할 씬의 이름</param>
    public static void LoadScene(string sceneName)
    {
        nextScene = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }

    /// <summary>
    /// [수정] 리소스 정리와 씬 로딩을 함께 처리하는 최종 코루틴입니다.
    /// </summary>
    private IEnumerator LoadSceneWithCleanup()
    {
        yield return null; // 한 프레임 대기하여 UI가 제대로 표시될 시간을 줌

        // --- 1단계: 리소스 정리 (프로그레스 바 0% -> 50%) ---
        if (loadText != null) loadText.text = "Cleaning up resources...";
        
        // 사용되지 않는 에셋을 메모리에서 강제로 해제합니다.
        AsyncOperation unloadOperation = Resources.UnloadUnusedAssets();
        while (!unloadOperation.isDone)
        {
            // 정리 과정 동안 프로그레스 바를 0%에서 50%까지 채웁니다.
            if (progressBar != null) progressBar.value = unloadOperation.progress * 0.5f;
            if (loadText != null) loadText.text = $"Cleaning up... {progressBar.value * 100:F0}%";
            yield return null;
        }

        // 가비지 컬렉터를 수동으로 호출하여 메모리를 정리합니다.
        System.GC.Collect();
        // Debug.Log("리소스 정리 및 가비지 컬렉션 완료.");

        // --- 2단계: 다음 씬 비동기 로드 (프로그레스 바 50% -> 100%) ---
        if (loadText != null) loadText.text = "Loading scene...";
        
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene);
        operation.allowSceneActivation = false;

        float timer = 0f;
        while (!operation.isDone)
        {
            yield return null;

            if (operation.progress < 0.9f)
            {
                // 씬 로딩 과정을 프로그레스 바의 50%에서 95%까지 채웁니다. (0.5f + progress * 0.45f)
                progressBar.value = 0.5f + (operation.progress * 0.45f);
                if (loadText != null) loadText.text = $"Loading... {progressBar.value * 100:F0}%";
            }
            else
            {
                // 씬 로딩이 거의 끝나면 95%에서 100%까지 부드럽게 채웁니다.
                timer += Time.unscaledDeltaTime;
                progressBar.value = Mathf.Lerp(0.95f, 1f, timer);
                if (loadText != null) loadText.text = $"Loading... {progressBar.value * 100:F0}%";

                if (progressBar.value >= 1f)
                {
                    operation.allowSceneActivation = true; // 씬 활성화
                    yield break;
                }
            }
        }
    }
}
