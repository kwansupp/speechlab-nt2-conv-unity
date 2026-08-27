using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Networking;

public class RunPiper : MonoBehaviour
{
    [Header("Required Assets")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private ESpeakTokenizer tokenizer;
    [SerializeField] private AudioSource audioSource;

    [Header("Punctuation Delays")]
    [Range(0.0f, 1.0f)] public float commaDelay = 0.1f;
    [Range(0.0f, 1.0f)] public float periodDelay = 0.5f;
    [Range(0.0f, 1.0f)] public float questionExclamationDelay = 0.6f;

    private Worker engine;
    private bool isInitialized = false;
    private bool isPlayingChunks = false;

    // Regex precompiled for performance
    private static readonly Regex delayPunctuationRegex = new Regex(@"([,.?!;:])", RegexOptions.Compiled);
    private static readonly Regex nonDelayPunctuationRegex = new Regex(@"[^\w\s,.?!;:]", RegexOptions.Compiled);

    private Coroutine currentSynthesisCoroutine;

    void Start()
    {
        if (modelAsset == null) Debug.LogError($"{nameof(modelAsset)} is not assigned.");
        if (tokenizer == null) Debug.LogError($"{nameof(tokenizer)} is not assigned.");
        if (audioSource == null) Debug.LogError($"{nameof(audioSource)} is not assigned.");

        StartCoroutine(InitializePiper());
    }

    private IEnumerator InitializePiper()
    {
        string espeakDataPath;

#if UNITY_ANDROID && !UNITY_EDITOR
        espeakDataPath = Path.Combine(Application.persistentDataPath, "espeak-ng-data");
        if (!Directory.Exists(espeakDataPath))
        {
            Debug.Log("Android: eSpeak data not found. Copying...");
            string zipSourcePath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data.zip");
            string zipDestPath = Path.Combine(Application.persistentDataPath, "espeak-ng-data.zip");
            using (UnityWebRequest www = UnityWebRequest.Get(zipSourcePath))
            {
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load espeak-ng-data.zip: {www.error}");
                    yield break;
                }
                File.WriteAllBytes(zipDestPath, www.downloadHandler.data);
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipDestPath, Application.persistentDataPath);
                    Debug.Log("eSpeak data unzipped.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unzipping eSpeak data failed: {e.Message}");
                    yield break;
                }
                finally
                {
                    if (File.Exists(zipDestPath)) File.Delete(zipDestPath);
                }
            }
        }
        else
        {
            Debug.Log("Android: eSpeak data already exists.");
        }
#else
        espeakDataPath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data");
        Debug.Log($"Editor/Standalone: Using eSpeak data from: {espeakDataPath}");
        yield return null;
