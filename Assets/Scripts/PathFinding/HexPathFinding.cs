using System;
using System.Collections.Generic;
using Settworks.Hexagons;

class HexPathFinding : IShortestPath<HexCoord, HexCoord>
{
    private readonly HexGrid _hexGrid;
    public HexPathFinding(HexGrid hexGrid)
    {
        _hexGrid = hexGrid;
    }

    public float Heuristic(HexCoord fromLocation, HexCoord toLocation)
    {
        return Math.Abs(fromLocation.q - toLocation.q) + Math.Abs(fromLocation.r - toLocation.r);
    }

    public List<HexCoord> Expand(HexCoord fromLocation)
    {
        //Can be made Linq but foreach is more readable.
        List<HexCoord> neighbourTiles = new List<HexCoord>();
        foreach (HexCoord neighborCoord in fromLocation.Neighbors())
        {
            //Make sure the coordinate is inside the board
            if (!_hexGrid.IsCordinateValid(neighborCoord)) continue;

            HexTile hexTile = _hexGrid.GetHexTile(neighborCoord);
            //Make sure it is passable
            if (hexTile.IsPassable)
            {
                neighbourTiles.Add(neighborCoord);
            }
        }
        return neighbourTiles;
    }

    public float ActualCost(HexCoord fromLocation, HexCoord toLocation)
    {
        return Heuristic(fromLocation, toLocation) * _hexGrid.GetHexTile(toLocation).MovementCost;
    }

    public HexCoord ApplyAction(HexCoord state, HexCoord action)
    {
        return action;
    }

    public bool Comparison(HexCoord fromLocation, HexCoord toLocation)
    {
        return fromLocation == toLocation;
    }

    public HexCoord DefaultValue()
    {
        return HexCoord.INVALID;
    }
}
