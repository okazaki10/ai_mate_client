using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class ApiResponse<T>
{
    public string status;
    public T data;
    public string message;
}

[Serializable]
public class ActionParams
{
    public ArrayList<string> emotions = new ArrayList<string>();
    public ArrayList<string> actions = new ArrayList<string>();
}

[Serializable]
public class ApiData
{
    public string character_name;
    public string generated_text;
    public string prompt;
    public string full_response;
    public int prompt_token;
    public int output_token;
    public string base64_audio;
    public ActionParams actionParams;
}

[Serializable]
public class ApiRequest
{
    public string name;
    public string prompt;
    public string language;
}

public class RestApiClient : MonoBehaviour
{
    //[Header("API Configuration")]
    //public string apiBaseUrl = "https://your-api-endpoint.com/api";
    //public string apiKey = "your-api-key-here";

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public Text chatText;

    public InputField inputFieldUsername;
    public InputField inputFieldIpAddress;
    public ScrollRect scrollRectChat;
    public Button buttonDeleteLastChat;
    public TMP_InputField inputFieldVadThd;
    public TMP_InputField inputFieldVadStopTime;
    public MicrophoneRecord microphoneRecord;
    public LocaleDropdown localeDropDown;

    private void Start()
    {
        inputFieldIpAddress.text = "http://127.0.0.1";
        inputFieldUsername.text = "raka";
        inputFieldVadThd.text = "1.1";
        inputFieldVadStopTime.text = "3";

        onApplySettings();

        // Get AudioSource component if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        buttonDeleteLastChat.onClick.AddListener(onButtonDeleteLastChat);
        StartCoroutine(getChats(onSuccess: (response) =>
        {
            chatText.text = response.data;
            ScrollDown();
        },
            onError: (error) =>
            {
                Debug.LogError($"Failed to fetch chats: {error}");
            }
           )
        );
    }

    private void onApplySettings()
    {
        try
        {
            microphoneRecord.vadThd = float.Parse(inputFieldVadThd.text);
            microphoneRecord.vadStopTime = float.Parse(inputFieldVadStopTime.text);
        }
        catch (FormatException)
        {
            print($"Error: Could not parse float.");
        }
    }
    
    private void onButtonDeleteLastChat()
    {
        StartCoroutine(deleteLastChat(onSuccess: (response) =>
        {
            chatText.text = response.data;
            ScrollDown();
        },
         onError: (error) =>
         {
             Debug.LogError($"Failed to fetch chats: {error}");
         }
        )
     );
    }

    // Method to send text and receive audio response
    public void SendTextRequest(string text, Action<ApiResponse<ApiData>> onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(SendTextRequestCoroutine(text, onSuccess, onError));
    }

