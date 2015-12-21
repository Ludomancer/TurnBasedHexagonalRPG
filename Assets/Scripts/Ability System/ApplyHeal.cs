using System;
using UnityEngine;

/// <summary>
/// Heals target.
/// </summary>
[RequireComponent(typeof (Ability))]
internal class ApplyHeal : MonoBehaviour, IAppliable
{
    #region Fields

    [SerializeField]
    private int _healAmount;

    #endregion

    #region Other Members

    private void Awake()
    {
#if UNITY_EDITOR
        if (GetComponents<ApplyHeal>().Length > 1) throw new Exception("An ability can only have 1 ApplyHeal");
#endif
    }

    #endregion

    #region IAppliable Members

    public void Apply()
    {
        HexUnit target = GetComponent<Ability>().Target.GetComponent<HexUnit>();
        if (!target) throw new Exception("ApplyHeal requirest HexUnit as Ability target");
        target.HealthLeft += _healAmount;
    }

    #endregion
}