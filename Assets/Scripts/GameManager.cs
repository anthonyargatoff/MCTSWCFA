using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreEvent
{
    public static readonly int BarrelJump = 100;
    public static readonly int BarrelHammerDestroy = 200;
    public static readonly int BarrelRewindDestroy = 400;
}

public class GameManager : MonoBehaviour
{
    private static bool _debugMode;
    public static GameManager Instance { get; private set; }

    private const int TargetFrameRate = 60;
    private static float _frameRatio = 1.0f;
    
    public const string DebugScene = "SampleScene";
    private static bool _initialized;
    
    public const string MainMenu = "MainMenu";
    public const string VictoryScene = "VictoryScene";
    public const string LevelPrefix = "LEVEL ";
    private const int NumLevels = 4;
    private const int StartingLives = 3;

    private static GameObject _scoreTextPopup;

    public static int TotalScore { get; private set; }
    public static int CurrentLevel { get; private set; }
    public static int CurrentLives { get; private set; }
    public static int CurrentScore { get; private set; }
    
    public static bool isGamePaused { get; private set; }

    public static bool isCompletingLevel { get; private set; }

    public static bool isStartingLevel { get; private set; }

    public delegate void OnPausedDelegate();
    public static event OnPausedDelegate OnPaused;
    
    public delegate void OnLevelCompletedDelegate();
    public static event OnLevelCompletedDelegate OnLevelCompleted;
    
    // 3 minutes
    private const float ClearTimer = 180f;
    public static float LevelTimer { get; private set; } = 180f;
    
    private static PlayerController currentController;
    private static PlayerRewindController currentRewindController;

    private static Canvas _mainUI;
    private static Image _fadePanel;
    private static LoadingScreen _loadingScreen;
    private static GameOverScreen _gameOverScreen;
    private static Transform _hud;
    private static TextMeshProUGUI _livesText;
    private static TextMeshProUGUI _scoreText;
    private static TextMeshProUGUI _timerText;
    private static TextMeshProUGUI _fpsText;
    private static GameObject _pauseMenu;
    private static GameObject _debugMenu;

    private static int frameCount = 0;
    private static float timeSinceLastFrameRateCheck = 0f;

    private GameObject player;
    
    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
        
        Instance = this;
        _scoreTextPopup = Resources.Load<GameObject>("Prefabs/ScoreTextPopup");
        _mainUI = GameObject.Find("MainUICanvas").GetComponent<Canvas>();
        _fadePanel = _mainUI.transform.Find("FadePanel").GetComponent<Image>();
        _loadingScreen = _mainUI.transform.Find("LoadingScreen").GetComponent<LoadingScreen>();
        _gameOverScreen = _mainUI.transform.Find("GameOverScreen").GetComponent<GameOverScreen>();
        _pauseMenu = _mainUI.transform.Find("PauseMenu").gameObject;
        _debugMenu = _mainUI.transform.Find("DebugMenu").gameObject;
        _fpsText = _mainUI.transform.Find("FPSText").GetComponent<TextMeshProUGUI>();

        if (_debugMenu)
        {
            var container = _debugMenu.transform.Find("DebugPanel/LayoutContainer");
            var button = container?.Find("ButtonTemplate");
            if (container && button)
            {
                var sceneNames = new List<string>
                {
                    MainMenu,
                    VictoryScene,
                };
                for (var i = 1; i <= NumLevels; i++)
                {
                    sceneNames.Add($"{LevelPrefix}{i}");
                }

                foreach (var sceneName in sceneNames)
                {
                    var b = Instantiate(button.gameObject, container);
                    var but = b.GetComponent<Button>();
                    but.onClick.AddListener(() =>
                    {
                        if (!_debugMode) return;
                        var isLevel = sceneName.ToLowerInvariant().Contains(LevelPrefix.ToLowerInvariant());
                        if (isLevel)
                        {
                            var valid = int.TryParse(sceneName[LevelPrefix.Length..], out var levelIndex);
                            if (valid) CurrentLevel = levelIndex;
                            else return;
                            StartCoroutine(LoadLevel());
                        }
                        else
                        {
                            StartCoroutine(LoadScene(sceneName));   
                        }
                        
                        _debugMenu.SetActive(false);
                    });
                    var text = b.GetComponentInChildren<TextMeshProUGUI>();
                    text.SetText(sceneName);  
                    b.name = sceneName;
                    b.SetActive(true);
                }
            }
        }
        
        _hud = _mainUI.transform.Find("HUD");
        var hudContainer = _hud.Find("Container");
        _livesText = hudContainer.Find("Lives")?.transform.Find("LivesText").GetComponent<TextMeshProUGUI>();
        _scoreText = hudContainer.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        _timerText = hudContainer.Find("TimerText")?.GetComponent<TextMeshProUGUI>();

