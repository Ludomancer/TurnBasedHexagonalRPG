using UnityEngine;
using System.Collections;
using System.Runtime.Remoting;
using Settworks.Hexagons;
using UnityEngine.EventSystems;

public class HexTile : MonoBehaviour, IPointerClickHandler
{
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("PointerEventData.OnPointerClick: " + eventData.pointerPress.name, eventData.pointerPress);

        HexGrid hexGrid = FindObjectOfType<HexGrid>();
        HexCoord myHexCoord = hexGrid.GetHexAtXYCoordinate(new Vector2(transform.position.x, transform.position.z));
        Debug.Log("Self: " + myHexCoord.ToString());
        foreach (HexCoord hexCoord in hexGrid.HexesInRange(myHexCoord, 2))
        {
            Debug.Log("Range: " + hexCoord.ToString() + ", dis: " + HexCoord.Distance(hexCoord, myHexCoord));
        }
    }
}
