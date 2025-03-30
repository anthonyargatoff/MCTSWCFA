using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]

    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource hammerSource;

    [Header("Audio Clips")]

    public AudioClip destroyClip;
    public AudioClip dieClip;
    public AudioClip grabCollectible;
    public AudioClip hammerClip;
    public AudioClip jumpClip;
    public AudioClip jumpOverBarrel;
    public AudioClip ladderClip;
    public AudioClip loseClip;
    public AudioClip mainMenuMusicClip;
    public AudioClip mainMusicClip;
    public AudioClip menuClickClip;
    public AudioClip moveClip;
    public AudioClip movingLevelClip;
    public AudioClip rewindObjectClip;
    public AudioClip roundWinClip;
    public AudioClip startLevelClip;
    public AudioClip victoryMusicClip;
    public AudioClip winGameClip;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        PlayMusicSound(mainMenuMusicClip);
    }

    public void PlayMusicSound(AudioClip source)
    {
        if (source != null && musicSource != null)
        {
            musicSource.clip = source;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void ChangeMusicSound(string sceneName)
    {
        sceneName = sceneName.ToLowerInvariant();
        if (sceneName.ToLowerInvariant().Equals("mainmenu"))
        {
            PlayMusicSound(mainMenuMusicClip);
        }
        else if (sceneName.Equals("level 1") || sceneName.Equals("level 2") || sceneName.Equals("level 3"))
        {
            PlayMusicSound(mainMusicClip);
        }
        else if (sceneName.Equals("victoryscene"))
        {
            PlaySound(winGameClip);
            musicSource.clip = victoryMusicClip;
            musicSource.PlayScheduled(AudioSettings.dspTime + winGameClip.length + 1.5f);
            musicSource.loop = true;
        }
    }

    public void PauseMusicSound()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }

    public void StopMusicSound()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void ResumeMusicSound()
    {
        if (musicSource != null)
        {
            musicSource.Play();
        }
    }

    public void StartTimeWarp()
    {
        if (mainMusicClip != null && musicSource != null)
        {
            musicSource.pitch = 0.5f;

        }
        if (hammerClip != null && hammerSource != null)
        {
            hammerSource.pitch = 0.5f;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = 0f;
        }

    }

    public void EndTimeWarp()
    {
        if (mainMusicClip != null && musicSource != null)
        {
            musicSource.pitch = 1f;
        }
        if (hammerClip != null && hammerSource != null)
        {
            hammerSource.pitch = 1f;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = 1f;
        }
    }

    public void PlayHammerMusic()
    {
        PauseMusicSound();
        if (hammerSource != null && hammerClip != null)
        {
            hammerSource.clip = hammerClip;
            hammerSource.loop = true;
            hammerSource.Play();
        }
    }

    public void StopHammerMusic()
    {
        if (hammerSource != null && hammerSource.isPlaying)
        {
            hammerSource.Stop();
        }
        ResumeMusicSound();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayerDied()
    {
        StopHammerMusic();
        PauseMusicSound();
        PlaySound(dieClip);
    }

    public void EndLevel()
    {
        StopHammerMusic();
        StopMusicSound();
        PlaySound(roundWinClip);
    }

}
