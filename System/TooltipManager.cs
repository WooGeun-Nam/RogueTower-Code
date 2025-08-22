using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // SceneManagement 네임스페이스 추가

/// <summary>
/// 툴팁 UI를 관리하는 싱글톤 클래스입니다.
/// </summary>
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("툴팁 패널 프리팹")] 
    [SerializeField] private GameObject tooltipPanelPrefab;
    private GameObject tooltipPanelInstance; // 인스턴스화된 툴팁 패널 
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI contentText;
    
    [Tooltip("화면 좌우 가장자리와 툴팁 사이의 최소 여유 공간(픽셀)")]
    public float horizontalPadding = 10f;
    
    [Tooltip("마우스 포인터와 툴팁 사이의 수직 간격")]
    public float verticalOffset = 10f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded; 
        // 현재 씬이 로드된 상태라면 즉시 툴팁 패널을 설정
        // (Awake/Start보다 OnEnable이 먼저 호출될 수 있으므로)
        if (gameObject.scene.isLoaded)
        {
            OnSceneLoaded(gameObject.scene, LoadSceneMode.Single);
        }
    }

    private void OnDisable()
    {
        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded; 
        // 씬이 언로드될 때 현재 툴팁 인스턴스 파괴
        if (tooltipPanelInstance != null) 
        {
            Destroy(tooltipPanelInstance);
            tooltipPanelInstance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 이전 씬의 툴팁 인스턴스가 남아있다면 파괴
        if (tooltipPanelInstance != null)
        {
            Destroy(tooltipPanelInstance);
            tooltipPanelInstance = null;
        }

        // 현재 씬의 Canvas를 찾아서 툴팁 패널을 그 자식으로 인스턴스화
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[TooltipManager] No Canvas found in the current scene. Tooltip will not be displayed.");
            return;
        }

        tooltipPanelInstance = Instantiate(tooltipPanelPrefab, canvas.transform); // Canvas의 자식으로 인스턴스화
        // CanvasGroup 컴포넌트 추가 및 Raycast 비활성화
        CanvasGroup canvasGroup = tooltipPanelInstance.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        tooltipPanelInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // 중앙으로 초기화
        tooltipPanelInstance.name = "TooltipPanel_RuntimeInstance"; // 디버깅을 위해 이름 지정

        // 인스턴스화된 툴팁 패널의 자식에서 TextMeshProUGUI 컴포넌트 찾기
        headerText = tooltipPanelInstance.transform.Find("TooltipHeader").GetComponent<TextMeshProUGUI>();
        contentText = tooltipPanelInstance.transform.Find("TooltipContent").GetComponent<TextMeshProUGUI>();

        if (headerText == null) Debug.LogError("[TooltipManager] headerText not found! Make sure 'TooltipHeader' is a child of tooltipPanelPrefab.");
        if (contentText == null) Debug.LogError("[TooltipManager] contentText not found! Make sure 'TooltipContent' is a child of tooltipPanelPrefab.");

        HideTooltip(); // 시작 시 툴팁 숨기기
    }

    private void Update()
    {
        // Update 메서드에서는 툴팁 위치를 지속적으로 업데이트하지 않습니다.
        // 위치 설정은 ShowTooltip에서 한 번만 이루어집니다.
    }

    /// <summary>
    /// 툴팁을 표시하고 내용을 설정합니다。
    /// </summary>
    /// <param name="header">툴팁 제목</param>
    /// <param name="content">툴팁 내용</param>
    // [수정 1] 클래스 상단에 패딩 값을 조절할 수 있는 public 변수 추가
    // 이렇게 하면 유니티 인스펙터 창에서 값을 쉽게 바꿀 수 있습니다.
    public void ShowTooltip(string header, string content)
    {
        if (tooltipPanelInstance == null) return;
        if (tooltipPanelInstance.activeSelf && headerText.text == header && contentText.text == content) return;

        StopAllCoroutines();
        StartCoroutine(ShowTooltipRoutine(header, content));
    }

    private IEnumerator ShowTooltipRoutine(string header, string content)
    {
        // 1. 초기 설정 (이전과 동일)
        tooltipPanelInstance.SetActive(true);
        headerText.text = header;
        contentText.text = content;

        // 2. 캔버스 동적 탐색 및 크기 강제 업데이트 (이전과 동일)
        Canvas rootCanvas = tooltipPanelInstance.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogError("툴팁이 어떤 Canvas에도 속해있지 않습니다!");
            yield break;
        }
        Canvas.ForceUpdateCanvases();
        yield return null;

        // 3. 툴팁의 실제 픽셀 크기 계산 (이전과 동일)
        RectTransform panelRect = tooltipPanelInstance.GetComponent<RectTransform>();
        Vector2 panelPixelSize = new Vector2(
            panelRect.sizeDelta.x * rootCanvas.scaleFactor,
            panelRect.sizeDelta.y * rootCanvas.scaleFactor
        );

        Vector2 mousePos = Input.mousePosition;
        Vector2 desiredPosition;

        // 4. [핵심 로직 변경] 수직 위치 동적 결정
        // 마우스 커서 아래쪽의 사용 가능한 공간이, 툴팁의 실제 높이 + 약간의 간격보다 작은지 확인합니다.
        float requiredVerticalSpace = panelPixelSize.y + verticalOffset;

        if (mousePos.y < requiredVerticalSpace)
        {
            // 공간이 부족하면 툴팁을 마우스 위로 올립니다.
            desiredPosition = new Vector2(mousePos.x, mousePos.y + verticalOffset);
            panelRect.pivot = new Vector2(0.5f, 0f); // Pivot을 하단으로 변경
        }
        else
        {
            // 공간이 충분하면 기본값인 마우스 아래에 배치합니다.
            desiredPosition = new Vector2(mousePos.x, mousePos.y - verticalOffset);
            panelRect.pivot = new Vector2(0.5f, 1f); // Pivot을 상단으로 변경
        }

        // 5. 수평 위치 결정 (좌우 경계는 고정된 패딩 값 사용)
        float panelLeftEdge = desiredPosition.x - panelPixelSize.x * panelRect.pivot.x;
        if (panelLeftEdge < horizontalPadding)
        {
            desiredPosition.x = horizontalPadding + panelPixelSize.x * panelRect.pivot.x;
        }

        float panelRightEdge = desiredPosition.x + panelPixelSize.x * (1 - panelRect.pivot.x);
        if (panelRightEdge > Screen.width - horizontalPadding)
        {
            desiredPosition.x = Screen.width - horizontalPadding - panelPixelSize.x * (1 - panelRect.pivot.x);
        }

        // 6. 최종 위치 적용
        panelRect.position = desiredPosition;
    }
    
    /// <summary>
    /// 툴팁을 숨깁니다.
    /// </summary>
    public void HideTooltip()
    {
        StopAllCoroutines();
        
        if (tooltipPanelInstance == null) return; // 인스턴스가 없으면 숨기지 않음
        if (!tooltipPanelInstance.activeSelf) return; // 이미 비활성화되어 있으면 중복 호출 방지

        tooltipPanelInstance.SetActive(false);
    }
}