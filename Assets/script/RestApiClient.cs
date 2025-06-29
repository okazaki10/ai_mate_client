using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TextCore.Text;
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
    public List<string> emotions = new List<string>();
    public List<string> actions = new List<string>();
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
    public ActionParams action_params;
}

[Serializable]
public class CharacterDto
{
    public string name = "";
    public string description = "";
    public string rvc_model = "";
    public string vrm_path = "";
}

[Serializable]
public class ResponseCharacter
{
    public List<CharacterDto> characters = new List<CharacterDto>();
}

[Serializable]
public class ApiRequest
{
    public string character_name;
    public string name;
    public string prompt;
    public string language;
}

[Serializable]
public class ChatRequest
{
    public string name;
}

[Serializable]
public class RequestCharacter
{
    public string name;
    public string description;
    public string rvc_model;
    public string vrm_path;
}

public class RestApiClient : MonoBehaviour
{
    //[Header("API Configuration")]
    //public string apiBaseUrl = "https://your-api-endpoint.com/api";
    //public string apiKey = "your-api-key-here";

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public TMP_Text chatText;

    public ScrollRect scrollRectChat;
    public LocaleDropdown localeDropDown;
    public PopUpMessage popUpMessage;
    public MenuManager menuManager;
    public VRMAutoLoader vRMAutoLoader;

    public List<CharacterDto> characters = new List<CharacterDto>();
    public CharacterDto character = new CharacterDto();
    public List<string> rvcList = new List<string>();

    private void Start()
    {
        // Get AudioSource component if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        onGetChats();
        onGetCharacters();
    }

    public void onButtonDeleteLastChat()
    {
        StartCoroutine(deleteLastChat(onSuccess: (response) =>
        {
            chatText.text = response.data;
            ScrollDown();
        },
         onError: (error) =>
         {
             Debug.LogError($"Failed to fetch chats: {error}, please run start_server.bat");
             popUpMessage.showMessage(error);
         }
        )
     );
    }

    public void onGetChats()
    {
        StartCoroutine(getChats(onSuccess: (response) =>
        {
            chatText.text = response.data;
            ScrollDown();
        },
          onError: (error) =>
          {
              Debug.LogError($"Failed to fetch chats: {error}");
              popUpMessage.showMessage($"Failed to fetch chats: {error}, please run start_server.bat");
          }
         )
      );
    }

    public void onGetCharacters()
    {
        StartCoroutine(getCharacters(onSuccess: (response) =>
        {
            characters = response.data.characters;

            menuManager.populateCharacter(characters);

            populateCharacterSetting();

            onGetRvc();
        },
         onError: (error) =>
         {
             Debug.LogError($"Failed to fetch chats: {error}, please run start_server.bat");
             popUpMessage.showMessage(error);
         }
        )
     );
    }

    public void onGetRvc()
    {
        StartCoroutine(getRvc(onSuccess: (response) =>
        {
            rvcList = response.data;
            menuManager.populateRvc(rvcList);
            populateCharacterSetting();

        },
         onError: (error) =>
         {
             Debug.LogError($"Failed to fetch chats: {error}, please run start_server.bat");
             popUpMessage.showMessage(error);
         }
        )
     );
    }

    void populateCharacterSetting()
    {
        var charPref = PlayerPrefs.GetString(MenuManager.CHARACTER_NAME) != "" ? PlayerPrefs.GetString(MenuManager.CHARACTER_NAME) : "Hatsune Miku";
        character = characters.Where(value => value.name == charPref).FirstOrDefault() ?? new CharacterDto();
        menuManager.dropdownCharaters.value = characters.FindIndex(value => value.name == character.name);
        menuManager.dropdownCharaters.RefreshShownValue();

        menuManager.inputFieldCharacterName.text = character.name;
        menuManager.inputFieldCharacterDescription.text = character.description;

        menuManager.dropdownRvcModels.value = rvcList.FindIndex(value => value == character.rvc_model);
        menuManager.dropdownRvcModels.RefreshShownValue();

        if (menuManager.inputFieldVrmPath.text != character.vrm_path)
        {
            if (character.vrm_path == "")
            {
                vRMAutoLoader.useDefaultModel();
            }
            else
            {
                _ = vRMAutoLoader.LoadVRMFromPath(character.vrm_path);
            }
       
        }
        menuManager.inputFieldVrmPath.text = character.vrm_path;
    }

    //public void onAddNewCharacter()
    //{
    //    var requestData = new RequestCharacter
    //    {
    //        name = "New Character",
    //        description = "",
    //        rvc_model = "",
    //        vrm_path = ""
    //    };

    //    StartCoroutine(addCharacter(requestData, onSuccess: (response) =>
    //    {
    //        PlayerPrefs.SetString(MenuManager.CHARACTER_NAME, requestData.name);
    //        onGetCharacters();
    //    },
    //      onError: (error) =>
    //      {
    //          Debug.LogError($"Failed to add character: {error}");
    //          popUpMessage.showMessage($"Failed to add character: {error}, please run start_server.bat");
    //      }
    //     )
    //  );
    //}

