using UnityEngine;

[RequireComponent(typeof (Ability))]
internal class SpawnEffect : MonoBehaviour, IAppliable
{
    #region Fields

    [SerializeField]
    protected GameObject _effectPrefab;

    #endregion

    #region IAppliable Members

    public void Apply()
    {
        if (_effectPrefab)
        {
            PoolManager.instance.GetObjectForName(_effectPrefab.name, false,
                GetComponent<Ability>().Target.transform.position,
                Quaternion.identity, null);
        }
    }

    #endregion
}