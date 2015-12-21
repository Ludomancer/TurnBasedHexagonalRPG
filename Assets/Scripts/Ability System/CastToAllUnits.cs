using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof (Ability))]
internal class CastToAllUnits : MonoBehaviour, ICastable
{
    #region Fields

    private bool _isBusy;

    [SerializeField]
    private GameObject _subEffect;

    #endregion

    #region Other Members

    private void Awake()
    {
#if UNITY_EDITOR
        if (GetComponents<CastToAllUnits>().Length > 1)
            throw new Exception("An ability can only have 1 CastToAllUnits");
        if (!_subEffect) throw new Exception("CastToAllUnits requires a SubEffect.");
        if (!_subEffect.GetComponent<Ability>())
            throw new Exception("CastToAllUnits requires a SubEffect with an Ability attached.");

#endif
    }

    private IEnumerator CastRoutine(Player target)
    {
        _isBusy = true;
        List<Component> castables = new List<Component>();
        List<GameObject> tempInstances = new List<GameObject>(target.UnitCount);
        for (int i = 0; i < target.UnitCount; i++)
        {
            //Instantiate sub ability so we can multi-cast it.
            GameObject newAbilityIns = PoolManager.instance.GetObjectForName(_subEffect.name, false);
            tempInstances.Add(newAbilityIns);

            Ability newAbility = newAbilityIns.GetComponent<Ability>();
            Component[] subAbilityCastables = _subEffect.GetComponents(typeof (ICastable));
            castables.AddRange(subAbilityCastables);
            //Set target for sub ability.
            newAbility.Target = target.PeekUnit(i).gameObject;
        }

        //Wait for all casts to be completed.
        while (castables.Any(castable => (castable as ICastable).IsBusy()))
        {
            yield return null;
        }

        //Recycle temp abilities.
        foreach (GameObject instance in tempInstances)
        {
            PoolManager.instance.Recycle(instance);
        }

        _isBusy = false;
    }

    #endregion

    #region ICastable Members

    public void Cast()
    {
        Player target = GetComponent<Ability>().Target.GetComponent<Player>();
        if (!target) throw new Exception("CastToAllUnits requirest Player as Ability target");

        StartCoroutine(CastRoutine(target));
    }

    public bool IsBusy()
    {
        return _isBusy;
    }

    #endregion
}