using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워와 그에 연관된 스킬 정보를 포함하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewTowerArchetype", menuName = "Tower Defense/Tower Archetype")]
public class TowerArchetype : ScriptableObject
{
    [Header("General Info")]
    public string archetypeID; // 아키타입 고유 ID
    public string archetypeDisplayName; // UI에 표시될 이름
    [TextArea(3, 10)]
    public string archetypeDescription; // 아키타입 전체 설명

    [Header("Tower Data")]
    [Tooltip("타워 아이콘")]
    public Sprite towerIcon;
    [Tooltip("이 아키타입에 해당하는 타워 프리팹")]
    public GameObject towerPrefab;
    [Tooltip("타워의 무기 관련 스탯")]
    public TowerTemplate.Weapon towerWeaponStats; // TowerTemplate의 Weapon 구조체 재사용
    [Tooltip("업그레이드 계수")]
    public TowerTemplate.UpgradeCoefficient upgradeCoefficient;
    [Tooltip("타워의 데미지 타입")]
    public DamageType towerDamageType; // TowerDefense.Enums.DamageType
    [Tooltip("타워가 다중 공격 타워인지 여부")]
    public bool isMultiTargetTower;

    [Header("Associated Skill Data")]
    [Tooltip("스킬 아이콘")]
    public Sprite skillIcon;
    [Tooltip("이 타워 아키타입에 연관된 스킬 프리팹")]
    public GameObject skillPrefab;
    [Tooltip("스킬 타입 (타워, 액티브, 패시브)")]
    public SkillManager.SkillType skillType; // SkillManager의 SkillType 열거형 재사용
    [Tooltip("스킬 지속 시간")]
    public float skillDuration;
    [Tooltip("스킬 범위")]
    public float skillRange;
    [Tooltip("스킬 사용에 필요한 스킬 포인트")]
    public int skillPointCost;
    [Tooltip("스킬 이름")]
    public string skillDisplayName;
    [TextArea(3, 10)]
    public string skillDescription; // 스킬 설명

    /// <summary>
    /// 현재 플레이어 스탯을 반영한 타워의 상세 설명을 반환합니다.
    /// </summary>
    public string GetFormattedTowerDescription(PlayerUpgrade playerUpgrade, PlayerData playerData)
    {
        // TowerWeapon.cs의 Damage 프로퍼티 계산 로직을 여기에 통합
        // 기본 데미지
        float baseDamage = towerWeaponStats.damage;
        string damageText = $"데미지 : {Mathf.FloorToInt(baseDamage)}";

        // 업그레이드 증가 데미지
        float upgradeDamage = GetUpgradeDamage(towerDamageType, playerUpgrade);
        if (upgradeDamage > 0)
        {
            damageText += $"(<color=red>+{upgradeDamage:F0}</color>";
        }

        // 장비로 증가한 데미지 (TowerWeapon의 Damage 프로퍼티 로직 참고)
        float currentTowerDamage = (baseDamage + upgradeDamage) * (1 + playerData.attackDamage / 100f);
        
        float specificTowerDamageBonus = 0f;
        // TowerArchetype의 ID나 이름을 기반으로 특정 타워 데미지 보너스 가져오기
        if (archetypeID.Contains("Default")) specificTowerDamageBonus = playerData.defaultTowerDamage;
        else if (archetypeID.Contains("Arrow")) specificTowerDamageBonus = playerData.arrowTowerDamage;
        else if (archetypeID.Contains("Laser")) specificTowerDamageBonus = playerData.laserTowerDamage;
        else if (archetypeID.Contains("Priests")) specificTowerDamageBonus = playerData.priestsTowerDamage;
        else if (archetypeID.Contains("Spear")) specificTowerDamageBonus = playerData.spearTowerDamage;
        else if (archetypeID.Contains("Sword")) specificTowerDamageBonus = playerData.swordTowerDamage;

        currentTowerDamage *= (1 + specificTowerDamageBonus / 100f);

        // 버프 데미지는 현재 TowerWeapon에서 직접 관리되므로, 여기서는 포함하지 않음.
        // 만약 버프 데미지도 툴팁에 포함하려면 GameManager 등에서 버프 정보를 가져와야 함.

        float equipmentDamageBonus = currentTowerDamage - (baseDamage + upgradeDamage); // 순수 장비로 인한 증가량

        if (equipmentDamageBonus > 0)
        {
            if (upgradeDamage <= 0) damageText += " ("; // 업그레이드 데미지가 없으면 괄호 시작
            damageText += "<color=green>+" + Mathf.FloorToInt(equipmentDamageBonus) + "</color>";
        }

        // 버프 데미지는 현재 TowerWeapon에서 직접 관리되므로, 여기서는 포함하지 않음.
        // 툴팁에 포함하려면 GameManager 등에서 버프 정보를 가져와야 함.
        // if (currentTower.BuffDamage > 0) { ... }

        // 괄호 닫기
        if (upgradeDamage > 0 || equipmentDamageBonus > 0)
        {
            damageText += ")";
        }

        string content = damageText + "\n";
        if (towerWeaponStats.attackSpeed == 0f)
        {
            content += $"공격 속도: 지속\n";
        }
        else
        {
            content += $"공격 속도: {towerWeaponStats.attackSpeed * (1 + playerData.attackSpeed / 100f):F1}\n";
        }
        
        content += $"공격 범위: {towerWeaponStats.range * (1 + playerData.attackRange / 100f):F1}\n";
        content += $"타입: {GetDamageTypeString(towerDamageType)}";
        content += $"\n{archetypeDescription}"; // 아키타입 전체 설명

        return content;
    }

