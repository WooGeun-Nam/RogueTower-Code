using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeightedRandomReward : MonoBehaviour
{
    [Header("Player References")]
    public PlayerHP playerHP;
    public PlayerSP playerSP;
    public PlayerTowerCost playerTowerCost;
    public PlayerGold playerGold;
    public PlayerUpgrade playerUpgrade;
    public TowerSpawner towerSpawner;
    public GameManager gameManager;
    public WaveSystem waveSystem;

    [Header("Reward UI Elements")]
    public GameObject rewardUIPrefab;
    public Transform rewardUIParent;

    private Reward[] randRewards;

    private void OnEnable()
    {
        foreach (Transform child in rewardUIParent)
        {
            Destroy(child.gameObject);
        }

        bool isFirstWavePerk = (waveSystem != null && waveSystem.CurrentWave == 1);
        List<Reward> rewardPoolSource;
        Reward skipOption = null;

        // [수정] 1 웨이브와 일반 웨이브 모두에 적용할 '스킵' 옵션을 미리 찾습니다.
        List<Reward> normalRewards = RewardFactory.GetNormalRewards();

        if (isFirstWavePerk)
        {
            rewardPoolSource = RewardFactory.GetFirstWavePerks();
            // 1 웨이브의 스킵 옵션은 '최종 점수 증가(index 6)'를 사용합니다.
            skipOption = normalRewards.Find(r => r.index == 6);
        }
        else
        {
            rewardPoolSource = new List<Reward>(normalRewards);
            
            // 2 웨이브 이후의 스킵 옵션은 '보상 스킵(index 8)'을 사용합니다.
            skipOption = rewardPoolSource.Find(r => r.index == 8);
            if (skipOption != null)
            {
                rewardPoolSource.Remove(skipOption); // 추첨 목록에서는 제거
            }
        }

        List<Reward> temporaryRewardPool = new List<Reward>(rewardPoolSource);
        int numRewardsToShow = gameManager != null ? gameManager.numberOfRewardsToShow : 2;
    
        // 최종 UI 패널 개수는 랜덤 보상 + 스킵 보상(있을 경우)
        int finalPanelCount = skipOption != null ? numRewardsToShow + 1 : numRewardsToShow;
        randRewards = new Reward[finalPanelCount];

        // 1. 랜덤 보상 생성 (중복 방지)
        // 1웨이브가 아니면서 '과부하 코어'가 활성화된 경우를 확인하는 조건문 추가
        if (!isFirstWavePerk && GameManager.isSPRewardFixed)
        {
            // '과부하 코어' 활성화 시: 첫 보상을 SP로 고정합니다.
            Reward spReward = FixSPReward();
            if (spReward != null)
            {
                randRewards[0] = spReward; // 첫 번째 슬롯(index 0)에 SP 보상을 고정
                temporaryRewardPool.RemoveAll(r => r.index == 2); // 중복을 막기 위해 추첨 목록에서 SP 보상(index 2) 제거
            }

            // 나머지 보상(numRewardsToShow - 1개)만 랜덤으로 추첨합니다.
            for (int i = 0; i < numRewardsToShow - 1; i++)
            {
                if (temporaryRewardPool.Count == 0) break;
                Reward selectedReward = RandomReward(temporaryRewardPool);
                if (selectedReward == null) continue;
        
                temporaryRewardPool.RemoveAll(r => r.index == selectedReward.index);
                randRewards[i + 1] = selectedReward; // 두 번째 슬롯(index 1)부터 채워나갑니다.
            }
        }
        else
        {
            // '과부하 코어'가 비활성화되었거나 1웨이브일 경우, 기존의 완전 랜덤 로직을 실행합니다.
            for (int i = 0; i < numRewardsToShow; i++)
            {
                if (temporaryRewardPool.Count == 0) break;
                Reward selectedReward = RandomReward(temporaryRewardPool);
                if (selectedReward == null) continue;
        
                temporaryRewardPool.RemoveAll(r => r.index == selectedReward.index);
                randRewards[i] = selectedReward;
            }
        }

        // 2. 고정 스킵 보상 추가
        if (skipOption != null)
        {
            randRewards[numRewardsToShow] = skipOption;
        }
        
        // 3. 최종적으로 randRewards 배열에 담긴 모든 보상들을 UI로 생성합니다.
        for (int i = 0; i < randRewards.Length; i++)
        {
            // 배열에 이미 저장된 보상을 순서대로 가져옵니다. (다시 뽑지 않음)
            Reward selectedReward = randRewards[i]; 
            if (selectedReward == null) continue;

            GameObject rewardUI = Instantiate(rewardUIPrefab, rewardUIParent);
            Button rewardButton = rewardUI.GetComponent<Button>();
            int buttonIndex = i;
            rewardButton.onClick.AddListener(() => RewardButton(buttonIndex));

            Image panelBackground = rewardUI.GetComponent<Image>();
            Image rewardImage = rewardUI.transform.Find("ImageReward").GetComponent<Image>();
            TextMeshProUGUI rewardText = rewardUI.transform.Find("TextReward").GetComponent<TextMeshProUGUI>();

            // 웨이브 보상 로직
            if (isFirstWavePerk)
            {
                if (panelBackground != null) panelBackground.color = new Color(0.53f, 0.81f, 0.98f);
                if (rewardImage != null) rewardImage.gameObject.SetActive(false);
                if (rewardText != null) rewardText.text = selectedReward.desc;
            }
            else
            {
                
                
                if (panelBackground != null) panelBackground.color = Color.white;
                if (rewardImage != null)
                {
                    rewardImage.gameObject.SetActive(true);
                    rewardImage.sprite = selectedReward.sprite;
                }

                if (selectedReward.index == 1)
                {
                    selectedReward.value = CalculateRewardGold(waveSystem.CurrentWave - 1);
                }

                if (rewardText != null)
                {
                    if (selectedReward.index == 7)
                    {
                        panelBackground.color = Color.yellow; // 장비 보상은 노란색 배경
                        float scoreForLoot = (DifficultyManager.Instance.CurrentDifficultyLevel * 10000f) + (gameManager.CurrentMapDifficulty.TotalScore * 10f);
                        PlayerEquipmentInstance droppedEquipment = LootManager.Instance.DropRewardEquipment(scoreForLoot);
                        if (droppedEquipment != null)
                        {
                            // 획득할 장비가 이미 보유 중인지 확인
                            bool isDuplicate = EquipmentManager.Instance.currentPlayerData.ownedEquipmentInstances.Any(e => e.equipmentID == droppedEquipment.equipmentID);

                            if (isDuplicate)
                            {
                                // 중복일 경우, 보상 정보를 RCoin으로 변환!
                                EquipmentDataModel data = droppedEquipment.equipmentData;
                                int rCoinAmount = 0;
                                switch (data.rarity)
                                {
                                    case Rarity.Common: rCoinAmount = 10000; break;
                                    case Rarity.Uncommon: rCoinAmount = 20000; break;
                                    case Rarity.Rare: rCoinAmount = 50000; break;
                                    case Rarity.Epic: rCoinAmount = 100000; break;
                                    case Rarity.Legendary: rCoinAmount = 200000; break;
                                }

                                selectedReward.index = 999; // RCoin 변환을 위한 임의의 인덱스 설정
                                selectedReward.value = rCoinAmount;
                                
                                if(rewardImage != null) rewardImage.sprite = Resources.Load<Sprite>(data.iconPath);
                                
                                string rarityColor = GetColorForRarity(data.rarity);
                                string equipmentNameText = $"<color={rarityColor}>{data.equipmentName}\n(중복)</color>";
                                string rCoinText = $"RCoin\n+{rCoinAmount}";
                                rewardText.text = $"{equipmentNameText}\n{rCoinText}";
                            }
                            else
                            {
                                selectedReward.acquiredEquipment = droppedEquipment;
                                if(rewardImage != null) rewardImage.sprite = Resources.Load<Sprite>(droppedEquipment.equipmentData.iconPath);
                                rewardText.text = $"{droppedEquipment.equipmentData.equipmentName} ({droppedEquipment.equipmentData.rarity})";
                            }
                        }
                    }
                    else if (selectedReward.index == 6 || selectedReward.index == 8)
                    {
                        rewardText.text = selectedReward.desc;
                    }
                    else if (selectedReward.index == 3 || selectedReward.index == 4) // 업그레이드 설명 추가
                    {
                        rewardText.text = selectedReward.desc + "\n+" + playerUpgrade.GetUpgradeCountByWave();
                    }
                    else if (selectedReward.value != 0)
                    {
                        rewardText.text = selectedReward.value + selectedReward.desc;
                    }
                    else
                    {
                        rewardText.text = selectedReward.desc;
                    }
                }
            }
        }
    }

    public Reward RandomReward(List<Reward> rewardPool)
    {
        List<Reward> availableRewards = new List<Reward>(rewardPool);
        if (playerHP.CurrentHP >= playerHP.MaxHP)
        {
            availableRewards.RemoveAll(r => r.index == 0);
        }
        
        // '타워 전문가' 특성 활성화 시 SP 보상을 제외합니다.
        if (PerkManager.Instance != null && PerkManager.Instance.perk_isTowerSpecialistEnabled)
        {
            availableRewards.RemoveAll(r => r.index == 2); // SP 보상(index 2) 제외
        }
        
        int currentTotalWeight = 0;
        foreach(var reward in availableRewards) currentTotalWeight += reward.weight;
        if(currentTotalWeight <= 0) return null;

        int selectNum = Random.Range(0, currentTotalWeight) + 1;
        int weight = 0;
        
        foreach(var reward in availableRewards)
        {
            weight += reward.weight;
            if (selectNum <= weight)
            {
                Reward temp = new Reward(reward);
                if (temp.index < 3 || temp.index == 5)
                {
                    if(reward.maxRange > reward.minRange)
                        temp.value = reward.value * (Random.Range(reward.minRange, reward.maxRange));
                }
                return temp;
            }
        }
        return null;
    }

    public void RewardButton(int type)
    {
        // 플레이어가 선택한 보상 기록 (수정됨)
        if (GameAnalyticsManager.Instance != null)
        {
            string rewardName = randRewards[type].name;

            // rewardName이 비어있는 경우를 대비한 방어 코드
            if (string.IsNullOrEmpty(rewardName))
            {
                // 이름이 없으면 설명(desc)을 대신 사용
                rewardName = randRewards[type].desc;

                // 설명도 비어있으면 임시 이름 사용
                if (string.IsNullOrEmpty(rewardName))
                {
                    rewardName = $"Unnamed_Reward_Index_{randRewards[type].index}";
                }
            }
            
            // 장비 보상의 경우, 실제 장비 이름으로 기록 (기존 로직 유지)
            if (randRewards[type].index == 7 && randRewards[type].acquiredEquipment != null)
            {
                rewardName = randRewards[type].acquiredEquipment.equipmentData.equipmentName;
            }
            GameAnalyticsManager.Instance.RecordRandomRewardChoice(rewardName);
        }
        
        int index = randRewards[type].index;
        
        switch (index)
        {
            // --- 기존 일반 보상 ---
            case 0: playerHP.HealHP(randRewards[type].value); break;
            case 1: playerGold.CurrentGold += randRewards[type].value; break;
            case 2: playerSP.CurrentSkillPoint = Mathf.Min(playerSP.MaxSP, playerSP.CurrentSkillPoint + randRewards[type].value); break;
            case 3: playerUpgrade.RewardPhysicUpgrade(); break;
            case 4: playerUpgrade.RewardMagicUpgrade(); break;
            case 5: 
                if (gameManager != null && gameManager.TowerArchetypes != null && gameManager.TowerArchetypes.Length > 0)
                {
                    towerSpawner.SpawnRewardTower(gameManager.TowerArchetypes[Random.Range(0, gameManager.TowerArchetypes.Length)]);
                }
                break;
            case 6: // 최종 점수 증가
            case 8: // 보상 스킵
                if (GameManager.Instance != null)
                {
                    if (GameModeManager.CurrentMode == GameMode.Normal)
                    {
                        // 보상 객체에 저장된 value 값을 가져와 더해줍니다.
                        GameManager.Instance.bonusScore += randRewards[type].value;
                    }
                    else
                    {
                        GameManager.Instance.infiniteModeScore += randRewards[type].value;
                    }
                }
                break;
            case 7: 
                if (randRewards[type].acquiredEquipment != null)
                {
                    EquipmentManager.Instance.AddEquipmentToPlayer(randRewards[type].acquiredEquipment);
                }
                break;
                
            // --- 새로운 시작 특성 활성화 ---
            case 201: PerkManager.Instance.perk_isExplosionEnabled = true; break;
            case 202: PerkManager.Instance.perk_isExecutionerEnabled = true; break;
            case 203: // 희생 전략
                if (gameManager != null && gameManager.TowerArchetypes != null && gameManager.TowerArchetypes.Length > 0)
                {
                    int sacrificedIndex = Random.Range(0, gameManager.TowerArchetypes.Length);
                    PerkManager.Instance.perk_isSacrificeEnabled = true;
                    PerkManager.Instance.perk_sacrificedTowerIndex = sacrificedIndex;
                    PerkManager.Instance.perk_sacrificeDamageBonus = 0.25f; // 25%
        
                    // GameManager의 함수를 호출하여 UI를 비활성화합니다.
                    GameManager.Instance.DisableSacrificedTowerUI(sacrificedIndex);
                }
                break;
            case 204: PerkManager.Instance.perk_hasInterest = true; break;
            case 205: 
                PerkManager.Instance.perk_upgradeCostModifier = 0.75f; 
                playerUpgrade.RewardPhysicUpgrade();
                playerUpgrade.RewardMagicUpgrade();
                break; // 25% 할인
            case 206: // 위험 수당
                PerkManager.Instance.perk_enemySpeedModifier = 1.15f; // 15% 증가
                PerkManager.Instance.perk_goldModifier = 1.2f;      // 20% 증가
                break;
            case 207: PerkManager.Instance.perk_isFirstStrikeEnabled = true; break;
            case 208: // 자원 증폭
                if (GameModeManager.CurrentMode == GameMode.Normal)
                {
                    PerkManager.Instance.perk_towerCostPerWave = 130;
                }
                else
                {
                    PerkManager.Instance.perk_towerCostPerWave = 30;
                }
                playerTowerCost.AddTowerCost(30);
                break;
            case 209: // 타워 전문가
                PerkManager.Instance.perk_isTowerSpecialistEnabled = true;
                if (GameModeManager.CurrentMode == GameMode.Normal)
                {
                    PerkManager.Instance.perk_towerCostPerWave = 200;
                }
                else
                {
                    PerkManager.Instance.perk_towerCostPerWave = 100;
                }
                playerTowerCost.AddTowerCost(100); // 즉시 1회 지급
                break;
            case 210: PerkManager.Instance.perk_hasIndomitableWill = true; break;
            case 999:
                EquipmentManager.Instance.currentPlayerData.rCoin += randRewards[type].value;
                // UIEventManager를 통해 RCoin 획득 알림을 보낼 수도 있습니다.
                break;
        }
        
        if (TutorialManager.Instance != null && TutorialManager.Instance.isActiveAndEnabled)
        {
            // 시간을 다시 흐르게 하고 다음 튜토리얼 단계로 진행시킵니다.
            Time.timeScale = 1f;
            TutorialManager.Instance.ProceedToNextStep();
        }
        
        SoundManager.Instance.PlaySFX("SFX_UI_ButtonClick2"); // 보상패널 선택 사운드
        
        gameObject.SetActive(false);
        if (waveSystem != null)
        {
            waveSystem.OnRewardSelected();
        }
    }

    private int CalculateRewardGold(int waveIndex)
    {
        int calculatedGold = 200;
        calculatedGold += (50 * waveIndex);
        int thresholdWave = 20;
        if (waveIndex >= thresholdWave)
        {
            int effectiveIndex = waveIndex - thresholdWave + 1;
            calculatedGold += (200 * effectiveIndex * effectiveIndex);
        }
        return calculatedGold;
    }

    private Reward FixSPReward()
    {
        Reward spRewardTemplate = RewardFactory.GetNormalRewards().Find(r => r.index == 2);
        if (spRewardTemplate != null)
        {
            Reward spReward = new Reward(spRewardTemplate);
            if (playerSP != null)
            {
                spReward.value = Mathf.FloorToInt(playerSP.MaxSP * 0.2f); 
                if (spReward.value == 0) spReward.value = 20;
            }
            else
            {
                spReward.value = 50;
            }
            return spReward;
        }
        return null;
    }
    
    /// <summary>
    /// 장비 등급에 맞는 색상 코드를 반환합니다.
    /// </summary>
    private string GetColorForRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return "#808080"; // 회색
            case Rarity.Uncommon: return "green";   // 초록색
            case Rarity.Rare: return "blue";    // 파란색
            case Rarity.Epic: return "purple";  // 보라색
            case Rarity.Legendary: return "orange";  // 주황색
            default: return "black";   // 기본값
        }
    }

    [System.Serializable]
    public class Reward
    {
        public int index;
        public Sprite sprite;
        public string name;
        public string desc;
        public int value;
        public int minRange;
        public int maxRange;
        public int weight;
        public TowerArchetype towerArchetype;
        public PlayerEquipmentInstance acquiredEquipment;
        public Reward() {}
        public Reward(Reward reward)
        {
            this.index = reward.index;
            this.sprite = reward.sprite;
            this.name = reward.name;
            this.desc = reward.desc;
            this.value = reward.value;
            this.minRange = reward.minRange;
            this.maxRange = reward.maxRange;
            this.weight = reward.weight;
            this.towerArchetype = reward.towerArchetype;
        }
    }
}