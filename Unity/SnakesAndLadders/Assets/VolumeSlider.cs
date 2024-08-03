using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This class is for the volume slider on the main menu
public class VolumeSlider : MonoBehaviour
{
    // Stores the slider instance
    [SerializeField] Slider volumeSlider;
    // Start is called before the first frame update
    void Start()
    {
        // If volume has never been chosen before (we can know because every time volume is chosen it gets saved to the local storage)
        if (!PlayerPrefs.HasKey("Volume"))
        {
            // Set the default volume to 50%
            PlayerPrefs.SetFloat("Volume", 0.5f);
        }
        // Load volume from local storage. Something must be there because we just stored 50% in the default case.
        LoadVolume();
    }

    // Listening to slider value changed. On change will set the new volume and store to the local storage
    public void ChangeVolume()
    {
        AudioListener.volume = volumeSlider.value;
        SaveVolume();
    }

    // Loads the volume from the local storage
    private void LoadVolume()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("Volume");
    }

    // Stores the new volume to the local storage
    private void SaveVolume()
    {
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
    }
}
