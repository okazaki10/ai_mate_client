using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class MenuManager : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject chatCanvas;
    public GameObject settingCanvas;
    public GameObject characterCanvas;
    public TMP_InputField inputFieldUsername;
    public TMP_InputField inputFieldIpAddress;
    public TMP_InputField inputFieldVadThd;
    public TMP_InputField inputFieldVadStopTime;

    public TMP_Dropdown dropdownCharaters;
    public TMP_InputField inputFieldCharacterName;
    public TMP_InputField inputFieldCharacterDescription;
    public TMP_Dropdown dropdownRvcModels;
    public TMP_InputField inputFieldVrmPath;

    public MicrophoneRecord microphoneRecord;
    public LocaleDropdown localeDropdown;

  

    public const string USER_NAME = "userName";
    public const string IP_ADDRESS = "ipAddress";
    public const string IS_DEFAULT = "isDefault";
    public const string LANGUAGE = "language";
    public const string VAD_THD = "vadThd";
    public const string VAD_STOP_TIME = "vadStopTime";
    public const string CHARACTER_NAME = "characterName";

    void Start()
    {
        var isDefault = PlayerPrefs.GetInt(IS_DEFAULT, 0);
        if (isDefault == 0)
        {
            resetToDefault();
            PlayerPrefs.SetInt(IS_DEFAULT, 1);
            PlayerPrefs.Save();
        }
        onLoadSettings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void onToggleChatOnOff()
    {
        chatCanvas.SetActive(!chatCanvas.activeSelf);
    }

    public void onShowSettings()
    {
        settingCanvas.SetActive(true);
        menuCanvas.SetActive(false);
    }

    public void onBackFromSettings()
    {
        settingCanvas.SetActive(false);
        menuCanvas.SetActive(true);
    }

    public void onShowCharacter()
    {
        characterCanvas.SetActive(true);
        menuCanvas.SetActive(false);
    }

    public void onBackFromCharacter()
    {
        characterCanvas.SetActive(false);
        menuCanvas.SetActive(true);
    }

    public void onChangeUserName(string text)
    {
        PlayerPrefs.SetString(USER_NAME, text);
        PlayerPrefs.Save();
    }

    public void OnLocaleChanged(int index)
    {
        PlayerPrefs.SetInt(LANGUAGE, index);
        PlayerPrefs.Save();
    }

    public void onSaveSettings()
    {
        PlayerPrefs.SetString(IP_ADDRESS, inputFieldIpAddress.text);
        PlayerPrefs.SetString(VAD_THD, inputFieldVadThd.text);
        PlayerPrefs.SetString(VAD_STOP_TIME, inputFieldVadStopTime.text);
        PlayerPrefs.Save();
        onLoadSettings();
    }

    public void onLoadSettings()
    {
        inputFieldUsername.text = PlayerPrefs.GetString(USER_NAME);
        localeDropdown.setLocale(PlayerPrefs.GetInt(LANGUAGE, 0));
        inputFieldIpAddress.text = PlayerPrefs.GetString(IP_ADDRESS);
        loadMicrophoneSettings();
    }

    public void resetToDefault()
    {
        inputFieldIpAddress.text = "http://127.0.0.1";
        inputFieldVadThd.text = "1.1";
        inputFieldVadStopTime.text = "3";
        PlayerPrefs.SetString(CHARACTER_NAME, "Hatsune Miku");
        onSaveSettings();
        onLoadSettings();
    }

    private void loadMicrophoneSettings()
    {
        try
        {
            inputFieldVadThd.text = PlayerPrefs.GetString(VAD_THD);
            inputFieldVadStopTime.text = PlayerPrefs.GetString(VAD_STOP_TIME);
            microphoneRecord.vadThd = float.Parse(inputFieldVadThd.text);
            microphoneRecord.vadStopTime = float.Parse(inputFieldVadThd.text);
        }
        catch (FormatException)
        {
            print($"Error: Could not parse float.");
        }
    }

    public void populateCharacter(List<CharacterDto> responseCharacters)
    {
        List<string> characters = new List<string>();
        foreach (var res in responseCharacters)
        {
            characters.Add(res.name);
        }
        dropdownCharaters.ClearOptions();
        dropdownCharaters.AddOptions(characters);
        dropdownCharaters.RefreshShownValue();
    }

    public void populateRvc(List<string> rvcList)
    {
        dropdownRvcModels.ClearOptions();
        dropdownRvcModels.AddOptions(rvcList);
        dropdownRvcModels.RefreshShownValue();
    }
}
