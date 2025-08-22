using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 웨이브 시스템을 관리하는 클래스
/// </summary>
public class WaveSystem : MonoBehaviour
{
    public static event System.Action OnRewardPanelActivated; // 보상 패널 활성화 이벤트
    
    [Header("Game Manager")][Tooltip("게임 매니저 참조")]
    [SerializeField] private GameManager gameManager;
    
    // [수정] 3개의 개별 프리팹 대신, 모든 적 프리팹을 담는 배열을 사용합니다.
    [Header("Enemy Prefabs")]
    [Tooltip("등장할 수 있는 모든 적 프리팹 배열 (최소 3개 이상)")]
    public GameObject[] allEnemyPrefabs;
    
    // [추가] 이번 게임에서 사용될 적 프리팹을 저장할 내부 변수
    private GameObject currentDefaultEnemy;
    private GameObject currentMiddleEnemy;
    private GameObject currentBossEnemy;

    [Header("UI Panels")][Tooltip("보상 패널")]
    public GameObject panelReward;
    [Tooltip("스테이지 종료 패널")]
    public GameObject stageFinish;

    [Header("Player Components")][Tooltip("플레이어 SP 컴포넌트")]
    public PlayerSP playerSP;
    [Tooltip("플레이어 HP 컴포넌트")]
    public PlayerHP playerHP;
    [Tooltip("플레이어 타워 코스트 컴포넌트")]
    public PlayerTowerCost playerTowerCost; // PlayerTowerCost 참조 추가
    [Tooltip("플레이어 골드 컴포넌트")]
    public PlayerGold playerGold; // PlayerGold 참조 추가

    [Header("System References")][Tooltip("적 스포너 참조")]
    public EnemySpawner enemySpawner;
    [Tooltip("스킬 매니저 참조")]
    public SkillManager skillManager;

    // [추가] 런타임에 생성된 적 variant를 관리하여 메모리 누수를 방지하기 위한 리스트
    private List<GameObject> runtimeEnemyVariants = new List<GameObject>();
    
    private readonly int maxWave = 30; // 최대 웨이브 수
    private Wave[] waves;              // 모든 웨이브 정보
    private int currentWaveIndex = -1; // 현재 웨이브 인덱스
    private bool isWaveInProgress = false; // 웨이브 진행중 여부 플래그
    public bool isWaitingForRewardSelection = false; // 보상 선택 대기중 여부 플래그

    // 프로퍼티
    public int MaxWave => maxWave;
    public int CurrentWave => currentWaveIndex + 1;

    private void OnEnable()
    {
        PlayerHP.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        PlayerHP.OnPlayerDied -= HandlePlayerDeath;
    }
    
    /// <summary>
    /// [추가] WaveSystem 오브젝트가 파괴될 때, 생성했던 복제본들도 함께 파괴합니다.
    /// </summary>
    private void OnDestroy()
    {
        foreach (var variant in runtimeEnemyVariants)
        {
            Destroy(variant);
        }
        runtimeEnemyVariants.Clear();
    }
    
    /// <summary>
    /// 보상 선택이 완료되었음을 외부(WeightedRandomReward)에서 호출할 메서드
    /// </summary>
    public void OnRewardSelected()
    {
        isWaitingForRewardSelection = false;
    }

    private void HandlePlayerDeath()
    {
        if (GameModeManager.CurrentMode == GameMode.Normal)
        {
            Time.timeScale = 0f;
            stageFinish.SetActive(true);
            SoundManager.Instance.PlaySFX("SFX_BGM_GameOver");
            
            // 플레이어 사망(실패) 시 데이터 수집 및 전송
            CollectAndSendFinalData(false);
        }
    }
    
