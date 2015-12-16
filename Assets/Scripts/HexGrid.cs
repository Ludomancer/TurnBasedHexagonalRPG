using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Settworks.Hexagons;
using UnityEditor;

public class HexGrid : MonoBehaviour
{
    [SerializeField]
    private Collider _board;
    [SerializeField]
    private GameObject _hexPrefab;
    [SerializeField]
    private int _widthInHexes;
    private int _heightInHexes;

    private Transform _transform;

    private HexCoord[][] _hexGrid;

    private float _hexSize;
    private float _hexScaleFactor;

    private Vector3 _startPos;

    // Use this for initialization
    void Start()
    {
        if (_hexPrefab == null) throw new ArgumentNullException("_prefab");
        if (_board == null) throw new ArgumentNullException("_board");

        _transform = transform;

        float boardWidth = _board.bounds.size.x;
        float boardHeight = _board.bounds.size.z;
        float gameObjectScale = (boardWidth / _widthInHexes);

        //Multiply with height to width ratio.
        float sideToSideSize = gameObjectScale / (Mathf.Sqrt(3) / 2f);
        _hexPrefab.transform.localScale = Vector3.one * (sideToSideSize);

        float sideSize = _hexPrefab.transform.localScale.z / 2f;

        _hexScaleFactor = 1f / sideSize;

        //2 Vertical hexagons coupled.
        float totalHexHeightCoupled = Mathf.RoundToInt(boardHeight / (sideSize * 3));

        //Find total hexagon count.
        _heightInHexes = (int)(totalHexHeightCoupled * 2);

        //Find total hexagon height.
        float totalHexHeight = totalHexHeightCoupled * sideSize * 3;

        //Clip overflow.
        if (totalHexHeight > boardHeight)
        {
            if (_heightInHexes % 2 != 0) totalHexHeight -= sideSize * 2;
            else totalHexHeight -= sideSize;
            _heightInHexes--;
        }

        float verticalPadding = Mathf.Max(0, (boardHeight - totalHexHeight) / 2f);

        _startPos = _transform.position -
           new Vector3((boardWidth - gameObjectScale) * 0.5f,
           0,
           (boardHeight - sideToSideSize) * 0.5f - verticalPadding);

        _hexGrid = new HexCoord[_heightInHexes][];
        //for (int r = 0; r < _heightInHexes; r++)
        //{
        //    int tempWidth = r % 2 == 0 ? _widthInHexes : _widthInHexes - 1;
        //    for (int q = 0; q < tempWidth; q++)
        //    {
        //        _hexGrid[r] = new HexCoord[tempWidth];
        //        _hexGrid[r][q] = new HexCoord(q - (r - (r & 1)) / 2, r);
        //        Vector2 hexPos = _hexGrid[r][q].Position();
        //        Vector3 worldPosition = _startPos + new Vector3(hexPos.x, 0, hexPos.y) / _hexScaleFactor;
        //        GameObject spawnedHex = Instantiate(_hexPrefab, worldPosition, Quaternion.identity) as GameObject;
        //        spawnedHex.name = _hexGrid[r][q].ToString() +  "(q:" + q + ", r:" + r + ")" + GetHexAtQRCoordinate(q,r);
        //        spawnedHex.transform.parent = _transform;
        //    }
        //}

        for (int r = 0; r < _heightInHexes; r++)
        {
            int tempWidth = r % 2 == 0 ? _widthInHexes : _widthInHexes - 1;
            _hexGrid[r] = new HexCoord[tempWidth];
            for (int q = 0; q < tempWidth; q++)
            {
                _hexGrid[r][q] = new HexCoord(q - (r - (r & 1)) / 2, r);
                Vector2 hexPos = _hexGrid[r][q].Position();
                Debug.Log("(index: q:" + q + ", r:" + r + "), data stored: " + _hexGrid[r][q] + "-------");
                Vector3 worldPosition = _startPos + new Vector3(hexPos.x, 0, hexPos.y) / _hexScaleFactor;
                GameObject spawnedHex = Instantiate(_hexPrefab, worldPosition, Quaternion.identity) as GameObject;
                spawnedHex.name = _hexGrid[r][q].ToString() + "(q:" + q + ", r:" + r + ")";
                spawnedHex.transform.parent = _transform;

                HexCoord test = GetHexAtXYCoordinate(new Vector2(worldPosition.x, worldPosition.z));
                if (test != _hexGrid[r][q])
                {
                    Debug.LogError("Entry: " + _hexGrid[r][q]);
                    Debug.LogError("Output: " + test);
                }

                Debug.Log("------------------------------");
            }
        }
    }

    /// <param name="x">Column</param>
    /// <param name="y">Row</param>
    public HexCoord GetHexAtQRCoordinate(Vector2 coordinate)
    {
        return GetHexAtQRCoordinate(Mathf.RoundToInt(coordinate.x), Mathf.RoundToInt(coordinate.y));
    }

    /// <param name="q">Column</param>
    /// <param name="r">Row</param>
    public bool IsQRCoordinateValid(int q, int r)
    {
        if (_hexGrid.Length > 0 && r >= 0 && r < _hexGrid.Length)
        {
            int calculatedQ = (q + r / 2);
            return (_hexGrid[r].Length > 0 && calculatedQ >= 0 && calculatedQ < _hexGrid[r].Length);
        }
        return false;
    }

    /// <param name="q">Column</param>
    /// <param name="r">Row</param>
    public HexCoord GetHexAtQRCoordinate(int q, int r)
    {
        Debug.Log("Trying: " + "(q:" + (q + r / 2) + ", r:" + r + ")");
        return _hexGrid[r][q + r / 2];
    }

    /// <param name="q">Column</param>
    /// <param name="r">Row</param>
    public HexCoord GetHexAtXYCoordinate(Vector2 coordinate)
    {
        Vector2 noOffset = new Vector2(coordinate.x - _startPos.x, coordinate.y - _startPos.z) * _hexScaleFactor;
        Debug.Log("noOffset: " + noOffset);
        Vector2 qrVector2 = HexCoord.VectorXYtoQR(noOffset);
        Debug.Log("qrVector2: " + qrVector2);
        return GetHexAtQRCoordinate(qrVector2);
    }

    public IEnumerable<HexCoord> HexesInRange(HexCoord centerHex, int range)
    {
        for (int r = centerHex.r - range; r <= centerHex.r + range; r++)
        {
            for (int q = centerHex.q - range; q <= centerHex.q + range; q++)
            {
                if (IsQRCoordinateValid(q, r))
                {
                    HexCoord temp = GetHexAtQRCoordinate(q, r);
                    if (Mathf.Abs(HexCoord.Distance(centerHex, temp)) <= range)
                        yield return temp;
                }
            }
        }
    }
}
