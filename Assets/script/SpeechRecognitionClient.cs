using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using NativeWebSocket;
using TMPro;

[System.Serializable]
public class TranscriptionMessage
{
    public string type;
    public string text;
    public bool is_final;
    public string timestamp;
}

public class SpeechRecognitionClient : MonoBehaviour
{
    [Header("WebSocket Settings")]
    public string serverUrl = "ws://localhost:8765";

    [Header("UI Elements")]
    public InputField inputFieldMessage;
    public TMP_Text recordText;
    public PopUpMessage popUpMessage;
    public AiMateClient aiMateClient;

    private WebSocket websocket;
    private bool isConnected = false;
    private string finalTranscription = "";
    DateTime phraseTime = DateTime.Now;
    bool isTranscribing = false;
    private bool toggleOffRecord = false;

    void Start()
    {
        buttonConnect();
    }

    bool isDoneTranscribe = true;
    void Update()
    {
        // Dispatch WebSocket messages on main thread
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }

        //if (isTranscribing) { 
        //    var now = DateTime.Now;
        //    if ((now - phraseTime) > TimeSpan.FromSeconds(3))
        //    {
        //        print("done");
        //        isTranscribing = false;
        //    }
        //}
    }

    public void OnButtonRecordPressed()
    {
        if (toggleOffRecord)
        {
            buttonConnect();
        }
        else
        {
            buttonDisconnect();
        }
    }

    public void buttonConnect()
    {
        ConnectToServer();
        recordText.text = "Stop Record";
        toggleOffRecord = false;
    }

    public void buttonDisconnect()
    {
        DisconnectFromServer();
        recordText.text = "Start Record";
        toggleOffRecord = true;
    }

    public async void ConnectToServer()
    {
        if (isConnected) return;

        try
        {
            websocket = new WebSocket(serverUrl);

            websocket.OnOpen += () =>
            {
                Debug.Log("Connected to speech recognition server");
                isConnected = true;
                //UpdateConnectionStatus("Connected");
                buttonConnect();
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError($"WebSocket error: {e}");
                UpdateConnectionStatus($"Error: {e}, please run start_whisper_speech_recognition.bat");
                buttonDisconnect();
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log($"Disconnected from speech recognition server. Code: {e}");
                isConnected = false;
                //UpdateConnectionStatus("Disconnected");
                buttonDisconnect();
            };

            websocket.OnMessage += (bytes) =>
            {
                try
                {
                    var messageString = Encoding.UTF8.GetString(bytes);
                    var message = JsonUtility.FromJson<TranscriptionMessage>(messageString);
                    HandleTranscriptionMessage(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing message: {ex.Message}");
                }
            };

            // Connect to the server
            await websocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect: {ex.Message}");
            UpdateConnectionStatus($"Connection failed: {ex.Message}");
        }
    }

    public async void DisconnectFromServer()
    {
        if (websocket != null && isConnected)
        {
            await websocket.Close();
            websocket = null;
            isConnected = false;
        }
    }

    private async System.Threading.Tasks.Task DisconnectFromServerAsync()
    {
        if (websocket != null && isConnected)
        {
            await websocket.Close();
            websocket = null;
            isConnected = false;
        }
    }

    private void HandleTranscriptionMessage(TranscriptionMessage message)
    {
        if (message.type == "transcription")
        {
            // If this is a final transcription, add it to the history
            if (message.is_final)
            {
                //finalTranscription += message.text + "\n";

                //if (finalTranscriptionText != null)
                //{
                //    finalTranscriptionText.text = finalTranscription;
                //}

                //// Auto-scroll to bottom
                //if (transcriptionScrollRect != null)
                //{
                //    StartCoroutine(ScrollToBottom());
                //}

                //// Clear current transcription for next phrase
                //if (currentTranscriptionText != null)
                //{
                //    currentTranscriptionText.text = "";
                //}

                //// You can add custom events here
                //OnFinalTranscription?.Invoke(message.text);

                print("done " + message.text);
                if (inputFieldMessage.text.Replace(" ","") != "") {
                    aiMateClient.onSendMessage();
                }
            }
            else
            {
                // You can add custom events for real-time transcription
                inputFieldMessage.text = message.text;
                OnCurrentTranscription?.Invoke(message.text);
            }
        }
    }

    private void UpdateConnectionStatus(string status)
    {
        popUpMessage.showMessage($"Speech Recognition status: {status}");

        if (!isConnected)
        {
            buttonDisconnect();
        } else
        {
            buttonConnect();
        }
    }

    // Events that other scripts can subscribe to
    public System.Action<string> OnCurrentTranscription;
    public System.Action<string> OnFinalTranscription;

    void OnDestroy()
    {
        if (websocket != null)
        {
            // Use synchronous close for OnDestroy to avoid async issues
            websocket.Close();
            websocket = null;
            isConnected = false;
        }
    }

    //async void OnApplicationPause(bool pauseStatus)
    //{
    //    if (pauseStatus && websocket != null)
    //    {
    //        await DisconnectFromServerAsync();
    //    }
    //}

    //async void OnApplicationFocus(bool hasFocus)
    //{
    //    if (!hasFocus && websocket != null)
    //    {
    //        await DisconnectFromServerAsync();
    //    }
    //}
}