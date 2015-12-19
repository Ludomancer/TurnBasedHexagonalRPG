using System.Collections.Generic;
using Settworks.Hexagons;

class AbilityAimPlayer : AbilityAim
{
    void OnEnable()
    {
        if (_ability.IsInitialized)
        {
            _ability.Target = TurnManager.Instance.GetActivePlayer().gameObject;
        }
    }

    public override List<HexTile> GetAvailableHexes(HexGrid hexGrid)
    {
        return null;
    }
}
