using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponState { SearchTarget = 0, TryAttack }
public enum DamageType { PhysicalType = 0, MagicalType, HybridType }
public enum AttackTimingType { Projectile, Instant, Continuous }
public abstract class TowerWeapon : MonoBehaviour
{
    [Header("Commons")]
    [Tooltip("타워의 기본 정보 (ScriptableObject)")]
    public TowerArchetype towerArchetype;
    [Tooltip("타워의 공격 타입 (물리 또는 마법)")]
    public DamageType damageType;

    protected int level = 0; // 타워 레벨
    protected IWeaponState currentState; // 현재 타워 무기의 상태
    public Enemy attackTarget = null; // 공격 대상
    public List<Enemy> attackMultiTarget = new(); // 다중 공격 대상 리스트
    protected TowerSpawner towerSpawner; // 타워 스포너 참조
    protected EnemySpawner enemySpawner; // 적 스포너 참조
    protected Animator animator; // 타워 애니메이터 참조
    public bool isMultiTargetTower = false; // 다중 공격 타워인지 여부
    public AttackTimingType attackTimingType; // 공격 타이밍 타입

    [Header("Animation Settings")]
    [Tooltip("지속 공격 타워인지 여부 (레이저 타워 등)")]
    public bool isContinuousAttack = false;
    public bool animationEventTriggered = false; // 애니메이션 이벤트 발생 여부 플래그

    protected Coroutine currentCoroutine; // 현재 실행 중인 코루틴 참조

    // 히트 이펙트 오브젝트 풀 관리를 위한 딕셔너리
    private Dictionary<GameObject, ObjectPool<GameObject>> hitEffectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();

    protected float damage; // 기본 데미지
    protected float physic; // 물리 방어력 관통
    protected float magic;  // 마법 방어력 관통

    

    private float addedDamage; // 업그레이드에 따른 추가 데미지
    private float buffDamage;  // 버프에 따른 추가 데미지
    
    private PlayerTowerCost playerTowerCost;
    
    private Tile ownerTile; // 타워가 건설된 타일

    private int sellCost; // 판매 비용
    protected int physicUpgradeLevel; // 물리 업그레이드 레벨
    protected int magicUpgradeLevel;  // 마법 업그레이드 레벨
    
    [Header("특수 능력: 체력 비례 데미지")]
    protected bool dealsPercentHealthDamage; // 이 능력이 활성화되었는지 여부
    protected float percentHealthDamageAmount; // 최대 체력의 몇 %만큼 데미지를 줄지 (예: 0.01f = 1%)
    
    /// <summary>
    /// 타워의 물리 업그레이드 레벨
    /// </summary>
    public int PhysicUpgradeLevel
    {
        get { return physicUpgradeLevel; }
        set { physicUpgradeLevel = value; }
    }

    /// <summary>
    /// 타워의 마법 업그레이드 레벨
    /// </summary>
    public int MagicUpgradeLevel { get; set; }

