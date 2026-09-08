using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple script that generates text from voice input by running local inference with the WhisperTiny model.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SpeechToText : SpeechBase
{
   [SerializeField] private RunWhisperTiny runWhisper;
   [SerializeField] private int microphoneId = 0; // Index in Microphone.devices
   [SerializeField] private int recordingDuration = 10; // seconds
   [SerializeField] private int sampleRate = 16000; // Hz


   public bool IsRecording => m_isRecording;
   private bool m_isRecording = false;

   void Awake()
    {
        base.audioSource ??= GetComponent<AudioSource>();

        if (!audioSource) Debug.LogError("VoiceGenerationManager requires an AudioSource!");
        if (!lineRenderer) Debug.LogError("VoiceGenerationManager requires a LineRenderer!");
        if (!runWhisper) Debug.LogWarning("Whisper reference is not set.");
    }

   void Update()
   {
        if (!m_isRecording)
            return;

        bool isPlaying = audioSource.clip != null && audioSource.isPlaying;

        lineRenderer.enabled = base.enableVisualization && isPlaying;

        if (isPlaying && base.enableVisualization)
        {
            Debug.Log("Update Frequency Band");
            base.UpdateFrequencyBand();
        }
   }

   /// <summary>
   /// Starts recording from the selected microphone.
   /// </summary>
   public void StartRecording()
   {
#if UNITY_WEBGL && !UNITY_EDITOR
       Debug.LogWarning("Microphone recording is not supported in WebGL builds.");
       m_isRecording = false;
       enableVisualization = false;
       lineRenderer.enabled = false;
       return;
#else
       if (m_isRecording)
       {
           Debug.LogWarning("Already recording.");
           return;
       }


       if (Microphone.devices == null || Microphone.devices.Length == 0)
       {
           Debug.LogError("No microphone devices found.");
           return;
       }


       if (microphoneId < 0 || microphoneId >= Microphone.devices.Length)
       {
           Debug.LogError($"Microphone ID {microphoneId} is out of range. Available: {Microphone.devices.Length} devices.");
           return;
       }


       string micDevice = Microphone.devices[microphoneId];
       if (string.IsNullOrEmpty(micDevice))
       {
           Debug.LogError($"Microphone at index {microphoneId} is not available.");
           return;
       }


       audioSource.Stop();
       audioSource.clip = null;


       audioSource.clip = Microphone.Start(micDevice, true, recordingDuration, sampleRate);


       audioSource.loop = true;
       // audioSource.mute = true; // optional

       StartCoroutine(WaitForMicrophoneStart(micDevice));

       enableVisualization = true;
       lineRenderer.enabled = false;


       m_isRecording = true;
#endif
   }


#if !UNITY_WEBGL || UNITY_EDITOR
   private IEnumerator WaitForMicrophoneStart(string micDevice)
   {
       float initTime = Time.time;
       while (!(Microphone.GetPosition(micDevice) > 0))
       {
           if (Time.time - initTime > 2f)
           {
               Debug.LogError("Microphone failed to start recording in time.");
               m_isRecording = false;
               yield break;
           }
           yield return null;
       }


       audioSource.Play();
       Debug.Log("Microphone recording started and playback begun.");
   }
#endif


   /// <summary>
   /// Stops recording and processes the audio with Whisper.
   /// </summary>
   public void StopRecording()
   {
#if UNITY_WEBGL && !UNITY_EDITOR
       Debug.LogWarning("Microphone recording is not supported in WebGL builds.");
       m_isRecording = false;
       enableVisualization = false;
       lineRenderer.enabled = false;
       return;
#else
       if (!m_isRecording)
       {
           Debug.LogWarning("Not currently recording.");
           return;
       }


       m_isRecording = false;


       if (Microphone.devices == null || Microphone.devices.Length == 0)
       {
           Debug.LogError("No microphone devices found.");
           return;
       }


       if (microphoneId < 0 || microphoneId >= Microphone.devices.Length)
       {
           Debug.LogError($"Microphone ID {microphoneId} is out of range.");
           return;
       }


       string micDevice = Microphone.devices[microphoneId];
       Microphone.End(micDevice);


       if (audioSource.clip == null)
       {
           Debug.LogError("No audio clip recorded.");
           return;
       }

       audioSource.Stop();

       enableVisualization = false;
       lineRenderer.enabled = false;


       if (runWhisper != null)
       {
           runWhisper.RunWhisper(audioSource.clip);
       }
       else
       {
           Debug.LogWarning("RunWhisperTiny reference is missing.");
       }
#endif
   }
}