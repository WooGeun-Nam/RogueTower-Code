using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "Skill_StatueOfDestructionGod", menuName = "SpecialSkills/Passive/StatueOfDestructionGod")]
public class Skill_StatueOfDestructionGod : EquipmentPassiveSkill
{
    [Header("파괴신의 조각상 설정")]
    [Range(0f, 1f)]
    public float activationChance = 0.05f; // 5% 확률
    [Range(0f, 1f)]
    public float damagePercentage = 0.10f; // 최대 체력의 10% 데미지

    // 스킬 발동 횟수를 저장할 변수
    // NonSerialized 어트리뷰트는 플레이 모드를 종료해도 런타임에 변경된 값이 저장되지 않도록 합니다.
    [NonSerialized]
    public int activationCount = 0;
    
    public override void ApplyGameEffect()
    {
        // 스킬 효과가 적용될 때(게임 시작, 장착 시) 발동 횟수를 초기화
        activationCount = 0;
        EnemySpawner.OnEnemyKilled += OnEnemyKilled; // 적 처치 이벤트 구독
    }

    public override void RemoveGameEffect()
    {
        EnemySpawner.OnEnemyKilled -= OnEnemyKilled; // 적 처치 이벤트 구독 해제
    }

    private void OnEnemyKilled()
    {
        if (Random.value <= activationChance)
        {
            activationCount++;
            UIEventManager.Instance.ShowSystemMessage(SystemType.StatueOfDestructionGod);
            SoundManager.Instance.PlaySFX("SFX_Skill_Explosion");
        
            if (GameManager.Instance != null && GameManager.Instance.enemySpawner != null)
            {
                // 원본 리스트의 '복사본'을 만듭니다.
                var enemyListCopy = new List<Enemy>(GameManager.Instance.enemySpawner.EnemyList);

                // 이제 원본이 아닌 '복사본' 리스트를 순회하여 안전합니다.
                foreach (Enemy enemy in enemyListCopy)
                {
                    if (enemy != null)
                    {
                        EnemyHP enemyHP = enemy.GetComponent<EnemyHP>();
                        if (enemyHP != null)
                        {
                            enemyHP.TakeDamage(enemyHP.MaxHP * damagePercentage);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("[Skill_StatueOfDestructionGod] Required references (GameManager, EnemySpawner) are null. Cannot apply damage to enemies.");
            }
        }
    }
}