    /// <summary>
    /// 타워의 스프라이트
    /// </summary>
    public Sprite TowerSprite => gameObject.GetComponent<SpriteRenderer>().sprite;
    /// <summary>
    /// 타워의 색상
    /// </summary>
    public Color TowerColor => gameObject.GetComponent<SpriteRenderer>().color;
    /// <summary>
    /// 타워의 현재 데미지
    /// </summary>
    public float Damage 
    {
        get
        {
            float baseDmg = damage + addedDamage + buffDamage;
            float globalMultiplier = 1 + (EquipmentManager.Instance.GetEffectiveAttackDamage() / 100f);
            float specificMultiplier = 1 + (GetSpecificTowerDamage() / 100f);
            float finalCalculatedDamage = baseDmg * globalMultiplier * specificMultiplier;
            // '희생 전략' 특성의 공격력 보너스를 적용합니다.
            if (PerkManager.Instance != null && PerkManager.Instance.perk_isSacrificeEnabled)
            {
                finalCalculatedDamage *= (1 + PerkManager.Instance.perk_sacrificeDamageBonus);
            }
            return finalCalculatedDamage;
        }
    }
    /// <summary>
    /// 타워의 공격 속도
    /// </summary>
    public float AttackSpeed => towerArchetype.towerWeaponStats.attackSpeed * (1 + EquipmentManager.Instance.GetEffectiveAttackSpeed() / 100f);
    /// <summary>
    /// 타워의 공격 범위
    /// </summary>
    public float Range => towerArchetype.towerWeaponStats.range * (1 + EquipmentManager.Instance.GetEffectiveAttackRange() / 100f);
    /// <summary>
    /// 타워가 판매되었는지 여부
    /// </summary>
    public bool IsSell { protected set; get; } = false;
    /// <summary>
    /// 타워의 판매 비용
    /// </summary>
    public int SellCost
    {
        get
        {
            if (GameManager.isTowerSellCostFixed)
            {
                return towerArchetype.towerWeaponStats.cost; // 구매 비용과 동일하게 설정
            }
            return sellCost;
        }
    }
    /// <summary>
    /// 타워가 건설된 타일
    /// </summary>
    public Tile OwnerTile => ownerTile;

    /// <summary>
    /// 타워에 추가되는 데미지 (버프 등)
    /// </summary>
    public float AddedDamage
    {
        set => addedDamage = Mathf.Max(0, value);
        get => addedDamage;
    }

    public float BuffDamage
    {
        set => buffDamage = Mathf.Max(0, value);
        get => buffDamage;
    }

