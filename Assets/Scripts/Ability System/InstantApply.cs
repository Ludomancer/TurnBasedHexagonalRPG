using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class InstantApply : MonoBehaviour, ICastable
{
    public void Cast()
    {
        //Apply all effects
        foreach (IAppliable appliable in GetComponents(typeof(IAppliable)))
        {
            appliable.Apply();
        }
    }

    public bool IsBusy()
    {
        return false;
    }
}
