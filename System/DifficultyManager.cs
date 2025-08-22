using UnityEngine;
using System;
using System.Collections.Generic;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    public static event Action<int> OnDifficultyLevelChanged; // 난이도 레벨 변경 이벤트

    private int _currentDifficultyLevel = 1; // 현재 난이도 레벨 (1~100)

    public int CurrentDifficultyLevel
    {
        get { return _currentDifficultyLevel; }
        set
        {
            _currentDifficultyLevel = Mathf.Clamp(value, 1, 100);
            OnDifficultyLevelChanged?.Invoke(_currentDifficultyLevel);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 현재 난이도 레벨에 따른 적 체력 배율을 반환합니다.
    /// 1~20 : 완만 상승 (1.0 → 3.0)
    /// 21~80 : 완만 상승 (3.0 → 5.0)
    /// 81~100 : 완만 상승 (5.0 → 6.0)
    /// </summary>
    public float GetHealthMultiplier()
    {
        int level = CurrentDifficultyLevel;

        if (level < 20)
        {
            // 1~19: 완만한 초반 구간 (1.0x -> 2.9x)
            return 1.0f + (level - 1) * 0.1f;
        }
        else if (level < 80)
        {
            // 20~79: 1차 벽. 20레벨에 진입하며 난이도 점프. (4.0x -> 7.0x)
            return 4.0f + (level - 20) * 0.05f;
        }
        else // 80~100
        {
            // 80~100: 2차 벽. 80레벨에 진입하며 최종 난이도 점프. (8.0x -> 10.0x)
            return 8.0f + (level - 80) * 0.1f;
        }
    }

    /// <summary>
    /// 현재 난이도 레벨에 따른 적 방어력 배율을 반환합니다.
    /// 체력 배율과 동일한 곡선을 사용
    /// </summary>
    public float GetDefenseMultiplier()
    {
        return GetHealthMultiplier();
    }
    
    /// <summary>
    /// 현재 난이도 레벨에 따라 무작위 색상을 반환합니다. (고도화 버전)
    /// 난이도가 20 레벨 단위로 오를수록 테마를 가진 4가지 색상이 추가됩니다.
    /// </summary>
    /// <returns>선택된 색상</returns>
    public Color GetRandomColorForDifficulty()
    {
        // 사용 가능한 색상 목록. Color32를 사용하여 0-255 범위의 RGB 값으로 색을 정의합니다.
        List<Color32> colorPalette = new List<Color32>();

        // 기본 색상 (항상 포함)
        colorPalette.Add(new Color32(255, 255, 255, 255)); // 1. 원래 색상 (흰색)

        // 현재 난이도 레벨 가져오기
        int level = this.CurrentDifficultyLevel;

        // --- 난이도 20 이상 ---
        // 테마: 파스텔톤 (부드러운 색상)
        if (level >= 20)
        {
            colorPalette.Add(new Color32(173, 216, 230, 255)); // 2. 라이트 블루
            colorPalette.Add(new Color32(255, 182, 193, 255)); // 3. 라이트 핑크
            colorPalette.Add(new Color32(144, 238, 144, 255)); // 4. 라이트 그린
            colorPalette.Add(new Color32(255, 255, 160, 255)); // 5. 라이트 옐로우
        }
        
        // --- 난이도 40 이상 ---
        // 테마: 보석 (선명하고 화려한 색상)
        if (level >= 40)
        {
            colorPalette.Add(new Color32(220, 20, 60, 255));   // 6. 루비 (크림슨 레드)
            colorPalette.Add(new Color32(0, 0, 205, 255));    // 7. 사파이어 (미디엄 블루)
            colorPalette.Add(new Color32(0, 128, 0, 255));    // 8. 에메랄드 (그린)
            colorPalette.Add(new Color32(148, 0, 211, 255));   // 9. 자수정 (다크 바이올렛)
        }

        // --- 난이도 60 이상 ---
        // 테마: 대지 (자연적이고 차분한 색상)
        if (level >= 60)
        {
            colorPalette.Add(new Color32(139, 69, 19, 255));   // 10. 흙 (새들 브라운)
            colorPalette.Add(new Color32(85, 107, 47, 255));   // 11. 이끼 (다크 올리브 그린)
            colorPalette.Add(new Color32(112, 128, 144, 255));// 12. 암석 (슬레이트 그레이)
            colorPalette.Add(new Color32(210, 180, 140, 255));// 13. 모래 (탠)
        }

        // --- 난이도 80 이상 ---
        // 테마: 타락/오염 (기괴하고 위협적인 색상)
        if (level >= 80)
        {
            colorPalette.Add(new Color32(128, 0, 128, 255));   // 14. 오염된 보라 (퍼플)
            colorPalette.Add(new Color32(50, 205, 50, 255));   // 15. 독성 녹색 (라임 그린)
            colorPalette.Add(new Color32(255, 140, 0, 255));   // 16. 용암 주황 (다크 오렌지)
            colorPalette.Add(new Color32(0, 206, 209, 255));   // 17. 기묘한 청록 (다크 터콰이즈)
        }

        // 결정된 색상 팔레트에서 무작위로 색상 하나를 선택하여 반환
        int randomIndex = UnityEngine.Random.Range(0, colorPalette.Count);
        return colorPalette[randomIndex];
    }
}