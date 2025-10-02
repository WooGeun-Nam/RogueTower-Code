using UnityEngine;

// 스킬의 종류를 정의하는 Enum
public enum SkillType
{
    Passive,        // 장착 시 지속적으로 효과 적용
    Active,         // 플레이어가 직접 사용 (버튼 클릭 등)
    Tower    // 특정 타워 설치
}

public abstract class SpecialSkillBase : ScriptableObject
{
    [Header("스킬 기본 정보")]
    public string skillID;
    public string skillName;
    [TextArea]
    public string description;
    public Sprite icon;
    public SkillType skillType; // 스킬 타입 추가

    // 모든 특수 스킬이 구현해야 할 기능들 (추상 메서드)
    public abstract void Activate();    // 스킬 활성화 (장착 시) - 씬 독립적인 초기화/정리
    public abstract void Deactivate();  // 스킬 비활성화 (해제 시) - 씬 독립적인 초기화/정리

    // 스킬 타입에 따라 선택적으로 구현될 메서드
    public virtual void UseSkill(GameObject skillButton) { } // 사용형 스킬 (Active)에서 오버라이드
    public virtual GameObject GetTowerPrefab() { return null; } // 타워 설치형 스킬 (TowerInstall)에서 오버라이드

    // 인게임에서 실제 게임 플레이 효과를 적용/제거하는 메서드 (씬 종속적)
    public virtual void ApplyGameEffect() { }
    public virtual void RemoveGameEffect() { }
}
