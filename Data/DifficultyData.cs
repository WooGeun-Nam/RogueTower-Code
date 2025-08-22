using UnityEngine;

[CreateAssetMenu(fileName = "NewDifficultyData", menuName = "Data/DifficultyData")]
public class DifficultyData : ScriptableObject
{
    // 이 ScriptableObject는 현재 사용되지 않지만, 향후 난이도별 프리셋을 정의할 때 활용될 수 있습니다.
    // 현재는 슬라이더 값(1~100)을 직접 난이도 레벨로 사용합니다.
    public string difficultyName;
    public float healthMultiplier;
    public float defenseMultiplier;
}