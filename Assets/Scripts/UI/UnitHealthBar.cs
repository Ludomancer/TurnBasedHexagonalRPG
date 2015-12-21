using UnityEngine;
using UnityEngine.UI;

internal class UnitHealthBar : MonoBehaviour
{
    #region Fields

    private RectTransform _rectTransform;

    [SerializeField]
    private Slider _slider;

    #endregion

    #region Properties

    public RectTransform GetRectTransform
    {
        get { return _rectTransform; }
    }

    public Slider GetSlider
    {
        get { return _slider; }
    }

    #endregion

    #region Other Members

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    #endregion
}