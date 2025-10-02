using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TowerDefense.Enums;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 게임의 전반적인 흐름과 맵 생성을 관리하는 클래스
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public EnemySpawner enemySpawner; // 적 스포너 참조
    
    [Header("Required Components")]
    private RandomPathGenerator pathGenerator; // 랜덤 경로 생성기 참조
    private MapGenerator mapGenerator;         // 맵 생성기 참조

    public int enemiesKilled; // 처치한 적의 수
    public float gameTime; // 게임 시간
    public float totalDamageTaken; // 받은 총 데미지
    public int middleBossesKilled; // 처치한 중간 보스 수
    public int bossesKilled; // 처치한 최종 보스 수
    public int bonusScore; // 보너스 점수
    private MapDifficulty _currentMapDifficulty; // 현재 맵 난이도 정보
    public GameObject currentWaveEnemyPrefabSample;

    [Header("Real-time Score UI")]
    [Tooltip("최종 점수를 표시할 텍스트")]
    [SerializeField] private TMP_Text finalScoreText;
    
    [Header("Infinite Mode UI")]
    [Tooltip("무한 모드 웨이브 타이머를 포함하는 부모 패널")]
    [SerializeField] private GameObject infiniteModeTimerPanel; // [수정] 타이머 텍스트를 담는 패널
    
    [Tooltip("플레이어 HP 참조 (패널티 적용용)")]
    [SerializeField] private PlayerHP playerHP;
    
    public long infiniteModeScore = 0; // 무한 모드 점수 (값이 매우 커질 수 있으므로 long 사용)
    // 무한 모드 세부 점수를 저장할 변수들
    private long _infiniteKillScore = 0;
    private long _infiniteWaveClearScore = 0;
    private long _infiniteTimeBonusScore = 0;
    
    private TextMeshProUGUI _timerTextComponent; // [추가] 타이머 패널 하위의 텍스트 컴포넌트를 저장할 변수
    
    public MapDifficulty CurrentMapDifficulty => _currentMapDifficulty;

    [Header("UI References for Buttons")]
    [Tooltip("타워 버튼 UI 프리팹")]
    [SerializeField] private GameObject towerButtonPrefab;
    [Tooltip("스킬 버튼 UI 프리팹")]
    [SerializeField] private GameObject skillButtonPrefab;
    [Tooltip("타워 버튼들이 배치될 UI 부모 Transform")]
    [SerializeField] private Transform towerButtonParent;
    [Tooltip("스킬 버튼들이 배치될 UI 부모 Transform")]
    [SerializeField] private Transform skillButtonParent;
    [Tooltip("표시할 랜덤 보상 패널의 개수")]
    public int numberOfRewardsToShow = 2; // 기본값 2
    public static bool isSPRewardFixed = false; // SP 보상 고정 플래그
    public static bool isTowerSellCostFixed = false; // 타워 판매 비용 고정 플래그

    [Header("Data References")]
    [Tooltip("모든 타워 아키타입 ScriptableObject")]
    [SerializeField] private TowerArchetype[] towerArchetypes;
    public TowerArchetype[] TowerArchetypes => towerArchetypes;

    [Tooltip("플레이어 업그레이드 참조")]
    [SerializeField] private PlayerUpgrade playerUpgrade;
    
    public PlayerUpgrade PlayerUpgrade => playerUpgrade;
    
    [Tooltip("스킬 매니저 참조")]
    public SkillManager skillManager;
    [Tooltip("웨이브 시스템 참조")]
    public WaveSystem waveSystem;
    [Tooltip("타워 스포너 참조")]
    public TowerSpawner towerSpawner;
    
    [Tooltip("현재 게임 속도를 표시하는 UI 텍스트")]
    public TextMeshProUGUI textGameSpeed;
    
    // 생성된 버튼들을 관리할 변수
    private List<Button> towerButtons = new List<Button>();
    private List<Button> skillButtons = new List<Button>();
    
    public List<Button> TowerButtons => towerButtons;
    public List<Button> SkillButtons => skillButtons;
    
    public Vector3 startCastlePosition;
    public Vector3 endCastlePosition;
    
    private float gameSpeed = 1f; // 현재 게임 속도 (1x, 2x, 4x)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        ClearSceneData.Reset();
        Time.timeScale = gameSpeed;
    }
    
    private void Start()
    {
        // 새 게임 기본 분석정보 기록
        if (GameAnalyticsManager.Instance != null)
        {
            GameAnalyticsManager.Instance.StartNewSession();
            string mode = GameModeManager.CurrentMode == GameMode.Normal ? "Normal" : "Infinite";
            int diff = DifficultyManager.Instance.CurrentDifficultyLevel;
            float mapDiff = CurrentMapDifficulty != null ? CurrentMapDifficulty.TotalScore : 0;
            GameAnalyticsManager.Instance.SetInitialGameInfo(mode, diff, mapDiff);
        }
        
        PerkManager.Instance.ResetPerks();
        
        enemiesKilled = 0; // 게임 시작 시 초기화
        gameTime = 0f; // 게임 시간 초기화
        totalDamageTaken = 0f; // 받은 총 데미지 초기화
        middleBossesKilled = 0; // 중간 보스 처치 수 초기화
        bossesKilled = 0; // 최종 보스 처치 수 초기화
        bonusScore = 0; // 보너스 점수 초기화

        // 필요한 컴포넌트 초기화
        pathGenerator = GetComponent<RandomPathGenerator>();
        mapGenerator = GetComponent<MapGenerator>();
        
        // 게임 모드에 따라 UI 상태를 설정합니다.
        if (GameModeManager.CurrentMode == GameMode.Infinite)
        {
            // 타이머 패널 하위의 Text 컴포넌트를 찾아 저장합니다.
            if (infiniteModeTimerPanel != null)
            {
                _timerTextComponent = infiniteModeTimerPanel.GetComponentInChildren<TextMeshProUGUI>();
                infiniteModeTimerPanel.SetActive(true);
            }
        }
        else // 일반 모드
        {
            if (infiniteModeTimerPanel != null)
            {
                infiniteModeTimerPanel.SetActive(false);
            }
        }
        
        // 점수판은 항상 켜져 있도록 합니다.
        if(finalScoreText != null) finalScoreText.gameObject.SetActive(true);
        
        // 게임 시작 시 새로운 맵 생성
        GenerateNewMap();

        // UI 초기화 코루틴 시작
        StartCoroutine(InitializeGameUI());
    }
    
    private void Update()
    {
        gameTime += Time.deltaTime;
        
        // [수정] 모드에 따라 다른 점수판을 업데이트
        if (GameModeManager.CurrentMode == GameMode.Infinite)
        {
            if (GameModeManager.CurrentMode == GameMode.Infinite)
            {
                // 무한 모드일 경우: infiniteModeScore를 표시
                finalScoreText.text = $"점 수 : {infiniteModeScore:N0}";
            }
        }
        else
        {
            if (finalScoreText != null)
            {
                int clearedDifficultyLevel = DifficultyManager.Instance.CurrentDifficultyLevel;
            
                float currentScore =
                    (clearedDifficultyLevel * 10000f) +
                    (_currentMapDifficulty.TotalScore * 100f) +
                    (enemiesKilled * 10f) +
                    (totalDamageTaken * -500f) + // 받은 데미지는 감점
                    (middleBossesKilled * 1000f) +
                    (bossesKilled * 10000f) +
                    (bonusScore * 1f);
                finalScoreText.text = $"점 수 : {currentScore:N0}";
            }
        }
    }

    // 무한 모드 점수 추가 메소드들
    public void AddScore_Infinite_Kill(int baseGold)
    {
        // 적 처치 점수 = (적이 주는 기본 골드 * 맵 난이도 배율)
        long score = (long)(baseGold * _currentMapDifficulty.TotalScore * 0.01f);
        _infiniteKillScore += score;
        infiniteModeScore += score;
    }

    public void AddScore_Infinite_WaveClear()
    {
        // 웨이브 클리어 기본 점수
        long score = (long)(100 * Mathf.Pow(1.1f, waveSystem.CurrentWave));
        _infiniteWaveClearScore += score;
        infiniteModeScore += score;
    }

    public void AddScore_Infinite_TimeBonus(float remainingTime)
    {
        // 남은 시간 1초당 1000점 (예시)
        long score = (long)(remainingTime * 10);
        _infiniteTimeBonusScore += score;
        infiniteModeScore += score;
    }
    
    private IEnumerator InitializeGameUI()
    {
        // 필요한 매니저들이 준비될 때까지 대기
        yield return new WaitUntil(() => EquipmentManager.IsReady && playerUpgrade != null && skillManager != null && towerSpawner != null);
        
        // 타워 버튼 생성 및 설정
        for (int i = 0; i < towerArchetypes.Length; i++)
        {
            if (towerArchetypes[i] == null) continue;
            SetupButton(towerButtonPrefab, towerButtonParent, towerArchetypes[i], true, i);
        }
        
        // 스킬 버튼 생성 및 설정
        for (int i = 0; i < towerArchetypes.Length; i++)
        {
            if (towerArchetypes[i] == null || towerArchetypes[i].skillPrefab == null) continue;
            SetupButton(skillButtonPrefab, skillButtonParent, towerArchetypes[i], false, i);
        }
    }

    /// <summary>
    /// 인게임 '시작' 버튼을 눌렀을 때 호출될 통합 메소드입니다.
    /// </summary>
    public void StartGameFlow()
    {
        Time.timeScale = gameSpeed;
        
        // 시작 버튼을 찾아서 비활성화합니다. (중복 클릭 방지)
        // 버튼 이름이 "ButtonStartWave"라고 가정합니다. 실제 이름에 맞게 수정해주세요.
        GameObject startButton = GameObject.Find("ButtonStartWave");
        if (startButton != null)
        {
            startButton.SetActive(false);
        }

        // 현재 게임 모드를 확인하고 그에 맞는 게임 루프를 시작합니다.
        if (GameModeManager.CurrentMode == GameMode.Infinite)
        {
            // 무한 모드 일때는 타워 코스트 보상이 0
            PerkManager.Instance.perk_towerCostPerWave = 0;
            
            StartCoroutine(InfiniteModeLoop());
        }
        else // 일반 모드
        {
            waveSystem.StartWave();
        }
    }
    
    /// <summary>
    /// 타워 또는 스킬 버튼을 설정하는 헬퍼 메서드
    /// </summary>
    /// <param name="prefab">버튼 프리팹</param>
    /// <param name="parent">버튼이 배치될 부모 Transform</param>
    /// <param name="archetype">관련된 TowerArchetype</param>
    /// <param name="isTowerButton">타워 버튼인지 스킬 버튼인지 여부</param>
    private void SetupButton(GameObject prefab, Transform parent, TowerArchetype archetype, bool isTowerButton, int index)
    {
        GameObject buttonGO = Instantiate(prefab, parent);

        // 이미지 설정
        string imageName = isTowerButton ? "ImageTower" : "ImageSkill";
        Transform imageTransform = buttonGO.transform.Find(imageName);
        if (imageTransform != null)
        {
            Image buttonImage = imageTransform.GetComponent<Image>();
            Sprite icon = isTowerButton ? archetype.towerIcon : archetype.skillIcon;
            if (buttonImage != null && icon != null) buttonImage.sprite = icon;
        }

        // 텍스트 설정
        TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (isTowerButton)
            {
                buttonText.text = $"{archetype.towerWeaponStats.cost}"; 
            }
            else
            {
                buttonText.text = $"<color=blue>{archetype.skillPointCost}SP</color>";
            }
        }

        // TooltipTrigger 설정
        TooltipTrigger tooltipTrigger = buttonGO.GetComponent<TooltipTrigger>();
        if (tooltipTrigger == null)
        {
            tooltipTrigger = buttonGO.AddComponent<TooltipTrigger>();
        }
        tooltipTrigger.SetHeader(isTowerButton ? archetype.archetypeDisplayName : archetype.skillDisplayName);
        tooltipTrigger.GetContentCallback = () =>
        {
            return isTowerButton ?
                archetype.GetFormattedTowerDescription(playerUpgrade, EquipmentManager.Instance.currentPlayerData) :
                archetype.GetFormattedSkillDescription(playerUpgrade, EquipmentManager.Instance.currentPlayerData);
        };

        // 클릭 이벤트 연결
        UnityEngine.UI.Button uiButton = buttonGO.GetComponent<UnityEngine.UI.Button>();
        if (isTowerButton)
        {
            uiButton.onClick.AddListener(() => towerSpawner.ReadyToSpawnTower(buttonGO.transform, archetype, false)); // 일반 타워
            towerButtons.Add(uiButton);
        }
        else
        {
            uiButton.onClick.AddListener(() => skillManager.UseSkill(buttonGO.transform, archetype));
            skillManager.RegisterSkillButton(archetype.archetypeID, uiButton, index); // SkillManager에 버튼 등록
            skillButtons.Add(uiButton);
        }
    }

    /// <summary>
    /// 새로운 맵을 생성하고 관련 컴포넌트들을 설정합니다。
    /// 이 메소드는 웨이브 시스템에서 다음 스테이지로 넘어갈 때 호출될 수 있습니다。
    /// </summary>
    [ContextMenu("Generate New Map")] // 유니티 에디터에서 쉽게 호출할 수 있도록 ContextMenu 추가
    public void GenerateNewMap() {
        // 1. 랜덤 경로 생성
        Vector2Int[] path = pathGenerator.GenerateWaypoints();
        bool isLeftToRight = pathGenerator.IsLeftToRight;
        
        // 2. 맵 난이도 계산 및 저장
        _currentMapDifficulty = MapDifficulty.CalculateDifficulty(path, pathGenerator.SegmentCount);
        // Debug.Log($"맵 난이도: {_currentMapDifficulty.TotalScore}");

        // 3. 생성된 경로를 기반으로 맵 타일 생성
        mapGenerator.GenerateMap(path, isLeftToRight);

        // 4. 생성된 경로 정보를 적 스포너에 전달하여 적 이동 경로 설정
        enemySpawner.SetupWaypoints(path);
    }
    
    /// <summary>
    /// '희생 전략' 특성에 의해 선택된 타워와 스킬 UI 버튼을 비활성화합니다.
    /// </summary>
    /// <param name="towerIndex">비활성화할 타워의 인덱스</param>
    public void DisableSacrificedTowerUI(int towerIndex)
    {
        if (PerkManager.Instance == null || !PerkManager.Instance.perk_isSacrificeEnabled)
        {
            // 특성이 활성화되지 않았다면, 이 메서드가 호출되었더라도 아무것도 하지 않고 즉시 종료합니다.
            return;
        }
        
        // 타워 버튼 비활성화
        if (towerIndex >= 0 && towerIndex < towerButtons.Count)
        {
            // [수정] SetActive(false) 대신 interactable을 false로 변경
            towerButtons[towerIndex].interactable = false;
        }

        // 스킬 버튼 비활성화
        if (towerIndex >= 0 && towerIndex < skillButtons.Count)
        {
            // [수정] SetActive(false) 대신 interactable을 false로 변경
            skillButtons[towerIndex].interactable = false;
        }
    
        // Debug.Log($"Sacrifice Perk Activated: Tower/Skill at index {towerIndex} has been disabled.");
    }
    
    /// <summary>
    /// 게임 속도를 다음 단계로 변경합니다. (1x -> 2x -> 4x -> 1x 순환)
    /// </summary>
    public void SpeedUp()
    {
        // 게임 속도 변경 로직
        if (gameSpeed == 1f)
        {
            gameSpeed = 2f;
        }
        else if (gameSpeed == 2f)
        {
            gameSpeed = 4f;
        }
        else
        {
            gameSpeed = 1f;
        }

        // UI 텍스트 업데이트 및 실제 게임 속도 적용
        textGameSpeed.text = gameSpeed.ToString() + "x";
        Time.timeScale = gameSpeed;
    }
    
    /// <summary>
    /// 무한 모드의 메인 게임 루프를 관리하는 코루틴입니다.
    /// </summary>
    private IEnumerator InfiniteModeLoop()
    {
        while (playerHP.CurrentHP > 0)
        {
            waveSystem.StartNextInfiniteWave();
            float waveTimer = 120f;

            while (waveTimer > 0)
            {
                waveTimer -= Time.deltaTime;
                
                // 캐시해둔 텍스트 컴포넌트를 업데이트합니다.
                if (_timerTextComponent != null)
                {
                    int minutes = (int)waveTimer / 60;
                    int seconds = (int)waveTimer % 60;
                    _timerTextComponent.text = $"{minutes:00}:{seconds:00}";
                }
                
                if (enemySpawner.ActiveEnemyCount == 0 && enemySpawner.SpawnEnemyCount >= waveSystem.GetCurrentWaveMaxEnemies())
                {
                    // 점수 추가 로직 (기존과 동일)
                    AddScore_Infinite_WaveClear();
                    AddScore_Infinite_TimeBonus(waveTimer);

                    // 타이머 루프를 빠져나가 다음 웨이브로 진행
                    break;
                }
                yield return null;
            }
            
            // 먼저 타임 오버 시 패널티를 적용합니다.
            if (waveTimer <= 0)
            {
                int remainingEnemies = enemySpawner.ActiveEnemyCount;
                if (remainingEnemies > 0)
                {
                    playerHP.TakeDamage(remainingEnemies);
                }
            }
        
            // 패널티 적용 후에도 플레이어가 살아있는지 확인합니다.
            if (playerHP.CurrentHP <= 0)
            {
                break; // 루프를 탈출하여 게임 오버 처리
            }

            // 웨이브 시작 전 남은 적 처리
            enemySpawner.ClearAllEnemies();
            
            // 살아있다면, 이제 모든 경우에 보상 패널을 호출하고 선택을 기다립니다.
            waveSystem.WaveReward();
            yield return new WaitUntil(() => !waveSystem.isWaitingForRewardSelection);

            // 다음 웨이브 시작 전 짧은 대기 시간
            UIEventManager.Instance.ShowSystemMessage(SystemType.WaveStart);
            yield return new WaitForSecondsRealtime(3f);
        }
        
        EndInfiniteModeRun();
    }
    
    /// <summary>
    /// 무한 모드 게임 오버 시 호출될 메소드
    /// </summary>
    private void EndInfiniteModeRun()
    {
        Time.timeScale = 0f; // 게임 정지

        // 무한 모드 종료 시 데이터 수집 및 전송
        if (GameAnalyticsManager.Instance != null)
        {
            // 샘플 몬스터 정보로 최종 몬스터 정보 저장
            List<object> finalEnemies = new List<object>();
            if (currentWaveEnemyPrefabSample != null)
            {
                var enemyInfo = currentWaveEnemyPrefabSample.GetComponent<Enemy>();
                var enemyHP = currentWaveEnemyPrefabSample.GetComponent<EnemyHP>();
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
            
            // 무한 모드 세부 점수를 정확하게 기록
            GameAnalyticsManager.Instance.RecordScoreDetail("killScore", _infiniteKillScore);
            GameAnalyticsManager.Instance.RecordScoreDetail("waveClearScore", _infiniteWaveClearScore);
            GameAnalyticsManager.Instance.RecordScoreDetail("timeBonusScore", _infiniteTimeBonusScore);
            GameAnalyticsManager.Instance.RecordScoreDetail("reachedWave", waveSystem.CurrentWave);
            
            // --- 최종 데이터 설정 ---
            GameAnalyticsManager.Instance.SetFinalUpgradeLevels(playerUpgrade.physicUpgrade, playerUpgrade.magicUpgrade);
            CollectAndSetFinalEquipmentStats();
            CollectAndSetFinalTowerDps();

            // --- 데이터 전송 ---
            GameAnalyticsManager.Instance.FinalizeAndSendEvent("N/A", waveSystem.CurrentWave, infiniteModeScore);
        }
        
        // ClearSceneData에 무한 모드 결과 데이터를 저장
        ClearSceneData.IsInfiniteModeResult = true; // 무한 모드 결과임을 알림
        ClearSceneData.InfiniteModeScore = infiniteModeScore;
        ClearSceneData.ReachedWave = waveSystem.CurrentWave;

        // 결과창으로 이동
        LoadingSceneController.LoadScene("ClearScene");
    }
    
    // 데이터 분석 관련 메소드
    private void CollectAndSetFinalEquipmentStats()
    {
        if (EquipmentManager.Instance == null || EquipmentManager.Instance.currentPlayerData == null) return;

        var playerData = EquipmentManager.Instance.currentPlayerData;

        // 2. PlayerData에 최종 합산된 모든 스탯 정보들을 Dictionary 형태로 수집합니다.
        Dictionary<string, object> finalAggregatedStats = new Dictionary<string, object>
        {
            // 글로벌 스탯 (곱연산 보너스 %)
            { "attackDamage_Mul", playerData.attackDamage },
            { "attackSpeed_Mul", playerData.attackSpeed },
            { "attackRange_Mul", playerData.attackRange },
            { "skillDamage_Mul", playerData.skillDamage },
        
            // 타워 개별 데미지 증가량 (곱연산 보너스 %)
            { "defaultTowerDamage_Mul", playerData.defaultTowerDamage },
            { "arrowTowerDamage_Mul", playerData.arrowTowerDamage },
            { "laserTowerDamage_Mul", playerData.laserTowerDamage },
            { "priestsTowerDamage_Mul", playerData.priestsTowerDamage },
            { "spearTowerDamage_Mul", playerData.spearTowerDamage },
            { "swordTowerDamage_Mul", playerData.swordTowerDamage },
        
            // 부가 스탯 (합연산)
            { "goldPerSecond", playerData.goldPerSecond },
            { "finalMaxHP", playerData.maxHP }, // 장비 효과가 적용된 최종 MaxHP
            { "finalStartSP", playerData.startSP },
            { "finalMaxSP", playerData.maxSP },
            { "finalStartGold", playerData.startGold }
        };

        // 3. 수집된 장비 이름 리스트와 최종 스탯 딕셔너리를 분석 매니저로 보냅니다.
        GameAnalyticsManager.Instance.SetFinalEquipment(new List<string>(), finalAggregatedStats);
    }

    private void CollectAndSetFinalTowerDps()
    {
        Dictionary<string, float> dpsDict = new Dictionary<string, float>();
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (var towerGO in towers)
        {
            var weapon = towerGO.GetComponent<TowerWeapon>();
            if (weapon != null)
            {
                // "타워이름_고유ID" 형태로 저장하여 구분
                string uniqueTowerID = $"{weapon.towerArchetype.archetypeDisplayName}_{towerGO.GetInstanceID()}";
                dpsDict[uniqueTowerID] = weapon.Damage;
            }
        }
        GameAnalyticsManager.Instance.SetFinalTowerDps(dpsDict);
    }

    public TowerArchetype GetTowerArchetypeByName(string name)
    {
       return towerArchetypes.FirstOrDefault(a => a.archetypeDisplayName == name);
    }
}