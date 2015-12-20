using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof (Ability))]
internal class ShootProjectile : MonoBehaviour, ICastable
{
    #region Fields

    private bool _isBusy;

    [SerializeField]
    private GameObject _projectilePrefab;

    public float travelSpeed;

    #endregion

    #region Other Members

    private IEnumerator UpdateSkill()
    {
        _isBusy = true;
        Ability ability = GetComponent<Ability>();
        if (!ability.Target) throw new Exception("ShootProjectile requirest HexUnit as Ability target");
        float progress = Time.deltaTime * travelSpeed;
        Vector3 startPos = transform.position;
        Vector3 endPos = ability.Target.transform.position;

        GameObject effect = PoolManager.instance.GetObjectForName(_projectilePrefab.name, false, startPos,
            Quaternion.identity, null);

        effect.transform.LookAt(endPos);

        while (progress < 1)
        {
            effect.transform.position = Vector3.Lerp(startPos, endPos, progress);
            yield return null;
            progress += Time.deltaTime * travelSpeed;
        }
        effect.transform.position = endPos;

        //Apply all effects
        foreach (IAppliable appliable in GetComponents(typeof (IAppliable)))
        {
            appliable.Apply();
        }

        PoolManager.instance.Recycle(effect);
        _isBusy = false;
    }

    #endregion

    #region ICastable Members

    public void Cast()
    {
        StartCoroutine(UpdateSkill());
    }

    public bool IsBusy()
    {
        return _isBusy;
    }

    #endregion
}