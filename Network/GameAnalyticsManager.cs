// GameAnalyticsManager.cs (최종 버전)
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;

public class GameAnalyticsManager : MonoBehaviour
{
    public static GameAnalyticsManager Instance { get; private set; }

    #region 데이터 저장 필드
    private string gameMode;
    private int difficulty;
    private float mapDifficulty;
    private string clearStatus;
    private Dictionary<string, float> scoreDetails = new Dictionary<string, float>();
    private float finalScore;
    private int finalWave;
    private float totalGoldSpent;
    private float totalTowerCostSpent;
    private Dictionary<string, int> randomRewardChoices = new Dictionary<string, int>();
    private int physicalUpgradeLevel;
    private int magicalUpgradeLevel;
    private float totalDamageTaken;
    private float totalGoldAcquired;
    private float sessionStartTime;
    private Dictionary<string, float> towerDamageDealt = new Dictionary<string, float>();
    private Dictionary<string, List<float>> towerPenetrationSamples = new Dictionary<string, List<float>>(); // 관통력 비율 샘플을 저장할 Dictionary 
    private Dictionary<string, int> towerBuildCount = new Dictionary<string, int>();
    private Dictionary<string, int> towerSellCount = new Dictionary<string, int>();
    private Dictionary<string, float> towerFinalDps = new Dictionary<string, float>();
    private Dictionary<string, int> skillUsageCount = new Dictionary<string, int>();
    private Dictionary<string, float> skillDamageDealt = new Dictionary<string, float>();
    private List<object> finalWaveEnemyInfo = new List<object>();
    private List<string> equippedItems = new List<string>();
    private Dictionary<string, object> equippedItemFinalStats; 
    #endregion

    public string GetGameMode() { return gameMode; }
    public float GetSessionStartTime() { return sessionStartTime; }
    public IReadOnlyDictionary<string, float> GetTowerDamageDealt() { return towerDamageDealt; }
    
    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void StartNewSession()
    {
        // 모든 데이터 초기화
        scoreDetails.Clear();
        randomRewardChoices.Clear();
        towerDamageDealt.Clear();
        towerPenetrationSamples.Clear();
        towerBuildCount.Clear();
        towerSellCount.Clear();
        towerFinalDps.Clear();
        skillUsageCount.Clear();
        skillDamageDealt.Clear();
        finalWaveEnemyInfo.Clear();
        equippedItems = new List<string>();
        equippedItemFinalStats = new Dictionary<string, object>();
        totalGoldSpent = 0f;
        totalTowerCostSpent = 0f;
        physicalUpgradeLevel = 0;
        magicalUpgradeLevel = 0;
        totalDamageTaken = 0f;
        totalGoldAcquired = 0f;
        sessionStartTime = Time.time;
        // Debug.Log("[Analytics] 새 세션을 시작합니다.");
    }
    
    #region 데이터 수집 API
    public void SetInitialGameInfo(string mode, int diff, float mapDiff) { gameMode = mode; difficulty = diff; mapDifficulty = mapDiff; }
    public void RecordScoreDetail(string category, float value) { scoreDetails[category] = value; }
    public void RecordGoldAcquired(float amount) { totalGoldAcquired += amount; }
    public void RecordGoldSpent(float amount) { totalGoldSpent += amount; }
    public void RecordTowerCostSpent(float cost) { totalTowerCostSpent += cost; }
    public void RecordRandomRewardChoice(string choice) { if (!randomRewardChoices.ContainsKey(choice)) randomRewardChoices[choice] = 0; randomRewardChoices[choice]++; }
    public void RecordDamageTaken(float amount) { totalDamageTaken += amount; }
    public void RecordTowerDamage(string towerType, float damage) { if (!towerDamageDealt.ContainsKey(towerType)) towerDamageDealt[towerType] = 0; towerDamageDealt[towerType] += damage; }
    public void RecordTowerPenetration(string towerType, float penetrationRate)
    {
        if (!towerPenetrationSamples.ContainsKey(towerType))
        {
            towerPenetrationSamples[towerType] = new List<float>();
        }
        towerPenetrationSamples[towerType].Add(penetrationRate);
    }
    public void RecordTowerBuild(string towerType) { if (!towerBuildCount.ContainsKey(towerType)) towerBuildCount[towerType] = 0; towerBuildCount[towerType]++; }
    public void RecordTowerSell(string towerType) { if (!towerSellCount.ContainsKey(towerType)) towerSellCount[towerType] = 0; towerSellCount[towerType]++; }
    public void RecordSkillUsage(string skillName) { if (!skillUsageCount.ContainsKey(skillName)) skillUsageCount[skillName] = 0; skillUsageCount[skillName]++; }
    public void RecordSkillDamage(string skillName, float damage) { if (!skillDamageDealt.ContainsKey(skillName)) skillDamageDealt[skillName] = 0; skillDamageDealt[skillName] += damage; }
    #endregion

