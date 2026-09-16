# speechlab-nt2-conv-unity

A 3d environment for pedagogically testing Dutch NT2 conversations.

## setup
1. clone this repository
2. load project into Unity Hub, editor version: 6000.5.0f1
3. Open `Classroom_Scene_LLM` from `Assets/Scenes/`
4. install LLMUnity from Unity registry, and download LLM (Qwen 3.5 2B, under tiny models)
5. download this zip file, containing model assets and espeak-ng plugin (unzip and copy contents to Assets directory). This should contain three directories: StreamingAssets, Plugins, and Data. WhisperTinyModels and PiperModels (NL-alex) is included, for any other models, feel free to add.
    https://uva.works.surf.nl/s/yFspTNdnstmP8aH
6. download Piper model files (onnx, json), save to `Assets/Data/PiperModels`
7. attach Whisper model files to SpeechToText object (under Run Whisper Tiny) and Piper models to TextToSpeech, if necessary.

## LLM config
Update prompt for Convo Agent under LLMAgent chat settings.

## player controls
In play mode, move the player around the environment with arrowkeys or 'WASD'. Move towards the conversational avatar to trigger 'Convo Mode', which will lock the camera on the character and make the UI appear.

There are two modes of input: writing responses as text, or recording voice to turn speech to text (with microphone icon). On enter, the response is submitted to the LLM, and a reply will show on screen.

To exit 'Convo Mode', press 'esc'. This will bring you back to 'Gameplay Mode'. To enter 'Convo Mode' again, player mmust walk into the conversational avatar.

## check / debugging
in the case that unity crashes while running, make sure that the models are properly attached as seen in the screenshot for the different gameobjects and components, expecially for `SpeechToText`, `TextToSpeech`, and `TextToSpeech > RunPiper` game objects

## architecture

Hierarchy of game objects
![Components_GameStateManager](Screenshots/Hierarchy.png)

The interactions are set up between two modes: gameplay and convomode. This has been implemented through a state machine architecture, with CameraPositions storing world positions for GameplayView and ConvoView.

- A StateController script with two classes (GameplayState and ConvoState) is attached to GameStateManager game object. This script checks the state and handles the necessarily triggers and actions. 

![Components_GameStateManager](Screenshots/Components_GameStateManager.png)

- Convo_Avatar game object has a ConvoAvatar script that animates a bobble effect for the avatar. Bobble range may be adjusted.
The ConvoController manages the flow of the conversation, and how data flows between LLM Agent outputs and UI.

![Components_ConvoAvatar](Screenshots/Components_ConvoAvatar.png)

- Player_Avatar has a controller script that allows for keyboard input to controller player actions.

![Components_PlayerAvatar](Screenshots/Components_PlayerAvatar.png)

- LLM, this is where UndreamAI API is loaded. LLM models can be downloaded and selected under Model Settings.

![Components_LLM](Screenshots/Components_LLM.png)

- LLMAgent, handles chat settings for LLM.

![Components_LLMAgent](Screenshots/Components_LLMAgent.png)

- SpeechToText

![Components_SpeechToText](Screenshots/Components_SpeechToText.png)

- TextToSpeech

![Components_TextToSpeech](Screenshots/Components_TextToSpeech.png)

- TextToSpeech > RunPiper (nl)

![Components_RunPiper](Screenshots/Components_RunPiper.png)

- Main Camera, has a camera controller, which animates between GameplayView and ConvoView

![Components_MainCamera](Screenshots/Components_MainCamera.png)

- Main Camera > ConvoUI > Canvas
The settings for the UI components can be found here.
`StatusDisplay` shows the status message for the different controls while in ConvoMode.
`Entry > TextInput` is where text input is received, more specifically `Text Area`. `Record` object contains the icon image for recording, as well as UI handling for when icon is clicked, to start/stop recording for speech to text.
`DialogueDisplay` contains the UI components for responses received from the LLMAgent. `Speak` object contains the icon image and button event handling for text to speech feature.
`FrequencyBandRenderer` is a feature for when recording audio, to give visual feedback on frequency that is being recorded.

## dependencies
LLM implementation via LLMUnity (v3.0.3) package: https://github.com/undreamai/LLMUnity
Some code for TTS/STT adapted from: https://github.com/danielbierwirth/Inference-Whisper-Piper-Unity
