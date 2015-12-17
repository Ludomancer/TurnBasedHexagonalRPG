using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    private bool _isAiControlled;
    private int _id = -1;

    List<HexUnit> _units = new List<HexUnit>();

    public int Id
    {
        get { return _id; }
    }

    public bool IsAiControlled
    {
        get { return _isAiControlled; }
    }

    // Use this for initialization
    public void Initialize(int id, bool isAiControlled)
    {
        _id = id;
        _isAiControlled = isAiControlled;
    }

    public void AddUnit(HexUnit newUnit)
    {
        if (newUnit == null) throw new NullReferenceException("newUnit is null!");
        newUnit.OwnerId = Id;
        _units.Add(newUnit);
    }

    public void RemoveUnit(HexUnit unit)
    {
        if (unit == null) throw new NullReferenceException("unit is null!");
        _units.Remove(unit);
    }

    public HexUnit GetUnit(int i)
    {
        return _units[i];
    }

    public int UnitCount
    {
        get
        {
            return _units == null ? 0 : _units.Count;
        }
    }

    public override string ToString()
    {
        return "Player " + (Id + 1);
    }
}
