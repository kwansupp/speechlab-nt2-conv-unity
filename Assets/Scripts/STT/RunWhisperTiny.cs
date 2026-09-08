using System.Collections.Generic;
using UnityEngine;

using System.Text;
using Unity.Collections;
using System.Threading.Tasks;
using UnityEngine.Rendering;

/// <summary>
/// Runtime inference script that uses WhisperTiny model for speech to text conversion
/// using the Unity Interference Package
/// </summary>
public class RunWhisperTiny : MonoBehaviour
{
    Unity.InferenceEngine.Worker decoder1, decoder2, encoder, spectrogram, argmax;

    [SerializeField] Unity.InferenceEngine.BackendType backendType = Unity.InferenceEngine.BackendType.GPUCompute;
    [SerializeField] TMPro.TMP_InputField outputField;

    const int maxTokens = 100;
    const int END_OF_TEXT = 50257;
    const int START_OF_TRANSCRIPT = 50258;
    // const int ENGLISH = 50259;
    // const int GERMAN = 50261;
    // const int FRENCH = 50265;
    const int DUTCH = 50271;
    const int TRANSCRIBE = 50359;
    const int TRANSLATE = 50358;
    const int NO_TIME_STAMPS = 50363;
    const int START_TIME = 50364;
    const int maxSamples = 30 * 16000;

    int numSamples;
    string[] tokens;
    int tokenCount = 0;

    NativeArray<int> outputTokens;
    NativeArray<int> lastToken;
    Unity.InferenceEngine.Tensor<int> lastTokenTensor;
    Unity.InferenceEngine.Tensor<int> tokensTensor;
    Unity.InferenceEngine.Tensor<float> audioInput;
    Unity.InferenceEngine.Tensor<float> encodedAudio;

    int[] whiteSpaceCharacters = new int[256];

    bool transcribe = false;
    string outputString;

    public Unity.InferenceEngine.ModelAsset audioDecoder1, audioDecoder2;
    public Unity.InferenceEngine.ModelAsset audioEncoder;
    public Unity.InferenceEngine.ModelAsset logMelSpectro;
    public TextAsset jsonFile;

    Awaitable m_Awaitable;

    // private UIHandler languageSelector;


    void Start()
    {
        SetupWhiteSpaceShifts();
        GetTokens();

        // languageSelector ??= FindAnyObjectByType<UIHandler>();
    }

    public async void RunWhisper(AudioClip audioClip)
    {
        // Dispose previous resources if any
        DisposeResources();

        try
        {
            decoder1 = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(audioDecoder1), backendType);
            decoder2 = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(audioDecoder2), backendType);

            // Build argmax model
            Unity.InferenceEngine.FunctionalGraph graph = new Unity.InferenceEngine.FunctionalGraph();
            var input = graph.AddInput(Unity.InferenceEngine.DataType.Float, new Unity.InferenceEngine.DynamicTensorShape(1, 1, 51865));
            var amax = Unity.InferenceEngine.Functional.ArgMax(input, -1, false);
            var selectTokenModel = graph.Compile(amax);
            argmax = new Unity.InferenceEngine.Worker(selectTokenModel, backendType);

