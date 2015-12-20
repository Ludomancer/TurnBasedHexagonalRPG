using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

class UnitHealthBar : MonoBehaviour
{
    [SerializeField]
    private Slider _slider;

    private RectTransform _rectTransform;

    public RectTransform GetRectTransform
    {
        get { return _rectTransform; }
    }

    public Slider GetSlider
    {
        get { return _slider; }
    }

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }
}