    private float GetSpecificTowerDamage()
    {
        switch (this)
        {
            case WeaponDefault:
                return EquipmentManager.Instance.GetEffectiveDefaultTowerDamage();
            case WeaponArrow:
                return EquipmentManager.Instance.GetEffectiveArrowTowerDamage();
            case WeaponLaser:
                return EquipmentManager.Instance.GetEffectiveLaserTowerDamage();
            case WeaponPriests:
                return EquipmentManager.Instance.GetEffectivePriestsTowerDamage();
            case WeaponSpear:
                return EquipmentManager.Instance.GetEffectiveSpearTowerDamage();
            case WeaponSword:
                return EquipmentManager.Instance.GetEffectiveSwordTowerDamage();
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 타워 무기를 초기 설정합니다.
    /// </summary>
    /// <param name="towerSpawner">타워 스포너 참조</param>
    /// <param name="enemySpawner">적 스포너 참조</param>
    /// <param name="ownerTile">타워가 건설된 타일</param>
    /// <param name="upgrade">업그레이드 레벨</param>
    public virtual void Setup(TowerArchetype towerArchetype, TowerSpawner towerSpawner, EnemySpawner enemySpawner, 
                                 PlayerTowerCost playerTowerCost, Tile ownerTile, int physicUpgradeLevel, int magicUpgradeLevel)
    {
        // [추가] towerArchetype 유효성 검사
        if (towerArchetype == null)
        {
            Debug.LogError($"[TowerWeapon] Critical Error: towerArchetype is not assigned on {gameObject.name}. Tower setup cannot proceed.");
            return; // towerArchetype이 없으면 더 이상 진행하지 않음
        }
        
        this.towerArchetype = towerArchetype;
        this.damage = towerArchetype.towerWeaponStats.damage;
        this.AddedDamage = 0f;
        this.buffDamage = 0f;
        this.physic = towerArchetype.towerWeaponStats.physicPenetrate;
        this.magic = towerArchetype.towerWeaponStats.magicPenetrate;
        this.towerSpawner = towerSpawner;
        this.enemySpawner = enemySpawner;
        this.playerTowerCost = playerTowerCost;
        this.ownerTile = ownerTile;
        this.physicUpgradeLevel = physicUpgradeLevel;
        this.magicUpgradeLevel = magicUpgradeLevel;
        this.sellCost = towerArchetype.towerWeaponStats.cost - (towerArchetype.towerWeaponStats.cost / 3);
        this.isMultiTargetTower = towerArchetype.isMultiTargetTower;
        
        UpgradeDamage(); // 업그레이드에 따른 데미지 적용

        // 타워의 종류에 따라 초기 상태를 다르게 설정
        if (isMultiTargetTower)
        {
            ChangeState(new SearchMultiTargetState()); // 다중 공격 타워는 SearchMultiTargetState로 시작
        }
        else
        {
            ChangeState(new SearchTargetState()); // 단일 공격 타워는 SearchTargetState로 시작
        }
        animator = GetComponent<Animator>(); // Animator 컴포넌트 초기화
    }

    

    /// <summary>
    /// 업그레이드에 따라 타워의 데미지를 업데이트합니다.
    /// </summary>
    public virtual void UpgradeDamage()
    {
        // 기본 데미지와 관통력은 towerArchetype에서 가져온 초기값으로 설정
        damage = towerArchetype.towerWeaponStats.damage;
        physic = towerArchetype.towerWeaponStats.physicPenetrate;
        magic = towerArchetype.towerWeaponStats.magicPenetrate;
        AddedDamage = 0f; // AddedDamage 초기화

        // 모든 계산은 통합된 'upgradeCoefficient'를 사용합니다.
        var coefficient = towerArchetype.upgradeCoefficient;

        switch (damageType)
        {
            case DamageType.PhysicalType:
                // 물리 업그레이드 레벨(PhysicUpgradeLevel)을 공용 계수에 적용
                AddedDamage += (coefficient.damage * PhysicUpgradeLevel) + (coefficient.damageQuadratic * PhysicUpgradeLevel * PhysicUpgradeLevel);
                physic += (coefficient.penetrate * PhysicUpgradeLevel) + (coefficient.penetrateQuadratic * PhysicUpgradeLevel * PhysicUpgradeLevel);
                break;

            case DamageType.MagicalType:
                // 마법 업그레이드 레벨(MagicUpgradeLevel)을 공용 계수에 적용
                AddedDamage += (coefficient.damage * MagicUpgradeLevel) + (coefficient.damageQuadratic * MagicUpgradeLevel * MagicUpgradeLevel);
                magic += (coefficient.penetrate * MagicUpgradeLevel) + (coefficient.penetrateQuadratic * MagicUpgradeLevel * MagicUpgradeLevel);
                break;

            case DamageType.HybridType:
                // 물리 부분 계산
                float totalPhysicDmg = (coefficient.damage * PhysicUpgradeLevel) + (coefficient.damageQuadratic * PhysicUpgradeLevel * PhysicUpgradeLevel);
                physic += (coefficient.penetrate * PhysicUpgradeLevel) + (coefficient.penetrateQuadratic * PhysicUpgradeLevel * PhysicUpgradeLevel);

                // 마법 부분 계산
                float totalMagicDmg = (coefficient.damage * MagicUpgradeLevel) + (coefficient.damageQuadratic * MagicUpgradeLevel * MagicUpgradeLevel);
                magic += (coefficient.penetrate * MagicUpgradeLevel) + (coefficient.penetrateQuadratic * MagicUpgradeLevel * MagicUpgradeLevel);
            
                AddedDamage += (totalPhysicDmg + totalMagicDmg) * 0.6f;
                break;
        }
    }

    /// <summary>
    /// 타워의 현재 상태를 변경하고 해당 상태의 코루틴을 시작합니다.
    /// </summary>
    /// <param name="newState">새로운 무기 상태</param>
    public void ChangeState(IWeaponState newState)
    {
        // 이전에 실행 중이던 코루틴이 있다면 중지
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentState = newState; // 현재 상태 업데이트
        currentCoroutine = StartCoroutine(currentState.Execute(this)); // 새로운 상태 코루틴 시작
    }

    /// <summary>
    /// 공격 시도 코루틴을 호출하는 헬퍼 메소드입니다.
    /// </summary>
    /// <returns>공격 시도 코루틴</returns>
    public IEnumerator CallTryAttackCoroutine()
    {
        yield return TryAttackCoroutine();
    }

    /// <summary>
    /// 가장 가까운 공격 대상을 찾아 반환합니다.
    /// </summary>
    /// <returns>가장 가까운 Enemy 객체, 없으면 null</returns>
    public Enemy FindClosetAttackTarget()
    {
        attackTarget = null; // 공격 대상 초기화
        float closestDistSqr = Mathf.Infinity; // 가장 가까운 거리 초기화

        // 맵에 존재하는 모든 적을 순회하며 가장 가까운 적 탐색
        for(int i = 0; i < enemySpawner.EnemyList.Count; ++i)
        {
            Enemy currentEnemy = enemySpawner.EnemyList[i];
            // 적이 활성화 상태인지 확인
            if (currentEnemy == null || currentEnemy.gameObject == null || !currentEnemy.gameObject.activeSelf) continue;

            float distance = Vector3.Distance(currentEnemy.transform.position, transform.position);
            // 공격 범위 내에 있고, 현재까지 찾은 가장 가까운 적보다 가까우면 업데이트
            if (distance <= Range && distance < closestDistSqr)
            {
                closestDistSqr = distance;
                attackTarget = enemySpawner.EnemyList[i];
            }
        }

        return attackTarget;
    }

    /// <summary>
    /// 공격 범위 내의 모든 적을 찾아 리스트로 반환합니다.
    /// </summary>
    /// <returns>공격 범위 내의 적 리스트</returns>
    public List<Enemy> FindMultiAttackTarget()
    {   
        List<Enemy> enemyList = new();

        // 맵에 존재하는 모든 적을 순회하며 공격 범위 내의 적을 리스트에 추가
        for (int i = 0; i < enemySpawner.EnemyList.Count; ++i)
        {
            Enemy currentEnemy = enemySpawner.EnemyList[i];
            if (currentEnemy == null || currentEnemy.gameObject == null || !currentEnemy.gameObject.activeSelf) continue;

            float distance = Vector3.Distance(currentEnemy.transform.position, transform.position);
            if (distance <= Range)
            {
                enemyList.Add(enemySpawner.EnemyList[i]);
            }
        }

        return enemyList;
    }

    /// <summary>
    /// 현재 공격 대상이 유효하며 공격 가능한 상태인지 확인합니다.
    /// </summary>
    /// <returns>공격 가능하면 true, 그렇지 않으면 false</returns>
    public bool IsPossibleToAttackTarget()
    {
        // 공격 대상이 null이거나 비활성화된 경우 (적이 죽거나 사라진 경우)
        if(attackTarget == null || attackTarget.gameObject == null || !attackTarget.gameObject.activeSelf)
        {
            return false;
        }

        // 공격 대상이 공격 범위를 벗어났는지 확인
        float distance = Vector3.Distance(attackTarget.transform.position, transform.position);
        if (distance > Range)
        {
            attackTarget = null; // 범위 밖으로 벗어나면 대상 초기화
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// 타워를 판매하고 판매 비용을 플레이어 골드에 추가합니다.
    /// </summary>
    public static event Action OnTowerSold; // 타워 판매 이벤트

    /// <summary>
    /// 타워를 판매하고 판매 비용을 플레이어 골드에 추가합니다.
    /// </summary>
    public virtual void Sell()
    {
        // 타워 판매 기록
        if (GameAnalyticsManager.Instance != null && towerArchetype != null)
        {
            GameAnalyticsManager.Instance.RecordTowerSell(towerArchetype.archetypeDisplayName);
        }
        
        playerTowerCost.AddTowerCost(SellCost);
        ownerTile.IsBuildTower = false; // 타일의 건설 상태 해제
        Destroy(gameObject); // 타워 오브젝트 파괴
        OnTowerSold?.Invoke(); // 타워 판매 이벤트 발생
    }

    /// <summary>
    /// 최종 데미지를 계산합니다. (적의 방어력 관통 포함)
    /// </summary>
    /// <param name="targetEnemy">데미지를 적용할 대상 적</param>
    /// <returns>계산된 최종 데미지</returns>
    public float finalDamage(Enemy targetEnemy)
    {
        EnemyHP enemyHP = targetEnemy.GetComponent<EnemyHP>();
        if (enemyHP == null) return 0;
        
        // '약자 멸시' 특성을 먼저 확인합니다.
        if (PerkManager.Instance != null && PerkManager.Instance.perk_isExecutionerEnabled)
        {
            // 일반 등급의 적이고 체력이 10% 이하이면 즉사 데미지를 반환합니다.
            if (targetEnemy.enemyType == EnemyType.Default && (enemyHP.CurrentHP / enemyHP.MaxHP <= 0.10f))
            {
                return enemyHP.MaxHP; // 즉사
            }
        }
        
        float initialDamage = Damage;
        float calculatedDamage = 0;
        const float DefenseCoefficient = 500f;

        float damageReduction = 0f;
        
        // 타워의 공격 타입(damageType)에 따라 올바른 관통력을 적용합니다.
        switch (damageType)
        {
            case DamageType.PhysicalType:
            {
                float effectiveArmor = Mathf.Max(0, enemyHP.CurrentPhysicArmor - physic);
                damageReduction = effectiveArmor / (effectiveArmor + DefenseCoefficient);
                break;
            }
            case DamageType.MagicalType:
            {
                float effectiveArmor = Mathf.Max(0, enemyHP.CurrentMagicArmor - magic);
                damageReduction = effectiveArmor / (effectiveArmor + DefenseCoefficient);
                break;
            }
            case DamageType.HybridType:
            {
                float effectivePhysicArmor = Mathf.Max(0, enemyHP.CurrentPhysicArmor - physic);
                float physicDamageReduction = effectivePhysicArmor / (effectivePhysicArmor + DefenseCoefficient);

                float effectiveMagicArmor = Mathf.Max(0, enemyHP.CurrentMagicArmor - magic);
                float magicDamageReduction = effectiveMagicArmor / (effectiveMagicArmor + DefenseCoefficient);

                damageReduction = (physicDamageReduction + magicDamageReduction) / 2f;
                break;
            }
        }
        
        // 계산된 데미지 감소율을 기반으로 최종 데미지를 계산합니다.
        calculatedDamage = initialDamage * (1 - damageReduction);
    
        // GameAnalyticsManager에 관통력 기록
        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.RecordTowerPenetration(towerArchetype.archetypeDisplayName, 1 - damageReduction);
        
        // '체력 비례 데미지' 능력이 활성화되었는지 확인합니다.
        if (dealsPercentHealthDamage)
        {
            // 최대 체력에 비례한 추가 데미지를 계산합니다.
            float percentDamage = enemyHP.MaxHP * percentHealthDamageAmount;
            // 최종 데미지에 합산합니다.
            calculatedDamage += percentDamage;
        }
        
        // 최종 데미지가 0보다 작으면 0 반환
        if(calculatedDamage < 0)
        {
            return 0;
        }
        
        // 공격 타입이 'Continuous'가 아닐 경우에만 데미지를 기록합니다.
        // 지속형 타워의 데미지는 매 프레임 적용 시점에 따로 기록합니다.
        if (attackTimingType != AttackTimingType.Continuous)
        {
            if (GameAnalyticsManager.Instance != null && calculatedDamage > 0)
            {
                GameAnalyticsManager.Instance.RecordTowerDamage(towerArchetype.archetypeDisplayName, calculatedDamage);
            }
        }
        
        return calculatedDamage; 
    }

    /// <summary>
    /// 공격 속도 값을 실제 대기 시간(초)으로 변환합니다.
    /// </summary>
    /// <returns>공격 간 대기 시간 (초)</returns>
    public float AttackSpeedToRate()
    {
        // [변경점] 새로운 공식을 사용하여 지연 시간(Rate) 계산
        // 입력값은 towerArchetype.towerWeaponStats.attackSpeed를 사용합니다.
        float calculatedRate = 50f / (AttackSpeed + 20f);
        
        // 최소 대기 시간을 보장
        return Mathf.Max(0.1f, calculatedRate);
    }

    /// <summary>
    /// 히트 이펙트를 생성하고 일정 시간 후 풀에 반환하는 코루틴
    /// </summary>
    /// <param name="target">히트 이펙트가 발생할 대상</param>
    /// <param name="hitEffectPrefab">사용할 히트 이펙트 프리팹</param>
    /// <param name="time">히트 이펙트 지속 시간</param>
    public IEnumerator HitEffect(Enemy target, GameObject hitEffectPrefab, float time)
    {
        // 대상이 이미 파괴되었으면 코루틴 종료
        if (target == null)
        {
            yield break;
        }

        // 해당 히트 이펙트 프리팹에 대한 오브젝트 풀이 없으면 새로 생성
        if (!hitEffectPools.ContainsKey(hitEffectPrefab))
        {
            hitEffectPools.Add(hitEffectPrefab, new ObjectPool<GameObject>(hitEffectPrefab, 5, transform));
        }
        GameObject hitEffect = hitEffectPools[hitEffectPrefab].Get(); // 풀에서 히트 이펙트 가져오기

        hitEffect.transform.position = target.transform.position; // 대상 위치로 이동

        yield return new WaitForSeconds(time); // 지정된 시간만큼 대기
        hitEffectPools[hitEffectPrefab].Return(hitEffect); // 히트 이펙트를 풀에 반환
    }

    /// <summary>
    /// 공격 시도 코루틴 (자식 클래스에서 구현)
    /// </summary>
    protected abstract IEnumerator TryAttackCoroutine();

    /// <summary>
    /// 타워가 공격 대상을 바라보도록 좌우 반전합니다.
    /// </summary>
    public void FlipTowerTowardsTarget()
    {
        if (attackTarget == null) return;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        // 적이 타워의 오른쪽에 있으면
        if (attackTarget.transform.position.x > transform.position.x)
        {
            spriteRenderer.flipX = false; // 오른쪽을 보도록 설정
        }
        // 적이 타워의 왼쪽에 있으면
        else if (attackTarget.transform.position.x < transform.position.x)
        {
            spriteRenderer.flipX = true; // 왼쪽을 보도록 설정
        }
        // 적이 타워와 같은 x축에 있으면 -> 현재 방향 유지
    }

    /// <summary>
    /// 공격 애니메이션을 재생합니다.
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (animator != null)
        {
            float attackRate = AttackSpeedToRate();
            if (attackRate > 0) // 0으로 나누는 것을 방지
            {
                // 타워의 공격 속도에 따라 애니메이션 재생 속도 조절
                // AttackSpeedToRate() 값이 작을수록 (공격 속도가 빠를수록) animator.speed는 커짐
                animator.speed = 1f / attackRate;
            }
            else
            {
                animator.speed = 1f; // 공격 속도가 0이면 기본 속도로 재생
            }
            animator.SetTrigger("OnAttack");
        }
    }

    /// <summary>
    /// 타워를 적절한 탐색 상태로 전환합니다.
    /// </summary>
    public void ReturnToSearchState()
    {
        if (animator != null)
        {
            animator.speed = 1f; // 공격 애니메이션 종료 후 애니메이터 속도를 기본값으로 재설정
        }

        if (isMultiTargetTower)
        {
            ChangeState(new SearchMultiTargetState());
        }
        else
        {
            ChangeState(new SearchTargetState());
        }
    }
}