    private IEnumerator SendTextRequestCoroutine(string text, Action<ApiResponse<ApiData>> onSuccess, Action<string> onError)
    {
        // Create request data
        ApiRequest requestData = new ApiRequest
        {
            name = inputFieldUsername.text,
            prompt = text,
            language = localeDropDown.GetSelectedLocaleCode()
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(inputFieldIpAddress.text + ":7874" + "/generate", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            //// Add API key if provided
            //if (!string.IsNullOrEmpty(apiKey))
            //{
            //    request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            //}

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"Request failed: {request.error} - {request.responseCode}";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            string responseText = request.downloadHandler.text;

            // Parse response outside of try-catch to avoid yield issues
            ApiResponse<ApiData> response = ParseApiResponse<ApiData>(responseText);

            if (response == null)
            {
                string error = "Failed to parse API response";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            Debug.Log($"API Response Status: {response.status}");
            Debug.Log($"Generated Text: {response.data.generated_text}");
            Debug.Log($"Tokens - Prompt: {response.data.prompt_token}, Output: {response.data.output_token}");

            onSuccess?.Invoke(response);
        }
    }

    private IEnumerator getChats(Action<ApiResponse<String>> onSuccess, Action<string> onError)
    {
        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(inputFieldIpAddress.text + ":7874" + "/get-chat", "GET"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            //// Add API key if provided
            //if (!string.IsNullOrEmpty(apiKey))
            //{
            //    request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            //}

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"Request failed: {request.error} - {request.responseCode}";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            string responseText = request.downloadHandler.text;

            // Parse response outside of try-catch to avoid yield issues
            var response = ParseApiResponse<String>(responseText);

            if (response == null)
            {
                string error = "Failed to parse API response";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            Debug.Log($"API Response Status: {response.status}");
            Debug.Log($"Generated Text: {response.data}");

            onSuccess?.Invoke(response);
        }
    }

    private IEnumerator deleteLastChat(Action<ApiResponse<String>> onSuccess, Action<string> onError)
    {
        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(inputFieldIpAddress.text + ":7874" + "/delete-last-chat", "DELETE"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            //// Add API key if provided
            //if (!string.IsNullOrEmpty(apiKey))
            //{
            //    request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            //}

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"Request failed: {request.error} - {request.responseCode}";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            string responseText = request.downloadHandler.text;

            // Parse response outside of try-catch to avoid yield issues
            var response = ParseApiResponse<String>(responseText);

            if (response == null)
            {
                string error = "Failed to parse API response";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            Debug.Log($"API Response Status: {response.status}");
            Debug.Log($"Generated Text: {response.data}");

            onSuccess?.Invoke(response);
        }
    }

    private ApiResponse<T> ParseApiResponse<T>(string responseText)
    {
        try
        {
            return JsonUtility.FromJson<ApiResponse<T>>(responseText);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse response: {e.Message}");
            return null;
        }
    }

    // Method to convert base64 to AudioClip and play
    public void PlayBase64Audio(string base64Audio, Action onAudioDonePlaying)
    {
        if (string.IsNullOrEmpty(base64Audio))
        {
            Debug.LogWarning("No base64 audio data provided");
            return;
        }

        StartCoroutine(ConvertAndPlayAudio(base64Audio, onAudioDonePlaying));
    }

    private IEnumerator ConvertAndPlayAudio(string base64Audio, Action onAudioDonePlaying)
    {
        // Convert base64 to byte array and write to file outside of try-catch
        byte[] audioBytes = ConvertBase64ToBytes(base64Audio);
        if (audioBytes == null)
        {
            yield break;
        }

        // Create temporary file path
        string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "temp_audio.wav");

        // Write bytes to file
        bool fileWritten = WriteAudioFile(tempPath, audioBytes);
        if (!fileWritten)
        {
            yield break;
        }

        // Load audio clip from file
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                if (audioSource != null && clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    StartCoroutine(WaitForAudioToEnd(onAudioDonePlaying));
                }
                else
                {
                    Debug.LogError("AudioSource or AudioClip is null");
                }
            }
            else
            {
                Debug.LogError($"Failed to load audio: {www.error}");
            }
        }

        // Clean up temporary file
        CleanupTempFile(tempPath);
    }

    private IEnumerator WaitForAudioToEnd(Action onAudioDonePlaying)
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        onAudioDonePlaying?.Invoke();
    }

    private byte[] ConvertBase64ToBytes(string base64Audio)
    {
        try
        {
            return Convert.FromBase64String(base64Audio);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error converting base64 to bytes: {e.Message}");
            return null;
        }
    }

    private bool WriteAudioFile(string path, byte[] audioBytes)
    {
        try
        {
            System.IO.File.WriteAllBytes(path, audioBytes);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error writing audio file: {e.Message}");
            return false;
        }
    }

    private void CleanupTempFile(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cleaning up temp file: {e.Message}");
        }
    }

    // Convenience method to send text and automatically play returned audio
    public void SendTextAndPlayAudio(string text, Action onAudioDonePlaying)
    {
        SendTextRequest(text,
            onSuccess: (response) =>
            {
                chatText.text += "\n\n" + response.data.character_name + " : " + response.data.generated_text;
                ScrollDown();
                if (!string.IsNullOrEmpty(response.data.base64_audio))
                {
                    PlayBase64Audio(response.data.base64_audio, onAudioDonePlaying);
                }
                else
                {
                    Debug.Log("No audio data in response");
                    onAudioDonePlaying.Invoke();
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"Failed to get audio: {error}");
                onAudioDonePlaying.Invoke();
            }
        );
    }

    public void ScrollDown()
    {
        // Scroll to bottom
        Canvas.ForceUpdateCanvases(); // ensures layout updates first
        scrollRectChat.verticalNormalizedPosition = 0f;
    }
}