    private void Awake()
    {
        // [추가] 게임 시작 시, 이번 판에 사용할 적 3종을 무작위로 선택합니다.
        SelectEnemiesForRun();
        
        // 웨이브 정보 초기화
        waves = new Wave[maxWave];
        for (int i = 0; i < maxWave; i++)
        {
            waves[i] = new Wave
            {
                index = i,
                spawnTime = 1.0f, // 기본 스폰 시간
                maxEnemyCount = 20, // 기본 적 수 (완만하게 증가)
                enemyPrefabs = currentDefaultEnemy,
                enemyType = EnemyType.Default // 기본 적 타입 설정
            };

            // 중간 보스 웨이브 설정 (20번째 웨이브)
            if (i == 19) // 0-indexed 이므로 19는 20번째 웨이브
            {
                waves[i].spawnTime = 1.5f;
                waves[i].maxEnemyCount = 10;
                waves[i].enemyPrefabs = currentMiddleEnemy;
                waves[i].enemyType = EnemyType.MiddleBoss; // 중간 보스 타입 설정
            }
            // 최종 보스 웨이브 설정 (마지막 웨이브)
            else if (i == maxWave - 1)
            {
                waves[i].maxEnemyCount = 1;
                waves[i].enemyPrefabs = currentBossEnemy;
                waves[i].enemyType = EnemyType.Boss; // 보스 타입 설정
            }
            // 10의 배수 웨이브 (20웨이브 제외)는 일반 적 웨이브로 유지
            else if ((i + 1) % 10 == 0)
            {
                // 이 부분은 필요에 따라 다른 중간 보스 또는 특수 웨이브로 설정 가능
                // 현재는 20웨이브만 중간 보스로 설정하고 나머지는 일반 웨이브로 둡니다.
            }
        }
    }

    /// <summary>
    /// 무한 모드에서 다음 웨이브를 시작합니다. GameManager가 호출합니다.
    /// </summary>
    public void StartNextInfiniteWave()
    {
        currentWaveIndex++;
        int waveNumber = CurrentWave; // 현재 웨이브 번호 (1부터 시작)

        int maxEnemyCount;
        EnemyType enemyType;
        int totalArmorPoints;

        // 1. 웨이브 번호에 따라 웨이브 타입 결정 (보스, 중간보스, 일반)
        if (waveNumber % 10 == 0) // 10, 20, 30... 웨이브는 보스 웨이브
        {
            enemyType = EnemyType.Boss;
            maxEnemyCount = 1;
            // 보스의 방어력은 웨이브에 따라 강력하게 증가 (예시)
            totalArmorPoints = 150 * (waveNumber / 10);
        }
        else if (waveNumber % 5 == 0) // 5, 15, 25... 웨이브는 중간보스 웨이브
        {
            enemyType = EnemyType.MiddleBoss;
            maxEnemyCount = 10;
            // 중간보스의 방어력은 웨이브에 따라 적당히 증가 (예시)
            totalArmorPoints = 70 * (waveNumber / 5);
        }
        else // 나머지 모든 웨이브는 일반 웨이브
        {
            enemyType = EnemyType.Default;
            maxEnemyCount = 30;
            // 일반 적의 방어력은 완만하게 증가 (예시)
            totalArmorPoints = 20 + (waveNumber * 5);
        }

        // 2. 해당 웨이브에 등장할 적 프리팹을 '모든 적 목록'에서 무작위로 선택
        GameObject originalPrefab = allEnemyPrefabs[Random.Range(0, allEnemyPrefabs.Length)];

        // 3. 선택된 프리팹으로 능력치/색상이 변형된 '변종(Variant)'을 생성
        // CreateEnemyVariant는 이미 색상과 방어력을 랜덤화하는 기능이 있습니다.
        GameObject waveEnemyPrefab = CreateEnemyVariant(originalPrefab, totalArmorPoints);
    
        // 현재 웨이브의 적 정보를 저장
        if (gameManager != null)
        {
            gameManager.currentWaveEnemyPrefabSample = waveEnemyPrefab;
        }
        
        // 4. 최종 웨이브 정보를 구성합니다.
        Wave infiniteWave = new Wave
        {
            index = currentWaveIndex,
            spawnTime = Mathf.Max(0.1f, 1.0f - (currentWaveIndex * 0.01f)), // 웨이브가 지날수록 스폰 시간 단축
            maxEnemyCount = maxEnemyCount,
            enemyPrefabs = waveEnemyPrefab,
            enemyType = enemyType
        };
    
        // 5. 구성된 웨이브 정보로 스포너에게 적 생성을 요청합니다.
        enemySpawner.StartWave(infiniteWave);
    }
    
