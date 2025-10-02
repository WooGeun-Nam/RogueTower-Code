using System.Collections;
using UnityEngine;

/// <summary>
/// 타워 무기의 '적 탐색' 상태를 정의하는 클래스입니다.
/// 이 상태에서는 공격할 적을 찾고, 적을 찾으면 '공격 시도' 상태로 전환합니다.
/// </summary>
public class SearchTargetState : IWeaponState
{
    /// <summary>
    /// '적 탐색' 상태의 로직을 실행합니다.
    /// </summary>
    /// <param name="weapon">상태를 실행할 TowerWeapon 인스턴스</param>
    /// <returns>상태 로직을 위한 코루틴</returns>
    public IEnumerator Execute(TowerWeapon weapon)
    {
        while (true)
        {
            // 가장 가까운 적을 탐색
            weapon.attackTarget = weapon.FindClosetAttackTarget();

            // 적을 찾았으면 '공격 시도' 상태로 전환하고 현재 코루틴 종료
            if (weapon.attackTarget != null)
            {
                weapon.ChangeState(new TryAttackState());
                yield break;
            }

            // 적을 찾지 못했으면 다음 프레임까지 대기
            yield return null;
        }
    }
}