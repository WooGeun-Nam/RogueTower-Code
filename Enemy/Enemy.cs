using System.Collections;
using UnityEngine;

public enum EnemyDestroyType { Kill = 0, Arrive }
public class Enemy : MonoBehaviour
{
    public EnemyType enemyType; // 적의 종류를 나타내는 필드 추가

    private int wayPointCount; // 이동 경로 개수
    private Transform[] wayPoints; // 이동 경로 정보
    private int currentIndex = 0; // 현재 목표지점 인덱스
    private EnemySpawner enemySpawner; // 적의 삭제를 본인이 하지 않고 EnemySpawner에서 제어
    private SpriteRenderer spriteRenderer;

    public SliderPositionAutoSetter HpSlider { get; set; } // 추가: HP 슬라이더 참조

    public float moveSpeed;
    private float baseMoveSpeed;  // 추가

    private int _gold; // Private backing field for gold
    public int Gold // Public property for gold
    {
        get { return _gold; }
        private set { _gold = value; } // Make setter private to control modification
    }
    
    public void Setup(EnemySpawner enemySpawer, Transform[] wayPoints, int waveIndex, EnemyType type) // Add EnemyType parameter
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseMoveSpeed = moveSpeed;  // 추가: 초기 속도 저장
        
        // '위험 수당' 특성의 이동 속도 증가를 적용합니다.
        if (PerkManager.Instance != null)
        {
            moveSpeed *= PerkManager.Instance.perk_enemySpeedModifier;
        }
        
        this.enemySpawner = enemySpawer;
        this.enemyType = type; // EnemyType 설정

        // 적 이동 경로 WayPoints 정보 생성
        wayPointCount = wayPoints.Length;
        this.wayPoints = new Transform[wayPointCount];
        this.wayPoints = wayPoints;

        // 적이 위치를 첫번째 wayPoint 위치로 설정
        transform.position = wayPoints[currentIndex].position;

        setWaveGoldValue(waveIndex); // Call the new method here

        // 적 이동/목표지점 설정 코루틴 함수 시작
        NextMoveTo();
    }
    
    public void setWaveGoldValue(int index)
    {
        // Base gold amount
        int calculatedGold = 10; // Starting gold

        // Linear increase
        calculatedGold += (5 * index); // 5 gold per wave

        // Threshold for increased scaling (e.g., after wave 20)
        int thresholdWave = 20;
        if (index >= thresholdWave)
        {
            int effectiveIndex = index - thresholdWave + 1;
            calculatedGold += (10 * effectiveIndex * effectiveIndex); // Additional quadratic increase
        }

        Gold = calculatedGold;
        
        // '위험 수당' 특성의 골드 획득량 증가를 적용합니다.
        if (PerkManager.Instance != null)
        {
            Gold = (int)(calculatedGold * PerkManager.Instance.perk_goldModifier);
        }
        else
        {
            Gold = calculatedGold; // PerkManager가 없을 경우 대비
        }
    }
    
    // 추가: 속도 리셋 메서드
    public void ResetMoveSpeed() 
    {
        moveSpeed = baseMoveSpeed;
    }

    private void NextMoveTo()
    {
        // 아직 이동할 wayPoints가 남아있다면
        if (currentIndex < wayPointCount - 1)
        {
            // 적의 위치를 정확하게 목표 위치로 설정
            transform.position = wayPoints[currentIndex].position;
            // 이동 방향 설정 => 다음 목표지점(wayPoints)
            currentIndex++;

            if (spriteRenderer != null)
            {
                Vector3 nextPosition = wayPoints[currentIndex].position;
                if (nextPosition.x > transform.position.x)
                {
                    // 다음 목적지가 오른쪽에 있으면 flipX = false (오른쪽을 봄)
                    spriteRenderer.flipX = false;
                }
                else if (nextPosition.x < transform.position.x)
                {
                    // 다음 목적지가 왼쪽에 있으면 flipX = true (왼쪽을 봄)
                    spriteRenderer.flipX = true;
                }
                // x축 이동이 없으면 (수직 이동) 현재 방향을 유지합니다.
            }
            
            StartCoroutine(MoveTo());
        }
        // 현재 위치가 마지막 wayPoints이면
        else 
        {
            // 무한 모드인지 확인
            if (GameModeManager.CurrentMode == GameMode.Infinite)
            {
                // 적 재순환: 위치와 인덱스를 처음으로 되돌립니다.
                transform.position = wayPoints[0].position;
                currentIndex = 0;
                // 다시 이동을 시작합니다.
                NextMoveTo();
            }
            else
            {
                // [기존 로직] 일반 모드일 경우
                Gold = 0;
                OnDie(EnemyDestroyType.Arrive);
            }
        }
    }

    public void OnDie(EnemyDestroyType type)
    {
        // 무한 모드이고, 적이 '처치'되었을 경우 '무한 모드 전용 점수'를 추가합니다.
        if (GameModeManager.CurrentMode == GameMode.Infinite && type == EnemyDestroyType.Kill)
        {
            // GameManager에 적 처치 점수를 추가해달라고 요청합니다.
            // (점수 공식: 적 골드 * 맵 난이도 배율)
            GameManager.Instance.AddScore_Infinite_Kill(this.Gold);
        }

        // EnemySpawner에 죽음을 알리는 것은 모든 모드에서 동일하게 실행됩니다.
        enemySpawner.DestroyEnemy(type, this, Gold);
    }

    private IEnumerator MoveTo()
    {
        float distance = 1f;

        while (distance > 0.04f)
        {
            distance = Vector3.Distance(transform.position, wayPoints[currentIndex].position);

            if(distance < 0.04f)
            {
                transform.position = wayPoints[currentIndex].position;
                NextMoveTo();
                yield break;
            }

            transform.position = Vector3.MoveTowards(transform.position, wayPoints[currentIndex].position, Time.deltaTime * moveSpeed);

            yield return null;
        }
    }
}