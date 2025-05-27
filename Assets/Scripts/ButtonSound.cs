using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSound : MonoBehaviour
{
    public enum SoundType
    {
        ButtonClick1,
        ButtonClick2
    }

    public SoundType soundType;

    public void PlaySound()
    {
        if (SoundManager.instance == null) return;

        switch (soundType)
        {
            case SoundType.ButtonClick1:
                SoundManager.instance.PlayButtonClick1();
                break;
            case SoundType.ButtonClick2:
                SoundManager.instance.PlayButtonClick2();
                break;
        }
    }
}