    #region 최종 데이터 설정 API
    public void SetFinalUpgradeLevels(int physical, int magical) { physicalUpgradeLevel = physical; magicalUpgradeLevel = magical; }
    public void SetFinalTowerDps(Dictionary<string, float> dpsDict) { towerFinalDps = new Dictionary<string, float>(dpsDict); }
    public void SetFinalEquipment(List<string> items, Dictionary<string, object> stats)
    {
        equippedItems = new List<string>(items);
        equippedItemFinalStats = new Dictionary<string, object>(stats);
    }
    public void SetFinalWaveEnemies(List<object> enemies) { finalWaveEnemyInfo = new List<object>(enemies); }
    #endregion
    
    public void FinalizeAndSendEvent(string finalClearStatus, int finalW, float finalS)
    {
        if (PlayerPrefs.GetInt("DataConsent", 0) != 1)
        {
            // Debug.Log("[Analytics] 데이터 수집에 동의하지 않아 전송을 건너뜁니다.");
            return;
        }
        
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        clearStatus = finalClearStatus;
        finalWave = finalW;
        finalScore = finalS;
        float playTime = Time.time - sessionStartTime;

        Dictionary<string, float> towerAveragePenetration = new Dictionary<string, float>();
        foreach (var entry in towerPenetrationSamples)
        {
            if (entry.Value.Count > 0)
            {
                towerAveragePenetration[entry.Key] = entry.Value.Average();
            }
        }
        
        var request = new WriteClientPlayerEventRequest
        {
            EventName = "session_summary",
            Body = new Dictionary<string, object>
            {
                { "gameInfo", new { gameMode, difficulty, mapDifficulty, isClear = clearStatus, playTime, finalWave }},
                { "scoreInfo", new { scoreDetails, finalScore }},
                { "economyInfo", new { goldSpent = totalGoldSpent, goldAcquired = totalGoldAcquired, towerCostSpent = totalTowerCostSpent }},
                { "playerStats", new { damageTaken = totalDamageTaken, physicalUpgrade = physicalUpgradeLevel, magicalUpgrade = magicalUpgradeLevel }},
                { "towerStats", new
                {
                    damageDealt = towerDamageDealt,
                    penetrationRate = towerAveragePenetration,
                    buildCount = towerBuildCount,
                    sellCount = towerSellCount, 
                    finalDps = towerFinalDps
                }},
                { "skillStats", new { usageCount = skillUsageCount, damageDealt = skillDamageDealt }},
                { "enemyInfo", new { finalWaveEnemies = finalWaveEnemyInfo }},
                { "equipmentStats", new { equippedItems, finalStats = equippedItemFinalStats }},
                { "choiceInfo", new { randomRewardSelection = randomRewardChoices }},
                { "misc", new { gameVersion = Application.version }}
            }
        };
        
        PlayFabClientAPI.WritePlayerEvent(request, 
            result => Debug.Log("<color=cyan>[PlayFab] 분석 데이터 전송 성공!</color>"), 
            error => Debug.LogError("[PlayFab] 분석 데이터 전송 실패: " + error.GenerateErrorReport())
        );
    }
}