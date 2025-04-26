using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Menu UI")]
    [SerializeField] private Canvas pauseMenuCanvas;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Header("Variant Selection UI (disable Pause while this is up)")]
    [SerializeField] private Canvas variantSelectionCanvas;

    [Header("HUD Canvas (will be hidden when paused)")]
    [SerializeField] private Canvas hudCanvas;

    [Header("Audio")]
    [Tooltip("The one-shot selection sound to play on any button press")]
    [SerializeField] private AudioClip selectionSfx;
    [Tooltip("Where to play the selection SFX")]
    [SerializeField] private AudioSource audioSource;

    private bool isPaused = false;

    private void Awake()
    {
        if (audioSource == null)
            Debug.LogWarning("PauseManager: no AudioSource assigned! Selection SFX won't play.");

        // Hide pause menu at start
        pauseMenuCanvas.gameObject.SetActive(false);
    }

    private void Start()
    {
        continueButton.onClick.AddListener(OnContinuePressed);
        quitButton.onClick.AddListener(OnQuitPressed);
    }

    private void Update()
    {
        // Never open Pause if the variant-selection screen is active
        if (variantSelectionCanvas != null && variantSelectionCanvas.gameObject.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    private void TogglePause()
    {
        isPaused = !isPaused;

        // show/hide pause menu
        pauseMenuCanvas.gameObject.SetActive(isPaused);

        // hide your HUD when paused, show it when unpaused
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(!isPaused);

        // actually pause/unpause time
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void OnContinuePressed()
    {
        if (audioSource && selectionSfx)
            audioSource.PlayOneShot(selectionSfx);

        if (isPaused)
            TogglePause();
    }

    private void OnQuitPressed()
    {
        if (audioSource && selectionSfx)
            audioSource.PlayOneShot(selectionSfx);

        // reset time before leaving
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
