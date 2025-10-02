using System.Collections;
using UnityEngine;

/// <summary>
/// 타워 무기의 상태를 정의하는 인터페이스입니다.
/// 각 상태는 이 인터페이스를 구현하여 무기의 특정 행동을 정의합니다.
/// </summary>
public interface IWeaponState
{
    /// <summary>
    /// 현재 상태의 로직을 실행합니다.
    /// </summary>
    /// <param name="weapon">상태를 실행할 TowerWeapon 인스턴스</param>
    /// <returns>상태 로직을 위한 코루틴</returns>
    IEnumerator Execute(TowerWeapon weapon);
}
