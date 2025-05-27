using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    public Slider slider;

    void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (SoundManager.instance != null && SoundManager.instance.bgmSource != null)
            slider.value = SoundManager.instance.bgmSource.volume;

        slider.onValueChanged.AddListener(SetVolume);
    }

    void SetVolume(float value)
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetBGMVolume(value);
        }
    }
}
