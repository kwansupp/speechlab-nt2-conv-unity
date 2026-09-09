using UnityEngine;

public class GameplayState : IGameState
{
    public void OnEnter(StateController sc)
    {
        // show player 
        sc.player.SetActive(true);
        
        // deactivate conversation camera

        // hide UI
        sc.uiHandler.HideConvoUI();
        
    }

    public void UpdateState(StateController sc)
    {

    }

    public void OnExit(StateController sc)
    {

    }
}
