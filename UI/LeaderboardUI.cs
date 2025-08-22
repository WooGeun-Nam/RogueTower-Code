// LeaderboardUI.cs 파일을 아래 코드로 교체해주세요.
using UnityEngine;
using TMPro;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    
    [SerializeField] private GameObject loadingSpinnerObject; 
    [SerializeField] private TextMeshProUGUI statusText;

    private void OnEnable()
    {
        LeaderboardManager.OnLeaderboardLoaded += UpdateLeaderboardUI;
        RefreshLeaderboard();
    }

    private void OnDisable()
    {
        LeaderboardManager.OnLeaderboardLoaded -= UpdateLeaderboardUI;
    }

    public void RefreshLeaderboard()
    {
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }
        
        SetLoadingState(true);
        if (statusText != null) statusText.gameObject.SetActive(false);
        
        LeaderboardManager.Instance.RequestLeaderboard();
    }

    private void UpdateLeaderboardUI(List<EnrichedLeaderboardEntry> leaderboard)
    {
        SetLoadingState(false);
        
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }
        
        if (leaderboard == null)
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "순위 정보를 불러오는 데 실패했습니다.";
            }
            return;
        }

        if (leaderboard.Count == 0)
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                statusText.text = "등록된 기록이 없습니다.";
            }
            return;
        }

        foreach (var entry in leaderboard)
        {
            GameObject entryGO = Instantiate(leaderboardEntryPrefab, leaderboardContent);
            
            entryGO.transform.Find("RankText").GetComponent<TextMeshProUGUI>().text = $"{entry.baseEntry.Position + 1}.";
            entryGO.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = string.IsNullOrEmpty(entry.baseEntry.DisplayName) ? "Player" : entry.baseEntry.DisplayName;
            entryGO.transform.Find("WaveText").GetComponent<TextMeshProUGUI>().text = $"Wave {entry.wave}"; 
            entryGO.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = $"{entry.baseEntry.StatValue:N0}";
        }
    }

    public void SetLoadingState(bool isLoading)
    {
        if (loadingSpinnerObject != null)
        {
            loadingSpinnerObject.SetActive(isLoading);
        }
    }
    
    public void ClosePanel()
    {
        SoundManager.Instance.PlaySFX("SFX_UI_ButtonClick");
        gameObject.SetActive(false);
    }
}