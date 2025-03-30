using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Audios
{
    public const string Destroy = "destroy";
    public const string Die = "die";
    public const string GrabCollectible = "grabcollectible";
    public const string Hammer = "hammer";
    public const string Jump = "jump";
    public const string JumpOverBarrel = "jumpOverBarrel";
    public const string Ladder = "ladder";
    public const string Lose = "lose";
    public const string MainMenu = "main_menu";
    public const string LevelTheme = "main_song";
    public const string MenuClick = "menuClick";
    public const string Move = "move";
    public const string MovingLevel = "movingLevel";
    public const string FinishLevel = "roundwin";
    public const string Rewind = "rewindObject";
    public const string StartLevel = "startLevel";
    public const string VictoryMusic = "victoryMusic";
    public const string Win = "win";
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private static bool _initializedSound;

    private static PlayerController _playerController;
    private static PlayerRewindController _currentRewindController;
    
    private record AudioClipRecord
    {
        public readonly string Name;
        public AudioClip Clip;
        public float Volume { get; private set; }
        public float Pitch { get; private set; }
        public bool Loop { get; }
        public AudioSource CurrentSource { get; private set; }

        public bool IsPaused { get; private set; }

        public AudioClipRecord(string name, AudioClip clip = null, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            Name = name;
            Clip = clip;
            Volume = volume;
            Pitch = pitch;
            Loop = loop;
        }

        public void PlayClip(float delay = 0f, float? volume = null, float? pitch = null)
        {
            if (Loop)
            {
                var src = GetOrAddSource(volume, pitch);
                Play(src, delay); 
            }
            else
            {
                Instance.StartCoroutine(PlayAndDispose(delay,volume,pitch));
            }
        }

        private IEnumerator PlayAndDispose(float delay, float? volume = null, float? pitch = null)
        {
            var src = GetOrAddSource(volume, pitch);
            Play(src, delay);

            while (src.isPlaying)
            {
                yield return new WaitForEndOfFrame();
            }
            Destroy(src);
        }

        private static void Play(AudioSource src, float delay = 0f)
        {
            if (delay > 0f)
            {
                src.PlayScheduled(delay);   
            }
            else
            {
                src.Play();
            }
        }
        
        public void StopClip()
        {
            if (CurrentSource) CurrentSource.Stop();
        }

        public void PauseClip()
        {
            if (CurrentSource && CurrentSource.isPlaying)
            {
                CurrentSource.Pause();
                IsPaused = true;
            }
        }

        public void ResumeClip()
        {
            if (CurrentSource != null)
            {
                CurrentSource.UnPause();
                IsPaused = false;
            }
        }
        
        private AudioSource GetOrAddSource(float? volume = null, float? pitch = null)
        {
            if (CurrentSource) return CurrentSource;
            if (Clip == null) throw new ArgumentNullException(Clip.name, "Audio clip has not been loaded!");
            
            var src = Instance.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.clip = Clip;
            
            src.volume = volume ?? Volume; 
            src.pitch = pitch ?? Pitch;
            src.loop = Loop;

            if (Loop) CurrentSource = src;
            
            return src;
        }
        
        public void SetVolume(float volume)
        {
            Volume = volume;
            if (CurrentSource)
            {
                CurrentSource.volume = Volume;
            }
        }
        
        public void SetPitch(float pitch)
        {
            Pitch = pitch;
            if (CurrentSource)
            {
                CurrentSource.pitch = Pitch;
            }
        }

        public void Reset()
        {
            SetVolume(1f);
            SetPitch(1f);
            StopClip();
            IsPaused = false;
        }
    }
    
    private static readonly Dictionary<string, AudioClipRecord> AudioClips = new();
    
    private static readonly AudioClipRecord[] ClipList =
    {
        new(Audios.Destroy),
        new(Audios.Die),
        new(Audios.GrabCollectible),
        new(Audios.Hammer, loop: true),
        new(Audios.Jump),
        new(Audios.JumpOverBarrel),
        new(Audios.Ladder),
        new(Audios.Lose),
        new(Audios.MainMenu, loop: true),
        new(Audios.LevelTheme, loop: true),
        new(Audios.MenuClick),
        new(Audios.Move),
        new(Audios.MovingLevel),
        new(Audios.FinishLevel),
        new(Audios.Rewind),
        new(Audios.StartLevel),
        new(Audios.VictoryMusic, loop: true),
        new(Audios.Win)
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!_initializedSound)
        {
            InitializeSound();
        }

        GameManager.OnLevelCompleted += () =>
        {
            ResetSounds();
            PlaySound(Audios.FinishLevel);
        };
    }

    private static void InitializeSound()
    {
        foreach (var clip in ClipList)
        {
            clip.Clip = Resources.Load<AudioClip>($"Audio/{clip.Name}");
            AudioClips.Add(clip.Name, clip);
        }

        _initializedSound = true;
    }

    public static void ResetSounds()
    {
        foreach (var clip in AudioClips.Select(kvp => kvp.Value))
        {
            clip.Reset();
        }
    }

    private void Start()
    {
        ChangeBackgroundMusic(GameManager.MainMenu);
    }

    public static void PlaySound(string soundName, float delay = 0f, float? volume = null, float? pitch = null)
    {
        var valid = AudioClips.TryGetValue(soundName, out var clip);
        if (valid)
        {
            clip.PlayClip(delay, volume, pitch);
        }
        else
        {
            Debug.LogWarning($"Could not play sound: {soundName} was not found!");
        }
    }

    public static void StopSound(string soundName)
    {
        var valid = AudioClips.TryGetValue(soundName, out var clip);
        if (valid)
        {
            clip.StopClip();
        }
        else
        {
            Debug.LogWarning($"Could not stop sound: {soundName} was not found!");
        }
    }

    public static void ChangeBackgroundMusic(string sceneName = null)
    {
        sceneName ??= SceneManager.GetActiveScene().name;

        var nextSound = string.Empty;
        var delay = 0f;
        switch (sceneName)
        {
            case GameManager.MainMenu:
                nextSound = Audios.MainMenu;
                break;
            case GameManager.VictoryScene:
                var foundClip = AudioClips.TryGetValue("win", out var winGameClip);
                if (foundClip)
                {
                    PlaySound("win");
                    nextSound = Audios.VictoryMusic;
                    delay = (float)AudioSettings.dspTime + winGameClip.Clip.length + 1.5f;
                }
                break;
            default:
                if (sceneName.ToLowerInvariant().Contains(GameManager.LevelPrefix.ToLowerInvariant()))
                {
                    nextSound = Audios.LevelTheme;
                }
                break;
        }

        if (!nextSound.Equals(String.Empty))
        {
            PlaySound(nextSound, delay);
        }

        foreach (var clip in AudioClips.Select(kvp => kvp.Value).Where(clip => !clip.Name.Equals(nextSound) && clip.Loop))
        {
            clip.StopClip();
        }
    }

    public static void PauseBackgroundMusic()
    {
        foreach (var clip in AudioClips.Select(kvp => kvp.Value).Where(clip => clip.Loop))
        {
            clip.PauseClip();
        }    
    }

    public static void StopBackgroundMusic()
    {
        foreach (var clip in AudioClips.Select(kvp => kvp.Value).Where(clip => clip.Loop))
        {
            clip.StopClip();
        }       
    }
    
    private static void ResumeBackgroundMusic()
    {
        foreach (var clip in AudioClips.Select(kvp => kvp.Value).Where(clip => clip.Loop))
        {
            clip.ResumeClip();
        }   
    }

    public static void PlayHammerMusic()
    {
        PauseBackgroundMusic();
        PlaySound(Audios.Hammer);
    }

    public static void StopHammerMusic()
    {
        StopSound(Audios.Hammer);
        ResumeBackgroundMusic();
    }

    public static void OnPauseToggle(bool isPaused)
    {
        foreach (var clip in AudioClips.Select(kvp => kvp.Value).Where(clip => clip.Loop))
        {
            clip.SetVolume(isPaused ? 0.5f : 1f);
        }
    }

    public static void LinkPlayerController(PlayerController controller, PlayerRewindController rewindController)
    {
        if (!Instance) return;

        _playerController = controller;
        _currentRewindController = rewindController;
        
        _currentRewindController.OnRewindToggle.AddListener(mode =>
        {
            foreach (var clip in AudioClips.Select(kvp => kvp.Value))
            {
                if (clip.Loop) 
                    clip.SetPitch(mode ? 0.5f : 1.0f);
                else clip.SetVolume(mode ? 0f : 1.0f);
            }
        });

        _playerController.OnDeath += () =>
        {
            ResetSounds();
            PlaySound(Audios.Die);
        };
    }
}
