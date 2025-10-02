using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 게임 내 UI 패널의 표시, 전환, 애니메이션을 관리하는 클래스
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("타워 선택 패널의 RectTransform")]
    public RectTransform TowerPanel;
    [Tooltip("스킬 선택 패널의 RectTransform")]
    public RectTransform SkillPanel;
    [Tooltip("설정 패널 GameObject")]
    public GameObject panelMenu;
    [Tooltip("메뉴 버튼")]
    public Button buttonMenu;

    public Sprite buttonSpriteOpen;
    public Sprite buttonSpriteClose;

    public GameObject scorePanel;
    public GameObject timePanel;
    
    [Tooltip("게임 매니저 참조")]
    public GameManager gameManager; // GameManager 참조 추가

    private bool switchState = false; // 현재 활성화된 패널 상태 (false: TowerPanel, true: SkillPanel)
    private bool isSwitching = false; // 패널 전환 애니메이션 진행 중 여부
    private bool isPanelMenu = false; // 설정 패널 활성화 여부
    private bool isUIVisible = true; // 하단 UI 그룹의 가시성 여부
    private bool isAnimating = false; // UI 표시/숨김 애니메이션 진행 중 여부
    
    [Header("Bottom UI Elements")]
    [Tooltip("하단 UI 그룹의 부모 GameObject")]
    public GameObject bottomUIGroup;
    [Tooltip("하단 UI 그룹 내의 모든 패널 RectTransform 배열")]
    public RectTransform[] bottomUIPanels;
    
    // 새로 추가할 변수
    [Header("Animation Settings")]
    [Range(0.1f, 2f)]
    [Tooltip("UI 애니메이션의 총 지속 시간 (초)")]
    public float animationDuration = 0.15f; // 0.15초 동안 애니메이션이 재생되도록 설정

    private Vector2[] originalPositions;  // 각 하단 UI 패널의 초기 위치 저장
    private Vector2 towerPanelOriginalPos;  // TowerPanel의 초기 위치
    private Vector2 skillPanelOriginalPos;  // SkillPanel의 초기 위치
    private float hideOffset = -350f;     // UI를 숨길 때 이동할 Y축 거리
    
    private void Start()
    {
        // 모든 하단 UI 패널의 초기 위치 저장
        originalPositions = new Vector2[bottomUIPanels.Length];
        for(int i = 0; i < bottomUIPanels.Length; i++)
        {
            originalPositions[i] = bottomUIPanels[i].anchoredPosition;
        }

        // 타워/스킬 패널의 초기 위치도 따로 저장
        towerPanelOriginalPos = TowerPanel.anchoredPosition;
        skillPanelOriginalPos = SkillPanel.anchoredPosition;
        
        // 게임 시작 시 스킬 패널을 아래로 숨김
        SkillPanel.anchoredPosition += new Vector2(0, hideOffset);
        
        // 초기 상태 설정
        switchState = false;
    }

    private void Update()
    {
        // 숫자 키 (1-6)를 이용한 타워/스킬 사용
        if (Input.GetKeyDown(KeyCode.Alpha1)) HandleArchetypeInput(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) HandleArchetypeInput(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) HandleArchetypeInput(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) HandleArchetypeInput(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) HandleArchetypeInput(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) HandleArchetypeInput(5);

        // 'T' 키로 타워 패널 전환
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SwitchTowerPanel();
        }

        // 'S' 키로 스킬 패널 전환
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            SwitchSkillPanel();
        }

        // 'Tab' 키로 하단 UI 그룹 토글 (표시/숨김)
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleUI();
        }

        // 'Q' 키로 게임속도 단축키
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            GameManager.Instance.SpeedUp();
        }
        
        // 'Escape' 키 처리
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 설정 패널이 열려있으면 닫기
            if(isPanelMenu)
            {
                ButtonMenu();
            }
            // 하단 UI가 숨겨져 있으면 다시 표시
            else if(!isUIVisible)  
            {
                ToggleUI();
            }
        }
    }

    private void HandleArchetypeInput(int index)
    {
        var buttonList = !switchState ? gameManager.TowerButtons : gameManager.SkillButtons;

        if (buttonList != null && index < buttonList.Count)
        {
            Button targetButton = buttonList[index];
            if (targetButton != null && targetButton.interactable)
            {
                targetButton.onClick.Invoke();
            }
        }
    }

    /// <summary>
    /// 타워 패널로 전환합니다.
    /// </summary>
    public void SwitchTowerPanel()
    {
        // 이미 타워 패널이 활성화되어 있거나 전환 애니메이션 중이면 무시
        if (!switchState || isSwitching)
        {
            return;
        }
        switchState = false; // 타워 패널 상태로 설정
        StartCoroutine(SwitchPanel(TowerPanel, SkillPanel)); // 패널 전환 애니메이션 시작
    }

    /// <summary>
    /// 스킬 패널로 전환합니다.
    /// </summary>
    public void SwitchSkillPanel()
    {
        // 이미 스킬 패널이 활성화되어 있거나 전환 애니메이션 중이면 무시
        if (switchState || isSwitching)
        {
            return;
        }
        switchState = true; // 스킬 패널 상태로 설정
        StartCoroutine(SwitchPanel(SkillPanel,TowerPanel)); // 패널 전환 애니메이션 시작
    }

    /// <summary>
    /// 설정 패널을 토글합니다. (열기/닫기)
    /// </summary>
    public void ButtonMenu()
    {
        isPanelMenu = !isPanelMenu;
        panelMenu.SetActive(isPanelMenu);

        if (isPanelMenu)
        {
            buttonMenu.GetComponent<Image>().sprite = buttonSpriteClose;
        }
        else
        {
            buttonMenu.GetComponent<Image>().sprite = buttonSpriteOpen;
        }
        
        SoundManager.Instance.PlaySFX("SFX_UI_PanelOpen"); // UI 버튼 클릭 사운드 재생
    }

    public void ButtonHome()
    {
        SoundManager.Instance.PlaySFX("SFX_UI_ButtonClick"); // UI 버튼 클릭 사운드 재생
        SceneManager.LoadScene("Lobby");
    }
    
    public void ButtonWaitingRoom()
    {
        SoundManager.Instance.PlaySFX("SFX_UI_ButtonClick"); // UI 버튼 클릭 사운드 재생
        SceneManager.LoadScene("WaitingRoom");
    }
    
    /// <summary>
    /// 현재 씬을 다시 로드하여 게임을 재시작합니다.
    /// </summary>
    public void ButtonRetry()
    {
        SoundManager.Instance.PlaySFX("SFX_UI_ButtonClick"); // UI 버튼 클릭 사운드 재생
        LoadingSceneController.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// 하단 UI 그룹의 가시성을 토글합니다. (표시/숨김)
    /// </summary>
    private void ToggleUI()
    {
        // 애니메이션 진행 중이면 무시
        if(isAnimating || isSwitching) return;

        isUIVisible = !isUIVisible;
        
        if(isUIVisible)
        {
            bottomUIGroup.SetActive(true); // UI 그룹 활성화
            StartCoroutine(ShowUIAnimation()); // UI 표시 애니메이션 시작
        }
        else
        {
            StartCoroutine(HideUIAnimation()); // UI 숨김 애니메이션 시작
        }
        
        SoundManager.Instance.PlaySFX("SFX_UI_Switching");
    }
    
   /// <summary>
   /// 하단 UI 그룹을 부드럽게 표시하는 애니메이션 코루틴 (수정된 버전)
   /// </summary>
   private IEnumerator ShowUIAnimation()
   { 
       isAnimating = true;
       bottomUIGroup.SetActive(true);
       
       float elapsedTime = 0f;

       // 애니메이션 시작 시점의 위치들을 저장
       Vector2[] startPositions = new Vector2[bottomUIPanels.Length];
       for(int i = 0; i < bottomUIPanels.Length; i++)
       {
           startPositions[i] = bottomUIPanels[i].anchoredPosition;
       }
       
       // switchState를 기반으로 각 패널의 최종 목표 위치를 동적으로 계산합니다.
       Vector2[] targetPositions = new Vector2[bottomUIPanels.Length];
       for(int i=0; i < bottomUIPanels.Length; i++)
       {
           // 기본적으로는 원래 저장된 위치를 목표로 설정
           targetPositions[i] = originalPositions[i]; 

           // 만약 현재 패널이 TowerPanel이라면
           if (bottomUIPanels[i] == TowerPanel)
           {
               // switchState가 false(타워 활성)일 때만 원래 위치로, 아니면 숨김 위치로 설정
               targetPositions[i] = !switchState ? towerPanelOriginalPos : towerPanelOriginalPos + new Vector2(0, hideOffset);
           }
           // 만약 현재 패널이 SkillPanel이라면
           else if (bottomUIPanels[i] == SkillPanel)
           {
               // switchState가 true(스킬 활성)일 때만 원래 위치로, 아니면 숨김 위치로 설정
               targetPositions[i] = switchState ? skillPanelOriginalPos : skillPanelOriginalPos + new Vector2(0, hideOffset);
           }
       }
       
       while(elapsedTime < animationDuration)
       {
           elapsedTime += Time.deltaTime;
           float progress = Mathf.Clamp01(elapsedTime / animationDuration);

           // 모든 패널을 동시에 계산된 목표 위치로 움직임
           for(int i = 0; i < bottomUIPanels.Length; i++)
           {
               bottomUIPanels[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], progress);
           }
           yield return null;
       }
       
       // 애니메이션 종료 후 정확한 위치로 고정
       for(int i = 0; i < bottomUIPanels.Length; i++)
       {
           bottomUIPanels[i].anchoredPosition = targetPositions[i];
       }

       if (scorePanel != null)
       {
           scorePanel.SetActive(true);
       }

       if (timePanel != null && GameModeManager.CurrentMode == GameMode.Infinite)
       {
           timePanel.SetActive(true);
       }
       
       isAnimating = false;
   }

   /// <summary>
   /// 하단 UI 그룹을 부드럽게 숨기는 애니메이션 코루틴 (프레임률 독립적으로 수정)
   /// </summary>
   private IEnumerator HideUIAnimation()
   {
       isAnimating = true;
       
       float elapsedTime = 0f;

       // 시작 위치와 목표 위치를 미리 계산
       Vector2[] startPositions = new Vector2[bottomUIPanels.Length];
       Vector2[] targetPositions = new Vector2[bottomUIPanels.Length];
       for(int i = 0; i < bottomUIPanels.Length; i++)
       {
           startPositions[i] = bottomUIPanels[i].anchoredPosition;
           targetPositions[i] = originalPositions[i] + new Vector2(0, hideOffset);
       }

       while(elapsedTime < animationDuration)
       {
           elapsedTime += Time.deltaTime;
           float progress = Mathf.Clamp01(elapsedTime / animationDuration);

           // 모든 패널을 동시에 움직임
           for(int i = 0; i < bottomUIPanels.Length; i++)
           {
               bottomUIPanels[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], progress);
           }
           yield return null;
       }
       
       // 애니메이션 종료 후 정확한 위치로 고정하고 그룹을 비활성화
       for(int i = 0; i < bottomUIPanels.Length; i++)
       {
            bottomUIPanels[i].anchoredPosition = targetPositions[i];
       }
       bottomUIGroup.SetActive(false);

       if (scorePanel != null)
       {
           scorePanel.SetActive(false);
       }

       if (timePanel != null && GameModeManager.CurrentMode == GameMode.Infinite)
       {
           timePanel.SetActive(false);
       }
       
       isAnimating = false;
   }

   /// <summary>
   /// 두 UI 패널을 서로 전환하는 애니메이션 코루틴 (프레임률 독립적으로 수정)
   /// </summary>
   /// <param name="UpPanel">위로 올라올 패널</param>
   /// <param name="DownPanel">아래로 내려갈 패널</param>
   private IEnumerator SwitchPanel(RectTransform UpPanel, RectTransform DownPanel)
   {
       // UI가 숨겨져 있거나 다른 애니메이션 중이면 전환하지 않음
       if(!isUIVisible || isAnimating) yield break;

       isSwitching = true; // 전환 애니메이션 시작
       
       float elapsedTime = 0f; // 애니메이션 경과 시간 측정용 변수

       // 각 패널의 시작 위치와 목표 위치를 미리 계산
       Vector2 upStartPos = UpPanel.anchoredPosition;
       Vector2 downStartPos = DownPanel.anchoredPosition;
       Vector2 upTargetPos = (UpPanel == TowerPanel) ? towerPanelOriginalPos : skillPanelOriginalPos;
       Vector2 downTargetPos = (DownPanel == TowerPanel) ? towerPanelOriginalPos + new Vector2(0, hideOffset) 
           : skillPanelOriginalPos + new Vector2(0, hideOffset);
       
       SoundManager.Instance.PlaySFX("SFX_UI_Switching");
       
       // 경과 시간이 설정한 애니메이션 지속 시간보다 작을 동안 반복
       while(elapsedTime < animationDuration)
       {
           // 매 프레임의 실제 시간만큼 경과 시간을 더해줌
           elapsedTime += Time.deltaTime;
           
           // 애니메이션 진행률 (0.0 ~ 1.0) 계산
           float progress = Mathf.Clamp01(elapsedTime / animationDuration);

           // 진행률에 따라 시작 위치에서 목표 위치로의 중간 지점을 계산하여 적용
           UpPanel.anchoredPosition = Vector2.Lerp(upStartPos, upTargetPos, progress);
           DownPanel.anchoredPosition = Vector2.Lerp(downStartPos, downTargetPos, progress);

           // 다음 프레임까지 대기
           yield return null; 
       }

       // 애니메이션 종료 후, 정확한 최종 위치로 설정
       UpPanel.anchoredPosition = upTargetPos;
       DownPanel.anchoredPosition = downTargetPos;
       
       isSwitching = false; // 전환 애니메이션 종료
   }
}
