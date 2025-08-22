using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 요소에 마우스 오버 시 툴팁을 표시하는 트리거 스크립트입니다.
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("툴팁에 표시될 제목")]
    [SerializeField] private string header;
    [Tooltip("툴팁에 표시될 내용")]
    [SerializeField] [TextArea(3, 10)] private string content;

    public System.Func<string> GetContentCallback; // 툴팁 내용을 동적으로 가져올 콜백 함수

    /// <summary>
    /// 툴팁의 제목을 설정합니다.
    /// </summary>
    /// <param name="newHeader">새로운 제목</param>
    public void SetHeader(string newHeader)
    {
        header = newHeader;
    }

    /// <summary>
    /// 툴팁의 내용을 설정합니다.
    /// </summary>
    /// <param name="newContent">새로운 내용</param>
    public void SetContent(string newContent)
    {
        content = newContent;
    }

    /// <summary>
    /// 마우스 포인터가 UI 요소에 진입했을 때 호출됩니다.
    /// </summary>
    /// <param name="eventData">이벤트 데이터</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        string currentContent = content; // 기본적으로 content 필드 사용
        if (GetContentCallback != null)
        {
            currentContent = GetContentCallback.Invoke(); // 콜백이 할당되어 있으면 콜백 호출
        }

        // 헤더나 내용이 비어있지 않을 때만 툴팁 표시
        if (!string.IsNullOrEmpty(header) || !string.IsNullOrEmpty(currentContent))
        {
            if (TooltipManager.Instance != null)
            {
                TooltipManager.Instance.ShowTooltip(header, currentContent);
            }
            else
            {
                Debug.LogWarning("[TooltipTrigger] TooltipManager.Instance is null.");
            }
        }
    }

    /// <summary>
    /// 마우스 포인터가 UI 요소에서 벗어났을 때 호출됩니다.
    /// </summary>
    /// <param name="eventData">이벤트 데이터</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
        else
        {
            Debug.LogWarning("[TooltipTrigger] TooltipManager.Instance is null.");
        }
    }
}
