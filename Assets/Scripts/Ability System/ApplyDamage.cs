using System;
using UnityEngine;

[RequireComponent(typeof (Ability))]
internal class ApplyDamage : MonoBehaviour, IAppliable
{
    #region Fields

    [SerializeField]
    private bool _canBeDodged;

    [SerializeField]
    private int _damageAmount;

    [SerializeField]
    private bool _ignoreArmor;

    #endregion

    #region Other Members

    private void Awake()
    {
#if UNITY_EDITOR
        if (GetComponents<ApplyDamage>().Length > 1) throw new Exception("An ability can only have 1 ApplyDamage");
#endif
    }

    #endregion

    #region IAppliable Members

    public void Apply()
    {
        HexUnit target = GetComponent<Ability>().Target.GetComponent<HexUnit>();
        if (!target) throw new Exception("ApplyDamage requirest HexUnit as Ability target");

        if (_canBeDodged && UnityEngine.Random.value < target.DodgeChance)
        {
            Messenger.Broadcast(HexUnit.ON_DODGED, target);
        }
        else
        {
            if (_ignoreArmor) target.HealthLeft -= _damageAmount;
            else target.HealthLeft -= Mathf.Max(1, target.Armor - _damageAmount);
        }
    }

    #endregion
}