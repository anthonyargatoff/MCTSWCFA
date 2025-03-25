using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private bool debug_game_manager;
    private const string DebugScene = "Scenes/SampleScene";
    private static bool _initialized;
    
    private const string MainMenu = "Scenes/MainMenu";
    private const string LevelPrefix = "Level ";
    private const int NumLevels = 3;
    private const int StartingLives = 3;
    
    private static int currentLevel = 1;

    private static int currentLives;

    private static PlayerController currentController;
    
    private static Canvas _mainUI;
    private static LoadingScreen _loadingScreen;
    private void Awake()
    {
        _mainUI = GameObject.Find("MainUICanvas").GetComponent<Canvas>();
        _loadingScreen = _mainUI.transform.Find("LoadingScreen").GetComponent<LoadingScreen>();
        ResetGame(true);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ResetGame(bool init = false)
    {
        if (init && _initialized) return;
        _initialized = true;
        
        currentLevel = 1;
        currentLives = StartingLives;
        
        if (debug_game_manager)
        {
            StartCoroutine(LoadScene(DebugScene, true));
            return;
        }
        
        StartCoroutine(LoadScene(MainMenu));
    }
    

    private IEnumerator LoadScene(string sceneName, bool showLoadingScreen = false)
    {
        Time.timeScale = 0;
        
        var checkScene = SceneManager.GetSceneByName(sceneName);
        if (checkScene.IsValid() && !checkScene.isLoaded)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(DebugScene, LoadSceneMode.Single);
        }
        yield return _loadingScreen.ShowLoadingScreen(currentLives);
        GetPlayerController();
        Time.timeScale = 1;
    }

    private void GetPlayerController()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;
        
        currentController = player.GetComponent<PlayerController>();
        currentController.OnDeath += () =>
        {
            StartCoroutine(OnPlayerDeath());
        };
    }

    private IEnumerator OnPlayerDeath()
    {
        currentLives--;
        
        if (currentLives == 0)
        {
            // TODO: Game Over
        }
        else
        {
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(3f);
            yield return LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
