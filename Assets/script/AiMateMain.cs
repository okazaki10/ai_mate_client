using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using TMPro;
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
        public Button buttonRecord;
        public Text recordText;
        public TMP_Text chatText;
        public Text timeText;
        public TMP_Dropdown languageDropdown;
        public Toggle translateToggle;
        public Toggle vadToggle;
        public ScrollRect scroll;
        public RestApiClient restApiClient;

        public Canvas chatCanvas;
        public Canvas settingCanvas;
        public Canvas menuCanvas;
        public Button buttonToggleChatOnOff;
        public Button buttonShowSettings;
        public Button buttonBackFromSettings;
        public InputField inputFieldMessage;
        public Button buttonSendMessage;
        public ScrollRect scrollRectChat;

        private string _buffer;

        private AudioSource audioSource;
        private bool isProcessing = false;

        public bool isEcho = false;

        private bool toggleOffRecord = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            //whisper.OnNewSegment += OnNewSegment;
            //whisper.OnProgress += OnProgressHandler;

            microphoneRecord.OnRecordStop += OnRecordStop;
            microphoneRecord.OnVadChanged += OnVadDetected;

            buttonRecord.onClick.AddListener(OnButtonPressed);
            buttonToggleChatOnOff.onClick.AddListener(onToggleChatOnOff);
            buttonShowSettings.onClick.AddListener(onShowSettings);
            buttonBackFromSettings.onClick.AddListener(onBackFromSettings);
            buttonSendMessage.onClick.AddListener(onSendMessage);
            inputFieldMessage.onEndEdit.AddListener(OnInputEndEdit);

            //languageDropdown.value = languageDropdown.options
            //    .FindIndex(op => op.text == whisper.language);
            //languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            //translateToggle.isOn = whisper.translateToEnglish;
            //translateToggle.onValueChanged.AddListener(OnTranslateChanged);

            vadToggle.isOn = microphoneRecord.vadStop;
            vadToggle.onValueChanged.AddListener(OnVadChanged);

            startRecord();
        }

        void OnInputEndEdit(string input)
        {
            if (Input.GetKeyDown(KeyCode.Return) && inputFieldMessage.text != "")
            {
                onSendMessage();
            }
        }

        private void onSendMessage()
        {
            chatText.text += "\n\n" + restApiClient.inputFieldUsername.text + " : " + inputFieldMessage.text;
            restApiClient.SendTextAndPlayAudio(inputFieldMessage.text, onAudioDonePlaying);
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

        private void onToggleChatOnOff()
        {
            chatCanvas.enabled = !chatCanvas.enabled;
        }

        private void onShowSettings()
        {
            settingCanvas.enabled = true;
            menuCanvas.enabled = false;
        }

        private void onBackFromSettings()
        {
            settingCanvas.enabled = false;
            menuCanvas.enabled = true;
        }

        private void OnVadChanged(bool vadStop)
        {
            microphoneRecord.vadStop = vadStop;
        }

        private void OnButtonPressed()
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
                recordText.text = "Stop";
            }
        }

        private void stopRecord()
        {
            microphoneRecord.StopRecord();
            recordText.text = "Record";
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
            //recordText.text = "Record";
            //_buffer = "";

            //var sw = new Stopwatch();
            //sw.Start();

            //var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
            //if (res == null || !chatText) 
            //    return;

            //var time = sw.ElapsedMilliseconds;
            //var rate = recordedAudio.Length / (time * 0.001f);
            //timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";

            //var text = res.Result;
            //if (printLanguage)
            //    text += $"\n\nLanguage: {res.Language}";

            //chatText.text = text;
            //UiUtils.ScrollDown(scroll);

            var audioClip = AudioClip.Create("echo", recordedAudio.Data.Length, recordedAudio.Channels, recordedAudio.Frequency, false);
            audioClip.SetData(recordedAudio.Data, 0);
            if (isEcho)
            {
                audioSource.PlayOneShot(audioClip);
            }

            StartCoroutine(SendAudioToAPI(audioClip));

            //microphoneRecord.StartRecord();
            //recordText.text = "Stop";
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

        void onAudioDonePlaying()
        {
            startRecord();
        }

        IEnumerator SendAudioToAPI(AudioClip audioClip)
        {
            byte[] wavData = AudioClipToWav(audioClip);

            string endpoint = restApiClient.inputFieldIpAddress.text + ":7839" + "/recognize";

            WWWForm form = new WWWForm();
            form.AddBinaryData("audio_file", wavData, "audio.wav", "audio/wav");
            form.AddField("language", restApiClient.dropDownLanguage.options[restApiClient.dropDownLanguage.value].text);

            using (UnityWebRequest request = UnityWebRequest.Post(endpoint, form))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
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
                    chatText.text += "\n\n" + restApiClient.inputFieldUsername.text + " : " + response.text;
                    ScrollDown();
                    restApiClient.SendTextAndPlayAudio(response.text, onAudioDonePlaying);


                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log($"Failed to parse API response: {e.Message}");
                    //OnErrorOccurred?.Invoke($"Failed to parse response: {e.Message}");
                    startRecord();
                }
            }


            isProcessing = false;
        }

        private void OnLanguageChanged(int ind)
        {
            var opt = languageDropdown.options[ind];
            //whisper.language = opt.text;
        }

        private void OnTranslateChanged(bool translate)
        {
            //whisper.translateToEnglish = translate;
        }

        private void OnProgressHandler(int progress)
        {
            if (!timeText)
                return;
            timeText.text = $"Progress: {progress}%";
        }

        //private void OnNewSegment(WhisperSegment segment)
        //{
        //    if (!streamSegments || !chatText)
        //        return;

        //    _buffer += segment.Text;
        //    chatText.text = _buffer + "...";
        //    UiUtils.ScrollDown(scroll);
        //}
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