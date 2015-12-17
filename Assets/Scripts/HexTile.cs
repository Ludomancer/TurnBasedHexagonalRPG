using System;
using UnityEngine;
using Settworks.Hexagons;
using UnityEngine.EventSystems;


public class HexTile : MonoBehaviour, IPointerClickHandler
{
    public const string ON_HEX_CLICKED_EVENT_NAME = "OnHexClicked";

    [SerializeField]
    private State _idleState;
    [SerializeField]
    private State _highlightState;
    [SerializeField]
    private State _notPassableState;
    [SerializeField]
    private int _movementCost;

    private HexCoord _hexCoord;
    private SpriteRenderer _spriteRenderer;
    private bool _isPassable;
    private GameObject _occupyingObject;

    public HexCoord Coord
    {
        get { return _hexCoord; }
    }

    protected SpriteRenderer SpriteRenderer
    {
        get
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            return _spriteRenderer;
        }
    }

    public bool IsPassable
    {
        get { return _isPassable && !IsOccupied; }
    }

    public bool IsOccupied
    {
        get { return _occupyingObject != null; }
    }

    public int MovementCost
    {
        get { return _movementCost; }
    }

    public GameObject OccupyingObject
    {
        get { return _occupyingObject; }
        set { _occupyingObject = value; }
    }

    public void SetCoord(int q, int r, bool isPassable = true)
    {
        SetCoord(new HexCoord(q, r));
        _isPassable = isPassable;
        AutoSetState();
    }

    public void SetCoord(HexCoord coord, bool isPassable = true)
    {
        _hexCoord = coord;
        name = coord.ToString();
        _isPassable = isPassable;
        AutoSetState();
    }

    public void HighlightTile(Color c)
    {
        SpriteRenderer.sprite = _highlightState.Sprite;
        SpriteRenderer.color = c;
    }


    public void HighlightTile()
    {
        SpriteRenderer.sprite = _highlightState.Sprite;
        SpriteRenderer.color = _highlightState.Color;
    }

    public void ResetToIdle()
    {
        SpriteRenderer.sprite = _idleState.Sprite;
        SpriteRenderer.color = _idleState.Color;
    }

    public void SetImpassable()
    {
        SpriteRenderer.sprite = _notPassableState.Sprite;
        SpriteRenderer.color = _notPassableState.Color;
        _isPassable = false;
    }

    public void AutoSetState()
    {
        if (_isPassable) ResetToIdle();
        else SetImpassable();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Messenger.Broadcast<HexTile>(ON_HEX_CLICKED_EVENT_NAME, this);
    }

    [Serializable]
    protected struct State
    {
        [SerializeField]
        Sprite _sprite;
        [SerializeField]
        Color _color;

        public State(Sprite sprite, Color color)
        {
            _sprite = sprite;
            _color = color;
        }

        public Sprite Sprite
        {
            get { return _sprite; }
        }

        public Color Color
        {
            get { return _color; }
        }
    }
}
