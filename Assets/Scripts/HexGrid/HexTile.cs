using System;
using Settworks.Hexagons;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexTile : MonoBehaviour, IPointerClickHandler
{
    #region Fields

    public const string ON_HEX_CLICKED_EVENT_NAME = "OnHexClicked";
    private HexCoord _hexCoord;

    [SerializeField]
    private State _highlightState;

    [SerializeField]
    private State _idleState;

    private bool _isPassable;

    [SerializeField]
    private int _movementCost;

    [SerializeField]
    private State _notPassableState;

    private GameObject _occupyingObject;
    private SpriteRenderer _spriteRenderer;

    #endregion

    #region Properties

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

    #endregion

    #region Other Members

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

    #endregion

    #region IPointerClickHandler Members

    public void OnPointerClick(PointerEventData eventData)
    {
        Messenger.Broadcast(ON_HEX_CLICKED_EVENT_NAME, this);
    }

    #endregion

    #region Nested type: State

    [Serializable]
    protected struct State
    {
        #region Fields

        [SerializeField]
        private Color _color;

        [SerializeField]
        private Sprite _sprite;

        #endregion

        #region Properties

        public Sprite Sprite
        {
            get { return _sprite; }
        }

        public Color Color
        {
            get { return _color; }
        }

        #endregion

        #region Other Members

        public State(Sprite sprite, Color color)
        {
            _sprite = sprite;
            _color = color;
        }

        #endregion
    }

    #endregion
}