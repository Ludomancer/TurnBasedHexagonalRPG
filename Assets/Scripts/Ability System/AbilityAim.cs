using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for Target selectors of Abilit class.
/// </summary>
[RequireComponent(typeof (Ability))]
internal abstract class AbilityAim : MonoBehaviour
{
    #region Fields

    protected Ability _ability;

    #endregion

    #region Other Members

    private void Awake()
    {
        _ability = GetComponent<Ability>();
#if UNITY_EDITOR
        if (GetComponents<AbilityAim>().Length > 1) throw new Exception("An ability can only have 1 Aim Mode");
#endif
        Init();
    }

    protected virtual void Init()
    {
    }

    public abstract List<HexTile> GetAvailableHexes(HexGrid hexGrid);

    #endregion
}