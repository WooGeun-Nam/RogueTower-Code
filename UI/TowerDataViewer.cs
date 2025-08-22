using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerDefense.Enums;

/// <summary>
/// 타워의 상세 정보를 UI에 표시하고 관리하는 클래스
/// </summary>
public class TowerDataViewer : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("타워 이미지")]
    public Image imageTower;
    [Tooltip("타워 데미지 텍스트")]
    public TextMeshProUGUI textDamage;
    [Tooltip("타워 공격 속도 텍스트")]
    public TextMeshProUGUI textRate;
    [Tooltip("타워 공격 범위 텍스트")]
    public TextMeshProUGUI textRange;
    [Tooltip("타워 데미지 타입 텍스트")]
    public TextMeshProUGUI textType;
    [Tooltip("타워 공격 범위 표시 오브젝트")]
    public TowerAttackRange towerAttackRange;

    [Tooltip("타워 판매 버튼")]
    public Button buttonSell;
    [Tooltip("타워 판매 비용 텍스트")]
    public TextMeshProUGUI textSellCost;

    private TowerWeapon currentTower; // 현재 선택된 타워의 TowerWeapon 컴포넌트

    private void Awake()
    {
        OffPanel(); // 시작 시 패널 비활성화
    }
    
    private void Update()
    {
        // ESC 키를 누르면 패널 비활성화
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OffPanel();
        }
    }

    /// <summary>
    /// 타워 정보 패널을 활성화하고 선택된 타워의 정보를 표시합니다.
    /// </summary>
    /// <param name="towerWeapon">선택된 타워의 Transform</param>
    public void OnPanel(Transform towerWeapon)
    {
        currentTower = towerWeapon.GetComponent<TowerWeapon>(); // 선택된 타워의 TowerWeapon 컴포넌트 저장
        gameObject.SetActive(true); // 타워 정보 패널 활성화
        UpdateTowerData(); // 타워 정보 갱신
        towerAttackRange.ONAttackRange(currentTower.transform.position, currentTower.Range); // 타워 공격 범위 표시
    }

    /// <summary>
    /// 타워 정보 패널을 비활성화하고 공격 범위 표시를 숨깁니다.
    /// </summary>
    public void OffPanel()
    {
        gameObject.SetActive(false); // 타워 정보 패널 비활성화
        towerAttackRange.OffAttackRange(); // 타워 공격 범위 숨기기
    }

    /// <summary>
    /// 현재 선택된 타워의 정보를 UI에 업데이트합니다.
    /// </summary>
    public void UpdateTowerData()
    {
        if(currentTower == null) return; // 현재 선택된 타워가 없으면 종료

        // 타워 이미지 및 색상 업데이트
        imageTower.sprite = currentTower.TowerSprite;
        imageTower.color = currentTower.TowerColor;

        // 데미지, 공격 속도, 공격 범위 텍스트 업데이트
        string damageText = $"데미지 : {Mathf.FloorToInt(currentTower.Damage)}";
        
        // 업그레이드 증가 데미지
        float upgradeDamage = currentTower.AddedDamage;
        if (upgradeDamage > 0)
        {
            damageText += $"(<color=red>+{upgradeDamage:F0}</color>";
        }

        // 장비로 증가한 데미지
        float basePlusUpgradePlusBuffDamage = currentTower.towerArchetype.towerWeaponStats.damage + upgradeDamage + currentTower.BuffDamage;
        float equipmentDamageBonus = currentTower.Damage - basePlusUpgradePlusBuffDamage;

        if (equipmentDamageBonus > 0)
        {
            if (upgradeDamage <= 0) damageText += " ("; // 업그레이드 데미지가 없으면 괄호 시작
            damageText += $"<color=green>+{equipmentDamageBonus:F0}</color>";
        }

        // 버프 데미지
        if (currentTower.BuffDamage > 0)
        {
            if (upgradeDamage <= 0 && equipmentDamageBonus <= 0) damageText += " ("; // 업그레이드, 장비 데미지가 없으면 괄호 시작
            damageText += $"<color=blue>+{currentTower.BuffDamage:F0}</color>";
        }

        // 괄호 닫기
        if (upgradeDamage > 0 || equipmentDamageBonus > 0 || currentTower.BuffDamage > 0)
        {
            damageText += ")";
        }
        
        textDamage.text = damageText;
        if (currentTower.AttackSpeed == 0f)
        {
            textRate.text = $"공격 속도 : 지속\n";
        }
        else
        {
            textRate.text = $"공격 속도 : {currentTower.AttackSpeed:F1}\n";
        }
        
        textRange.text = $"공격 범위 : {currentTower.Range:F1}\n";

        // 데미지 타입 텍스트 업데이트
        if(currentTower.damageType == DamageType.PhysicalType)
        {
            textType.text = "데미지 타입 : 물리";
        }
        else if(currentTower.damageType == DamageType.MagicalType)
        {
            textType.text = "데미지 타입 : 마법";
        }
        else
        {
            textType.text = "데미지 타입 : 복합";
        }

        // 판매 비용 텍스트 업데이트
        textSellCost.text = currentTower.SellCost.ToString();
    }

    /// <summary>
    /// 타워 판매 버튼 클릭 시 호출됩니다.
    /// </summary>
    public void OnClickEventTowerSell()
    {
        currentTower.Sell(); // 현재 타워 판매
        OffPanel(); // 패널 비활성화
    }
}