using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    // for voice input STT
    [SerializeField] private GameObject voiceInputButton;
    [SerializeField] private SpeechToText speechToText;

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
