using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyHPSliderPrefab; // 적 체력바를 나타내는 Slider UI 프리팹
    public Transform canvasTransform; // UI를 표시하는 Canvas 오브젝트의 Transform
    public Transform[] wayPoints; // 적 이동 경로
    public PlayerHP playerHP;
    public PlayerGold playerGold;
    private Wave currentWave; // 현재 웨이브 정보
    private int currentEnemyCount; // 현재 웨이브에 남아있는 적 수 (웨이브 종료시 max로 초기화, 적 죽을 시 -1)
    private List<Enemy> enemyList;
    private int spawnEnemyCount;

    // Waypoint 관련 변수
    private List<Transform> waypointTransforms = new List<Transform>();
    private GameObject waypointsContainer;

    // [추가] EnemyHPViewer 오브젝트 풀
    // 기능: EnemyHPViewer UI 재활용을 통해 성능 최적화
    private ObjectPool<SliderPositionAutoSetter> hpSliderPool;

    // 스폰된 적을 관리하는 EnemySpawner에서 하기 때문에 set은 필요 없음.
    public List<Enemy> EnemyList => enemyList;
    // 현재 웨이브에 남아있는 적, 최대 적 수
    public int CurrentEnemyCount => currentEnemyCount;
    public int MaxEnemyCount => currentWave.maxEnemyCount;
    public int SpawnEnemyCount => spawnEnemyCount;

    /// <summary>
    /// 현재 활성화된 적의 수를 반환합니다.
    /// </summary>
    public int ActiveEnemyCount => enemyList.Count;

    // 적이 생성되는 시점의 구독
    public static event Action OnEnemySpawn; // 체력 감소 이벤트
    
    private void Awake()
    {
        // 적 리스트 메모리 할당
        enemyList = new List<Enemy>();

        // [추가] EnemyHPViewer 오브젝트 풀 초기화
        hpSliderPool = new ObjectPool<SliderPositionAutoSetter>(enemyHPSliderPrefab.GetComponent<SliderPositionAutoSetter>(), 10, canvasTransform.Find("SliderEnemyHP"));
    }

    /// <summary>
    /// 동적으로 생성된 경로를 기반으로 Waypoint를 설정합니다.
    /// </summary>
    /// <param name="pathPoints">경로 좌표 배열</param>
    public void SetupWaypoints(Vector2Int[] pathPoints)
    {
        // 기존에 생성된 Waypoint가 있다면 삭제
        if (waypointsContainer != null)
        {
            Destroy(waypointsContainer);
        }
        waypointTransforms.Clear();

        // Waypoint들을 담을 부모 오브젝트 생성
        waypointsContainer = new GameObject("Waypoints");
    
        // 경로 좌표를 기반으로 Waypoint 오브젝트 생성
        foreach(Vector2Int point in pathPoints)
        {
            GameObject waypoint = new GameObject($"Waypoint_{waypointTransforms.Count}");
            // 타일 중앙으로 좌표 조정
            Vector3 tileCenter = new Vector3(point.x + 0.5f, point.y + 0.5f, 0);
            waypoint.transform.position = tileCenter;
            waypoint.transform.SetParent(waypointsContainer.transform);
            waypointTransforms.Add(waypoint.transform);
        }

        // 배열로 변환하여 wayPoints에 할당
        wayPoints = waypointTransforms.ToArray();
    }

    public void StartWave(Wave wave)
    {
        // 파라미터로 받아온 웨이브 정보 저장
        currentWave = wave;
        // 현재 웨이브의 최대 적 숫자를 저장
        currentEnemyCount = currentWave.maxEnemyCount;
        // 현재 웨이브 생성 시작
        StartCoroutine(SpawnEnemy());
    }

    private IEnumerator SpawnEnemy()
    {
        // 현재 웨이브에서 생성된 적 수
        spawnEnemyCount = 0;

        // 현재 웨이브에서 생성되어야 하는 적 숫자만큼 생성하고 코루틴 실행
        while (spawnEnemyCount < currentWave.maxEnemyCount)
        {
            GameObject clone = Instantiate(currentWave.enemyPrefabs);
            clone.SetActive(true);
            Enemy enemy = clone.GetComponent<Enemy>(); // 새로 생성된 Enemy 오브젝트
            EnemyHP enemyHP = clone.GetComponent<EnemyHP>();

            clone.transform.SetParent(transform);

            // 웨이브 단계에 따라 능력치 설정
            enemyHP.setWavePowerValue(currentWave.index);

            EnemyType enemyType = currentWave.enemyType;
            enemy.Setup(this, wayPoints, currentWave.index, enemyType); // wayPoint 정보와 waveIndex, EnemyType을 파라미터로 Setup() 호출
            enemyList.Add(enemy); // 리스트에 새로 생성된 적 추가

            SliderPositionAutoSetter slider = SpawnEnemyHPSlider(clone);
            enemy.HpSlider = slider;

            // 현재 웨이브에 생성된 적+
            spawnEnemyCount++;
            
            // 적 생성 이벤트 발생
            OnEnemySpawn?.Invoke();
            
            // yield return new WaitForSeconds(spawnTime); // spawnTime 시간 대기 후 생성
            // 각 웨이브별 spawnTime이 다른 점을 고려하여 현재 웨이브(currentWave)의 spawnTime 사용
            yield return new WaitForSeconds(currentWave.spawnTime);
        }
    }

    public static event Action OnEnemyKilled; // 적 처치 이벤트

    public void DestroyEnemy(EnemyDestroyType type, Enemy enemy, int gold)
    {
        if(type == EnemyDestroyType.Arrive)
        {
            playerHP.TakeDamage(1);
        }
        else if(type == EnemyDestroyType.Kill)
        {
            playerGold.CurrentGold += gold;
            if (GameManager.Instance != null) // GameManager가 할당되어 있다면
            {
                GameManager.Instance.enemiesKilled++; // 처치한 적 수 증가
            }
            OnEnemyKilled?.Invoke(); // 적 처치 이벤트 발생
        }

        // 현재 남아있는 적 리스트에서 제거 (UI 표시에 사용)
        currentEnemyCount--;
        // 리스트에서 제거하는 적 오브젝트 제거
        enemyList.Remove(enemy);
        // 적 오브젝트 파괴
        // Destroy(enemy.gameObject);
        // [추가] SliderPositionAutoSetter가 null이 아닐 때만 풀에 반환
        if (enemy.HpSlider != null)
        {
            hpSliderPool.Return(enemy.HpSlider);
        }
        Destroy(enemy.gameObject);
    }

    private SliderPositionAutoSetter SpawnEnemyHPSlider(GameObject enemy)
    {
        // 오브젝트 풀에서 Slider UI 가져오기
        // 기능: Instantiate 대신 오브젝트 풀을 사용하여 Slider UI 생성/파괴 비용 절감
        // 삭제된 내용: GameObject sliderClone = Instantiate(enemyHPSliderPrefab);
        SliderPositionAutoSetter sliderClone = hpSliderPool.Get();

        // Slider UI 오브젝트의 parent("Canvas" 오브젝트)에 자식으로 설정
        // Tip. UI는 캔버스의 자식오브젝트로 설정되어 있어야 화면에 보인다
        // [삭제] SetParent는 오브젝트 풀 초기화 시 이미 설정됨
        // sliderClone.transform.SetParent(canvasTransform.Find("SliderEnemyHP"));
        // 스케일이 변경된 것을 다시 (1, 1, 1)로 설정
        sliderClone.transform.localScale = Vector3.one;

        // Slider UI에 어떤 적의 정보를 나타낼지 설정
        sliderClone.Setup(enemy.transform);
        // Slider UI에 자신의 체력 정보를 표시하도록 설정
        sliderClone.GetComponent<EnemyHPViewer>().Setup(enemy.GetComponent<EnemyHP>());

        return sliderClone;
    }

    /// <summary>
    /// [신규] 현재 맵에 남아있는 모든 적을 제거합니다.
    /// </summary>
    public void ClearAllEnemies()
    {
        // 리스트를 직접 순회하며 제거하면 오류가 발생할 수 있으므로,
        // 리스트에 적이 없을 때까지 첫 번째 적을 계속 제거하는 방식을 사용합니다.
        while (enemyList.Count > 0)
        {
            // DestroyEnemy는 적 오브젝트 파괴, 리스트에서 제거, HP바 반납까지 모두 처리해줍니다.
            // 패널티로 제거되는 적이므로 골드는 0을 지급합니다.
            DestroyEnemy(EnemyDestroyType.Kill, enemyList[0], 0);
        }
    }
}