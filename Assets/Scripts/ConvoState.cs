using UnityEngine;
using UnityEngine.InputSystem; 

public class ConvoState : IGameState
{
    public void OnEnter(StateController sc)
    {
        // activate conversation camera
        sc.cameraController.EnterConversation();
        // disable / hide player
        sc.player.SetActive(false);
        // show UI
        sc.uiHandler.ShowConvoUI();
        // start LLM convo
        sc.convoController.StartConvo();
        
    }

    public void UpdateState(StateController sc)
    {
        // convo specific logic
        // if convo finished / esc pressed, exit convo
        if (Keyboard.current.escapeKey.isPressed || Keyboard.current.escapeKey.isPressed)
        {
            sc.ChangeState(sc.gameplayState);
        }
    }

    public void OnExit(StateController sc)
    {
        // hide UI
        sc.uiHandler.HideConvoUI();
        // end conversation mode
        sc.cameraController.ExitConversation();
    }
}