    /// <summary>
    /// 웨이브 시작 버튼을 누르면 호출됩니다.
    /// </summary>
    public void StartWave()
    {
        // 일반 모드가 아니면 실행되지 않도록 방어 코드 추가
        if (GameModeManager.CurrentMode != GameMode.Normal) return;
        if (isWaveInProgress) return;
        
        isWaveInProgress = true;
        SoundManager.Instance.PlaySFX("SFX_System_WaveStart"); // 웨이브 시작 사운드 재생
        StartCoroutine(WaveClearDetector());
        
        // [추가] 튜토리얼 매니저에게 웨이브가 시작되었음을 알립니다.
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnWaveStarted();
        }
    }

    /// <summary>
    /// 현재 웨이브의 최대 적 수를 반환합니다. GameManager가 호출합니다.
    /// </summary>
    public int GetCurrentWaveMaxEnemies()
    {
        int waveNumber = CurrentWave;

        if (waveNumber <= 0) return 0;

        if (waveNumber % 10 == 0)
        {
            return 1; // 보스 웨이브
        }
        else if (waveNumber % 5 == 0)
        {
            return 10; // 중간보스 웨이브
        }
        else
        {
            return 30; // 일반 웨이브
        }
    }
    
    // [추가] 이번 게임에 사용할 적 3종을 무작위로 선택하는 함수
    private void SelectEnemiesForRun()
    {
        if (allEnemyPrefabs == null || allEnemyPrefabs.Length < 3)
        {
            Debug.LogError("WaveSystem의 allEnemyPrefabs 배열에 최소 3개 이상의 적 프리팹을 할당해야 합니다!");
            return;
        }

        // [수정] 튜토리얼 중인지 확인하여 몬스터 구성을 고정합니다.
        if (PlayerPrefs.GetInt("TutorialCompleted", 0) == 0)
        {
            // 튜토리얼은 원본 프리팹 그대로 사용
            currentDefaultEnemy = allEnemyPrefabs[0];
            currentMiddleEnemy = allEnemyPrefabs[1];
            currentBossEnemy = allEnemyPrefabs[2];
        }
        else
        {
            List<GameObject> tempList = allEnemyPrefabs.ToList();

            // 1. 보스 복제본 생성
            int randIndex = Random.Range(0, tempList.Count);
            GameObject bossPrefab = tempList[randIndex];
            tempList.RemoveAt(randIndex);
            currentBossEnemy = CreateEnemyVariant(bossPrefab, 100);

            // 2. 중간 보스 복제본 생성
            randIndex = Random.Range(0, tempList.Count);
            GameObject middlePrefab = tempList[randIndex];
            tempList.RemoveAt(randIndex);
            currentMiddleEnemy = CreateEnemyVariant(middlePrefab, 50);

            // 3. 기본 몬스터 복제본 생성
            randIndex = Random.Range(0, tempList.Count);
            GameObject defaultPrefab = tempList[randIndex];
            currentDefaultEnemy = CreateEnemyVariant(defaultPrefab, 30);
        }
    }
    
    /// <summary>
    /// 웨이브 클리어 시 보상을 지급합니다。
    /// </summary>
    public void WaveReward()
    {
        // 보상 패널을 띄우고, '보상 선택 대기' 상태로 전환합니다.
        isWaitingForRewardSelection = true;

        if (GameModeManager.CurrentMode == GameMode.Normal)
        {
            // 일반 모드면 스킬 포인트 지급 (최대 200)
            if (PerkManager.Instance != null && !PerkManager.Instance.perk_isTowerSpecialistEnabled)
            {
                playerSP.CurrentSkillPoint = Mathf.Min(playerSP.CurrentSkillPoint + 20, 200);
            }
            
        }
        
        // 타워 코스트 지급
        if (PerkManager.Instance != null)
        {
            playerTowerCost.AddTowerCost(PerkManager.Instance.perk_towerCostPerWave);
        }
        
        // '이자' 특성이 활성화되어 있으면 추가 골드를 지급합니다.
        if (PerkManager.Instance != null && PerkManager.Instance.perk_hasInterest)
        {
            int interestGold = (int)(playerGold.CurrentGold * 0.1f);
            playerGold.CurrentGold += interestGold;
        }
        
        // 스킬 버튼 활성화 및 스킬 타워 제거
        skillManager.AllButtonActive();
        skillManager.AllSkillTowerDestroy();
        skillManager.ResetUsedSkillsInWave();

        // 보상 패널 활성화
        panelReward.SetActive(true);
        
        // 튜토리얼 매니저에게 보상 패널이 활성화되었음을 알립니다.
        OnRewardPanelActivated?.Invoke();
    }

    /// <summary>
    /// 웨이브 클리어 여부를 지속적으로 확인하는 코루틴
    /// </summary>
    private IEnumerator WaveClearDetector()
    {
        // 첫 웨이브 시작
        currentWaveIndex++;
        
        // 몬스터 샘플 저장
        if (gameManager != null)
        {
            gameManager.currentWaveEnemyPrefabSample = waves[currentWaveIndex].enemyPrefabs;
        }
        
        enemySpawner.StartWave(waves[currentWaveIndex]);

        while (true)
        {
            // 현재 웨이브 클리어 조건 확인 (맵에 적이 남아있지 않으면 클리어)
            if (enemySpawner.EnemyList.Count == 0)
            {
                // 모든 적이 스폰되었는지 확인
                if (enemySpawner.SpawnEnemyCount >= waves[currentWaveIndex].maxEnemyCount)
                {
                    // 마지막 웨이브가 아니라면 다음 웨이브 진행
                    if (currentWaveIndex < waves.Length - 1)
                    {
                        SoundManager.Instance.PlaySFX("SFX_System_WaveReward"); // 웨이브 보상패널 사운드
                        WaveReward();

                        // 보상 선택이 완료될 때까지 대기
                        yield return new WaitUntil(() => !isWaitingForRewardSelection);

                        // 보상 선택이 끝나면 다음 웨이브 카운트다운 시작
                        UIEventManager.Instance.ShowSystemMessage(SystemType.WaveStart);
                        yield return new WaitForSecondsRealtime(3); // 다음 웨이브 전 대기

                        currentWaveIndex++;
                        
                        // 몬스터 샘플 저장
                        if (gameManager != null)
                        {
                            gameManager.currentWaveEnemyPrefabSample = waves[currentWaveIndex].enemyPrefabs;
                        }
                        
                        enemySpawner.StartWave(waves[currentWaveIndex]);
                    }
                    // 모든 웨이브 클리어 시 (30웨이브) 게임 승리
                    else
                    {
                        // ClearSceneData에 정보 저장
                        ClearSceneData.ClearedDifficultyLevel = DifficultyManager.Instance.CurrentDifficultyLevel;
                        ClearSceneData.PlayerRemainingHP = playerHP.CurrentHP;
                        ClearSceneData.EnemiesKilledCount = gameManager.enemiesKilled;
                        ClearSceneData.MapDifficultyScore = gameManager.CurrentMapDifficulty.TotalScore; // 맵 난이도 점수 저장
                        ClearSceneData.GameTime = gameManager.gameTime;
                        ClearSceneData.TotalDamageTaken = gameManager.totalDamageTaken;
                        ClearSceneData.MiddleBossesKilled = gameManager.middleBossesKilled;
                        ClearSceneData.BossesKilled = gameManager.bossesKilled;
                        ClearSceneData.BonusScore = gameManager.bonusScore;
                        ClearSceneData.RemainingGold = playerGold.CurrentGold; // 남은 골드 저장
                        ClearSceneData.IsInfiniteModeResult = false;
                        
                        // 게임 클리어(성공) 시 데이터 수집 및 전송
                        CollectAndSendFinalData(true);
                        
                        LoadingSceneController.LoadScene("ClearScene"); // ClearScene으로 이동
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(1f); // 1초마다 확인
        }
    }
    
    /// <summary>
    /// [추가] 원본 프리팹으로 색상과 능력치가 적용된 '복제본(Variant)'을 생성하는 최종 함수
    /// </summary>
    /// <param name="originalPrefab">복제할 원본 프리팹</param>
    /// <param name="totalArmorPoints">분배할 총 방어력 포인트</param>
    /// <returns>완성된 복제본 게임 오브젝트</returns>
    private GameObject CreateEnemyVariant(GameObject originalPrefab, int totalArmorPoints)
    {
        // 1. 원본으로부터 복제본을 생성합니다.
        GameObject variant = Instantiate(originalPrefab);
    
        // 2. 씬에 보이지 않도록 비활성화하고, 나중에 정리할 수 있도록 리스트에 추가합니다.
        variant.SetActive(false); 
        runtimeEnemyVariants.Add(variant);
    
        // 3. 색상을 무작위로 적용합니다.
        variant.GetComponent<SpriteRenderer>().color = DifficultyManager.Instance.GetRandomColorForDifficulty();
    
        // 4. 방어력을 무작위로 분배하여 적용합니다.
        EnemyHP enemyHP = variant.GetComponent<EnemyHP>();
        if (enemyHP != null)
        {
            int physicArmor = Random.Range(0, totalArmorPoints + 1);
            int magicArmor = totalArmorPoints - physicArmor;
            enemyHP.physicArmor = physicArmor;
            enemyHP.magicArmor = magicArmor;
        }
    
        // 5. 모든 설정이 완료된 복제본을 반환합니다.
        return variant;
    }
    
    // 최종 데이터를 취합하고 전송하는 메소드
    private void CollectAndSendFinalData(bool isSuccess)
    {
        if (GameAnalyticsManager.Instance == null) return;

        // 적 정보 수집 로직
        List<object> finalEnemies = new List<object>();
        if (gameManager != null && gameManager.currentWaveEnemyPrefabSample != null)
        {
            var enemyInfo = gameManager.currentWaveEnemyPrefabSample.GetComponent<Enemy>();
            var enemyHP = gameManager.currentWaveEnemyPrefabSample.GetComponent<EnemyHP>();
            if (enemyInfo != null && enemyHP != null)
            {
                finalEnemies.Add(new {
                    type = enemyInfo.enemyType.ToString(),
                    hp = enemyHP.MaxHP, // 샘플이므로 MaxHP를 기록
                    physicDef = enemyHP.CurrentPhysicArmor,
                    magicDef = enemyHP.CurrentMagicArmor
                });
            }
        }
        GameAnalyticsManager.Instance.SetFinalWaveEnemies(finalEnemies);
        
        string finalStatus = isSuccess ? "Success" : "Fail";
        int wave = CurrentWave;
        
        // --- [분석] 최종 점수 계산 로직 (ClearSceneManager에서 이동) ---
        // ClearSceneData에 이미 저장된 값들을 기반으로 계산합니다.
        float difficultyScore = ClearSceneData.ClearedDifficultyLevel * 10000f;
        float mapDifficultyScore = ClearSceneData.MapDifficultyScore * 100f;
        float remainingHpScore = ClearSceneData.PlayerRemainingHP * 1000f;
        float enemiesKilledScore = ClearSceneData.EnemiesKilledCount * 10f;
        float gameTimePenalty = ClearSceneData.GameTime * -100f;
        float damageTakenPenalty = ClearSceneData.TotalDamageTaken * -500f;
        float middleBossBonus = ClearSceneData.MiddleBossesKilled * 1000f;
        float bossBonus = ClearSceneData.BossesKilled * 10000f;
        float bonusScoreValue = ClearSceneData.BonusScore * 1f;

        float finalScore = difficultyScore + mapDifficultyScore + remainingHpScore + enemiesKilledScore +
                         gameTimePenalty + damageTakenPenalty + middleBossBonus + bossBonus + bonusScoreValue;

        // 계산된 최종 점수를 ClearSceneData에 저장
        ClearSceneData.FinalScore = finalScore;

        // --- [분석] 세부 점수 및 최종 데이터 수집 ---
        // GameAnalyticsManager에 세부 점수 기록
        if (GameAnalyticsManager.Instance != null)
        {
            GameAnalyticsManager.Instance.RecordScoreDetail("difficultyScore", difficultyScore);
            GameAnalyticsManager.Instance.RecordScoreDetail("mapDifficultyScore", mapDifficultyScore);
            GameAnalyticsManager.Instance.RecordScoreDetail("remainingHpScore", remainingHpScore);
            GameAnalyticsManager.Instance.RecordScoreDetail("enemiesKilledScore", enemiesKilledScore);
            GameAnalyticsManager.Instance.RecordScoreDetail("gameTimePenalty", gameTimePenalty);
            GameAnalyticsManager.Instance.RecordScoreDetail("damageTakenPenalty", damageTakenPenalty);
            GameAnalyticsManager.Instance.RecordScoreDetail("middleBossBonus", middleBossBonus);
            GameAnalyticsManager.Instance.RecordScoreDetail("bossBonus", bossBonus);
            GameAnalyticsManager.Instance.RecordScoreDetail("bonusScore", bonusScoreValue);
        }
        
        GameAnalyticsManager.Instance.SetFinalUpgradeLevels(gameManager.PlayerUpgrade.physicUpgrade, gameManager.PlayerUpgrade.magicUpgrade);
        gameManager.SendMessage("CollectAndSetFinalEquipmentStats", SendMessageOptions.DontRequireReceiver);
        gameManager.SendMessage("CollectAndSetFinalTowerDps", SendMessageOptions.DontRequireReceiver);
        
        // --- 최종 점수를 포함하여 데이터 전송 ---
        GameAnalyticsManager.Instance.FinalizeAndSendEvent(finalStatus, wave, finalScore);
    }
}

/// <summary>
/// 단일 웨이브의 정보를 담는 구조체
/// </summary>
[System.Serializable]
public struct Wave
{
    public int index;
    public float spawnTime;         // 현재 웨이브 적 생성 주기
    public int maxEnemyCount;       // 현재 웨이브 적 등장 숫자
    public GameObject enemyPrefabs; // 현재 웨이브 적 등장 종류
    public EnemyType enemyType;     // 적의 종류 추가
}