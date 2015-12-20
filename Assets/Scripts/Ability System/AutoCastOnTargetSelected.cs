using UnityEngine;

internal class AutoCastOnTargetSelected : CastAbility, IOnTargetSelected
{
    #region IOnTargetSelected Members

    public void OnTargetSelectionChanged(GameObject target)
    {
        if (target) Cast();
    }

    #endregion
}