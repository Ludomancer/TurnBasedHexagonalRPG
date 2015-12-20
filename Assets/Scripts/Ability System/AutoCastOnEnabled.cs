using UnityEngine;

class AutoCastOnEnabled : CastAbility
{
    void OnEnable()
    {
        Debug.Log("Cast");
        Cast();
    }
}
