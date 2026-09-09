using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private GameObject conversationUI;
    // for voice input STT
    [SerializeField] private GameObject voiceInputButton;
    [SerializeField] private SpeechToText speechToText;

    public void ShowConvoUI()
    {
        conversationUI.SetActive(true);
    }

    public void HideConvoUI()
    {
        conversationUI.SetActive(false);
    }

    public void StartOrStopRecording()
    {
        if (!speechToText.IsRecording)
        {
            speechToText.StartRecording();
            voiceInputButton.GetComponent<Image>().enabled = true;
        }
        else
        {
            voiceInputButton.GetComponent<Image>().enabled = false;
            speechToText.StopRecording();
        }
    }
}
