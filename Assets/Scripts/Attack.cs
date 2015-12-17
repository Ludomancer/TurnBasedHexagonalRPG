using UnityEngine;

public class Attack : MonoBehaviour
{
    [SerializeField]
    protected int _strenght;

    public int Strength
    {
        get { return _strenght; }
    }

    [SerializeField]
    protected int _maxRange;

    public int MaxRange
    {
        get { return _maxRange; }
    }

    [SerializeField]
    protected int _minRange;

    public int MinRange
    {
        get { return _minRange; }
    }

    [SerializeField]
    protected bool _ignoreArmor;

    public bool IgnoreArmor
    {
        get { return _ignoreArmor; }
    }
}
