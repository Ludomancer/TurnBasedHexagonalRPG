using UnityEngine;

class AutoCast : CastAbility, IOnTargetSelected
{
    public void OnTargetSelectionChanged(GameObject target)
    {
        if (target) Cast();
    }
}
