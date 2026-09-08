using UnityEngine;

public class StateController : MonoBehaviour
{
    IState currentState;

    void Update()
    {
        currentState.UpdateState();
    }

    public void ChangeState(IState newState)
    {
        currentState.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }
}

public interface IState
{
    public void OnEnter();

    public void UpdateState();

    public void OnExit();
}