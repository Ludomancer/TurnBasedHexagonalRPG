using UnityEngine;

/// <summary>
/// Automatically casts ability when a target is selected.
/// </summary>
internal class AutoCastOnTargetSelected : CastAbility, IOnTargetSelected
{
    #region IOnTargetSelected Members

    public void OnTargetSelectionChanged(GameObject target)
    {
        if (target) Cast();
    }

    #endregion
}