using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
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
    private const string LevelPrefix = "Level ";
    private const int NumLevels = 3;
    private const int StartingLives = 3;

    private static GameObject _scoreTextPopup;

    public static int TotalScore { get; private set; }
    public static int CurrentLevel { get; private set; }
    public static int CurrentLives { get; private set; }
    public static int CurrentScore { get; private set; }
    
    // 3 minutes
    private const float ClearTimer = 180f;
    public static float LevelTimer { get; private set; }
    
    private static PlayerController currentController;
    
    private static Canvas _mainUI;
    private static Image _fadePanel;
    private static LoadingScreen _loadingScreen;
    private static GameOverScreen _gameOverScreen;
    private static Transform _hud;
    private static TextMeshProUGUI _livesText;
    private static TextMeshProUGUI _scoreText;
    private static TextMeshProUGUI _timerText;
    
    private void Awake()
    {
        Instance = this;
        _scoreTextPopup = Resources.Load<GameObject>("Prefabs/ScoreTextPopup");
        _mainUI = GameObject.Find("MainUICanvas").GetComponent<Canvas>();
        _fadePanel = _mainUI.transform.Find("FadePanel").GetComponent<Image>();
        _loadingScreen = _mainUI.transform.Find("LoadingScreen").GetComponent<LoadingScreen>();
        _gameOverScreen = _mainUI.transform.Find("GameOverScreen").GetComponent<GameOverScreen>();
        
        _hud = _mainUI.transform.Find("HUD");
        var hudContainer = _hud.Find("Container");
        _livesText = hudContainer.Find("Lives")?.transform.Find("LivesText").GetComponent<TextMeshProUGUI>();
        _scoreText = hudContainer.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        _timerText = hudContainer.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        
        ResetGame(true);
    }
    
    private void Update()
    {
        _livesText?.SetText($"{CurrentLives}");
        _scoreText?.SetText($"SCORE {CurrentScore}");
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
        LevelTimer -= Time.fixedDeltaTime;
        lastTextUpdate += Time.fixedDeltaTime;

        var mins = Mathf.FloorToInt(LevelTimer / 60);
        var secs = Mathf.FloorToInt((LevelTimer - mins * 60) % 60);
            
        _timerText?.SetText($"{mins:0}:{secs:00}");
        if (!_timerText || !(LevelTimer <= 60) || !(lastTextUpdate >= 1f)) return;
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
        TotalScore = 0;
        CurrentLevel = 1;
        CurrentLives = StartingLives;

        Instance.StartCoroutine(Instance.LoadLevel());
    }

    public static void AdvanceLevel()
    {
        TotalScore += CurrentScore;
        CurrentScore = TotalScore;
        CurrentLevel++;
        
        if (CurrentLevel > NumLevels)
        {
            // TODO: Win Condition
            return;
        }

        Instance.StartCoroutine(Instance.LoadLevel());
    }

    private IEnumerator LoadLevel()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        yield return LoadScene(LevelPrefix + CurrentLevel, true);
    }

    private IEnumerator LoadScene(string sceneName, bool showLoadingScreen = false, bool respawn = false)
    {
        currentController = null;
        LevelTimer = ClearTimer;
        Time.timeScale = 0;
        
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        if (showLoadingScreen)
        {
            StartCoroutine(FadeScreen(false));
            StartCoroutine(FadeScreen(true,2f));
            yield return _loadingScreen.ShowLoadingScreen(respawn);
        }
        StartCoroutine(FadeScreen());
        GetPlayerController();
        Time.timeScale = 1;
    }

    private void GetPlayerController()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
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
            LevelTimer = 0;
            yield return LoadScene(SceneManager.GetActiveScene().name, true, true);
        }
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
        var popupObject = Instantiate(_scoreTextPopup, source.position, Quaternion.identity);
        var popup = popupObject.GetComponent<ScoreTextPopup>();
        Instance.StartCoroutine(popup.Popup(amount));
    }

    public static void IncreaseTimer(int amount) => LevelTimer += amount;
}
