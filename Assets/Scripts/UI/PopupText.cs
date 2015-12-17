using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class PopupText : MonoBehaviour
{
    private Color _defaultColor;

    #region Properties

    public Animator Animator
    {
        get
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            return _animator;
        }
    }

    public RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    public Text Text
    {
        get
        {
            if (_text == null)
            {
                _text = GetComponent<Text>();
            }
            return _text;
        }
    }

    public CanvasGroup CanvasGroup
    {
        get
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    #endregion

    #region Other Members

    internal void Show(string text, float fadeOutDelay, float fadeOutDuration)
    {
        Show(text, fadeOutDelay, DEFAULT_FADE_IN_DURATION, fadeOutDuration, _defaultColor, PopupAnimMode.Scale, false, false);
    }

    internal void Show(string text, float fadeOutDelay, float fadeOutDuration, Color popupColor)
    {
        Show(text, fadeOutDelay, DEFAULT_FADE_IN_DURATION, fadeOutDuration, popupColor, PopupAnimMode.Scale, false, false);
    }

    internal void Show(int text, float fadeOutDelay, float fadeOutDuration)
    {
        Show(text.ToString(), fadeOutDelay, DEFAULT_FADE_IN_DURATION, fadeOutDuration, _defaultColor, PopupAnimMode.Scale, false, false);
    }

    internal void Show(int text, float fadeOutDelay, float fadeOutDuration, Color popupColor)
    {
        Show(text.ToString(), fadeOutDelay, DEFAULT_FADE_IN_DURATION, fadeOutDuration, popupColor, PopupAnimMode.Scale, false, false);
    }

    internal void Show(int text, float fadeOutDelay, float fadeOutDuration, Color popupColor, PopupAnimMode animMode, bool resizeTextForBestFit)
    {
        Show(text.ToString(), fadeOutDelay, DEFAULT_FADE_IN_DURATION, fadeOutDuration, popupColor, animMode, resizeTextForBestFit, resizeTextForBestFit);
    }

    internal void Show(int text, float fadeOutDelay, float fadeOutDuration, Color popupColor, PopupAnimMode animMode, bool resizeTextForBestFit,
        bool forceSingleLine)
    {
        Show(text.ToString(), fadeOutDelay, DEFAULT_FADE_IN_DURATION, fadeOutDuration, popupColor, animMode, resizeTextForBestFit, forceSingleLine);
    }

    internal void Show(string text, float fadeOutDelay, float fadeOutDuration, Color popupColor, PopupAnimMode animMode)
    {
        Show(text, fadeOutDelay, DEFAULT_FADE_IN_DURATION, fadeOutDuration, popupColor, animMode, false, false);
    }

    #endregion

    #region Nested type: PopupAnimMode

    #region Enumerations

    internal enum PopupAnimMode
    {
        Fade,
        Scale
    }

    #endregion

    #endregion

    #region Variables

    public const float DEFAULT_FADE_DELAY = 0.5f;
    public const float DEFAULT_FADE_OUT_DURATION = 0.1f;
    public const float DEFAULT_FADE_IN_DURATION = 0.25f;
    public const float LONG_FADE_DELAY = 1.5f;
    public const int NO_AUTO_FADE_OUT = -1;
    private readonly int _popupShowScale = Animator.StringToHash("ShowScale");
    private readonly int _popupShowFade = Animator.StringToHash("ShowFade");
    private readonly int _popupHideScale = Animator.StringToHash("HideScale");
    private readonly int _popupHideFade = Animator.StringToHash("HideFade");
    private Text _text;
    private Animator _animator;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private float _fadeOutDelay;
    private PopupAnimMode _animMode;
    private float _hidePopupTime = -1;
    private int _fadeAnimKey;
    private bool _forceSingleLine;
    private Vector2 _defaultSizeDelta;
    private float _defaultSizeDeltaY;
    private bool _isShown;
    private float _fadeOutDuration;

    #endregion

    #region Methods

    void Awake()
    {
        _defaultColor = Text.color;
    }

    internal void Show(string text, float fadeOutDelay, float fadeInDuration, float fadeOutDuration, Color popupColor, PopupAnimMode animMode, bool resizeTextForBestFit, bool forceSingleLine)
    {
        _isShown = false;
        _forceSingleLine = forceSingleLine;
        _fadeOutDelay = fadeOutDelay == NO_AUTO_FADE_OUT ? NO_AUTO_FADE_OUT : fadeOutDelay + fadeInDuration;
        _fadeOutDuration = fadeOutDuration;
        Animator.speed = 1f / fadeInDuration;
        _animMode = animMode;
        Text.resizeTextForBestFit = resizeTextForBestFit;
        Text.color = popupColor;
        Text.text = text;
        if (_defaultSizeDelta == Vector2.zero)
        {
            _defaultSizeDeltaY = RectTransform.sizeDelta.y;
            _defaultSizeDelta = new Vector2(RectTransform.sizeDelta.x, _defaultSizeDeltaY);
        }
        RectTransform.sizeDelta = _defaultSizeDelta;
        if (!_forceSingleLine || !resizeTextForBestFit) ShowPopupTextInternal();
        else
        {
            CanvasGroup.alpha = 0;
            _hidePopupTime = -1;
        }
    }

    private void ShowPopupTextInternal()
    {
        switch (_animMode)
        {
            case PopupAnimMode.Fade:
                Animator.SetTrigger(_popupShowFade);
                _fadeAnimKey = _popupHideFade;
                break;
            case PopupAnimMode.Scale:
                Animator.SetTrigger(_popupShowScale);
                _fadeAnimKey = _popupHideScale;
                break;
        }

        //0.5 approx anim lenght
        _hidePopupTime = _fadeOutDelay == NO_AUTO_FADE_OUT ? NO_AUTO_FADE_OUT : Time.realtimeSinceStartup + _fadeOutDelay;
        _isShown = true;
    }

    private void Update()
    {
        if (_isShown)
        {
            if (_fadeAnimKey != -1 && NO_AUTO_FADE_OUT != _hidePopupTime && Time.realtimeSinceStartup > _hidePopupTime)
            {
                Hide();
            }
        }
        if (_forceSingleLine && Text.resizeTextForBestFit && Text.cachedTextGenerator.lineCount != 0)
        {
            if (Text.cachedTextGenerator.lineCount > 1)
            {
                RectTransform.sizeDelta = new Vector2(_defaultSizeDelta.x, RectTransform.sizeDelta.y * 0.7f);
            }
            else if (!_isShown) ShowPopupTextInternal();
        }
    }

    public void Hide()
    {
        Animator.speed = 1;
        Animator.CrossFadeInFixedTime(_fadeAnimKey, _fadeOutDuration);
        _hidePopupTime = -1;
        _fadeAnimKey = -1;
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}