    public void onResetToDefault()
    {
        var requestData = new RequestCharacter
        {
            name = "",
            description = "",
            rvc_model = "",
            vrm_path = ""
        };

        StartCoroutine(resetToDefault(requestData, onSuccess: (response) =>
        {
            onGetCharacters();
            onGetChats();
        },
          onError: (error) =>
          {
              Debug.LogError($"Failed to add character: {error}");
              popUpMessage.showMessage($"Failed to add character: {error}, please run start_server.bat");
          }
         )
      );
    }

    public void onSaveCharacter()
    {
        var requestData = new RequestCharacter
        {
            name = menuManager.inputFieldCharacterName.text,
            description = menuManager.inputFieldCharacterDescription.text,
            rvc_model = character.rvc_model,
            vrm_path = menuManager.inputFieldVrmPath.text
        };

        StartCoroutine(addCharacter(requestData, onSuccess: (response) =>
        {
            PlayerPrefs.SetString(MenuManager.CHARACTER_NAME, requestData.name);
            onGetCharacters();
            onGetChats();
        },
          onError: (error) =>
          {
              Debug.LogError($"Failed to add character: {error}");
              popUpMessage.showMessage($"Failed to add character: {error}, please run start_server.bat");
          }
         )
      );
    }

    public void onDeleteCharacter()
    {
        var requestData = new RequestCharacter
        {
            name = character.name,
            description = "",
            rvc_model = "",
            vrm_path = ""
        };

        StartCoroutine(deleteCharacter(requestData, onSuccess: (response) =>
        {
            if (response.status != "success")
            {
                Debug.LogError(response.message);
                popUpMessage.showMessage(response.message);
                return;
            }
            PlayerPrefs.SetString(MenuManager.CHARACTER_NAME, "Hatsune Miku");
            onGetCharacters();
        },
          onError: (error) =>
          {
              Debug.LogError($"Failed to add character: {error}");
              popUpMessage.showMessage($"Failed to add character: {error}, please run start_server.bat");
          }
         )
      );
    }

    public void onChangeCharacter(int index)
    {
        character = characters[index];
        PlayerPrefs.SetString(MenuManager.CHARACTER_NAME, character.name);
        populateCharacterSetting();
        onGetChats();
    }

    public void onChangeRvc(int index)
    {
        character.rvc_model = rvcList[index];
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
            character_name = PlayerPrefs.GetString(MenuManager.CHARACTER_NAME),
            name = PlayerPrefs.GetString(MenuManager.USER_NAME),
            prompt = text,
            language = localeDropDown.GetSelectedLocaleCode()
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(PlayerPrefs.GetString(MenuManager.IP_ADDRESS) + ":7874" + "/generate", "POST"))
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

    private IEnumerator getRvc(Action<ApiResponse<List<string>>> onSuccess, Action<string> onError)
    {
        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(PlayerPrefs.GetString(MenuManager.IP_ADDRESS) + ":7874" + "/get-rvc", "GET"))
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
            var response = ParseApiResponse<List<string>>(responseText);

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

    private IEnumerator getCharacters(Action<ApiResponse<ResponseCharacter>> onSuccess, Action<string> onError)
    {
        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(PlayerPrefs.GetString(MenuManager.IP_ADDRESS) + ":7874" + "/get-character", "GET"))
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
            var response = ParseApiResponse<ResponseCharacter>(responseText);

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

    private IEnumerator getChats(Action<ApiResponse<String>> onSuccess, Action<string> onError)
    {
        var requestData = new ChatRequest
        {
            name = PlayerPrefs.GetString(MenuManager.CHARACTER_NAME)
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(PlayerPrefs.GetString(MenuManager.IP_ADDRESS) + ":7874" + "/get-chat", "POST"))
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


    private IEnumerator resetToDefault(RequestCharacter requestData, Action<ApiResponse<String>> onSuccess, Action<string> onError)
    {
        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(PlayerPrefs.GetString(MenuManager.IP_ADDRESS) + ":7874" + "/default-character", "POST"))
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

    private IEnumerator addCharacter(RequestCharacter requestData, Action<ApiResponse<String>> onSuccess, Action<string> onError)
    {
        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(PlayerPrefs.GetString(MenuManager.IP_ADDRESS) + ":7874" + "/add-character", "POST"))
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

    private IEnumerator deleteCharacter(RequestCharacter requestData, Action<ApiResponse<String>> onSuccess, Action<string> onError)
    {
        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(PlayerPrefs.GetString(MenuManager.IP_ADDRESS) + ":7874" + "/delete-character", "DELETE"))
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
        var requestData = new ChatRequest
        {
            name = PlayerPrefs.GetString(MenuManager.CHARACTER_NAME)
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(PlayerPrefs.GetString(MenuManager.IP_ADDRESS) + ":7874" + "/delete-last-chat", "DELETE"))
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
    public void SendTextAndPlayAudio(string text, Action<ApiResponse<ApiData>> onSuccess = null, Action onError = null, Action onAudioDonePlaying = null)
    {
        SendTextRequest(text,
            onSuccess: (response) =>
            {
                chatText.text += "\n\n" + response.data.character_name + " : " + response.data.generated_text;
                ScrollDown();
                onSuccess?.Invoke(response);
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
                popUpMessage.showMessage($"Failed to fetch chats: {error}, please run start_server.bat");
                onError.Invoke();
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