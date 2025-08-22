using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Enums; // DamageType을 위해 추가

public class TowerBuffEffect : MonoBehaviour
{
    private class BuffedTowerInfo
    {
        public TowerWeapon Tower;
        public float LastAppliedBaseDamage;
        public float LastAppliedBuffValue;
    }

    [Range(0, 1)]
    public float buffFactor = 0.3f;
    public string targetTag = "Tower";

    private List<BuffedTowerInfo> buffedTowers = new List<BuffedTowerInfo>();
    private Collider2D triggerCollider;

    void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null)
        {
            Debug.LogError("이 오브젝트에 Collider2D가 없습니다!");
        }
    }

    void Start()
    {
        StartCoroutine(BuffCheckRoutine());
    }

    /// <summary>
    /// 타워의 순수 기본 데미지(업그레이드 포함, 버프 제외)를 외부에서 계산합니다.
    /// TowerWeapon.UpgradeDamage() 로직을 그대로 사용합니다.
    /// </summary>
    private float GetCurrentBaseDamage(TowerWeapon tower)
    {
        float initialDamage = tower.towerArchetype.towerWeaponStats.damage;
        float upgradeAddedDamage = 0f;

        switch (tower.damageType)
        {
            case DamageType.PhysicalType:
                upgradeAddedDamage = (tower.towerArchetype.upgradeCoefficient.damage * tower.PhysicUpgradeLevel) +
                                     (tower.towerArchetype.upgradeCoefficient.damageQuadratic * tower.PhysicUpgradeLevel * tower.PhysicUpgradeLevel);
                break;
            case DamageType.MagicalType:
                upgradeAddedDamage = (tower.towerArchetype.upgradeCoefficient.damage * tower.MagicUpgradeLevel) +
                                     (tower.towerArchetype.upgradeCoefficient.damageQuadratic * tower.MagicUpgradeLevel * tower.MagicUpgradeLevel);
                break;
            case DamageType.HybridType:
                float totalPhysicDamage = (tower.towerArchetype.upgradeCoefficient.damage * tower.PhysicUpgradeLevel) +
                                          (tower.towerArchetype.upgradeCoefficient.damageQuadratic * tower.PhysicUpgradeLevel * tower.PhysicUpgradeLevel);
                float totalMagicDamage = (tower.towerArchetype.upgradeCoefficient.damage * tower.MagicUpgradeLevel) +
                                         (tower.towerArchetype.upgradeCoefficient.damageQuadratic * tower.MagicUpgradeLevel * tower.MagicUpgradeLevel);
                upgradeAddedDamage = (totalPhysicDamage + totalMagicDamage) * 0.6f;
                break;
        }
        
        return initialDamage + upgradeAddedDamage;
    }

    private IEnumerator BuffCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (triggerCollider == null) continue;

            var towersInZoneNow = new List<TowerWeapon>();
            Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, triggerCollider.bounds.size, 0f);
            foreach (var col in colliders)
            {
                if (col.CompareTag(targetTag))
                {
                    TowerWeapon tw = col.GetComponent<TowerWeapon>();
                    if (tw != null) towersInZoneNow.Add(tw);
                }
            }

            for (int i = buffedTowers.Count - 1; i >= 0; i--)
            {
                var info = buffedTowers[i];
                if (info.Tower == null || !towersInZoneNow.Contains(info.Tower))
                {
                    if (info.Tower != null) info.Tower.BuffDamage -= info.LastAppliedBuffValue;
                    buffedTowers.RemoveAt(i);
                }
            }

            foreach (var tower in towersInZoneNow)
            {
                var existingInfo = buffedTowers.Find(info => info.Tower == tower);
                
                // 버프 계산의 기준이 되는 데미지를 외부에서 직접 계산
                float currentPureBaseDamage = GetCurrentBaseDamage(tower);

                if (existingInfo == null)
                {
                    float newBuffValue = currentPureBaseDamage * buffFactor;
                    tower.BuffDamage += newBuffValue;
                    buffedTowers.Add(new BuffedTowerInfo
                    {
                        Tower = tower,
                        LastAppliedBaseDamage = currentPureBaseDamage,
                        LastAppliedBuffValue = newBuffValue
                    });
                }
                else
                {
                    if (currentPureBaseDamage != existingInfo.LastAppliedBaseDamage)
                    {
                        tower.BuffDamage -= existingInfo.LastAppliedBuffValue;
                        float newBuffValue = currentPureBaseDamage * buffFactor;
                        tower.BuffDamage += newBuffValue;
                        existingInfo.LastAppliedBaseDamage = currentPureBaseDamage;
                        existingInfo.LastAppliedBuffValue = newBuffValue;
                    }
                }
            }
        }
    }
}