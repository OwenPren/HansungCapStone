using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    public AudioSource bgmSource;
    public AudioSource sfxSource;

    public AudioClip bgmClip;          // 배경음악 클립
    public AudioClip buttonClick1Clip;
    public AudioClip buttonClick2Clip;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayButtonClick1()
    {
        sfxSource.PlayOneShot(buttonClick1Clip);
    }

    public void PlayButtonClick2()
    {
        sfxSource.PlayOneShot(buttonClick2Clip);
    }
}

