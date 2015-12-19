using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(Ability))]
abstract class CastAbility : MonoBehaviour
{
    protected Ability _ability;

    protected bool _isBusy = false;

    public bool IsBusy
    {
        get { return _isBusy; }
    }

    void Awake()
    {
        _ability = GetComponent<Ability>();
#if UNITY_EDITOR
        if (GetComponents<CastAbility>().Length > 1) throw new Exception("An ability can only have 1 Cast Mode");
#endif
        Init();
    }

    protected virtual void Init() { }

    public virtual void Cast()
    {
        if (!_isBusy && _ability.Target)
        {
            StartCoroutine(CastRoutine());
        }
    }

    private IEnumerator CastRoutine()
    {
        _isBusy = true;
        //Use interfaces instead of send message for type safety.
        Component[] castables = GetComponents(typeof(ICastable));
        for (int i = 0; i < castables.Length; i++)
        {
            ((ICastable)castables[i]).Cast();
        }

        while (true)
        {
            bool isAnyBusy = false;
            for (int i = 0; i < castables.Length; i++)
            {
                if (((ICastable)castables[i]).IsBusy())
                {
                    isAnyBusy = true;
                    break;
                }
            }
            if (!isAnyBusy) break;
            yield return null;
        }

        _isBusy = false;

        _ability.AbilityCastCompleted(true);
    }
}
