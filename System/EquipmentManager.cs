using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static event Action OnPlayerDataUpdated;
    public static event Action<EquipmentDataModel> OnEquipmentAcquired; // 장비 획득 이벤트 추가
    public static event Action<SpecialSkillBase> OnEquipmentSkillEquipped; // 장비 스킬 장착 이벤트
    public static event Action<SpecialSkillBase> OnEquipmentSkillUnequipped; // 장비 스킬 해제 이벤트

    public static EquipmentManager Instance { get; private set; }
    public static bool IsReady { get; private set; } // IsReady 플래그 추가

    // 현재 플레이어의 데이터
    public PlayerData currentPlayerData { get; private set; }

    // 현재 활성화된 특수 스킬 (장신구용)
    public SpecialSkillBase activeSpecialSkill { get; private set; }

    // 임시 스탯 모디파이어 리스트 (스킬 등으로 추가되는 스탯)
    private List<StatModifierModel> temporaryStatModifiers = new List<StatModifierModel>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            IsReady = false; // 초기화 시작 시 false
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeEquipmentManager());
    }

    private System.Collections.IEnumerator InitializeEquipmentManager()
    {
        // GameDataManager와 EquipmentDataManager가 모두 준비될 때까지 기다림
        yield return new WaitUntil(() => GameDataManager.IsReady && EquipmentDataManager.IsReady);

        // GameDataManager에서 플레이어 데이터 로드
        currentPlayerData = GameDataManager.Instance.LoadPlayerData();
        // Debug.Log($"[EquipmentManager] PlayerData 로드 완료. ownedEquipmentInstances Count: {(currentPlayerData != null ? currentPlayerData.ownedEquipmentInstances.Count.ToString() : "null")}");

        // 장비데이터의 중복장비 제거 로직
        if (currentPlayerData != null && currentPlayerData.ownedEquipmentInstances.Count > 0)
        {
            int originalCount = currentPlayerData.ownedEquipmentInstances.Count;

            // equipmentID를 기준으로 그룹화하고, 각 그룹의 첫 번째 아이템만 선택하여 중복을 제거합니다.
            // ToDictionary를 사용하면 고유한 키(equipmentID)를 보장할 수 있습니다.
            var uniqueEquipment = currentPlayerData.ownedEquipmentInstances
                .GroupBy(instance => instance.equipmentID)
                .ToDictionary(group => group.Key, group => group.First());

            // 만약 중복이 있어서 아이템 개수가 줄었다면
            if (uniqueEquipment.Count < originalCount)
            {
                Debug.LogWarning($"[EquipmentManager] 중복된 장비 데이터가 발견되어 정리 작업을 수행합니다. {originalCount} -> {uniqueEquipment.Count}개");
            
                // 중복이 제거된 딕셔너리의 값들로 새로운 리스트를 만듭니다.
                currentPlayerData.ownedEquipmentInstances = new List<PlayerEquipmentInstance>(uniqueEquipment.Values);
            
                // 정리된 데이터를 즉시 다시 저장하여 문제를 해결합니다.
                GameDataManager.Instance.SavePlayerData(currentPlayerData);
            }
        }
        
        // 모든 데이터 로드가 완료된 후 UI 업데이트 이벤트 발생
        OnPlayerDataUpdated?.Invoke();
        IsReady = true; // 초기화 완료 시 true

        // 게임 시작 시 이미 장착된 장신구가 있다면 해당 스킬을 활성화
        EquipmentSlotEntry equippedAccessorySlot = currentPlayerData.equippedSlots.FirstOrDefault(s => s.type == EquipmentType.Accessory);
        if (equippedAccessorySlot != null && !string.IsNullOrEmpty(equippedAccessorySlot.instanceID))
        {
            Equip(equippedAccessorySlot.instanceID);
        }
    }
    // Resources 폴더에서 모든 장비 데이터를 불러와 딕셔너리에 저장

    /// <summary>
    /// 플레이어에게 장비를 추가합니다. 중복일 경우 RCoin을 지급합니다.
    /// </summary>
    /// <param name="newInstance">추가할 장비 인스턴스</param>
    /// <param name="canGrantRCoin">중복 시 RCoin으로 변환할지 여부</param>
    /// <returns>새 장비면 0, 중복이라 RCoin으로 변환됐으면 획득한 RCoin 양</returns>
    public int AddEquipmentToPlayer(PlayerEquipmentInstance newInstance, bool canGrantRCoin = true)
    {
        if (newInstance == null)
        {
            Debug.LogError("[EquipmentManager] 추가하려는 장비 인스턴스가 null입니다.");
            return 0;
        }

        // 먼저 장비가 중복인지 아닌지를 변수에 저장합니다.
        bool isDuplicate = currentPlayerData.ownedEquipmentInstances.Any(e => e.equipmentID == newInstance.equipmentID);

        // 1. 중복 장비인 경우의 처리
        if (isDuplicate)
        {
            // 1-1. RCoin 지급이 가능한 경우 (몬스터 드랍, 클리어 보상 등)
            if (canGrantRCoin)
            {
                EquipmentDataModel duplicateData = EquipmentDataManager.Instance.GetEquipmentData(newInstance.equipmentID);
                int rCoinAmount = GrantRCoinForDuplicate(duplicateData.rarity);
                
                GameDataManager.Instance.SavePlayerData(currentPlayerData);
                OnPlayerDataUpdated?.Invoke();
                
                // RCoin 지급 후, 지급된 양을 반환하며 메소드를 즉시 종료합니다.
                return rCoinAmount;
            }
            // 1-2. RCoin 지급이 불가능한 경우 (상점 뽑기)
            else
            {
                // Debug.Log($"[EquipmentManager] 상점 중복 획득: {newInstance.equipmentData.equipmentName}. RCoin으로 변환되지 않습니다.");
                // 아이템을 추가하지 않고, 0을 반환하며 메소드를 즉시 종료합니다.
                return 0;
            }
        }

        // 2. 중복이 아닌 '신규' 장비인 경우에만 이 코드가 실행됩니다.
        currentPlayerData.ownedEquipmentInstances.Add(newInstance);
        
        currentPlayerData.ownedEquipmentInstances = currentPlayerData.ownedEquipmentInstances
            .OrderByDescending(instance => EquipmentDataManager.Instance.GetEquipmentData(instance.equipmentID).rarity)
            .ToList();
        
        GameDataManager.Instance.SavePlayerData(currentPlayerData);
        OnPlayerDataUpdated?.Invoke();
        OnEquipmentAcquired?.Invoke(EquipmentDataManager.Instance.GetEquipmentData(newInstance.equipmentID));
        
        return 0; // 새 장비가 추가되었으므로 0을 반환
    }

    // 장비 장착 (instanceID 사용)
    public void Equip(string instanceID)
    {
        PlayerEquipmentInstance instanceToEquip = GetEquipmentInstance(instanceID);
        if (instanceToEquip == null)
        {
            Debug.LogError($"[EquipmentManager] 존재하지 않는 장비 인스턴스 ID입니다: {instanceID}");
            return;
        }

        EquipmentDataModel baseData = EquipmentDataManager.Instance.GetEquipmentData(instanceToEquip.equipmentID);
        if (baseData == null)
        {
            Debug.LogError($"[EquipmentManager] 원본 EquipmentDataModel을 찾을 수 없습니다: {instanceToEquip.equipmentID}");
            return;
        }

        // 기존에 해당 슬롯에 장착된 장비가 있다면 해제
        EquipmentSlotEntry slotToUpdate = currentPlayerData.equippedSlots.FirstOrDefault(s => s.type == baseData.equipmentType);
        if (slotToUpdate != null)
        {
            if (slotToUpdate.instanceID != null)
            {
                Unequip(baseData.equipmentType);
            }
            slotToUpdate.instanceID = instanceID;
        }
        // Debug.Log($"{baseData.equipmentName} (인스턴스 ID: {instanceID})을(를) 장착했습니다.");

        // 장신구이면서 특수 스킬이 있을 경우 특수 스킬 활성화
        if (baseData.equipmentType == EquipmentType.Accessory 
            && !string.IsNullOrEmpty(baseData.specialSkillID) && baseData.specialSkillID != "")
        {
            // Debug.Log("[특수 스킬] 특수 스킬 활성화");
            SpecialSkillBase skillToActivate = Resources.Load<SpecialSkillBase>($"SpecialSkills/{baseData.specialSkillID}");

            if (skillToActivate != null)
            {
                // Equip 시에는 Unequip이 먼저 호출되므로, 기존 스킬에 대한 Deactivate/Event 호출은 Unequip에서 처리됨
                activeSpecialSkill = skillToActivate;
                activeSpecialSkill.Activate();
                OnEquipmentSkillEquipped?.Invoke(activeSpecialSkill); // 새 스킬 장착 이벤트 호출
            }
            else
            {
                Debug.LogWarning($"[EquipmentManager] SpecialSkillBase asset not found for ID: {baseData.specialSkillID}");
            }
        }

        CalculateAndApplyStats();
        GameDataManager.Instance.SavePlayerData(currentPlayerData);
        OnPlayerDataUpdated?.Invoke();
    }

    // 장비 해제
    public void Unequip(EquipmentType slot)
    {
        EquipmentSlotEntry slotToUnequip = currentPlayerData.equippedSlots.FirstOrDefault(s => s.type == slot);

        if (slotToUnequip == null || slotToUnequip.instanceID == null)
        {
            Debug.LogWarning($"[EquipmentManager] {slot} 슬롯은 이미 비어있습니다.");
            return;
        }

        string unequippedInstanceID = slotToUnequip.instanceID;
        slotToUnequip.instanceID = null;

        // 로그를 위한 장비 데이터 안전하게 가져오기
        PlayerEquipmentInstance unequippedInstance = GetEquipmentInstance(unequippedInstanceID);
        string equipmentNameForLog = "알 수 없는 장비";
        if (unequippedInstance != null)
        {
            EquipmentDataModel unequippedData = EquipmentDataManager.Instance.GetEquipmentData(unequippedInstance.equipmentID);
            if (unequippedData != null)
            {
                equipmentNameForLog = unequippedData.equipmentName;
            }
        }
        // Debug.Log($"{equipmentNameForLog} (인스턴스 ID: {unequippedInstanceID})을(를) 해제했습니다.");

        // 장신구인 경우 특수 스킬 비활성화 및 이벤트 호출
        if (slot == EquipmentType.Accessory && activeSpecialSkill != null)
        {
            SpecialSkillBase skillToUnequip = activeSpecialSkill;
            skillToUnequip.Deactivate();

            OnEquipmentSkillUnequipped?.Invoke(skillToUnequip); // 스킬 해제 이벤트 호출
            activeSpecialSkill = null;
        }

        CalculateAndApplyStats();
        GameDataManager.Instance.SavePlayerData(currentPlayerData);
        OnPlayerDataUpdated?.Invoke();
    }
    
    // 스탯 계산 및 적용
    private void CalculateAndApplyStats()
    {
        // 1. 장비로 인해 변동되는 스탯들을 기본값으로 초기화
        ResetEquipmentStats();

        // 2. 현재 장착된 모든 장비들을 가져옴
        var equippedInstances = currentPlayerData.equippedSlots
            .Where(slot => !string.IsNullOrEmpty(slot.instanceID))
            .Select(slot => GetEquipmentInstance(slot.instanceID));

        // 3. 각 장비의 스탯을 플레이어 데이터에 합산
        foreach (var instance in equippedInstances)
        {
            if (instance == null) continue;

            EquipmentDataModel equipmentData = EquipmentDataManager.Instance.GetEquipmentData(instance.equipmentID);
            if (equipmentData == null || equipmentData.statModifiers == null) continue;

            foreach (var modifier in equipmentData.statModifiers)
            {
                ApplyStatModifier(modifier);
            }
        }
        
        // Debug.Log("--- 스탯 재계산 완료 ---");
        // Debug.Log($"공격력 증가: {currentPlayerData.attackDamage}%, 공격속도 증가: {currentPlayerData.attackSpeed}%, 사거리 증가: {currentPlayerData.attackRange}%");
    }

    // 장비 스탯을 기본값(0)으로 리셋하는 함수
    private void ResetEquipmentStats()
    {
        // PlayerData에서 장비가 영향을 주는 스탯들만 초기화합니다.
        // 이 값들은 모두 곱연산 보너스(%)입니다.
        currentPlayerData.attackDamage = 0f;
        currentPlayerData.attackSpeed = 0f;
        currentPlayerData.attackRange = 0f;
        currentPlayerData.skillDamage = 0f;
        currentPlayerData.goldPerSecond = 0f; // 초당 골드는 합연산으로 유지
        
        // 타워 개별 데미지 증가량 초기화
        currentPlayerData.defaultTowerDamage = 0f;
        currentPlayerData.arrowTowerDamage = 0f;
        currentPlayerData.laserTowerDamage = 0f;
        currentPlayerData.priestsTowerDamage = 0f;
        currentPlayerData.spearTowerDamage = 0f;
        currentPlayerData.swordTowerDamage = 0f;

        // 기본 스탯들을 초기화
        currentPlayerData.maxHP = GameConstants.PLAYER_BASE_MAX_HP;
        currentPlayerData.startSP = GameConstants.PLAYER_BASE_START_SP;
        currentPlayerData.maxSP = GameConstants.PLAYER_BASE_MAX_SP;
        currentPlayerData.startGold = GameConstants.PLAYER_BASE_START_GOLD;
    }

    // 개별 스탯 모디파이어를 플레이어 데이터에 적용하는 함수
    private void ApplyStatModifier(StatModifierModel modifier)
    {
        
        switch (modifier.statType)
        {
            // --- 글로벌 스탯 ---
            case StatType.Global_AttackDamage_Mul:
                currentPlayerData.attackDamage += modifier.value;
                break;
            case StatType.Global_AttackSpeed_Mul:
                currentPlayerData.attackSpeed += modifier.value;
                break;
            case StatType.Global_AttackRange_Mul:
                currentPlayerData.attackRange += modifier.value;
                break;
            case StatType.Global_SkillDamage_Mul:
                currentPlayerData.skillDamage += modifier.value;
                break;
            
            // --- 타워 개별 스탯 ---
            case StatType.DefaultTower_Damage_Mul:
                currentPlayerData.defaultTowerDamage += modifier.value;
                break;
            case StatType.ArrowTower_Damage_Mul:
                currentPlayerData.arrowTowerDamage += modifier.value;
                break;
            case StatType.LaserTower_Damage_Mul:
                currentPlayerData.laserTowerDamage += modifier.value;
                break;
            case StatType.PriestsTower_Damage_Mul:
                currentPlayerData.priestsTowerDamage += modifier.value;
                break;
            case StatType.SpearTower_Damage_Mul:
                currentPlayerData.spearTowerDamage += modifier.value;
                break;
            case StatType.SwordTower_Damage_Mul:
                currentPlayerData.swordTowerDamage += modifier.value;
                break;

            // --- 플레이어 부가 스탯 (합연산 유지) ---
            case StatType.Player_GoldPerSecond:
                currentPlayerData.goldPerSecond += modifier.value;
                break;
            case StatType.Player_StartGold:
                currentPlayerData.startGold += modifier.value;
                break;
            case StatType.Player_MaxHP:
                currentPlayerData.maxHP += modifier.value;
                break;
        }
    }

    // --- 유효 스탯 반환 메서드들 ---

    public float GetEffectiveAttackDamage()
    {
        float total = currentPlayerData.attackDamage;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.Global_AttackDamage_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveAttackSpeed()
    {
        float total = currentPlayerData.attackSpeed;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.Global_AttackSpeed_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveAttackRange()
    {
        float total = currentPlayerData.attackRange;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.Global_AttackRange_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveSkillDamage()
    {
        float total = currentPlayerData.skillDamage;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.Global_SkillDamage_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveDefaultTowerDamage()
    {
        float total = currentPlayerData.defaultTowerDamage;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.DefaultTower_Damage_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveArrowTowerDamage()
    {
        float total = currentPlayerData.arrowTowerDamage;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.ArrowTower_Damage_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveLaserTowerDamage()
    {
        float total = currentPlayerData.laserTowerDamage;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.LaserTower_Damage_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectivePriestsTowerDamage()
    {
        float total = currentPlayerData.priestsTowerDamage;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.PriestsTower_Damage_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveSpearTowerDamage()
    {
        float total = currentPlayerData.spearTowerDamage;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.SpearTower_Damage_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveSwordTowerDamage()
    {
        float total = currentPlayerData.swordTowerDamage;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.SwordTower_Damage_Mul)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveMaxHP()
    {
        float total = currentPlayerData.maxHP;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.Player_MaxHP)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveStartGold()
    {
        float total = currentPlayerData.startGold;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.Player_StartGold)
            {
                total += modifier.value;
            }
        }
        return total;
    }

    public float GetEffectiveGoldPerSecond()
    {
        float total = currentPlayerData.goldPerSecond;
        foreach (var modifier in temporaryStatModifiers)
        {
            if (modifier.statType == StatType.Player_GoldPerSecond)
            {
                total += modifier.value;
            }
        }
        return total;
    }
    
    // 인스턴스 ID로 플레이어 장비 인스턴스 가져오기
    public PlayerEquipmentInstance GetEquipmentInstance(string instanceID)
    {
        return currentPlayerData.ownedEquipmentInstances.FirstOrDefault(e => e.instanceID == instanceID);
    }

    /// <summary>
    /// EquipmentDataModel을 기반으로 PlayerEquipmentInstance를 생성하고 스탯을 무작위화합니다.
    /// </summary>
    /// <param name="baseEquipment">원본 EquipmentDataModel</param>
    /// <returns>무작위화된 스탯을 가진 PlayerEquipmentInstance</returns>
    public PlayerEquipmentInstance CreateRandomizedEquipmentInstance(EquipmentDataModel baseEquipment)
    {
        // 고유한 인스턴스 ID 생성 (GUID 사용)
        string instanceID = System.Guid.NewGuid().ToString();

        // PlayerEquipmentInstance 생성 (statModifiers는 포함하지 않음)
        return new PlayerEquipmentInstance(instanceID, baseEquipment.equipmentID, baseEquipment);
    }

    /// <summary>
    /// 임시 스탯 모디파이어를 추가합니다.
    /// </summary>
    /// <param name="modifier">추가할 StatModifierModel</param>
    public void AddTemporaryStatModifier(StatModifierModel modifier)
    {
        temporaryStatModifiers.Add(modifier);
        OnPlayerDataUpdated?.Invoke(); // UI 업데이트를 위해 이벤트 발생
    }

    /// <summary>
    /// 임시 스탯 모디파이어를 제거합니다.
    /// </summary>
    /// <param name="modifier">제거할 StatModifierModel</param>
    public void RemoveTemporaryStatModifier(StatModifierModel modifier)
    {
        temporaryStatModifiers.Remove(modifier);
        OnPlayerDataUpdated?.Invoke(); // UI 업데이트를 위해 이벤트 발생
    }
    
    // 등급에 따라 RCoin을 지급하는 새로운 헬퍼 메소드
    private int GrantRCoinForDuplicate(Rarity rarity)
    {
        int amount = 0;
        // [수정] 요청하신 새로운 등급별 RCoin 지급량을 적용합니다.
        switch (rarity)
        {
            case Rarity.Common:
                amount = 10000;
                break;
            case Rarity.Uncommon: // Uncommon 등급 추가
                amount = 20000;
                break;
            case Rarity.Rare:
                amount = 50000;
                break;
            case Rarity.Epic:
                amount = 100000;
                break;
            case Rarity.Legendary:
                amount = 200000;
                break;
        }

        if (amount > 0)
        {
            // PlayerData의 rCoin 값을 직접 증가시킵니다.
            currentPlayerData.rCoin += amount;
            // Debug.Log($"지급된 RCoin: +{amount} / 현재 총 RCoin: {currentPlayerData.rCoin}");

            // UI 알림을 위해 UIEventManager의 static 이벤트를 호출합니다.
            UIEventManager.NotifyRCoinAcquired(amount);
        }
    
        // 지급된 RCoin 양을 반환합니다.
        return amount;
    }
}
