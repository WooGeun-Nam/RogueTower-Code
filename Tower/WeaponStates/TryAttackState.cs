using System.Collections;
using UnityEngine;

/// <summary>
/// 타워 무기의 '공격 시도' 상태를 정의하는 클래스입니다.
/// 이 상태에서는 탐색된 적을 공격하고, 공격이 불가능해지면 '적 탐색' 상태로 전환합니다.
/// </summary>
public class TryAttackState : IWeaponState
{
    private const float PROJECTILE_ATTACK_RATIO = 0.35f; // 투사체 공격이 발생하는 애니메이션 시간 비율 (100% 지점)
    private const float INSTANT_ATTACK_RATIO = 0.2f;    // 즉발 공격이 발생하는 애니메이션 시간 비율 (50% 지점)

    /// <summary>
    /// '공격 시도' 상태의 로직을 실행합니다.
    /// </summary>
    /// <param name="weapon">상태를 실행할 TowerWeapon 인스턴스</param>
    /// <returns>상태 로직을 위한 코루틴</returns>
    public IEnumerator Execute(TowerWeapon weapon)
    {
        // 단일 공격 타워의 경우
        if (weapon.attackTarget != null)
        {
            if (weapon.IsPossibleToAttackTarget() == false)
            {
                weapon.ChangeState(new SearchTargetState());
                yield break;
            }
        }
        // 다중 공격 타워의 경우
        else if (weapon.attackMultiTarget.Count > 0)
        {
            // 다중 공격 타워는 항상 새로운 적을 탐색해야 하므로 바로 공격 후 상태 변경
        }
        // 공격 대상이 없는 경우
        else
        {
            weapon.ChangeState(new SearchTargetState());
            yield break;
        }

        // TowerWeapon의 공격 로직 코루틴 호출
        weapon.FlipTowerTowardsTarget(); // 적을 향해 타워 방향 전환
        weapon.PlayAttackAnimation();

        switch (weapon.attackTimingType)
        {
            case AttackTimingType.Projectile:
                // 투사체 타워: 애니메이션 재생 시간의 100% 지점에서 공격
                yield return new WaitForSeconds(weapon.AttackSpeedToRate() * PROJECTILE_ATTACK_RATIO);
                yield return weapon.CallTryAttackCoroutine();
                // 남은 애니메이션 시간만큼 대기
                yield return new WaitForSeconds(weapon.AttackSpeedToRate() * (1.0f - PROJECTILE_ATTACK_RATIO));
                break;
            case AttackTimingType.Instant:
                // 즉발 타워: 애니메이션 재생 시간의 50% 지점에서 공격
                yield return new WaitForSeconds(weapon.AttackSpeedToRate() * INSTANT_ATTACK_RATIO);
                yield return weapon.CallTryAttackCoroutine();
                // 남은 애니메이션 시간만큼 대기
                yield return new WaitForSeconds(weapon.AttackSpeedToRate() * (1.0f - INSTANT_ATTACK_RATIO));
                break;
            case AttackTimingType.Continuous:
                // 지속 공격 타워: 애니메이션 시작과 동시에 공격, 별도의 대기 없음
                yield return weapon.CallTryAttackCoroutine();
                break;
        }

        weapon.ReturnToSearchState(); // 공격 사이클 완료 후 탐색 상태로 복귀
    }
}
