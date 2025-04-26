using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Canvases")]
    [SerializeField] private Canvas variantSelectionCanvas;
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Canvas pauseMenuCanvas;
    [SerializeField] private Canvas gameOverCanvas;

    [Header("Pause Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button pauseQuitButton;

    [Header("Game Over Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button gameOverQuitButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip selectionSfx;

    bool _isPaused = false;

    void Awake()
    {
        // Hide all but variant-selection
        hudCanvas.gameObject.SetActive(false);
        pauseMenuCanvas.gameObject.SetActive(false);
        gameOverCanvas.gameObject.SetActive(false);

        // Wire up buttons
        continueButton.onClick.AddListener(TogglePause);
        pauseQuitButton.onClick.AddListener(QuitToMainMenu);
        retryButton.onClick.AddListener(RetryGame);
        gameOverQuitButton.onClick.AddListener(QuitToMainMenu);
    }

    void Update()
    {
        // Don't allow pause during variant selection
        if (variantSelectionCanvas.gameObject.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void OnGameStart()
    {
        // Call this once the variantSelectionCanvas has been dismissed
        hudCanvas.gameObject.SetActive(true);
    }

    void TogglePause()
    {
        PlayClick();
        _isPaused = !_isPaused;

        pauseMenuCanvas.gameObject.SetActive(_isPaused);
        hudCanvas.gameObject.SetActive(!_isPaused);
        Time.timeScale = _isPaused ? 0f : 1f;
    }

    void RetryGame()
    {
        PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    void QuitToMainMenu()
    {
        PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ShowGameOver()
    {
        // e.g. called when allSlider hits zero
        Time.timeScale = 0f;
        gameOverCanvas.gameObject.SetActive(true);
        hudCanvas.gameObject.SetActive(false);
        pauseMenuCanvas.gameObject.SetActive(false);
    }

    void PlayClick()
    {
        if (audioSource != null && selectionSfx != null)
            audioSource.PlayOneShot(selectionSfx);
    }
}