        ResetGame(true);
    }
    
    private void Update()
    {
        if (Application.isFocused)
        {
            ++frameCount;
            timeSinceLastFrameRateCheck += Time.unscaledDeltaTime;
            if (timeSinceLastFrameRateCheck >= 1f)
            {
                if (_debugMode)
                {
                    _fpsText?.SetText($"FPS: {frameCount / timeSinceLastFrameRateCheck:F1}");
                }

                _frameRatio = frameCount / timeSinceLastFrameRateCheck / TargetFrameRate;
                timeSinceLastFrameRateCheck = 0f;
                frameCount = 0;
            }
        }

        if (currentController && !currentController.IsDead && !isCompletingLevel && !isStartingLevel)
        {
            if (currentRewindController && !currentRewindController.IsRewinding || !currentRewindController)
            {
                Time.timeScale = isGamePaused ? 0f : 1f;
            }
        }
        
        _livesText?.SetText($"{CurrentLives}");
        _scoreText?.SetText($"SCORE {CurrentScore}");
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.D))
        {
            _debugMode = !_debugMode;
            _debugMenu.SetActive(false);
            _fpsText?.gameObject.SetActive(_debugMode);
        }

        if (_debugMode && Input.GetKeyDown(KeyCode.Tab))
        {
            _debugMenu.SetActive(!_debugMenu.activeSelf);
        } 
    }

    private float lastTextUpdate = 0f;
    private void FixedUpdate()
    {
        if (!currentController)
        {
            if (_timerText)
            {
                _timerText.SetText("0:00");
                _timerText.color = Color.white;   
            }
            return;
        }
        if (isCompletingLevel) return;
        
        LevelTimer -= Time.fixedDeltaTime;
        lastTextUpdate += Time.fixedDeltaTime;
        if (LevelTimer < 0) LevelTimer = 0;
        
        UpdateTimer();
    }

    private void UpdateTimer(bool updateColor = true)
    {
        var times = TimeToMinsAndSecs(LevelTimer);
            
        _timerText?.SetText($"{times.Item1:0}:{times.Item2:00}");
        if (!_timerText) return;
        _timerText.color = Color.white;
        
        if (!updateColor || !(LevelTimer <= 60) || !(lastTextUpdate >= 1f)) return;
        _timerText.color = _timerText.color == Color.white ? Color.red : Color.white;
        lastTextUpdate = 0f;
    }

    private void ResetGame(bool init = false)
    {
        if (init && _initialized) return;
        _initialized = true;

        TotalScore = 0;
        CurrentLevel = 1;
        CurrentLives = StartingLives;
        
        StartCoroutine(LoadScene(MainMenu));
    }

    public static void StartGame()
    {
        Physics2D.queriesHitTriggers = true;
        TotalScore = 0;
        CurrentLevel = 1;
        CurrentLives = StartingLives;
        Instance.StartCoroutine(Instance.LoadLevel());
    }

    private static IEnumerator AdvanceLevel()
    {
        TotalScore += CurrentScore;
        CurrentScore = TotalScore;
        CurrentLevel++;
        
        if (CurrentLevel > NumLevels)
        {
            yield return Instance.LoadScene(VictoryScene);
            yield break;
        }
        
        yield return Instance.LoadLevel();        
    }

    private IEnumerator LoadLevel()
    {
        isStartingLevel = true;
        yield return new WaitForSecondsRealtime(0.5f);
        yield return LoadScene(LevelPrefix + CurrentLevel, true);
        isStartingLevel = false;
    }

    private IEnumerator LoadScene(string sceneName, bool showLoadingScreen = false, bool respawn = false)
    {
        AudioManager.ResetSounds();
        isGamePaused = false;
        currentController = null;
        LevelTimer = ClearTimer;
        Time.timeScale = 0;
        
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        _hud.gameObject.SetActive(false);
        if (showLoadingScreen)
        {
            StartCoroutine(FadeScreen(false));
            StartCoroutine(FadeScreen(true,2f));
            yield return _loadingScreen.ShowLoadingScreen(respawn);
        }
        StartCoroutine(FadeScreen());
        
        AudioManager.ChangeBackgroundMusic(sceneName);
        if (sceneName.ToLowerInvariant().Contains(LevelPrefix.ToLowerInvariant())) 
            GetPlayerController();
        
        Time.timeScale = 1;
    }

    private void GetPlayerController()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        
        _hud.gameObject.SetActive(true);
        currentController = player.GetComponent<PlayerController>();
        currentRewindController = player.GetComponent<PlayerRewindController>();
        currentController.OnDeath += () =>
        {
            StartCoroutine(OnPlayerDeath());
        };
        AudioManager.LinkPlayerController(currentController,currentRewindController);
    }

    private IEnumerator OnPlayerDeath()
    {
        _hud.gameObject.SetActive(false);
        CurrentLives--;
        CurrentScore = 0;
        
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(2f);
        yield return FadeScreen(false);
        StartCoroutine(FadeScreen());
        
        if (CurrentLives == 0)
        {
            AudioManager.PlaySound(Audios.Lose);
            yield return _gameOverScreen.ShowGameOverScreen();
            ResetGame();
        }
        else
        {
            yield return LoadScene(SceneManager.GetActiveScene().name, true, true);
        }
    }

    public static void CompleteLevel()
    {
        Instance.StartCoroutine(OnCompleteLevel());
    }
    
    private static IEnumerator OnCompleteLevel()
    {
        if (isCompletingLevel) yield break;
        OnLevelCompleted?.Invoke();
        isCompletingLevel = true;

        Time.timeScale = 0;
        while (LevelTimer > 0)
        {
            LevelTimer -= 1;
            if (LevelTimer < 0) LevelTimer = 0;
            CurrentScore += 10;
            Instance.UpdateTimer();
            yield return new WaitForSecondsRealtime(0.01f);
        } 
        
        BeastSpriteController beastSprite = null;
        var candidates = FindObjectsByType<BeastSpriteController>(FindObjectsSortMode.InstanceID);
        if (candidates.Length > 0)
        {
            beastSprite = candidates[0];
        }
        if (CurrentLevel != NumLevels && beastSprite)
        {
            AudioManager.PlaySound(Audios.MovingLevel);
            yield return beastSprite.StartEndAnimation();
        }
       
        yield return AdvanceLevel();
        
        isCompletingLevel = false;
    }

    private static IEnumerator FadeScreen(bool fadeIn = true, float delay = 1f)
    {
        _fadePanel.gameObject.SetActive(true);
        var opaque = new Color(0, 0, 0, 1);
        var transparent = new Color(0, 0, 0, 0);
        
        _fadePanel.color = fadeIn ? opaque : transparent;
        _fadePanel.DOColor(fadeIn ? transparent : opaque, delay).SetEase(Ease.Linear).SetUpdate(true);
        yield return new WaitForSecondsRealtime(delay);
        _fadePanel.gameObject.SetActive(false);
    }
    
    public static void IncreaseScore(int amount, Transform source = null)
    {
        CurrentScore += amount;
        if (!source) return;
        
        if (!_scoreTextPopup)
        {
            _scoreTextPopup = Resources.Load<GameObject>("Prefabs/ScoreTextPopup");
        }
        if (!_scoreTextPopup) return;
        
        var popupObject = Instantiate(_scoreTextPopup, source.position, Quaternion.identity);
        var popup = popupObject.GetComponent<ScoreTextPopup>();
        Instance.StartCoroutine(popup.Popup(amount));
    }

    public static void IncreaseTimer(int amount, Transform source = null)
    {
        LevelTimer += amount;
        if (!source) return;
        
        if (!_scoreTextPopup)
        {
            _scoreTextPopup = Resources.Load<GameObject>("Prefabs/ScoreTextPopup");
        }
        if (!_scoreTextPopup) return;
        
        var popupObject = Instantiate(_scoreTextPopup, source.position, Quaternion.identity);
        var popup = popupObject.GetComponent<ScoreTextPopup>();
        
        var times = TimeToMinsAndSecs(amount);

        Instance.StartCoroutine(popup.Popup($"+{times.Item1:0}:{times.Item2:00}"));
    }

    public void TogglePause()
    {
        if (currentController && !currentController.IsDead)
        {
            AudioManager.PlaySound(Audios.MenuClick);
            isGamePaused = !isGamePaused;
            AudioManager.OnPauseToggle(isGamePaused);
            _pauseMenu.SetActive(isGamePaused);
            if (isGamePaused)
            {
                OnPaused?.Invoke();
            }
            Time.timeScale = isGamePaused ? 0f : 1f;
        }
    }

    public void RestartLevel()
    {
        isGamePaused = false;
        _pauseMenu.SetActive(false);
        CurrentScore = 0;
        AudioManager.PlaySound(Audios.MenuClick);
        StartCoroutine(LoadLevel());
    }

    public void ReturnToMainMenu()
    {
        isGamePaused = false;
        _pauseMenu.SetActive(false);
        AudioManager.PlaySound(Audios.MenuClick);
        ResetGame();
    }

    private static Tuple<int, int> TimeToMinsAndSecs(float time)
    {
        var mins = Mathf.FloorToInt(time / 60);
        var secs = Mathf.FloorToInt((time - mins * 60) % 60);
        return new Tuple<int, int>(mins, secs);
    }

    public static float GetScaledFrameCount(int frames)
    {
        return frames * _frameRatio;
    }
}
