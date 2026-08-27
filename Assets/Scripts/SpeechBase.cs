using UnityEngine;

public class SpeechBase : MonoBehaviour
{
    public enum Bands
    {
        Eight = 8,
        SixtyFour = 64,
    }

    [Header("Audio Components")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected LineRenderer lineRenderer;

    [Header("Spectrum Settings")]
    [SerializeField] private int frequencyBins = 512;
    private float[] samples;
    private float[] sampleBuffer;

    [Header("Visualization Settings")]
    [SerializeField] protected bool enableVisualization = true;
    [SerializeField] private Bands bandsToVisualize = Bands.SixtyFour;
    [SerializeField] private float scalar = 100f;
    [SerializeField] private float visualizerWidth = 10f;
    [SerializeField] private float visualizerYBase = 2f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private float lineWidth = 0.05f;

    [Header("Audio Processing")]
    [SerializeField] private float smoothDownRate = 10f;

    private float[] freqBands8;
    private float[] freqBands64;
    private Vector3[] positions;


    protected virtual void Start()
    {
        freqBands8 = new float[8];
        freqBands64 = new float[64];
        samples = new float[frequencyBins];
        sampleBuffer = new float[frequencyBins];
        SetupLineRenderer();
    }


    void SetupLineRenderer()
    {
        int bands = bandsToVisualize == Bands.Eight ? freqBands8.Length : freqBands64.Length;
        lineRenderer.positionCount = bands;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.useWorldSpace = false;
        positions = new Vector3[bands];
        //lineRenderer.enabled = enableVisualization;
    }

    protected void UpdateFrequencyBand()
    {
        audioSource.GetSpectrumData(sampleBuffer, 0, FFTWindow.BlackmanHarris);
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = sampleBuffer[i] > samples[i]
                ? sampleBuffer[i]
                : Mathf.Lerp(samples[i], sampleBuffer[i], Time.deltaTime * smoothDownRate);
        }
        UpdateFreqBands8();
        UpdateFreqBands64();
        if (enableVisualization)
        {
            DrawVisualizer();
        }
    }

    void DrawVisualizer()
    {
        float[] bands = bandsToVisualize == Bands.Eight ? freqBands8 : freqBands64;
        int bandCount = bands.Length;
        for (int i = 0; i < bandCount; i++)
        {
            float xPos = (float)i / (bandCount - 1) * visualizerWidth;
            float yPos = visualizerYBase + bands[i] * scalar;
            positions[i] = new Vector3(xPos, yPos, 0);
        }
        lineRenderer.SetPositions(positions);
    }

    void UpdateFreqBands8()
    {
        int count = 0;
        for (int i = 0; i < 8; i++)
        {
            float average = 0;
            int sampleCount = (int)Mathf.Pow(2, i) * 2;
            if (i == 7) sampleCount += 2;
            for (int j = 0; j < sampleCount && count < samples.Length; j++, count++)
                average += samples[count] * (count + 1);
            average /= count > 0 ? count : 1;
            freqBands8[i] = average;
        }
    }

    void UpdateFreqBands64()
    {
        int count = 0;
        int sampleCount = 1;
        int power = 0;
        for (int i = 0; i < 64; i++)
        {
            float average = 0;
            if (i == 16 || i == 32 || i == 40 || i == 48 || i == 56)
            {
                power++;
                sampleCount = (int)Mathf.Pow(2, power);
                if (power == 3) sampleCount -= 2;
            }
            for (int j = 0; j < sampleCount && count < samples.Length; j++, count++)
                average += samples[count] * (count + 1);
            average /= count > 0 ? count : 1;
            freqBands64[i] = average;
        }
    }
}
