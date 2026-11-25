using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI scoreText;
    public GameObject pausePanel;
    public Button pauseButton;
    public Button controlButton;
    public TextMeshProUGUI gameOverScoreText;
    public GameObject pointUi;
    public GameObject healthBarUi;

    [Header("Score")]
    [Min(0f)]
    public float scorePerSecond = 1f;

    public float Score { get; private set; }

    [Header("References")]
    public PlayerController playerController;
    public PipeManager pipeManager;

    [Header("Scenes")]
    public string homeSceneName = "Home";

    private bool gameOver = false;
    public bool IsPaused { get; private set; }

    private const string TwitterIntentAddress =
        "https://twitter.com/intent/tweet?text=";

    private void Awake()
    {
        // Make sure there is only one GameManager.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        FindPauseUi();

        // Hide Game Over UI when the game starts.
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        gameOver = false;
        SetPaused(false);
        SetGameplayHudVisible(true);
        Score = 0f;
        UpdateScoreText();
        UpdateControlButtonText();
    }

    private void Update()
    {
        if (gameOver || IsPaused)
            return;

        Score += scorePerSecond * Time.deltaTime;
        UpdateScoreText();
    }

    public void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);

        SetGameplayHudVisible(false);
        UpdateGameOverScoreText();

        Debug.Log("GAME OVER");

        // Stop player controls.
        if (playerController != null)
        {
            playerController.SetGameplayPaused(true);
            playerController.enabled = false;
        }

        // Stop pipe movement and spawning.
        if (pipeManager != null)
        {
            pipeManager.StopAllPipes();
            pipeManager.enabled = false;
        }

        // Show Game Over UI.
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.name
        );
    }

    public void Home()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    public void TogglePause()
    {
        if (!gameOver)
            SetPaused(!IsPaused);
    }

    public void ToggleControlMode()
    {
        if (playerController == null)
            return;

        playerController.ToggleControlMode();
        UpdateControlButtonText();
    }

    public void ShareScore()
    {
        string message = $"I scored {Mathf.FloorToInt(Score):N0} points in Roller Ball 360! Can you beat my score?";
        Application.OpenURL(TwitterIntentAddress + UnityWebRequest.EscapeURL(message));
    }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(paused);

        SetGameplayHudVisible(!paused);

        playerController?.SetGameplayPaused(paused);

        // Keep the pause button above the full-screen panel so it also acts as Resume.
        if (pauseButton != null)
            pauseButton.transform.SetAsLastSibling();
    }

    private void FindPauseUi()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        Transform[] uiObjects = canvas.GetComponentsInChildren<Transform>(true);

        foreach (Transform uiObject in uiObjects)
        {
            if (pausePanel == null && uiObject.name == "OptionPanel")
                pausePanel = uiObject.gameObject;
            else if (pauseButton == null && uiObject.name == "PauseButton")
                pauseButton = uiObject.GetComponent<Button>();
            else if (controlButton == null && uiObject.name == "Control")
                controlButton = uiObject.GetComponent<Button>();
            else if (gameOverScoreText == null && uiObject.name == "TotalScore")
                gameOverScoreText = uiObject.GetComponent<TextMeshProUGUI>();
            else if (pointUi == null && uiObject.name == "Point")
                pointUi = uiObject.gameObject;
            else if (healthBarUi == null && uiObject.name == "Healthbar")
                healthBarUi = uiObject.gameObject;
        }

        if (pauseButton != null)
        {
            Sprite pauseSprite = Resources.Load<Sprite>("UI/pause-fill");
            Image buttonImage = pauseButton.GetComponent<Image>();

            if (pauseSprite != null && buttonImage != null)
            {
                buttonImage.sprite = pauseSprite;
                buttonImage.type = Image.Type.Simple;
                buttonImage.preserveAspect = true;
            }
        }
    }

    private void UpdateControlButtonText()
    {
        if (controlButton == null || playerController == null)
            return;

        TextMeshProUGUI label = controlButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = $"Control: {playerController.ControlModeLabel}";
    }

    private void SetGameplayHudVisible(bool visible)
    {
        if (pointUi != null)
            pointUi.SetActive(visible);

        if (healthBarUi != null)
            healthBarUi.SetActive(visible);
    }

    private void UpdateGameOverScoreText()
    {
        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Total Score: {Mathf.FloorToInt(Score):N0}";
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = Mathf.FloorToInt(Score).ToString("N0");
    }
}
