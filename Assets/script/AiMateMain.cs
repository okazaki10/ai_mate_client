using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

namespace Whisper.Samples
{
    /// <summary>
    /// Record audio clip from microphone and make a transcription.
    /// </summary>
    public class AiMateMain : MonoBehaviour
    {
        public MicrophoneRecord microphoneRecord;
        public bool streamSegments = true;
        public bool printLanguage = true;

        [Header("UI")]
        public TMP_Text recordText;
        public TMP_Text chatText;
        public Text timeText;
        public TMP_Dropdown languageDropdown;
        public Toggle translateToggle;
        public ScrollRect scroll;
        public RestApiClient restApiClient;

        public InputField inputFieldMessage;
        public ScrollRect scrollRectChat;
        public VRMModelManager vrmModelManager;
        public VRMEmotionBlinkController vrmEmotionBlinkController;
        public PopUpMessage popUpMessage;
        public MenuManager menuManager;

        private AudioSource audioSource;
        private bool isProcessing = false;

        public bool isEcho = false;

        private bool toggleOffRecord = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();

            microphoneRecord.OnRecordStop += OnRecordStop;
            microphoneRecord.OnVadChanged += OnVadDetected;


            startRecord();
        }

        public void OnInputEndEdit(string input)
        {
            if (Input.GetKeyDown(KeyCode.Return) && inputFieldMessage.text != "")
            {
                onSendMessage();
            }
        }

        public void onSendMessage()
        {
            chatText.text += "\n\n" + menuManager.inputFieldUsername.text + " : " + inputFieldMessage.text;
            restApiClient.SendTextAndPlayAudio(inputFieldMessage.text, onSuccessFetch, onErrorFetch, onAudioDonePlaying);
            inputFieldMessage.text = "";
            toggleOffRecord = true;
            stopRecord();
            ScrollDown();
        }

        public void ScrollDown()
        {
            // Scroll to bottom
            Canvas.ForceUpdateCanvases(); // ensures layout updates first
            scrollRectChat.verticalNormalizedPosition = 0f;
        }


        public void OnButtonRecordPressed()
        {
            if (toggleOffRecord)
            {
                toggleOffRecord = false;
                startRecord();
            }
            else
            {
                toggleOffRecord = true;
                stopRecord();
            }
        }

        private void startRecord()
        {
            if (!toggleOffRecord)
            {
                microphoneRecord.StartRecord();
                recordText.text = "Stop Record";
            }
        }

        private void stopRecord()
        {
            microphoneRecord.StopRecord();
            recordText.text = "Start Record";
        }

        private void OnVadDetected(bool vad)
        {
            // if (vad) {
            //     if (!microphoneRecord.IsRecording)
            //     {
            //         microphoneRecord.StartRecord();
            //         recordText.text = "Stop";
            //     }
            //}
        }

        private void OnRecordStop(AudioChunk recordedAudio)
        {
            if (toggleOffRecord)
            {
                return;
            }

            var audioClip = AudioClip.Create("echo", recordedAudio.Data.Length, recordedAudio.Channels, recordedAudio.Frequency, false);
            audioClip.SetData(recordedAudio.Data, 0);
            if (isEcho)
            {
                audioSource.PlayOneShot(audioClip);
            }

            StartCoroutine(SendAudioToAPI(audioClip));
        }

        byte[] AudioClipToWav(AudioClip audioClip)
        {
            float[] samples = new float[audioClip.samples];
            audioClip.GetData(samples, 0);

            Int16[] intData = new Int16[samples.Length];
            Byte[] bytesData = new Byte[samples.Length * 2];

            int rescaleFactor = 32767;

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                Byte[] byteArr = new Byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            return CreateWAV(bytesData, audioClip.frequency, audioClip.channels);
        }

