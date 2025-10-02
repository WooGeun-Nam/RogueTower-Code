using System.Collections;
using UnityEngine;

public class SearchMultiTargetState : IWeaponState
{
    public IEnumerator Execute(TowerWeapon weapon)
    {
        while (true)
        {
            weapon.attackMultiTarget = weapon.FindMultiAttackTarget();

            if (weapon.attackMultiTarget.Count > 0)
            {
                weapon.ChangeState(new TryAttackState());
                yield break;
            }

            yield return null;
        }
    }
}
