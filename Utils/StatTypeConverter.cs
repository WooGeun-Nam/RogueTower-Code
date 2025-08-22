using System.Collections.Generic;

public static class StatTypeConverter
{
    private static readonly Dictionary<StatType, string> StatNames = new Dictionary<StatType, string>
    {
        { StatType.Global_AttackDamage_Mul, "공격력(%)" },
        { StatType.Global_AttackSpeed_Mul, "공격속도(%)" },
        { StatType.Global_AttackRange_Mul, "사거리(%)" },

        { StatType.DefaultTower_Damage_Mul, "기본(%)" },
        { StatType.ArrowTower_Damage_Mul, "궁수(%)" },
        { StatType.LaserTower_Damage_Mul, "마법사(%)" },
        { StatType.PriestsTower_Damage_Mul, "사제(%)" },
        { StatType.SpearTower_Damage_Mul, "창병(%)" },
        { StatType.SwordTower_Damage_Mul, "검사(%)" },

        { StatType.Global_SkillDamage_Mul, "스킬공격력(%)" },

        { StatType.Player_StartGold, "시작 골드" },
        { StatType.Player_GoldPerSecond, "초당 골드 수급량" },
        { StatType.Player_MaxHP, "최대 체력" }
    };

    public static string GetStatName(StatType statType)
    {
        if (StatNames.TryGetValue(statType, out string name))
        {
            return name;
        }
        return statType.ToString(); // 매핑되지 않은 경우 enum 이름 그대로 반환
    }

    public static string FormatStatValue(StatType statType, float value)
    {
        // 특정 스탯 타입에 따라 포맷팅을 다르게 할 수 있습니다.
        // 곱연산 스탯은 %로 표시
        switch (statType)
        {
            case StatType.Global_AttackDamage_Mul:
            case StatType.Global_AttackSpeed_Mul:
            case StatType.Global_AttackRange_Mul:
            case StatType.DefaultTower_Damage_Mul:
            case StatType.ArrowTower_Damage_Mul:
            case StatType.LaserTower_Damage_Mul:
            case StatType.PriestsTower_Damage_Mul:
            case StatType.SpearTower_Damage_Mul:
            case StatType.SwordTower_Damage_Mul:
            case StatType.Global_SkillDamage_Mul:
                return value.ToString("F1"); // 소수점 첫째 자리까지
            case StatType.Player_StartGold:
            case StatType.Player_GoldPerSecond:
            case StatType.Player_MaxHP:
                return value.ToString("F0"); // 정수로 표시
            default:
                return value.ToString("F0"); // 기본 정수 표시
        }
    }
}
