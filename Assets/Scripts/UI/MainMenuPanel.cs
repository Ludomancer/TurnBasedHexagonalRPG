using System.Collections;
using UnityEngine;

public class MainMenuPanel : PanelBase
{
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

    public void OnPlayButtonClicked()
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