        byte[] CreateWAV(byte[] audioData, int frequency, int channels)
        {
            int samples = audioData.Length / 2;

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // RIFF header
                    writer.Write("RIFF".ToCharArray());
                    writer.Write(36 + audioData.Length);
                    writer.Write("WAVE".ToCharArray());

                    // fmt chunk
                    writer.Write("fmt ".ToCharArray());
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)channels);
                    writer.Write(frequency);
                    writer.Write(frequency * channels * 2);
                    writer.Write((short)(channels * 2));
                    writer.Write((short)16);

                    // data chunk
                    writer.Write("data".ToCharArray());
                    writer.Write(audioData.Length);
                    writer.Write(audioData);
                }

                return stream.ToArray();
            }
        }
 
        void onSuccessFetch(ApiResponse<ApiData> response)
        {
            popUpMessage.showPopUpForever(response.data.generated_text);
            foreach (var emotion in response.data.action_params.emotions)
            {
                if (emotion.ContainsInsensitive("SHY"))
                {
                    vrmEmotionBlinkController.SetSad();
                    vrmModelManager.animator.SetInteger("animBaseInt", 1);
                }
                else if (emotion.ContainsInsensitive("ANG"))
                {
                    vrmEmotionBlinkController.SetAngry();
                    vrmModelManager.animator.SetInteger("animBaseInt", 2);
                }
                else if (emotion.ContainsInsensitive("SURPRISE"))
                {
                    vrmEmotionBlinkController.SetNeutral();
                    vrmModelManager.animator.SetInteger("animBaseInt", 3);
                }
                else if (emotion.ContainsInsensitive("HAPPY"))
                {
                    vrmEmotionBlinkController.SetHappy();
                    vrmModelManager.animator.SetInteger("animBaseInt", 4);
                }
                else if (emotion.ContainsInsensitive("CONCERN"))
                {
                    vrmEmotionBlinkController.SetSad();
                    vrmModelManager.animator.SetInteger("animBaseInt", 5);
                }
                else if (emotion.ContainsInsensitive("CURIOUS"))
                {
                    vrmEmotionBlinkController.SetNeutral();
                    vrmModelManager.animator.SetInteger("animBaseInt", 6);
                }
                else if (emotion.ContainsInsensitive("SAD"))
                {
                    vrmEmotionBlinkController.SetSad();
                    vrmModelManager.animator.SetInteger("animBaseInt", 7);
                }
            }
            foreach (var action in response.data.action_params.actions)
            {
                if (action.ContainsInsensitive("WAVE"))
                {
                    vrmEmotionBlinkController.SetHappy();
                    vrmModelManager.animator.SetInteger("animBaseInt", 8);
                }
            }
        }

        void onErrorFetch()
        {
            vrmEmotionBlinkController.SetNeutral();
            vrmModelManager.animator.SetInteger("animBaseInt", 0);
            startRecord();
        }

        void onAudioDonePlaying()
        {
            popUpMessage.HidePopUp();
            vrmEmotionBlinkController.SetNeutral();
            vrmModelManager.animator.SetInteger("animBaseInt", 0);
            startRecord();
        }

        IEnumerator SendAudioToAPI(AudioClip audioClip)
        {
            byte[] wavData = AudioClipToWav(audioClip);

            string endpoint = menuManager.inputFieldIpAddress.text + ":7839" + "/recognize";

            WWWForm form = new WWWForm();
            form.AddBinaryData("audio_file", wavData, "audio.wav", "audio/wav");
            form.AddField("language", restApiClient.localeDropDown.GetSelectedLocaleCode());

            using (UnityWebRequest request = UnityWebRequest.Post(endpoint, form))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    popUpMessage.showMessage($"API request failed: {request.error}, please run start_whisperx_speech_recognition.bat");
                    UnityEngine.Debug.Log($"API request failed: {request.error}");
                    //OnErrorOccurred?.Invoke($"API request failed: {request.error}");
                    startRecord();
                    isProcessing = false;
                    yield break;
                }

                try
                {
                    WhisperXResponse response = JsonUtility.FromJson<WhisperXResponse>(request.downloadHandler.text);

                    if (!response.success || string.IsNullOrEmpty(response.text))
                    {
                        UnityEngine.Debug.Log("No speech recognized");
                        startRecord();
                        yield break;
                    }

                    UnityEngine.Debug.Log($"Speech recognized: {response.text}");
                    //OnSpeechRecognized?.Invoke(response.text);

                    // Log segment information
                    if (response.segments != null && response.segments.Length > 0)
                    {
                        UnityEngine.Debug.Log($"Segments count: {response.segments.Length}");
                        foreach (var segment in response.segments)
                        {
                            UnityEngine.Debug.Log($"Segment: '{segment.text}' ({segment.start:F2}s - {segment.end:F2}s)");
                        }
                    }

                    //chatText.text = response.text;
                    chatText.text += "\n\n" + menuManager.inputFieldUsername.text + " : " + response.text;
                    ScrollDown();
                    restApiClient.SendTextAndPlayAudio(response.text, onSuccessFetch, onErrorFetch, onAudioDonePlaying);


                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log($"Failed to parse API response: {e.Message}");
                    //OnErrorOccurred?.Invoke($"Failed to parse response: {e.Message}");
                    popUpMessage.showMessage(e.Message);
                    startRecord();
                }
            }

            isProcessing = false;
        }
        
    }
}

[System.Serializable]
public class WhisperXResponse
{
    public bool success;
    public string text;
    public float confidence;
    public string language;
    public WhisperXSegment[] segments;
}

[System.Serializable]
public class WhisperXAlignmentResponse
{
    public bool success;
    public string text;
    public string language;
    public WhisperXSegment[] segments;
    public WhisperXWord[] words;
}

[System.Serializable]
public class WhisperXSegment
{
    public float start;
    public float end;
    public string text;
    public WhisperXWord[] words;
}

[System.Serializable]
public class WhisperXWord
{
    public string word;
    public float start;
    public float end;
    public float score;
}