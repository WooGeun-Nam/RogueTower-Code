using System.Collections.Generic;
using UnityEngine;

// 장비의 종류를 정의하는 Enum
public enum EquipmentType
{
    Helmet,     // 투구
    Armor,      // 상의
    Gloves,     // 장갑
    Boots,      // 신발
    Weapon,     // 무기
    Accessory   // 장신구
}

// 장비의 등급을 정의하는 Enum
public enum Rarity
{
    Common,     // 일반
    Uncommon,   // 고급
    Rare,       // 희귀
    Epic,       // 서사
    Legendary   // 전설
}

// 스탯의 종류를 정의하는 Enum
public enum StatType
{
    // 글로벌 스탯 (모든 타워 적용)
    Global_AttackDamage_Mul,        // 공격력 (곱연산, %)
    Global_AttackSpeed_Mul,         // 공격속도 (%)
    Global_AttackRange_Mul,         // 사거리 (%)

    // 특정 타워 강화 스탯
    DefaultTower_Damage_Mul,        // 기본 타워 공격력 (%)
    ArrowTower_Damage_Mul,          // 화살 타워 공격력 (%)
    LaserTower_Damage_Mul,          // 레이저 타워 공격력 (%)
    PriestsTower_Damage_Mul,        // 사제 타워 공격력 (%)
    SpearTower_Damage_Mul,          // 창 타워 공격력 (%)
    SwordTower_Damage_Mul,          // 검 타워 공격력 (%)

    // 스킬 강화 스탯
    Global_SkillDamage_Mul,         // 모든 스킬 데미지 (%)

    // 플레이어 스탯
    Player_StartGold,               // 시작 골드
    Player_GoldPerSecond,           // 초당 골드 수급량
    Player_MaxHP,                   // 최대 체력
}

// 스탯 적용 방식을 정의하는 Enum
public enum ModifierType
{
    Additive,       // 합연산
    Multiplicative  // 곱연산
}

[CreateAssetMenu(fileName = "NewEquipmentData", menuName = "Data/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [Header("기본 정보")]
    public string equipmentID;          // 장비 고유 ID
    public string equipmentName;        // 장비 이름
    public EquipmentType equipmentType; // 장비 종류
    public Rarity rarity;               // 장비 등급
    public Sprite icon;                 // 장비 아이콘
    [TextArea]
    public string description;          // 장비 설명

    [Header("스탯 정보")]
    public List<StatModifier> statModifiers; // 이 장비가 제공하는 스탯 목록

    [Header("특수 스킬 정보")]
    public SpecialSkillBase specialSkillTemplate;
}
