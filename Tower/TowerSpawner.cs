using System.Collections;
using System.Collections.Generic; // List<> 사용을 위해 추가
using UnityEngine;
using TowerDefense.Enums;
using UnityEngine.UI; // DamageType을 위해 추가

/// <summary>
/// 타워 생성 및 관리를 담당하는 클래스
/// </summary>
public class TowerSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("스킬 매니저 참조")]
    public SkillManager skillManager;
    [Tooltip("임시 타워 프리팹 (건설 미리보기용)")]
    public GameObject temporaryTowerPrefab;
    [Tooltip("타워 선택 시 표시되는 이미지 프리팹")]
    public GameObject imageSelect;
    [Tooltip("적 스포너 참조")]
    public EnemySpawner enemySpawner;
    [Tooltip("플레이어 골드 참조")]
    public PlayerGold playerGold;
    [Tooltip("플레이어 스킬 포인트 참조")]
    public PlayerSP playerSP;
    [Tooltip("플레이어 업그레이드 참조")]
    public PlayerUpgrade playerUpgrade;
    [Tooltip("플레이어 타워 코스트 컴포넌트")]
    public PlayerTowerCost playerTowerCost; // PlayerTowerCost 참조 추가

    private bool isOnTowerButton = false; // 타워 건설 버튼이 활성화되어 있는지 여부
    private GameObject temporaryTowerClone; // 임시 타워 프리팹의 인스턴스
    private TowerArchetype currentSelectedArchetype; // 현재 선택된 타워 아키타입
    
    private bool _isCurrentSpawnFromSkillButton = false; // 현재 타워 건설 요청이 스킬 버튼에서 왔는지 여부
    private bool _isSpecialSkill = false;
    
    private GameObject towerSelect; // 타워 선택 UI의 인스턴스
    private Transform buttonTransform;

    private GameObject towerPrefab;
    private float range;
    
    /// <summary>
    /// 타워 건설 버튼이 활성화되어 있는지 여부
    /// </summary>
    public bool IsOnTowerButton => isOnTowerButton;
    
    public static event System.Action OnTowerSpawned; // 타워 생성 완료 이벤트

    /// <summary>
    /// 타워 건설 준비를 시작합니다.
    /// </summary>
    /// <param name="archetype">건설할 타워 아키타입</param>
    /// <param name="isFromSkillButton">스킬 버튼을 통해 호출되었는지 여부</param>
    public void ReadyToSpawnTower(Transform buttonTransform, TowerArchetype archetype, bool isFromSkillButton)
    {
        currentSelectedArchetype = archetype;
        _isCurrentSpawnFromSkillButton = isFromSkillButton;
        this.buttonTransform = buttonTransform;

        // 이미 타워 건설 중이거나 스킬 사용 중이면 기존 임시 오브젝트 제거
        if (isOnTowerButton || skillManager.IsOnSkillButton)
        {
            skillManager.DestroyTemporary();
            Destroy(temporaryTowerClone);
            Destroy(towerSelect);
        }
        
        // 타워 건설 비용 확인
        if (!isFromSkillButton && playerTowerCost.CurrentTowerCost < currentSelectedArchetype.towerWeaponStats.cost)
        {
            UIEventManager.Instance.ShowSystemMessage(SystemType.TowerCost);
            return;
        }
        
        if (_isCurrentSpawnFromSkillButton)
        {
            towerPrefab = currentSelectedArchetype.skillPrefab;
            range = currentSelectedArchetype.skillRange;
        }
        else
        {
            towerPrefab = currentSelectedArchetype.towerPrefab;
            range = currentSelectedArchetype.towerWeaponStats.range;
        }
        
        range *= (1 + EquipmentManager.Instance.currentPlayerData.attackRange / 100f);
        
        SpawnUI(); // 타워 건설 UI 생성
    }
    
    public void ReadyToSpecialSkillTower(Transform buttonTransform, TowerArchetype archetype)
    {
        currentSelectedArchetype = archetype;
        this.buttonTransform = buttonTransform;

        _isSpecialSkill = true;

        // 이미 타워 건설 중이거나 스킬 사용 중이면 기존 임시 오브젝트 제거
        if (isOnTowerButton || skillManager.IsOnSkillButton)
        {
            skillManager.DestroyTemporary();
            Destroy(temporaryTowerClone);
            Destroy(towerSelect);
        }

        towerPrefab = currentSelectedArchetype.towerPrefab; 
        range = currentSelectedArchetype.towerWeaponStats.range;
        
        range *= (1 + EquipmentManager.Instance.currentPlayerData.attackRange / 100f);
        
        SpawnUI(); // 타워 건설 UI 생성
    }
    
    /// <summary>
    /// 타워 건설 미리보기 UI를 생성하고 설정합니다.
    /// </summary>
    public void SpawnUI()
    {
        SpriteRenderer towerSprite = towerPrefab.transform.GetComponent<SpriteRenderer>();
        
        isOnTowerButton = true; // 타워 건설 버튼 활성화 상태로 설정

        // GameManager에서 버튼을 동적으로 생성하므로 GameObject.Find는 더 이상 필요 없습니다.
        // 여기서는 임시 UI를 생성하는 로직만 유지합니다.
        towerSelect = Instantiate(imageSelect); // 타워 선택 UI 생성
        
        RectTransform towerSelectRect = towerSelect.GetComponent<RectTransform>();
        towerSelectRect.SetParent(buttonTransform, false);

        // 4. 이제 RectTransform의 속성을 사용하여 앵커와 위치 등을 설정할 수 있습니다.
        // 예시: 부모(버튼)에 꽉 채우도록 스트레치
        towerSelectRect.anchorMin = Vector2.zero;
        towerSelectRect.anchorMax = Vector2.one;
        towerSelectRect.sizeDelta = Vector2.zero;

        temporaryTowerClone = Instantiate(temporaryTowerPrefab); // 임시 타워 생성

        // 임시 타워 스프라이트 설정
        SpriteRenderer temporaryTowerSprite = temporaryTowerClone.GetComponent<SpriteRenderer>();
        temporaryTowerSprite.sprite = towerSprite.sprite;
        temporaryTowerSprite.color = towerSprite.color;

        // 공격 범위 표시 오브젝트 크기 설정
        GameObject temporaryAttackRange = temporaryTowerClone.transform.Find("AttackRange").gameObject;
        float diameterAttRange = range * 2;
        temporaryAttackRange.transform.localScale = new Vector3(diameterAttRange, diameterAttRange, 1);

        temporaryTowerClone.SetActive(false); // 초기에는 비활성화

        StartCoroutine(nameof(OnTowerCancelSystem)); // 타워 건설 취소 코루틴 시작
    }

    /// <summary>
    /// 보상으로 타워를 생성합니다. (랜덤 타일)
    /// </summary>
    /// <param name="archetype">생성할 타워 아키타입</param>
    public void SpawnRewardTower(TowerArchetype archetype)
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");

        List<Tile> availableTiles = new List<Tile>();
        Vector3 mapCenter = Vector3.zero;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (GameObject go in tiles)
        {
            Tile tile = go.GetComponent<Tile>();
            if (tile != null)
            {
                // Calculate map bounds for center calculation
                if (go.transform.position.x < minX) minX = go.transform.position.x;
                if (go.transform.position.x > maxX) maxX = go.transform.position.x;
                if (go.transform.position.y < minY) minY = go.transform.position.y;
                if (go.transform.position.y > maxY) maxY = go.transform.position.y;

                if (!tile.IsBuildTower)
                {
                    availableTiles.Add(tile);
                }
            }
        }

        if (availableTiles.Count == 0)
        {
            Debug.LogWarning("No available tiles to spawn reward tower.");
            return; // 건설 가능한 타일이 없으면 종료
        }

        mapCenter = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);

        // 맵 중앙에 가까운 타일을 우선적으로 선택하기 위해 정렬
        availableTiles.Sort((a, b) => Vector3.Distance(a.transform.position, mapCenter).CompareTo(Vector3.Distance(b.transform.position, mapCenter)));

        // 가장 중앙에 가까운 타일들 중 일부 (예: 상위 25%)에서 랜덤 선택
        int selectionPoolSize = Mathf.Max(1, availableTiles.Count / 4); // 최소 1개
        Tile selectedTile = availableTiles[Random.Range(0, selectionPoolSize)];
        Transform tileTransform = selectedTile.transform;

        selectedTile.IsBuildTower = true; // 타일에 타워가 건설되었음을 표시

        Vector3 position = tileTransform.position + Vector3.back; // 타일보다 z축 -1 위치에 배치
        GameObject clone = Instantiate(archetype.towerPrefab, position, Quaternion.identity); // 타워 생성

        // 타워에 현재 업그레이드 정보 설정
        clone.GetComponent<TowerWeapon>().Setup(archetype, this, enemySpawner, playerTowerCost, selectedTile, playerUpgrade.physicUpgrade, playerUpgrade.magicUpgrade); // 타워 무기 설정

        clone.transform.SetParent(transform); // 스포너의 자식으로 설정
    }

    /// <summary>
    /// 지정된 타일 위치에 타워를 생성합니다.
    /// </summary>
    /// <param name="tileTransform">타워를 건설할 타일의 Transform</param>
    public void SpawnTower(Transform tileTransform)
    {
        if (!isOnTowerButton) return; // 타워 건설 버튼이 활성화되어 있지 않으면 종료

        Tile tile = tileTransform.GetComponent<Tile>();

        // 클릭한 타일에 이미 타워가 건설되어 있는지 확인
        if (tile.IsBuildTower)
        {
            UIEventManager.Instance.ShowSystemMessage(SystemType.Build); // 이미 건설됨 메시지
            return; 
        }

        isOnTowerButton = false; // 타워 건설 버튼 비활성화
        tile.IsBuildTower = true; // 타일에 타워가 건설되었음을 표시

        // 호출된 방식에 따라 비용 소모 및 스킬 버튼 비활성화
        if(_isCurrentSpawnFromSkillButton) // 스킬 버튼을 통해 호출된 경우 (스킬타워)
        {
            playerSP.CurrentSkillPoint -= currentSelectedArchetype.skillPointCost; // 스킬 타워는 SP 소모
            skillManager.IsSkillUsedInCurrentWave(currentSelectedArchetype.archetypeID.GetHashCode()); // 스킬 사용 기록
            skillManager.ButtonInteracte(currentSelectedArchetype, false); // 스킬 버튼 비활성화
            _isCurrentSpawnFromSkillButton = false;
        }
        // 특수 스킬인 경우 호출한 버튼 transform 버튼 비활성화
        else if (_isSpecialSkill)
        {
            buttonTransform.gameObject.GetComponent<Button>().interactable = false;
            _isSpecialSkill = false;
        }
        else // 일반 타워 버튼을 통해 호출된 경우 (일반 타워)
        {
            // 분석 정보를 위한 타워 설치 정보 수집
            int cost = currentSelectedArchetype.towerWeaponStats.cost;
            string towerType = currentSelectedArchetype.archetypeDisplayName; // 또는 archetypeID
            
            playerTowerCost.UseTowerCost(cost); // 일반 타워는 TowerCost 소모
            // 타워 건설 및 비용 기록
            if (GameAnalyticsManager.Instance != null)
            {
                GameAnalyticsManager.Instance.RecordTowerBuild(towerType);
                GameAnalyticsManager.Instance.RecordTowerCostSpent(cost);
            }
        }
        
        Vector3 position = tileTransform.position + Vector3.back; // 타일보다 z축 -1 위치에 배치
        GameObject clone = Instantiate(towerPrefab, position, Quaternion.identity); // 타워 생성
        
        // 타워에 현재 업그레이드 정보 설정
        clone.GetComponent<TowerWeapon>().Setup(currentSelectedArchetype, this, enemySpawner, playerTowerCost, tile, playerUpgrade.physicUpgrade, playerUpgrade.magicUpgrade); // 타워 무기 설정

        clone.transform.SetParent(transform); // 스포너의 자식으로 설정

        // 임시 타워 및 선택 UI 파괴
        Destroy(temporaryTowerClone);
        Destroy(towerSelect);
        StopCoroutine(nameof(OnTowerCancelSystem)); // 타워 취소 코루틴 중지
        
        SoundManager.Instance.PlaySFX("SFX_Tower_Build");
        
        // 타워가 성공적으로 생성되었음을 알리는 이벤트를 호출합니다.
        OnTowerSpawned?.Invoke();
    }

    /// <summary>
    /// 타워 건설 미리보기 오브젝트의 가시성을 설정하고 위치를 업데이트합니다.
    /// </summary>
    /// <param name="view">가시성 여부</param>
    /// <param name="tileTransform">타일의 Transform</param>
    public void viewBuildTower(bool view, Transform tileTransform)
    {
        if(temporaryTowerClone == null) return; // 임시 타워가 없으면 종료

        if(!view)
        {
            temporaryTowerClone.SetActive(false); // 비활성화
            return;
        }

        temporaryTowerClone.SetActive(true); // 활성화
        temporaryTowerClone.transform.position = tileTransform.position; // 타일 위치로 이동
    }

    /// <summary>
    /// 타워 건설을 취소할 수 있는 코루틴 (ESC 또는 마우스 우클릭)
    /// </summary>
    private IEnumerator OnTowerCancelSystem()
    {
        while (true)
        {
            // ESC키 또는 마우스 우클릭으로 타워 건설 취소
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                isOnTowerButton = false;
                Destroy(temporaryTowerClone);
                Destroy(towerSelect);
                break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 임시 타워 및 선택 UI를 파괴하고 건설 상태를 해제합니다.
    /// </summary>
    public void DestroyTemporary()
    {
        isOnTowerButton = false;
        Destroy(temporaryTowerClone);
        Destroy(towerSelect);
    }
}
