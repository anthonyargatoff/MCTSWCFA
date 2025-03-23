using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]

    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource hammerSource;

    [Header("Audio Clips")]

    public AudioClip collectPointClip;
    public AudioClip destroyClip;
    public AudioClip hammerClip;
    public AudioClip jumpClip;
    public AudioClip ladderClip;
    public AudioClip loseMusicClip;
    public AudioClip mainMusicClip;
    public AudioClip moveClip;
    public AudioClip roundWinClip;

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
        PlayMusicSound();
    }

    public void PlayMusicSound()
    {
        if (mainMusicClip != null && musicSource != null)
        {
            musicSource.clip = mainMusicClip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void StartTimeWarp()
    {
        if (mainMusicClip != null && musicSource != null)
        {
            musicSource.pitch = 0.5f;
        }
        
    }

    public void EndTimeWarp()
    {
        if (mainMusicClip != null && musicSource != null)
        {
            musicSource.pitch = 1f;
        }

    }

    public void PlayHammerMusic()
    {
        musicSource.Pause();
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
        musicSource.Play();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

}
