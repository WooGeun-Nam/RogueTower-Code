using UnityEngine;

/// <summary>
/// 타워의 기본 정보와 무기 스탯을 정의하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewTowerTemplate", menuName = "Tower Defense/Tower Template")]
public class TowerTemplate : ScriptableObject
{
    [Tooltip("이 템플릿에 해당하는 타워 프리팹")]
    public GameObject towerPrefab;

    [Tooltip("타워 설명")]
    public string description;

    [Tooltip("타워의 무기 관련 스탯")]
    public Weapon weapon;

    [Tooltip("물리 업그레이드 계수")]
    public UpgradeCoefficient physicUpgradeCoefficient;
    [Tooltip("마법 업그레이드 계수")]
    public UpgradeCoefficient magicUpgradeCoefficient;

    /// <summary>
    /// 타워 무기의 상세 스탯을 정의하는 구조체
    /// </summary>
    [System.Serializable]
    public struct Weapon
    {
        [Tooltip("무기의 기본 공격력")]
        public float damage;
        [Tooltip("물리 방어력 관통 수치")]
        public float physicPenetrate;
        [Tooltip("마법 방어력 관통 수치")]
        public float magicPenetrate;
        [Tooltip("공격 속도 (높을수록 빠름)")]
        public float attackSpeed;
        [Tooltip("공격 범위")]
        public float range;
        [Tooltip("타워 건설 비용 (스킬 포인트 또는 골드)")]
        public int cost; // 기존 cost 필드 유지
        [Tooltip("스킬 타워의 경우, 해당 스킬의 인덱스")]
        public int skillIndex;
    }

    /// <summary>
    /// 업그레이드 계수를 정의하는 구조체
    /// </summary>
    [System.Serializable]
    public struct UpgradeCoefficient
    {
        [Tooltip("업그레이드 시 증가하는 데미지 계수 (선형)")]
        public float damage;
        [Tooltip("업그레이드 시 증가하는 데미지 계수 (제곱)")]
        public float damageQuadratic;

        [Tooltip("업그레이드 시 증가하는 관통력 계수 (선형)")]
        public float penetrate;
        [Tooltip("업그레이드 시 증가하는 관통력 계수 (제곱)")]
        public float penetrateQuadratic;
    }
}