using UnityEngine;

public class StateController : MonoBehaviour
{
    IGameState currentState;

    public UIHandler uiHandler;
    public ConvoController convoController;
    public GameObject player;
    
    public GameplayState gameplayState = new GameplayState();
    public ConvoState convoState = new ConvoState();

    private void Start()
    {
        ChangeState(gameplayState);
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.UpdateState(this);
        }
    }

    public void ChangeState(IGameState newState)
    {
        if (currentState != null)
        {
            currentState.OnExit(this);
        }
        currentState = newState;
        currentState.OnEnter(this);
        Debug.Log("Entered game state: " + currentState);
        // if (c)
    }
}

public interface IGameState
{
    public void OnEnter(StateController controller);

    public void UpdateState(StateController controller);

    public void OnExit(StateController controller);
}