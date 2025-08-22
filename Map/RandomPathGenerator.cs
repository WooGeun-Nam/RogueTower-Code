using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵의 랜덤 경로를 생성하는 클래스
/// </summary>
public class RandomPathGenerator : MonoBehaviour 
{
    [Header("Path Generation Settings")]
    [SerializeField] [Tooltip("경로 세그먼트의 최소 개수")]
    private int minSegments = 4;
    [SerializeField] [Tooltip("경로 세그먼트의 최대 개수")]
    private int maxSegments = 8;
   
    

    private int _segmentCount; // 실제로 사용된 세그먼트 수
    public int SegmentCount => _segmentCount; // 외부 접근용 프로퍼티

    // 맵의 시작점과 끝점 정의
    private readonly Vector2Int LEFT_POINT = new Vector2Int(-(GameConstants.MAP_WIDTH/2) + 1, 4);
    private readonly Vector2Int RIGHT_POINT = new Vector2Int((GameConstants.MAP_WIDTH/2) - 2, 4);

    private Vector2Int START_POINT; // 실제 경로 시작점
    private Vector2Int END_POINT;   // 실제 경로 끝점
    private bool isLeftToRight;     // 경로가 왼쪽에서 오른쪽으로 진행하는지 여부

    // 경로 방향 확인용 프로퍼티
    public bool IsLeftToRight => isLeftToRight;

    /// <summary>
    /// 랜덤 웨이포인트 경로를 생성하고 난이도를 계산합니다.
    /// </summary>
    /// <returns>생성된 웨이포인트 배열</returns>
    public Vector2Int[] GenerateWaypoints()
    {
        // 시작 방향을 랜덤하게 결정 (왼쪽에서 오른쪽 또는 오른쪽에서 왼쪽)
        isLeftToRight = Random.value > 0.5f;
        START_POINT = isLeftToRight ? LEFT_POINT : RIGHT_POINT;
        END_POINT = isLeftToRight ? RIGHT_POINT : LEFT_POINT;
    
        Vector2Int[] path;
        int attempts = 0;
        const int MAX_ATTEMPTS = 100; // 최대 경로 생성 시도 횟수
        int currentMaxSegments = maxSegments; // 현재 시도에서 사용할 최대 세그먼트 수
        int usedSegments = 0;  // 실제로 사용된 세그먼트 수

        // 유효한 경로가 생성될 때까지 반복 시도
        do
        {
            path = GeneratePath(currentMaxSegments, out usedSegments);
            attempts++;

            // 일정 횟수 이상 실패 시 세그먼트 수를 줄여서 재시도
            if (attempts % 20 == 0 && currentMaxSegments > minSegments)
            {
                currentMaxSegments--;
            }

            // 최대 시도 횟수를 초과하면 단순 경로로 대체
            if(attempts >= MAX_ATTEMPTS)
            {
                return GenerateSimplePath();
            }
        } 
        while (!ValidateWaypoints(path)); // 생성된 경로가 유효한지 검증

        _segmentCount = usedSegments; // 사용된 세그먼트 수 저장
    
        return path;
    }

    /// <summary>
    /// 경로 생성에 실패했을 때 대체로 사용되는 단순 경로를 생성합니다.
    /// </summary>
    /// <returns>단순 웨이포인트 배열</returns>
    private Vector2Int[] GenerateSimplePath()
    {
        List<Vector2Int> waypoints = new List<Vector2Int>();
        
        // 시작점에서 중간 높이로 이동 후 끝점으로 이어지는 단순 경로
        waypoints.Add(START_POINT);
        waypoints.Add(new Vector2Int(START_POINT.x, 0));
        waypoints.Add(new Vector2Int(END_POINT.x, 0));
        waypoints.Add(END_POINT);
        
        return waypoints.ToArray();
    }
    
