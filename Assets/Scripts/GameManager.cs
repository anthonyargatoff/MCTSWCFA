using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
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
    public static GameManager Instance { get; private set; }
    
    private const string DebugScene = "SampleScene";
    private static bool _initialized;
    
    private const string MainMenu = "MainMenu";
    private const string VictoryScene = "VictoryScene";
    private const string LevelPrefix = "LEVEL ";
    private const int NumLevels = 3;
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
    private static GameObject _pauseMenu;
    private bool isInTutorial = false;

    // Tutorial variables
    public static int CurrentTutorialLevel { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        _scoreTextPopup = Resources.Load<GameObject>("Prefabs/ScoreTextPopup");
        _mainUI = GameObject.Find("MainUICanvas").GetComponent<Canvas>();
        _fadePanel = _mainUI.transform.Find("FadePanel").GetComponent<Image>();
        _loadingScreen = _mainUI.transform.Find("LoadingScreen").GetComponent<LoadingScreen>();
        _gameOverScreen = _mainUI.transform.Find("GameOverScreen").GetComponent<GameOverScreen>();
        _pauseMenu = _mainUI.transform.Find("PauseMenu").gameObject;
        
        _hud = _mainUI.transform.Find("HUD");
        var hudContainer = _hud.Find("Container");
        _livesText = hudContainer.Find("Lives")?.transform.Find("LivesText").GetComponent<TextMeshProUGUI>();
        _scoreText = hudContainer.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        _timerText = hudContainer.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        ResetGame(true);
    }
    
    private void Update()
    {
        if (currentController && !currentController.IsDead && !isCompletingLevel && !isStartingLevel)
        {
            if (!currentRewindController)
            {
                currentRewindController = currentController.GetComponent<PlayerRewindController>();
            }

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

    private static void AdvanceLevel()
    {
        TotalScore += CurrentScore;
        CurrentScore = TotalScore;
        CurrentLevel++;
        
        if (CurrentLevel > NumLevels)
        {
            Instance.StartCoroutine(Instance.LoadScene(VictoryScene));
            return;
        }

        Instance.StartCoroutine(Instance.LoadLevel());
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
        
        if (sceneName.ToLowerInvariant().Contains(LevelPrefix.ToLowerInvariant())) 
            GetPlayerController();
        
        Time.timeScale = 1;
    }

    private void GetPlayerController()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log(player);
        if (!player) return;
        
        _hud.gameObject.SetActive(true);
        currentController = player.GetComponent<PlayerController>();
        currentController.OnDeath += () =>
        {
            StartCoroutine(OnPlayerDeath());
        };
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
            yield return beastSprite.StartEndAnimation();
        }
        
        AdvanceLevel();
        
        isCompletingLevel = false;
    }

    private static IEnumerator FadeScreen(bool fadeIn = true, float delay = 1f)
    {
        var opaque = new Color(0, 0, 0, 1);
        var transparent = new Color(0, 0, 0, 0);
        
        _fadePanel.color = fadeIn ? opaque : transparent;
        _fadePanel.DOColor(fadeIn ? transparent : opaque, delay).SetEase(Ease.Linear).SetUpdate(true);
        yield return new WaitForSecondsRealtime(delay);
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
        if ((currentController && !currentController.IsDead) || isInTutorial)
        {
            isGamePaused = !isGamePaused;
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
        if (isInTutorial) {
          RestartTutorialLevel();
        } else {
          StartCoroutine(LoadLevel());
        }
    }

    public void ReturnToMainMenu()
    {
        isInTutorial = false;
        isGamePaused = false;
        _pauseMenu.SetActive(false);
        ResetGame();
    }

    private static Tuple<int, int> TimeToMinsAndSecs(float time)
    {
        var mins = Mathf.FloorToInt(time / 60);
        var secs = Mathf.FloorToInt((time - mins * 60) % 60);
        return new Tuple<int, int>(mins, secs);
    }

    public static void NextTutorial()
    {
        Instance.isInTutorial = true;
        CurrentTutorialLevel++;
        Instance.StartCoroutine(Instance.LoadScene("Tutorial_" + CurrentTutorialLevel));
    }

    public static void RestartTutorialLevel()
    {
      Instance.StartCoroutine(Instance.LoadScene("Tutorial_" + CurrentTutorialLevel));
    }

    public static IEnumerator EndTutorial()
    {
      TextMeshProUGUI text = GameObject.Find("EndTutorial").GetComponent<TextMeshProUGUI>();
      text.text = "Congratulations, you've completed the tutorial!";
      Time.timeScale = 0;
      yield return new WaitForSecondsRealtime(3);
      Time.timeScale = 1;
      Instance.isInTutorial = false;
      Instance.StartCoroutine(Instance.LoadScene(MainMenu));
    }
}
