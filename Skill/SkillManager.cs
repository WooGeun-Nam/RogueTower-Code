using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// 스킬 사용 및 관리를 담당하는 클래스
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }
    
    [Header("References")]
    [Tooltip("타워 스포너 참조")]
    public TowerSpawner towerSpawner;
    [Tooltip("플레이어 스킬 포인트 참조")]
    public PlayerSP playerSP;
    [Tooltip("플레이어 업그레이드 참조")]
    public PlayerUpgrade playerUpgrade;

    [Header("Skill Data")] private HashSet<int> _usedSkillsInWave = new HashSet<int>(); // 현재 웨이브에서 사용된 스킬들을 추적
    
    [Header("UI Prefabs")]
    [Tooltip("임시 스킬 프리팹 (UI 표시용)")]
    public GameObject temporarySkillPrefab;
    [Tooltip("스킬 선택 시 표시되는 이미지 프리팹")]
    public GameObject imageSelect;
    private Transform buttonTransform;

    // --- 내부 변수 ---
    private Dictionary<string, (Button button, int index)> _skillButtons = new Dictionary<string, (Button, int)>();
    private HashSet<string> _usedSkillIDsInWave = new HashSet<string>(); // 웨이브 당 사용된 스킬 ID 추적

    // 현재 선택된 스킬 정보 (레거시 및 장비스킬 공용)
    private object currentSelectedSkill; // TowerArchetype 또는 SpecialSkillBase 저장
    private GameObject temporarySkillClone;
    private GameObject skillSelect;
    private bool isOnSkillButton = false;

    public bool IsOnSkillButton => isOnSkillButton;

    #region 장비 스킬 시스템 (신규)

    /// <summary>
    /// 장비 스킬 사용을 시도합니다. (UI 버튼에서 호출)
    /// </summary>
    public void UseEquipmentSkill(Transform buttonTransform, SpecialSkillBase skill)
    {
        if (skill == null) return;

        // TODO: 스킬 ID를 기반으로 쿨타임/사용 여부 확인 로직 추가 필요
        // if (_usedSkillsInWave.Contains(skill.skillID.GetHashCode()))
        // {
        //     UIEventManager.Instance.ShowSystemMessage(SystemType.AlreadyUsedSkill);
        //     return;
        // }

        currentSelectedSkill = skill;

        // 스킬 타입에 따라 분기
        switch (skill.skillType)
        {
            case global::SkillType.Active:
                var activeSkill = skill as EquipmentActiveSkill;
                if (activeSkill == null) return;

                if (activeSkill.skillPointCost > playerSP.CurrentSkillPoint)
                {
                    UIEventManager.Instance.ShowSystemMessage(SystemType.SP);
                    return;
                }
                
                playerSP.CurrentSkillPoint -= activeSkill.skillPointCost;
                // _usedSkillsInWave.Add(skill.skillID.GetHashCode());
                
                // 실제 스킬 효과 발동
                activeSkill.UseSkill(buttonTransform.gameObject);
                
                // TODO: UI에서 쿨타임 표시 로직 필요
                break;

            case global::SkillType.Tower:
                var towerSkill = skill as EquipmentTowerSkill;
                if (towerSkill == null) return;

                // PlayerTowerCost는 TowerSpawner에 있으므로 거기서 확인하도록 함.
                
                // 동적으로 수정된 아키타입을 가져옴
                TowerArchetype modifiedArchetype = towerSkill.GetModifiedTowerArchetype();
                if (modifiedArchetype != null)
                {
                    // TowerSpawner에 수정된 아키타입을 직접 전달
                    towerSpawner.ReadyToSpecialSkillTower(buttonTransform, modifiedArchetype);
                }
                break;
        }
    }

    #endregion

    #region 레거시 스킬 시스템 (기존)

    /// <summary>
    /// 스킬 버튼을 SkillManager에 등록합니다.
    /// </summary>
    public void RegisterSkillButton(string archetypeID, Button button, int index)
    {
        if (!_skillButtons.ContainsKey(archetypeID))
        {
            _skillButtons.Add(archetypeID, (button, index));
        }
        else
        {
            Debug.LogWarning($"Skill button with ID {archetypeID} already registered.");
        }
    }

    /// <summary>
    /// 스킬 사용을 시도합니다.
    /// </summary>
    public void UseSkill(Transform buttonTransform, TowerArchetype archetype)
    {
        currentSelectedSkill = archetype;
        this.buttonTransform = buttonTransform;

        if (isOnSkillButton || towerSpawner.IsOnTowerButton)
        {
            towerSpawner.DestroyTemporary();
            Destroy(temporarySkillClone);
            Destroy(skillSelect);
        }

        if (_usedSkillIDsInWave.Contains(archetype.archetypeID))
        {
            UIEventManager.Instance.ShowSystemMessage(SystemType.AlreadyUsedSkill);
            return;
        }

        if (archetype.skillPointCost > playerSP.CurrentSkillPoint)
        {
            UIEventManager.Instance.ShowSystemMessage(SystemType.SP);
            return;
        }
        
        // 스킬 사용 횟수 기록
        if (GameAnalyticsManager.Instance != null)
        {
            GameAnalyticsManager.Instance.RecordSkillUsage(archetype.skillDisplayName);
        }

        if (archetype.skillType == SkillManager.SkillType.Tower)
        {
            towerSpawner.ReadyToSpawnTower(buttonTransform, archetype, true);
        }
        else if (archetype.skillType == SkillManager.SkillType.Active)
        {
            SpawnUI(archetype.skillPrefab, archetype.skillRange);
        }
        else if (archetype.skillType == SkillManager.SkillType.Passive)
        {
            if (towerSpawner.enemySpawner.ActiveEnemyCount > 0)
            {
                isOnSkillButton = true;
                PassiveSkill();
            }
        }
    }

    /// <summary>
    /// 액티브 스킬 사용을 위한 UI를 생성합니다.
    /// </summary>
    public void SpawnUI(GameObject skillPrefab, float range)
    {
        SpriteRenderer skillSprite = skillPrefab.transform.GetComponent<SpriteRenderer>();
        
        isOnSkillButton = true;
        
        // 현재 사용중이 스킬 버튼 표기
        skillSelect = Instantiate(imageSelect);
        
        RectTransform skillSelectRect = skillSelect.GetComponent<RectTransform>();
        skillSelectRect.SetParent(buttonTransform, false);

        // 4. 이제 RectTransform의 속성을 사용하여 앵커와 위치 등을 설정할 수 있습니다.
        // 예시: 부모(버튼)에 꽉 채우도록 스트레치
        skillSelectRect.anchorMin = Vector2.zero;
        skillSelectRect.anchorMax = Vector2.one;
        skillSelectRect.sizeDelta = Vector2.zero;
        
        temporarySkillClone = Instantiate(temporarySkillPrefab);
        
        SpriteRenderer temporarySkillSprite = temporarySkillClone.GetComponent<SpriteRenderer>();
        temporarySkillSprite.sprite = skillSprite.sprite;
        temporarySkillSprite.color = skillSprite.color;

        GameObject temporaryAttackRange = temporarySkillClone.transform.Find("AttackRange").gameObject;
        float diameterAttRange = range * 2;
        temporaryAttackRange.transform.localScale = new Vector3(diameterAttRange, diameterAttRange, 1);

        StartCoroutine(nameof(OnSkillCancelSystem));
    }

    /// <summary>
    /// 마우스 클릭 위치에 액티브 스킬을 발동합니다.
    /// </summary>
    public void ActiveSkill(Vector3 mousePosition)
    {
        if (!isOnSkillButton) return;
        var archetype = currentSelectedSkill as TowerArchetype;
        if (archetype == null) return;

        isOnSkillButton = false;
        mousePosition = new Vector3(mousePosition.x, mousePosition.y, 0);

        GameObject activeSkillClone = Instantiate(archetype.skillPrefab, mousePosition, Quaternion.identity);
        ArrowRain arrowRain = activeSkillClone.GetComponent<ArrowRain>();
        if (arrowRain != null)
        {
            arrowRain.Setup(playerUpgrade);
        }

        ButtonInteracte(archetype, false);
        _usedSkillIDsInWave.Add(archetype.archetypeID);

        playerSP.CurrentSkillPoint -= archetype.skillPointCost;

        StartCoroutine(SelfDestroyObject(activeSkillClone, archetype.skillDuration));

        Destroy(temporarySkillClone);
        Destroy(skillSelect);
        StopCoroutine(nameof(OnSkillCancelSystem));
    }

    /// <summary>
    /// 패시브 스킬을 발동합니다.
    /// </summary>
    public void PassiveSkill()
    {
        if (!isOnSkillButton) return;
        var archetype = currentSelectedSkill as TowerArchetype;
        if (archetype == null) return;

        isOnSkillButton = false;

        ButtonInteracte(archetype, false);
        _usedSkillIDsInWave.Add(archetype.archetypeID);

        GameObject clone = Instantiate(archetype.skillPrefab);
        SkillThunder skillThunder = clone.GetComponent<SkillThunder>();
        if (skillThunder != null)
        {
            skillThunder.playerUpgrade = playerUpgrade;
        }

        var passiveSkillComponent = clone.GetComponent<PassiveSkill>();
        passiveSkillComponent.Setup(archetype.skillDuration);

        playerSP.CurrentSkillPoint -= archetype.skillPointCost;
    }

    /// <summary>
    /// 모든 스킬 버튼을 활성화합니다.
    /// </summary>
    public void AllButtonActive()
    {
        foreach (var buttonEntry in _skillButtons)
        {
            // '희생 전략'이 활성화되어 있고, 현재 버튼의 인덱스가 희생된 인덱스와 같다면 건너뜁니다.
            if (PerkManager.Instance.perk_isSacrificeEnabled && 
                buttonEntry.Value.index == PerkManager.Instance.perk_sacrificedTowerIndex)
            {
                continue; // 이 버튼은 활성화하지 않고 다음 버튼으로 넘어감
            }
        
            buttonEntry.Value.button.interactable = true;
        }
    }
    
    /// <summary>
    /// 특정 스킬 버튼의 상호작용 가능 여부를 설정합니다.
    /// </summary>
    public void ButtonInteracte(TowerArchetype archetype, bool state)
    {
        // 딕셔너리에서 스킬 버튼과 '인덱스'를 함께 찾아옵니다.
        if (_skillButtons.TryGetValue(archetype.archetypeID, out var skillButtonData))
        {
            // 만약 버튼을 '활성화'하려는 경우(state == true),
            // 그리고 '희생 전략' 특성이 활성화 상태이며,
            // 현재 버튼이 바로 그 '희생된 버튼'이라면,
            // 활성화하지 않고 명령을 무시합니다.
            if (state == true && 
                PerkManager.Instance.perk_isSacrificeEnabled &&
                skillButtonData.index == PerkManager.Instance.perk_sacrificedTowerIndex)
            {
                return; // 활성화하지 않고 메서드 종료
            }

            // 위의 조건에 해당하지 않는 모든 경우에는 정상적으로 상태를 변경합니다.
            skillButtonData.button.interactable = state;
        }
    }

    /// <summary>
    /// 모든 스킬 타워를 파괴합니다.
    /// </summary>
    public void AllSkillTowerDestroy()
    {
        GameObject[] skillTowers = GameObject.FindGameObjectsWithTag("Skill");
        foreach (GameObject skillTower in skillTowers)
        {
            if (skillTower != null)
            {
                TowerWeapon tower = skillTower.GetComponent<TowerWeapon>();
                if (tower != null && tower.OwnerTile != null)
                {
                    tower.OwnerTile.IsBuildTower = false;
                }
                Destroy(skillTower);
            }
        }
    }

    /// <summary>
    /// 임시 스킬 UI 및 프리팹을 파괴합니다.
    /// </summary>
    public void DestroyTemporary()
    {
        isOnSkillButton = false;
        Destroy(temporarySkillClone);
        Destroy(skillSelect);
    }

    /// <summary>
    /// 현재 웨이브에서 특정 스킬이 사용되었는지 확인합니다.
    /// </summary>
    /// <param name="skillIndex">확인할 스킬의 인덱스</param>
    /// <returns>스킬이 사용되었으면 true, 아니면 false</returns>
    public bool IsSkillUsedInCurrentWave(int skillIndex)
    {
        // archetypeID의 해시값을 사용하도록 변경
        return _usedSkillsInWave.Contains(skillIndex); // 이 부분은 SkillManager.Skill 구조체에 skillIndex가 없으므로, archetypeID의 해시값을 사용하도록 변경해야 합니다.
    }
    
    /// <summary>
    /// 현재 웨이브에서 사용된 스킬 기록을 초기화합니다.
    public void ResetUsedSkillsInWave()
    {
        _usedSkillIDsInWave.Clear();
        foreach (var buttonEntry in _skillButtons)
        {
            // '희생 전략'이 활성화되어 있고, 현재 버튼의 인덱스가 희생된 인덱스와 같다면 건너뜁니다.
            if (PerkManager.Instance.perk_isSacrificeEnabled && 
                buttonEntry.Value.index == PerkManager.Instance.perk_sacrificedTowerIndex)
            {
                continue; // 이 버튼은 활성화하지 않고 다음 버튼으로 넘어감
            }

            buttonEntry.Value.button.interactable = true;
        }
    }

    private IEnumerator SelfDestroyObject(GameObject clone, float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(clone);
    }

    private IEnumerator OnSkillCancelSystem()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                isOnSkillButton = false;
                Destroy(temporarySkillClone);
                Destroy(skillSelect);
                break;
            }
            yield return null;
        }
    }

    // Enum은 SkillManager 외부 또는 별도 파일로 이동하는 것을 권장
    public enum SkillType
    {
        Tower = 0,
        Active,
        Passive
    }

    #endregion
}
