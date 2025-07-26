using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AiMateClient : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text recordText;
    public TMP_Text chatText;
    public TMP_Dropdown languageDropdown;
    public RestApiClient restApiClient;

    public InputField inputFieldMessage;
    public ScrollRect scrollRectChat;
    public VRMModelManager vrmModelManager;
    public VRMEmotionBlinkController vrmEmotionBlinkController;
    public PopUpMessage popUpMessage;
    public MenuManager menuManager;
    public SingingAnimation singingAnimation;
    public SpeechRecognitionClient speechRecognitionClient;

    private void Update()
    {
        if (inputFieldMessage.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            inputFieldMessage.text = inputFieldMessage.text.Trim();
            if (inputFieldMessage.text.Trim() != "")
            {
                onSendMessage();
            } else
            {
                inputFieldMessage.text = "";
            }
        }
    }

    public void onSendMessage()
    {
        if (menuManager.inputFieldUsername.text == "")
        {
            menuManager.inputFieldUsername.text = "User";
        }

        chatText.text += "\n\n" + menuManager.inputFieldUsername.text + " : " + inputFieldMessage.text;
        restApiClient.audioSourceInstrument.Stop();
        restApiClient.SendTextAndPlayAudio(inputFieldMessage.text, false, onSuccessFetch, onErrorFetch, onAudioDonePlaying);
        inputFieldMessage.text = "";
        speechRecognitionClient.buttonDisconnect();
        ScrollDown();
    }

    public void ScrollDown()
    {
        // Scroll to bottom
        Canvas.ForceUpdateCanvases(); // ensures layout updates first
        scrollRectChat.verticalNormalizedPosition = 0f;
    }


    bool isGeneratingSong = false;
    bool isAboutToQuit = false;
    void onSuccessFetch(ApiResponse<ApiData> response)
    {
        popUpMessage.showPopUpForever(response.data.generated_text);
        foreach (var emotion in response.data.action_params.emotions)
        {
            if (emotion.ContainsInsensitive("SHY"))
            {
                vrmEmotionBlinkController.SetShy();
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
                vrmEmotionBlinkController.SetCurious();
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
            else if (action.ContainsInsensitive("SING"))
            {
                string pattern = "\\(\"(.*?)\"\\)";

                var match = Regex.Match(action, pattern);
                var url = "";
                if (match.Success)
                {
                    url = match.Groups[1].Value;
                    Debug.Log("Extracted: " + url);
                }

                print(url);

                if (url != "" && url != "YOUTUBE_URL")
                {
                    popUpMessage.SetMessage("Singing in process");
                    isGeneratingSong = true;
                    restApiClient.onGenerateSong(url, onSuccessGenerateSongs, onMusicDonePlaying, onErrorGenerateSong);
                }
            }
            else if (action.ContainsInsensitive("QUIT"))
            {
                isAboutToQuit = true;
            }
            else if (action.ContainsInsensitive("SEARCH"))
            {
                string pattern = "\\(\"(.*?)\"\\)";

                var match = Regex.Match(action, pattern);
                var query = "";
                if (match.Success)
                {
                    query = match.Groups[1].Value;
                    Debug.Log("Extracted: " + query);
                }

                print(query);

                if (query != "")
                {
                    popUpMessage.SetMessage("Web Search in process");
                    isGeneratingSong = true;
                    restApiClient.SendTextAndPlayAudio(query, true, onSuccessWebSearchFetch, onErrorFetch, onWebSearchDonePlaying);
                }
            }
        }
    }

    void onSuccessGenerateSongs(ApiResponse<ResponseSong> response)
    {
        popUpMessage.SetMessage("Singing : " + response.data.title + " bpm " + response.data.bpm);
        vrmEmotionBlinkController.SetNeutral();
        singingAnimation.StartSingingAnimation(response.data.bpm);
    }

    void onErrorGenerateSong(string error)
    {
        isGeneratingSong = false;
        vrmEmotionBlinkController.SetNeutral();
        vrmModelManager.animator.SetInteger("animBaseInt", 0);
        speechRecognitionClient.buttonConnect();
    }

    void onErrorFetch()
    {
        vrmEmotionBlinkController.SetNeutral();
        vrmModelManager.animator.SetInteger("animBaseInt", 0);
        speechRecognitionClient.buttonConnect();
    }

    void onAudioDonePlaying()
    {
        print("audio done playing");
        vrmEmotionBlinkController.SetNeutral();
        vrmModelManager.animator.SetInteger("animBaseInt", 0);
        if (isAboutToQuit)
        {
            Application.Quit();
        }
        if (!isGeneratingSong)
        {
            print("hide pop up");
            popUpMessage.HidePopUp();
            speechRecognitionClient.buttonConnect();
        }
    }

    void onMusicDonePlaying()
    {
        print("music done playing");
        isGeneratingSong = false;
        vrmEmotionBlinkController.SetNeutral();
        vrmModelManager.animator.SetInteger("animBaseInt", 0);
        popUpMessage.HidePopUp();
        singingAnimation.StopSingingAnimation();
        speechRecognitionClient.buttonConnect();
    }

    void onSuccessWebSearchFetch(ApiResponse<ApiData> response)
    {
        popUpMessage.showPopUpForever(response.data.generated_text);
    }

    void onWebSearchDonePlaying()
    {
        print("web search done playing");
        isGeneratingSong = false;
        vrmEmotionBlinkController.SetNeutral();
        vrmModelManager.animator.SetInteger("animBaseInt", 0);
        popUpMessage.HidePopUp();
        speechRecognitionClient.buttonConnect();
    }
}

