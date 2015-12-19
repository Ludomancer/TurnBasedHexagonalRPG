using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(Ability))]
class ApplyHeal : MonoBehaviour, IAppliable
{
    [SerializeField]
    private int _healAmount;

    void Awake()
    {
#if UNITY_EDITOR
        if (GetComponents<ApplyHeal>().Length > 1) throw new Exception("An ability can only have 1 ApplyHeal");
#endif
    }

    public void Apply()
    {
        HexUnit target = GetComponent<Ability>().Target.GetComponent<HexUnit>();
        if (!target) throw new Exception("ApplyHeal requirest HexUnit as Ability target");
        target.HealthLeft += _healAmount;
    }
}
