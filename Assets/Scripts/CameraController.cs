using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform gameplayView;
    [SerializeField] private Transform convoView;
    [SerializeField] private float speed = 3f;

    private Transform targetView;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        targetView = gameplayView;
    }

    private void Update()
    {
        Camera.main.transform.position = Vector3.Lerp(
            Camera.main.transform.position,
            targetView.position,
            speed * Time.deltaTime
        );

        Camera.main.transform.rotation = Quaternion.Lerp(
            Camera.main.transform.rotation,
            targetView.rotation,
            speed * Time.deltaTime
        );
    }
    
    public void EnterConversation()
    {
        // move camera to convoView
        targetView = convoView;
    }

    public void ExitConversation()
    {
        // move camera back to gameplayView
        targetView = gameplayView;
    }
}