    /// <summary>
    /// 현재 플레이어 스탯을 반영한 스킬의 상세 설명을 반환합니다.
    /// </summary>
    public string GetFormattedSkillDescription(PlayerUpgrade playerUpgrade, PlayerData playerData)
    {
        // string content = $"스킬 포인트 소모: {skillPointCost}\n";
        string content = $"지속 시간: {skillDuration:F1}초\n";

        if (skillRange == 0)
        {
            content += $"범위: (적 존재 시)즉시 발동\n";
        }
        else
        {
            content += $"범위: {skillRange:F1}\n";
        }
        content += $"\n{skillDescription}";

        // 스킬 데미지 등 플레이어 스탯에 영향을 받는 부분이 있다면 여기에 추가
        // 예: 스킬 데미지: {baseSkillDamage * (1 + playerData.skillDamage / 100f)}

        return content;
    }

    // DamageType을 문자열로 변환하는 헬퍼 함수 (TowerWeapon.cs에서 복사)
    private string GetDamageTypeString(DamageType type)
    {
        switch (type)
        {
            case DamageType.PhysicalType: return "물리";
            case DamageType.MagicalType: return "마법";
            case DamageType.HybridType: return "복합";
            default: return "알 수 없음";
        }
    }

    private float GetUpgradeDamage(DamageType damageType, PlayerUpgrade playerUpgrade)
    {
        float addedDamage = 0f;
        var coefficient = upgradeCoefficient;

        switch (damageType)
        {
            case DamageType.PhysicalType:
                // 제곱 성장(damageQuadratic) 공식을 추가합니다.
                addedDamage = (coefficient.damage * playerUpgrade.PhysicUpgrade) +
                              (coefficient.damageQuadratic * playerUpgrade.PhysicUpgrade * playerUpgrade.PhysicUpgrade);
                break;

            case DamageType.MagicalType:
                // 제곱 성장(damageQuadratic) 공식을 추가합니다.
                addedDamage = (coefficient.damage * playerUpgrade.MagicUpgrade) +
                              (coefficient.damageQuadratic * playerUpgrade.MagicUpgrade * playerUpgrade.MagicUpgrade);
                break;

            case DamageType.HybridType:
                // 하이브리드 타입에도 제곱 성장 공식을 적용합니다.
                float totalPhysicDmg = (coefficient.damage * playerUpgrade.PhysicUpgrade) +
                                       (coefficient.damageQuadratic * playerUpgrade.PhysicUpgrade * playerUpgrade.PhysicUpgrade);
                float totalMagicDmg = (coefficient.damage * playerUpgrade.MagicUpgrade) +
                                      (coefficient.damageQuadratic * playerUpgrade.MagicUpgrade * playerUpgrade.MagicUpgrade);

                addedDamage = (totalPhysicDmg + totalMagicDmg) * 0.6f;
                break;
        }
        return addedDamage;
    }
}