    /// <summary>
    /// 실제 랜덤 경로를 생성하는 핵심 메소드
    /// </summary>
    /// <param name="currentMaxSegments">현재 시도에서 사용할 최대 세그먼트 수</param>
    /// <param name="usedSegments">실제로 사용된 세그먼트 수 (out 파라미터)</param>
    /// <returns>생성된 웨이포인트 배열</returns>
    private Vector2Int[] GeneratePath(int currentMaxSegments, out int usedSegments)
    {
        List<Vector2Int> waypoints = new List<Vector2Int>();
        waypoints.Add(START_POINT);

        // 첫 번째 수직 이동 지점 결정
        int firstVerticalDepth = Random.Range(-(GameConstants.MAP_HEIGHT/2), START_POINT.y - 1);
        waypoints.Add(new Vector2Int(START_POINT.x, firstVerticalDepth));

        int currentX = START_POINT.x;
        int currentY = firstVerticalDepth;

        // 남은 X축 이동 거리를 기반으로 실제 사용할 세그먼트 수 결정
        int remainingX = Mathf.Abs(END_POINT.x - currentX);
        usedSegments = Mathf.Min(currentMaxSegments, Mathf.Max(minSegments, remainingX));

        for (int i = 0; i < usedSegments; i++)
        {
            // X 이동 거리 계산 (남은 세그먼트 수와 남은 X 거리를 고려)
            int remainingSegments = usedSegments - i;
            int minStep = Mathf.Max(1, remainingX / remainingSegments);
            int maxStep = Mathf.Min(2, remainingX);
            int xStep = Random.Range(minStep, maxStep + 1);

            // 경로 방향에 따라 X 좌표 업데이트
            if(isLeftToRight)
                currentX = Mathf.Min(currentX + xStep, END_POINT.x - 1);
            else
                currentX = Mathf.Max(currentX - xStep, END_POINT.x + 1);

            remainingX = Mathf.Abs(END_POINT.x - currentX);

            // Y 위치 결정 (맵 경계 및 다음 세그먼트 고려)
            int minY = -(GameConstants.MAP_HEIGHT/2);
            int maxY = (i == usedSegments - 1) ? END_POINT.y - 1 : (GameConstants.MAP_HEIGHT/2) - 1;

            int newY;
            int attempts = 0;
            // 유효한 Y 위치를 찾을 때까지 반복 시도
            do 
            {
                newY = Random.Range(minY, maxY + 1);
                attempts++;

                // 특정 시도 횟수 이상 실패 시, 현재 Y에서 1칸 이동 시도
                if(attempts > 5)
                {
                    newY = currentY + (Random.value > 0.5f ? 1 : -1);
                    newY = Mathf.Clamp(newY, minY, maxY); // 맵 경계 내로 클램프
                    break;
                }
            } 
            while (Mathf.Abs(newY - currentY) < 1); // 현재 Y와 최소 1칸 이상 차이나도록

            // 현재 X 좌표와 이전 Y 좌표로 웨이포인트 추가
            waypoints.Add(new Vector2Int(currentX, currentY));

            // 마지막 세그먼트가 아니면 새로운 Y 좌표로 웨이포인트 추가
            if (i < usedSegments - 1)
            {
                waypoints.Add(new Vector2Int(currentX, newY));
                currentY = newY;
            }
        }

        // 최종 끝점 추가
        waypoints.Add(new Vector2Int(END_POINT.x, currentY));
        waypoints.Add(END_POINT);

        return waypoints.ToArray();
    }
   
    /// <summary>
    /// 생성된 웨이포인트 경로의 유효성을 검사합니다.
    /// </summary>
    /// <param name="waypoints">검사할 웨이포인트 배열</param>
    /// <returns>경로가 유효하면 true, 그렇지 않으면 false</returns>
    private bool ValidateWaypoints(Vector2Int[] waypoints)
    {
        // 1. 최소 웨이포인트 개수 체크
        if (waypoints.Length < 3)
        {
            return false;
        }
           
        // 2. 시작점과 끝점 일치 여부 검사
        if (waypoints[0] != START_POINT || waypoints[waypoints.Length - 1] != END_POINT)
        {
            return false;
        }
           
        // 3. 모든 웨이포인트가 맵 경계 내에 있는지 검사
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i].x < -(GameConstants.MAP_WIDTH/2) || waypoints[i].x > (GameConstants.MAP_WIDTH/2) - 1 ||
                waypoints[i].y < -(GameConstants.MAP_HEIGHT/2) || waypoints[i].y > (GameConstants.MAP_HEIGHT/2) - 1)
            {
                return false;
            }
        }
       
        // 4. 연속된 웨이포인트 간의 최소 거리 검사 (너무 가까운 포인트 방지)
        for (int i = 1; i < waypoints.Length; i++)
        {
            float distance = Vector2.Distance(waypoints[i], waypoints[i-1]);
            if (distance < 1)
            {
                return false;
            }
        }
       
        return true;
    }
}