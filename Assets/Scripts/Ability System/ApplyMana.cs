using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

internal class ApplyMana : MonoBehaviour, IAppliable
{
    #region Fields

    [SerializeField]
    private int _manaAmount;

    #endregion

    #region Other Members

    private void Awake()
    {
#if UNITY_EDITOR
        if (GetComponents<ApplyMana>().Length > 1) throw new Exception("An ability can only have 1 ApplyMana");
#endif
    }

    #endregion

    #region IAppliable Members

    public void Apply()
    {
        HexUnit target = GetComponent<Ability>().Target.GetComponent<HexUnit>();
        if (!target) throw new Exception("ApplyHeal requirest HexUnit as Ability target");
        target.ManaLeft += _manaAmount;
    }

    #endregion
}
