using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
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
    private UnitCard _unitCard;

    [SerializeField]
    private Text _playerName;

    #endregion

    #region Methods

    private void Awake()
    {
        _transform = transform;
        Messenger.AddListener<HexUnit>(GameManager.UNIT_SELECTION_CHANGED_EVENT_NAME, OnUnitSelectionChanged);
        Messenger.AddListener<Player>(TurnManager.END_TURN_ANIM_NAME, OnEndTurn);
        Messenger.AddListener<Destructable, int>(Destructable.ON_HEALTH_CHANGED, OnUnitHealthChanged); ;
        Messenger.AddListener<HexUnit>(HexUnit.ON_DODGED, OnUnitDodged);
    }

    private void OnUnitDodged(HexUnit hexUnit)
    {
        ShowPopup("Miss!", hexUnit.transform.position, Color.white);
    }

    private void OnUnitHealthChanged(Destructable destructable, int delta)
    {
        ShowPopup(delta + "!", destructable.transform.position, delta > 0 ? Color.green : Color.red);
    }

    private void OnEndTurn(Player player)
    {
        _playerName.text = player.ToString();
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
        IsTransitioning = false;
    }

    private IEnumerator DismissPanelTransition()
    {
        yield return null;
        PoolManager.instance.RecycleAll();
        IsTransitioning = false;
    }

    public void ShowPopup(string text, Vector3 position, Color c)
    {
        ShowPopup(text, (Vector2)Camera.main.WorldToViewportPoint(position), c);
    }

    public void ShowPopup(string text, Vector2 viewportPoint, Color c)
    {
        GameObject popup = PoolManager.instance.GetObjectForName(_scorePopupPrefab.name, false, Vector3.zero, Quaternion.identity, null);
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