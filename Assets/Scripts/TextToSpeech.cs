using System.Collections;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;

/// <summary>
/// A simple script that generates synthesized voice output 
/// from input text by running local inference with the Piper model.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TextToSpeech : SpeechBase
{

    [Header("UI Components")]
    [SerializeField] private TMP_InputField textField;

    [Header("Voice Generation")]
    [SerializeField] private RunPiper[] runPiper;

    private UIHandler languageSelector;

    private int runningPiperIndex = 0;

    private bool synthesizeAndPlay = false;

    void Awake()
    {
        base.audioSource ??= GetComponent<AudioSource>();

        if (!audioSource) Debug.LogError("VoiceGenerationManager requires an AudioSource!");
        if (!lineRenderer) Debug.LogError("VoiceGenerationManager requires a LineRenderer!");
        if (!runPiper[runningPiperIndex]) Debug.LogWarning("RunPiper reference is not set.");
    }

    protected override void Start()
    {
        base.Start();

        languageSelector ??= FindAnyObjectByType<UIHandler>();
    }

    void Update()
    {
        if (!synthesizeAndPlay && !runPiper[runningPiperIndex].IsPlayingChunks())
            return;

        bool isPlaying = runPiper[runningPiperIndex] != null && runPiper[runningPiperIndex].IsPlayingChunks() && audioSource.isPlaying;

        lineRenderer.enabled = base.enableVisualization && isPlaying;
        if (isPlaying && base.enableVisualization)
        {
            base.UpdateFrequencyBand();
        }

        synthesizeAndPlay = isPlaying; 
    }

    public void Speak()
    {
        if (textField == null || string.IsNullOrEmpty(textField.text))
            return;

        Debug.Log($"Input text: {textField.text}");

        // if (languageSelector != null)
        // {
        //     switch (languageSelector.GetLanguage())
        //     {
        //         case "GERMAN":
        //             runningPiperIndex = 1;
        //             break;
        //         case "FRENCH":
        //             runningPiperIndex = 2;
        //             break;
        //         default:
        //             runningPiperIndex = 0;
        //             break;
        //     }
        // }
        runPiper[runningPiperIndex]?.SetVoice();
        runPiper[runningPiperIndex]?.SynthesizeAndPlay(textField.text);
        
        synthesizeAndPlay = true;
    }
}