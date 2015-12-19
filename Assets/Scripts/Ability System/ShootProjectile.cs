using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ability))]
class ShootProjectile : MonoBehaviour, ICastable
{
    public float travelSpeed;

    [SerializeField]
    private GameObject _projectilePrefab;

    private bool _isBusy;

    public void Cast()
    {
        StartCoroutine(UpdateSkill());
    }

    public bool IsBusy()
    {
        return _isBusy;
    }

    IEnumerator UpdateSkill()
    {
        _isBusy = true;
        Ability ability = GetComponent<Ability>();
        if (!ability.Target) throw new Exception("ShootProjectile requirest HexUnit as Ability target");
        float progress = Time.deltaTime * travelSpeed;
        Vector3 startPos = transform.position;
        Vector3 endPos = ability.Target.transform.position;

        GameObject effect = PoolManager.instance.GetObjectForName(_projectilePrefab.name, false, startPos, Quaternion.identity, null);

        while (progress < 1)
        {
            effect.transform.position = Vector3.Lerp(startPos, endPos, progress);
            yield return null;
            progress += Time.deltaTime * travelSpeed;
        }
        effect.transform.position = endPos;

        //Apply all effects
        foreach (IAppliable appliable in GetComponents(typeof(IAppliable)))
        {
            appliable.Apply();
        }

        PoolManager.instance.Recycle(effect);
        _isBusy = false;
    }
}
