using UnityEngine;

public class GameplayState : IGameState
{
    public void OnEnter()
    {
        // stop player movement

        // disable / hide player
        
        // activate conversation camera

        // show UI

        // start LLM convo

        
    }

    public void UpdateState()
    {
        // convo specific logic
        // if convo finished / esc pressed, exit convo
    }

    public void OnExit()
    {
        // end conversation mode
    }
}
