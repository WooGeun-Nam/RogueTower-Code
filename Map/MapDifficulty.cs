using UnityEngine;

/// <summary>
/// 생성된 맵의 난이도를 계산하고 저장하는 클래스
/// </summary>
public class MapDifficulty
{
    // 난이도 계산에 사용되는 기본 값 및 가중치
    [Header("Base Values for Difficulty")]
    private const float BASE_PATH_LENGTH = 50f;    // 경로 길이의 기준 값
    private const float BASE_SEGMENT_COUNT = 8f;   // 세그먼트 수의 기준 값
    private const float BASE_GAP = 5f;             // 경로 간격의 기준 값

    [Header("Weight Values")]
    private const float LENGTH_WEIGHT = 40f;       // 경로 길이 점수의 가중치
    private const float SEGMENT_WEIGHT = 30f;      // 세그먼트 수 점수의 가중치
    private const float GAP_WEIGHT = 30f;          // 경로 간격 점수의 가중치

    // 계산된 맵 정보
    public float TotalScore { get; private set; }      // 최종 난이도 점수
    public float PathLength { get; private set; }      // 전체 경로의 길이
    public int SegmentCount { get; private set; }      // 경로를 구성하는 주요 꺾임(세그먼트)의 수
    public float AverageGap { get; private set; }      // 경로의 평행 구간 사이의 평균 간격

    /// <summary>
    /// 주어진 웨이포인트와 세그먼트 수로 맵의 난이도를 계산합니다.
    /// </summary>
    /// <param name="waypoints">맵의 경로를 구성하는 웨이포인트 배열</param>
    /// <param name="segmentCount">경로의 세그먼트 수</param>
    /// <returns>계산된 난이도 정보를 담은 MapDifficulty 객체</returns>
    public static MapDifficulty CalculateDifficulty(Vector2Int[] waypoints, int segmentCount)
    {
        MapDifficulty difficulty = new MapDifficulty();
        
        difficulty.SegmentCount = segmentCount;
        CalculatePathLength(waypoints, difficulty);
        CalculateAverageGap(waypoints, difficulty);
        CalculateTotalScore(difficulty);
        
        return difficulty;
    }

    /// <summary>
    /// 웨이포인트 배열을 기반으로 전체 경로 길이를 계산합니다.
    /// </summary>
    private static void CalculatePathLength(Vector2Int[] waypoints, MapDifficulty difficulty)
    {
        float pathLength = 0f;
        for (int i = 1; i < waypoints.Length; i++)
        {
            pathLength += Vector2.Distance(waypoints[i - 1], waypoints[i]);
        }
        difficulty.PathLength = pathLength;
    }

    /// <summary>
    /// 웨이포인트 배열을 기반으로 경로의 평균 간격을 계산합니다.
    /// </summary>
    private static void CalculateAverageGap(Vector2Int[] waypoints, MapDifficulty difficulty)
    {
        float totalGap = 0f;
        int gapCount = 0;
        
        // y축이 평행한 두 세그먼트 간의 간격을 계산
        for (int i = 2; i < waypoints.Length - 1; i += 2)
        {
            float gap = Mathf.Abs(waypoints[i].y - waypoints[i - 2].y);
            totalGap += gap;
            gapCount++;
        }
        
        difficulty.AverageGap = gapCount > 0 ? totalGap / gapCount : 0f;
    }

    /// <summary>
    /// 각 요소별 점수를 합산하여 최종 난이도 점수를 계산합니다.
    /// </summary>
    private static void CalculateTotalScore(MapDifficulty difficulty)
    {
        float lengthScore = GetLengthScore(difficulty.PathLength);
        float segmentScore = GetSegmentScore(difficulty.SegmentCount);
        float gapScore = GetGapScore(difficulty.AverageGap);
        
        difficulty.TotalScore = lengthScore + segmentScore + gapScore;
    }

    // 각 점수 계산을 위한 private static 메소드
    private static float GetLengthScore(float pathLength) => (BASE_PATH_LENGTH / Mathf.Max(pathLength, 1f)) * LENGTH_WEIGHT;
    private static float GetSegmentScore(int segmentCount) => (BASE_SEGMENT_COUNT / Mathf.Max(segmentCount, 1)) * SEGMENT_WEIGHT;
    private static float GetGapScore(float averageGap) => (BASE_GAP / Mathf.Max(averageGap, 0.1f)) * GAP_WEIGHT;

    /// <summary>
    /// 난이도 정보를 문자열로 반환합니다. (디버그용)
    /// </summary>
    /// <returns>포맷팅된 난이도 정보 문자열</returns>
    public override string ToString()
    {
        return string.Format(
            "난이도 정보:\n" +
            "- 총 점수: {0:F1}\n" +
            "- 길이 점수: {1:F1} (실제 길이: {2:F1})\n" +
            "- 세그먼트 점수: {3:F1} (세그먼트 수: {4})\n" +
            "- 간격 점수: {5:F1} (평균 간격: {6:F1})",
            TotalScore,
            GetLengthScore(PathLength),
            PathLength,
            GetSegmentScore(SegmentCount),
            SegmentCount,
            GetGapScore(AverageGap),
            AverageGap
        );
    }
}