            encoder = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(audioEncoder), backendType);
            spectrogram = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(logMelSpectro), backendType);

            outputTokens = new NativeArray<int>(maxTokens, Allocator.Persistent);

            // Set initial prompt
            outputTokens[0] = START_OF_TRANSCRIPT;
            outputTokens[1] = DUTCH;

            // if (languageSelector != null)
            // {
            //     switch (languageSelector.GetLanguage())
            //     {
            //         case "GERMAN":
            //             outputTokens[1] = GERMAN;
            //             break;
            //         case "FRENCH":
            //             outputTokens[1] = FRENCH;
            //             break;
            //         case "DUTCH":
            //             outputTokens[1] = DUTCH;
            //             break;
            //         default:
            //             outputTokens[1] = ENGLISH;
            //             break;
            //     }
            // }    

            outputTokens[2] = TRANSCRIBE; // Or TRANSLATE
            tokenCount = 3;

            outputString = string.Empty;

            LoadAudio(audioClip);
            EncodeAudio();

            transcribe = true;

            tokensTensor = new Unity.InferenceEngine.Tensor<int>(new Unity.InferenceEngine.TensorShape(1, maxTokens));
            Unity.InferenceEngine.ComputeTensorData.Pin(tokensTensor);
            tokensTensor.Reshape(new Unity.InferenceEngine.TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload(outputTokens, tokenCount);

            lastToken = new NativeArray<int>(1, Allocator.Persistent);
            lastToken[0] = NO_TIME_STAMPS;
            lastTokenTensor = new Unity.InferenceEngine.Tensor<int>(new Unity.InferenceEngine.TensorShape(1, 1), new[] { NO_TIME_STAMPS });

            // Async inference loop
            while (transcribe && tokenCount < (outputTokens.Length - 1))
            {
                m_Awaitable = InferenceStep();
                await m_Awaitable;
                await Task.Yield(); // yield to avoid blocking main thread
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("RunWhisper exception: " + ex);
            transcribe = false;
        }
        finally
        {
            // Optionally, dispose resources here if you want one-shot usage
            // DisposeResources();
        }
    }

    void LoadAudio(AudioClip audioClip)
    {
        numSamples = Mathf.Min(audioClip.samples, maxSamples);
        var data = new float[maxSamples];
        audioClip.GetData(data, 0);
        // Zero-pad if needed
        for (int i = audioClip.samples; i < maxSamples; i++)
            data[i] = 0f;

        audioInput?.Dispose();
        audioInput = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, maxSamples), data);
    }

    void EncodeAudio()
    {
        spectrogram.Schedule(audioInput);
        using var logmel = spectrogram.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
        encoder.Schedule(logmel);
        encodedAudio?.Dispose();
        encodedAudio = encoder.PeekOutput() as Unity.InferenceEngine.Tensor<float>;
    }

    async Awaitable InferenceStep()
    {
        try
        {
            decoder1.SetInput("input_ids", tokensTensor);
            decoder1.SetInput("encoder_hidden_states", encodedAudio);
            decoder1.Schedule();

            // Fetch past_key_values
            var pastDecoderKeys = new Unity.InferenceEngine.Tensor<float>[4];
            var pastDecoderValues = new Unity.InferenceEngine.Tensor<float>[4];
            var pastEncoderKeys = new Unity.InferenceEngine.Tensor<float>[4];
            var pastEncoderValues = new Unity.InferenceEngine.Tensor<float>[4];
            for (int i = 0; i < 4; i++)
            {
                pastDecoderKeys[i] = decoder1.PeekOutput($"present.{i}.decoder.key") as Unity.InferenceEngine.Tensor<float>;
                pastDecoderValues[i] = decoder1.PeekOutput($"present.{i}.decoder.value") as Unity.InferenceEngine.Tensor<float>;
                pastEncoderKeys[i] = decoder1.PeekOutput($"present.{i}.encoder.key") as Unity.InferenceEngine.Tensor<float>;
                pastEncoderValues[i] = decoder1.PeekOutput($"present.{i}.encoder.value") as Unity.InferenceEngine.Tensor<float>;
            }

            decoder2.SetInput("input_ids", lastTokenTensor);
            for (int i = 0; i < 4; i++)
            {
                decoder2.SetInput($"past_key_values.{i}.decoder.key", pastDecoderKeys[i]);
                decoder2.SetInput($"past_key_values.{i}.decoder.value", pastDecoderValues[i]);
                decoder2.SetInput($"past_key_values.{i}.encoder.key", pastEncoderKeys[i]);
                decoder2.SetInput($"past_key_values.{i}.encoder.value", pastEncoderValues[i]);
            }

            decoder2.Schedule();

            var logits = decoder2.PeekOutput("logits") as Unity.InferenceEngine.Tensor<float>;
            argmax.Schedule(logits);
            using var t_Token = await argmax.PeekOutput().ReadbackAndCloneAsync() as Unity.InferenceEngine.Tensor<int>;
            int index = t_Token[0];

            outputTokens[tokenCount] = lastToken[0];
            lastToken[0] = index;
            tokenCount++;
            tokensTensor.Reshape(new Unity.InferenceEngine.TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload(outputTokens, tokenCount);
            lastTokenTensor.dataOnBackend.Upload(lastToken, 1);

            if (index == END_OF_TEXT)
            {
                transcribe = false;
            }
            else if (index < tokens.Length)
            {
                outputString += GetUnicodeText(tokens[index]);
            }

            if (outputField != null)
                outputField.text = outputString.ToString();

            Debug.Log(outputString.ToString());
        }
        catch (System.Exception ex)
        {
            Debug.LogError("InferenceStep exception: " + ex);
            transcribe = false;
        }
    }

    void GetTokens()
    {
        try
        {
            var vocab = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonFile.text);
            tokens = new string[vocab.Count];
            foreach (var item in vocab)
                tokens[item.Value] = item.Key;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error parsing tokens json: " + ex);
            tokens = new string[0];
        }
    }

    string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }
    string ShiftCharacterDown(string text)
    {
        var outText = new StringBuilder();
        foreach (char letter in text)
            outText.Append(((int)letter <= 256) ? letter : (char)whiteSpaceCharacters[Mathf.Clamp((int)(letter - 256), 0, 255)]);
        return outText.ToString();
    }

    void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
        }
    }

    bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
    }

    void DisposeResources()
    {
        decoder1?.Dispose();
        decoder2?.Dispose();
        encoder?.Dispose();
        spectrogram?.Dispose();
        argmax?.Dispose();
        audioInput?.Dispose();
        encodedAudio?.Dispose();
        lastTokenTensor?.Dispose();
        tokensTensor?.Dispose();

        if (outputTokens.IsCreated)
            outputTokens.Dispose();

        if (lastToken.IsCreated)
            lastToken.Dispose();
    }

    private void OnDestroy()
    {
        DisposeResources();
    }
}
