using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region Fields

    private int _id = -1;
    private bool _isAiControlled;
    private readonly ListQueue<HexUnit> _units = new ListQueue<HexUnit>();

    #endregion

    #region Properties

    public int Id
    {
        get { return _id; }
    }

    public bool IsAiControlled
    {
        get { return _isAiControlled; }
    }

    public int UnitCount
    {
        get { return _units == null ? 0 : _units.Count; }
    }

    #endregion

    #region Other Members

    // Use this for initialization
    public void Initialize(int id, bool isAiControlled)
    {
        _id = id;
        _isAiControlled = isAiControlled;
    }

    public void Enqueue(HexUnit newUnit)
    {
        if (newUnit == null) throw new NullReferenceException("newUnit is null!");
        newUnit.OwnerId = Id;
        _units.Enqueue(newUnit);
    }

    public HexUnit PeekUnit(int i)
    {
        return _units[i];
    }

    public HexUnit GetNextUnit()
    {
        HexUnit hexUnit = _units.Dequeue();
        hexUnit.SelectSkill(-1);
        hexUnit.ToggleWeapons(true);
        _units.Enqueue(hexUnit);
        return hexUnit;
    }

    public void RemoveUnit(HexUnit hexUnit)
    {
        hexUnit.OwnerId = -1;
        _units.Remove(hexUnit);
    }

    public override string ToString()
    {
        return "Player " + (Id + 1);
    }

    #endregion
}