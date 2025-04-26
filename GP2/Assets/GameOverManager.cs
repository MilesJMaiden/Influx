using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button quitButton;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip selectionSfx;

    private void Awake()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryPressed);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);
    }

    private void OnRetryPressed()
    {
        if (audioSource != null && selectionSfx != null)
            audioSource.PlayOneShot(selectionSfx);

        // un-pause
        Time.timeScale = 1f;
        // reload the game scene (make sure your scene name matches)
        SceneManager.LoadScene("Game");
    }

    private void OnQuitPressed()
    {
        if (audioSource != null && selectionSfx != null)
            audioSource.PlayOneShot(selectionSfx);

        // un-pause
        Time.timeScale = 1f;
        // go back to main menu
        SceneManager.LoadScene("MainMenu");
    }
}
