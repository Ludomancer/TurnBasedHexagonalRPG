using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Settworks.Hexagons;
using UnityEngine;

[RequireComponent(typeof(Ability))]
abstract class AbilityAim : MonoBehaviour
{
    protected Ability _ability;

    void Awake()
    {
        _ability = GetComponent<Ability>();
#if UNITY_EDITOR
        if (GetComponents<AbilityAim>().Length > 1) throw new Exception("An ability can only have 1 Aim Mode");
#endif
        Init();
    }

    protected virtual void Init() { }
    public abstract List<HexTile> GetAvailableHexes(HexGrid hexGrid);
}
