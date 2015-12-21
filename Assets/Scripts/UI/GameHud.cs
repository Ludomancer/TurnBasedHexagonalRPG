using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal class GameHud : PanelBase
{
    #region Enumerations

    #endregion

    #region Variables

    [SerializeField]
    private GameObject _scorePopupPrefab;

    private Transform _transform;

    [SerializeField]
    private GameObject _barPrefab;

    [SerializeField]
    private UnitCard _unitCard;

    [SerializeField]
    private Text _playerName;

    [SerializeField]
    private Vector3 _healthBarOffset;

    private readonly Dictionary<GameObject, UnitHealthBar> _healthBars = new Dictionary<GameObject, UnitHealthBar>();

    #endregion

    #region Methods

    private void Awake()
    {
        _transform = transform;
        Messenger.AddListener<HexUnit>(GameManager.UNIT_SELECTION_CHANGED, OnUnitSelectionChanged);
        Messenger.AddListener(TurnManager.ON_TURN_END, OnEndTurn);
        Messenger.AddListener<Destructable, int>(Destructable.ON_HEALTH_CHANGED, OnUnitHealthChanged);
        Messenger.AddListener<HexUnit>(HexUnit.ON_DODGED, OnUnitDodged);
        Messenger.AddListener<GameObject>(Player.ON_UNIT_REMOVED, OnUnitRemoved);
        Messenger.AddListener<HexUnit>(Player.ON_UNIT_ADDED, OnUnitAdded);
    }

    private void OnUnitAdded(HexUnit newUnit)
    {
        if (_healthBars.ContainsKey(newUnit.gameObject))
            throw new Exception("Unit already added to HealthBar list. Possible double spawn?");
        GameObject healtBarGo = PoolManager.instance.GetObjectForName(_barPrefab.name, false);
        healtBarGo.transform.SetParent(_transform, false);
        UnitHealthBar unitHealth = healtBarGo.GetComponent<UnitHealthBar>();
        unitHealth.GetRectTransform.anchorMin = -Vector2.one;
        unitHealth.GetRectTransform.anchorMax = -Vector2.one;
        _healthBars.Add(newUnit.gameObject, unitHealth);
    }

    private void OnUnitRemoved(GameObject go)
    {
        _healthBars[go].GetSlider.value = 1;
        PoolManager.instance.Recycle(_healthBars[go].gameObject);
        _healthBars.Remove(go);
    }

    private void LateUpdate()
    {
        foreach (KeyValuePair<GameObject, UnitHealthBar> unitHealth in _healthBars)
        {
            if (unitHealth.Key.activeSelf)
            {
                Vector2 unitViewPoint =
                    Camera.main.WorldToViewportPoint(_healthBarOffset + unitHealth.Key.transform.position);
                unitHealth.Value.GetRectTransform.anchorMin = unitViewPoint;
                unitHealth.Value.GetRectTransform.anchorMax = unitViewPoint;
            }
        }
    }

    private void OnUnitDodged(HexUnit hexUnit)
    {
        ShowPopup("Miss!", hexUnit.transform.position, Color.white);
    }

    private void OnUnitHealthChanged(Destructable destructable, int delta)
    {
        ShowPopup(delta + "!", destructable.transform.position, delta > 0 ? Color.green : Color.red);

        if (!_healthBars.ContainsKey(destructable.gameObject)) throw new Exception("Unit does not have a HealthBar!");
        _healthBars[destructable.gameObject].GetSlider.value = destructable.HealthLeft / (float)destructable.MaxHealth;
    }

    private void OnEndTurn()
    {
        _playerName.text = TurnManager.Instance.GetActivePlayer().ToString();
    }

    private void OnUnitSelectionChanged(HexUnit unit)
    {
        if (unit != null)
        {
            _unitCard.gameObject.SetActive(true);
            _unitCard.SetData(unit);
        }
        else _unitCard.gameObject.SetActive(false);
    }

    public override void BringPanel()
    {
        IsTransitioning = true;
        StartCoroutine("BringPanelTransition");
    }

    public override void DismissPanel()
    {
        IsTransitioning = true;
        StartCoroutine("DismissPanelTransition");
    }

    private IEnumerator BringPanelTransition()
    {
        yield return null;
        //Wait and hanlde transitions if there are any.
        IsTransitioning = false;
    }

    private IEnumerator DismissPanelTransition()
    {
        foreach (KeyValuePair<GameObject, UnitHealthBar> unitHealth in _healthBars)
        {
            unitHealth.Value.GetSlider.value = 1;
            PoolManager.instance.Recycle(unitHealth.Value.gameObject);
        }
        _healthBars.Clear();
        yield return null;
        //Wait and hanlde transitions if there are any.
        IsTransitioning = false;
    }

    public void ShowPopup(string text, Vector3 position, Color c)
    {
        ShowPopup(text, (Vector2)Camera.main.WorldToViewportPoint(position), c);
    }

    public void ShowPopup(string text, Vector2 viewportPoint, Color c)
    {
        GameObject popup = PoolManager.instance.GetObjectForName(_scorePopupPrefab.name, false, Vector3.zero,
            Quaternion.identity, null);
        if (popup)
        {
            popup.transform.SetParent(_transform, false);
            PopupText popupText = popup.GetComponent<PopupText>();
            popupText.RectTransform.anchorMin = viewportPoint;
            popupText.RectTransform.anchorMax = viewportPoint;
            popupText.Show(text,
                PopupText.DEFAULT_FADE_DELAY,
                PopupText.DEFAULT_FADE_IN_DURATION,
                PopupText.DEFAULT_FADE_OUT_DURATION,
                c, PopupText.PopupAnimMode.Scale,
                false, false);
        }
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}