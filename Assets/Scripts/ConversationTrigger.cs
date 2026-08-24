using UnityEngine;
using LLMUnity;
using TMPro;

public class ConversationTrigger : MonoBehaviour
{
    public GameObject convoAvatar;
    public GameObject uiObject;
    public TMP_Text dialogueDisplay;
    // public TMP_Text textInputField;
    [SerializeField] public TMP_InputField textInputField;

    // variable for LLMAgent
    public LLMAgent llmAgent;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textInputField.onSubmit.AddListener(HandleTextInputSubmit);
    }

    // on object trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // start convo - show text to click
            StartConvo();
        }
    }

    private void HandleTextInputSubmit(string input)
    {
        // Process the input text here
        Debug.Log("Submitted text: " + input);
        Converse(input);
        textInputField.text = "";
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void StartConvo()
    {
        Debug.Log("Start convo, launching convo mode");

        // make UI object active
        uiObject.SetActive(true);

        // LLM call
        Converse("Een student komt naar jouw toe, hoi zeggen, en begin het gesprek.");

        // get text input textInputField.text and send to LLM
    }

    void HandleReply(string replySoFar)
    {
        // do something with the reply from the model as it is being produced
        Debug.Log(replySoFar);
        // push text to game UI
        dialogueDisplay.text = replySoFar;
    }

    void ReplyCompleted()
    {
        // do something when the reply from the model is complete
        Debug.Log("The AI has finished replying");
    }

    // void Game(){
    //     // handle the response as it is being produced
    //     _ = llmAgent.Chat("Hello bot!", HandleReply, ReplyCompleted);
    // }

    // async void GameAsync()
    // {
    //     // or handle the entire response in one go
    //     string reply = await llmAgent.Chat("Een student komt naar jouw toe, hoi zeggen, en begin het gesprek.");
    //     Debug.Log(reply);
    //     // push text to game UI
    //     dialogueDisplay.text = reply;
    // }

    // function to send prompt and return reply
    public void Converse(string prompt)
    {
        // handle the response as it is being produced
        _ = llmAgent.Chat(prompt, HandleReply, ReplyCompleted);
    }

}
