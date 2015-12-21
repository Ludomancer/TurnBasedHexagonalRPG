using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : PanelBase
{
    #region Fields

    public string drawString;
    public string lostString;
    public Text player1StatusText;
    public Text player2StatusText;
    public string winString;

    #endregion

    #region Other Members

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
        yield return null;
        //Wait and hanlde transitions if there are any.
        IsTransitioning = false;
    }

    public void SetWinner(int playerId)
    {
        switch (playerId)
        {
            case 0:
                player1StatusText.text = winString;
                player2StatusText.text = lostString;
                break;
            case 1:
                player2StatusText.text = winString;
                player1StatusText.text = lostString;
                break;
            default:
                player1StatusText.text = drawString;
                player2StatusText.text = drawString;
                break;
        }
    }

    public void OnMainMenuButtonClicked()
    {
        GuiManager.Instance.CurrentState = GuiManager.Instance.GetState(UiStates.MAIN_MENU_STATE);
    }

    public void OnRestartButtonClicked()
    {
        GuiManager.Instance.CurrentState = GuiManager.Instance.GetState(UiStates.GAME_HUD_STATE);
        FindObjectOfType<GameManager>().StartGame();
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    #endregion
}