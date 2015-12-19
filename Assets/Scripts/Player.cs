using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    private bool _isAiControlled;
    private int _id = -1;

    readonly ListQueue<HexUnit> _units = new ListQueue<HexUnit>();

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
