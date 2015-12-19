using UnityEngine;

internal class AutoCast : CastAbility, IOnTargetSelected
{
    #region IOnTargetSelected Members

    public void OnTargetSelectionChanged(GameObject target)
    {
        if (target) Cast();
    }

    #endregion
}