#endif

        InitializeESpeak(espeakDataPath);

        var model = ModelLoader.Load(modelAsset);
        engine = new Worker(model, BackendType.CPU);

        isInitialized = true;
        _WarmupModel();
        Debug.Log("Piper Manager initialized and model warmed up.");
    }

    private void InitializeESpeak(string dataPath)
    {
        int initResult = ESpeakNG.espeak_Initialize(0, 0, dataPath, 0);

        if (initResult <= 0)
        {
            Debug.LogError($"{nameof(ESpeakNG.espeak_Initialize)} failed with code {initResult}");
            return;
        }

        Debug.Log($"eSpeak-ng initialized. Data path: {dataPath}");
        if (tokenizer == null || string.IsNullOrEmpty(tokenizer.Voice))
        {
            Debug.LogError("Tokenizer not assigned or voice name missing.");
            return;
        }

        int voiceResult = ESpeakNG.espeak_SetVoiceByName(tokenizer.Voice);
        if (voiceResult == 0)
            Debug.Log($"Set voice to '{tokenizer.Voice}' succeeded.");
        else
            Debug.LogError($"Set voice to '{tokenizer.Voice}' failed. Error code: {voiceResult}");
    }

    private void _WarmupModel()
    {
        Debug.Log("Warming up the model with a dummy run...");
        _SynthesizeInternal("hello", false);
    }

    private string Phonemize(string text)
    {
        Debug.Log($"Phonemizing text: \"{text}\"");
        IntPtr textPtr = IntPtr.Zero;
        try
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text + "\0");
            textPtr = Marshal.AllocHGlobal(textBytes.Length);
            Marshal.Copy(textBytes, 0, textPtr, textBytes.Length);

            IntPtr pointerToText = textPtr;

            int textMode = 0; // espeakCHARS_AUTO=0
            int phonemeMode = 2; // bit 1: 0=ascii phoneme names, 1=IPA (UTF-8)

            IntPtr resultPtr = ESpeakNG.espeak_TextToPhonemes(ref pointerToText, textMode, phonemeMode);

            if (resultPtr != IntPtr.Zero)
            {
                string phonemeString = PtrToUtf8String(resultPtr);
                Debug.Log($"[PHONEMES] {phonemeString}");
                return phonemeString;
            }
            else
            {
                Debug.LogError("Phonemize failed: function returned null pointer.");
                return null;
            }
        }
        finally
        {
            if (textPtr != IntPtr.Zero) Marshal.FreeHGlobal(textPtr);
        }
    }

    private static string PtrToUtf8String(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return "";
        var bytes = new List<byte>();
        for (int offset = 0; ; offset++)
        {
            byte b = Marshal.ReadByte(ptr, offset);
            if (b == 0) break;
            bytes.Add(b);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    public bool IsPlayingChunks() => isPlayingChunks;

    public void SetVoice()
    {
        int voiceResult = ESpeakNG.espeak_SetVoiceByName(tokenizer.Voice);
        if (voiceResult == 0)
            Debug.Log($"Set voice to '{tokenizer.Voice}' succeeded.");
        else
            Debug.LogError($"Set voice to '{tokenizer.Voice}' failed. Error code: {voiceResult}");
    }
    
    public void SynthesizeAndPlay(string text)
    {
        if (!isInitialized)
        {
            Debug.LogError("Piper Manager not initialized.");
            return;
        }

        // Only one synthesis at a time
        if (currentSynthesisCoroutine != null) StopCoroutine(currentSynthesisCoroutine);
        currentSynthesisCoroutine = StartCoroutine(SynthesizeAndPlayCoroutine(text));
    }

    private IEnumerator SynthesizeAndPlayCoroutine(string text)
    {
        isPlayingChunks = true;
        string[] parts = delayPunctuationRegex.Split(text);

        foreach (string part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;

            if (delayPunctuationRegex.IsMatch(part))
            {
                float delay = part switch
                {
                    "," or ";" or ":" => commaDelay,
                    "." => periodDelay,
                    "?" or "!" => questionExclamationDelay,
                    _ => 0f
                };
                if (delay > 0)
                {
                    Debug.Log($"Pausing for '{part}' for {delay} seconds.");
                    yield return new WaitForSeconds(delay);
                }
            }
            else
            {
                string cleanedChunk = nonDelayPunctuationRegex.Replace(part, " ").Trim();
                if (!string.IsNullOrEmpty(cleanedChunk))
                {
                    Debug.Log($"Processing chunk: \"{cleanedChunk}\"");
                    _SynthesizeInternal(cleanedChunk, true);
                    yield return new WaitWhile(() => audioSource.isPlaying);
                }
            }
        }
        Debug.Log("Finished playing all chunks.");
        isPlayingChunks = false;
        currentSynthesisCoroutine = null;
    }

    private void _SynthesizeInternal(string text, bool playAudio)
    {
        string phonemeStr = Phonemize(text);
        if (string.IsNullOrEmpty(phonemeStr))
        {
            Debug.LogError($"Phoneme conversion failed for chunk: \"{text}\"");
            return;
        }

        string[] phonemeArray = phonemeStr.Trim().Select(c => c.ToString()).ToArray();
        int[] phonemeTokens = tokenizer.Tokenize(phonemeArray);
        float[] scales = tokenizer.GetInferenceParams();
        int[] inputLength = { phonemeTokens.Length };

        using var phonemesTensor = new Tensor<int>(new TensorShape(1, phonemeTokens.Length), phonemeTokens);
        using var lengthTensor = new Tensor<int>(new TensorShape(1), inputLength);
        using var scalesTensor = new Tensor<float>(new TensorShape(3), scales);

        engine.SetInput("input", phonemesTensor);
        engine.SetInput("input_lengths", lengthTensor);
        engine.SetInput("scales", scalesTensor);

        engine.Schedule();

        using var outputTensor = (engine.PeekOutput() as Tensor<float>).ReadbackAndClone();
        float[] audioData = outputTensor.DownloadToArray();

        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("Generated audio data is empty.");
            return;
        }

        if (!playAudio) return;

        int sampleRate = tokenizer.SampleRate;
        AudioClip clip = AudioClip.Create("GeneratedSpeech", audioData.Length, 1, sampleRate, false);
        clip.SetData(audioData, 0);

        Debug.Log($"Speech generated! AudioClip length: {clip.length:F2}s. Playing.");

        if (audioSource)
            audioSource.PlayOneShot(clip);
        else
            Debug.LogWarning("No AudioSource assigned.");
    }

    void OnDestroy()
    {
        engine?.Dispose();
        if (currentSynthesisCoroutine != null) StopCoroutine(currentSynthesisCoroutine);
    }
}
