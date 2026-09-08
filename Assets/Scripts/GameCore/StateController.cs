using UnityEngine;

public class StateController : MonoBehaviour
{
    IGameState currentState;

    void Update()
    {
        currentState.UpdateState();
    }

    public void ChangeState(IGameState newState)
    {
        currentState.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }
}

public interface IGameState
{
    public void OnEnter();

    public void UpdateState();

    public void OnExit();
}