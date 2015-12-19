using System;
using UnityEngine;

[RequireComponent(typeof(Ability))]
class SpawnEffect : MonoBehaviour, IAppliable
{
    [SerializeField]
    protected GameObject _effectPrefab;

    public void Apply()
    {
        if (_effectPrefab)
        {
            PoolManager.instance.GetObjectForName(_effectPrefab.name, false, GetComponent<Ability>().Target.transform.position,
Quaternion.identity, null);
        }
    }
}
