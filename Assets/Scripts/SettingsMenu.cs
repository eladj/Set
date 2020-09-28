using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public Slider numCardstoDrawSlider;
    public Toggle showAvailableSetsToggle;
    public TextMeshProUGUI textNumCardstoDrawNumber;

    void Start()
    {
        LoadSettings();
        textNumCardstoDrawNumber.text = ((int)numCardstoDrawSlider.value).ToString();
    }

    public void SetNumCardsToDraw(float numCardsToDraw_)
    {
        Debug.Log("SetNumCardsToDraw: " + ((int)numCardstoDrawSlider.value).ToString());
        textNumCardstoDrawNumber.text = ((int)numCardstoDrawSlider.value).ToString();
    }

    public void SaveSettings()
    {
        Debug.Log("Saved settings");
        PlayerPrefs.SetInt("NumCardsToDraw", ((int)numCardstoDrawSlider.value));
        PlayerPrefs.SetInt("ShowAvailableSets", boolToInt(showAvailableSetsToggle.isOn));
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        Debug.Log("Loading settings");
        if (PlayerPrefs.HasKey("NumCardsToDraw"))
            numCardstoDrawSlider.value = PlayerPrefs.GetFloat("NumCardsToDraw");
        else
            numCardstoDrawSlider.value = 3;
        if (PlayerPrefs.HasKey("ShowAvailableSets"))
            showAvailableSetsToggle.isOn = intToBool(PlayerPrefs.GetInt("ShowAvailableSets"));
        else
            showAvailableSetsToggle.isOn = false;
    }

    // The PlayerPerfs.save() only works with this on Android
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PlayerPrefs.Save();
        }
    }

    private int boolToInt(bool val)
    {
        if (val)
            return 1;
        else
            return 0;
    }

    private bool intToBool(int val)
    {
        if (val != 0)
            return true;
        else
            return false;
    }
}
