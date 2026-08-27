# speechlab-nt2-conv-unity

## usage

1. clone this repository
2. load project into Unity Hub, editor version:
3. install LLMUnity, and download LLM (Qwen 3.5 2B, under tiny models)
4. download streaming assets file, containing model assets and espeak-ng plugin (copy to Assets directory)
    https://drive.google.com/file/d/11YeEAFsK_75mYJVibmLuOg8ujYmG7t4K/edit
5. download Piper model files (onnx, json)
6. attach Whisper model files to SpeechToText object (under Run Whisper Tiny) and Piper models to TextToSpeech, if necessary



## dependencies
https://github.com/undreamai/LLMUnity

some code adapted from: https://github.com/danielbierwirth/Inference-Whisper-Piper-Unity
